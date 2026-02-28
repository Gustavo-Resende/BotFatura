using System.Diagnostics;
using Ardalis.Result;
using BotFatura.Application.Common.Helpers;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Comprovantes.Services;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BotFatura.Application.Comprovantes.Commands.ProcessarComprovante;

public class ProcessarComprovanteCommandHandler : IRequestHandler<ProcessarComprovanteCommand, Result<ProcessarComprovanteResult>>
{
    private readonly IGeminiApiClient _geminiClient;
    private readonly IFaturaRepository _faturaRepository;
    private readonly IEvolutionApiClient _evolutionApiClient;
    private readonly ComprovanteValidationService _validationService;
    private readonly Domain.Interfaces.IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProcessarComprovanteCommandHandler> _logger;
    
    private const string GRUPO_SOCIOS_PADRAO_ID = "120363023769164146@g.us";

    public ProcessarComprovanteCommandHandler(
        IGeminiApiClient geminiClient,
        IFaturaRepository faturaRepository,
        IEvolutionApiClient evolutionApiClient,
        ComprovanteValidationService validationService,
        Domain.Interfaces.IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService,
        ILogger<ProcessarComprovanteCommandHandler> logger)
    {
        _geminiClient = geminiClient;
        _faturaRepository = faturaRepository;
        _evolutionApiClient = evolutionApiClient;
        _validationService = validationService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<ProcessarComprovanteResult>> Handle(ProcessarComprovanteCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Iniciando processamento de comprovante. Operation={Operation}, ClienteId={ClienteId}, MimeType={MimeType}, ArquivoTamanhoKB={ArquivoTamanhoKB:F2}",
            "ProcessarComprovante",
            request.ClienteId,
            request.MimeType,
            request.Arquivo.Length / 1024.0);

        try
        {
            // 1. Analisar comprovante com Gemini
            _logger.LogInformation(
                "Etapa 1: Analisando comprovante via Gemini. Operation={Operation}, ClienteId={ClienteId}",
                "ProcessarComprovante.AnalisarComprovante",
                request.ClienteId);

            var analiseResult = await _geminiClient.AnalisarComprovanteAsync(request.Arquivo, request.MimeType, cancellationToken);

            if (!analiseResult.IsSuccess)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "Análise do comprovante falhou. Operation={Operation}, ClienteId={ClienteId}, Success={Success}, DurationMs={DurationMs}, Errors={Errors}",
                    "ProcessarComprovante",
                    request.ClienteId,
                    false,
                    stopwatch.ElapsedMilliseconds,
                    string.Join(", ", analiseResult.Errors));
                return Result.Error($"Erro ao analisar comprovante: {string.Join(", ", analiseResult.Errors)}");
            }

            var analise = analiseResult.Value;

            // 2. Verificar se é um comprovante válido
            if (!analise.IsComprovante)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "Arquivo não é um comprovante válido. Operation={Operation}, ClienteId={ClienteId}, Success={Success}, DurationMs={DurationMs}, Confianca={Confianca}",
                    "ProcessarComprovante",
                    request.ClienteId,
                    false,
                    stopwatch.ElapsedMilliseconds,
                    analise.Confianca);

                await EnviarMensagemClienteAsync(
                    request.JidOriginal,
                    "❌ O arquivo enviado não é um comprovante de pagamento válido. Por favor, envie uma imagem ou PDF do comprovante.",
                    cancellationToken,
                    request.NumeroWhatsApp);

                return Result.Success(new ProcessarComprovanteResult(
                    Sucesso: false,
                    FaturaId: null,
                    Mensagem: "Arquivo não é um comprovante válido"
                ));
            }

            // 3. Validar destinatário (chave PIX ou nome do titular)
            _logger.LogInformation(
                "Etapa 2: Validando destinatário do comprovante. Operation={Operation}, ClienteId={ClienteId}",
                "ProcessarComprovante.ValidarDestinatario",
                request.ClienteId);

            var validacaoDestinatario = await _validationService.ValidarDestinatarioAsync(
                analise.DadosDestinatario,
                cancellationToken);

            if (!validacaoDestinatario.IsSuccess)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "Validação de destinatário falhou. Operation={Operation}, ClienteId={ClienteId}, Success={Success}, DurationMs={DurationMs}, DestinatarioNome={DestinatarioNome}, DestinatarioChavePix={DestinatarioChavePix}",
                    "ProcessarComprovante",
                    request.ClienteId,
                    false,
                    stopwatch.ElapsedMilliseconds,
                    analise.DadosDestinatario?.Nome ?? "(não extraído)",
                    analise.DadosDestinatario?.ChavePix ?? "(não extraído)");

                await EnviarMensagemClienteAsync(
                    request.JidOriginal,
                    "❌ O comprovante não corresponde aos nossos dados cadastrados. Verifique se o pagamento foi feito para a chave PIX correta.",
                    cancellationToken,
                    request.NumeroWhatsApp);

                return Result.Success(new ProcessarComprovanteResult(
                    Sucesso: false,
                    FaturaId: null,
                    Mensagem: "Destinatário do comprovante não corresponde"
                ));
            }

            // 4. Verificar se o valor foi extraído
            if (!analise.Valor.HasValue || analise.Valor.Value <= 0)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "Valor não identificado no comprovante. Operation={Operation}, ClienteId={ClienteId}, Success={Success}, DurationMs={DurationMs}",
                    "ProcessarComprovante",
                    request.ClienteId,
                    false,
                    stopwatch.ElapsedMilliseconds);

                await EnviarMensagemClienteAsync(
                    request.JidOriginal,
                    "❌ Não foi possível identificar o valor do comprovante. Por favor, envie um comprovante mais legível.",
                    cancellationToken,
                    request.NumeroWhatsApp);

                return Result.Success(new ProcessarComprovanteResult(
                    Sucesso: false,
                    FaturaId: null,
                    Mensagem: "Valor não identificado no comprovante"
                ));
            }

            // 5. Buscar faturas pendentes e verificar se existe alguma
            _logger.LogInformation(
                "Etapa 3: Verificando faturas pendentes. Operation={Operation}, ClienteId={ClienteId}, ValorComprovante={ValorComprovante}",
                "ProcessarComprovante.VerificarFaturasPendentes",
                request.ClienteId,
                analise.Valor.Value);

            var faturasPendentes = await _validationService.ObterFaturasPendentesAsync(request.ClienteId, cancellationToken);

            if (!faturasPendentes.IsSuccess || faturasPendentes.Value == null || !faturasPendentes.Value.Any())
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "Nenhuma fatura pendente encontrada. Operation={Operation}, ClienteId={ClienteId}, Success={Success}, DurationMs={DurationMs}",
                    "ProcessarComprovante",
                    request.ClienteId,
                    false,
                    stopwatch.ElapsedMilliseconds);

                await EnviarMensagemClienteAsync(
                    request.JidOriginal,
                    "❌ Não encontramos nenhuma fatura pendente para validar. Verifique se há faturas em aberto.",
                    cancellationToken,
                    request.NumeroWhatsApp);

                return Result.Success(new ProcessarComprovanteResult(
                    Sucesso: false,
                    FaturaId: null,
                    Mensagem: "Sem fatura pendente"
                ));
            }

            // 6. Encontrar fatura correspondente ao valor
            _logger.LogInformation(
                "Etapa 4: Validando valor e buscando fatura correspondente. Operation={Operation}, ClienteId={ClienteId}, ValorComprovante={ValorComprovante}, FaturasPendentes={FaturasPendentes}",
                "ProcessarComprovante.ValidarValor",
                request.ClienteId,
                analise.Valor.Value,
                faturasPendentes.Value.Count);

            var faturaCorrespondenteResult = await _validationService.EncontrarFaturaCorrespondenteAsync(
                request.ClienteId,
                analise.Valor.Value,
                cancellationToken);

            if (!faturaCorrespondenteResult.IsSuccess || faturaCorrespondenteResult.Value == null)
            {
                // Pegar a primeira fatura pendente para mostrar o valor esperado
                var faturaPendente = faturasPendentes.Value.First();
                
                stopwatch.Stop();
                _logger.LogWarning(
                    "Valor do comprovante não corresponde às faturas pendentes. Operation={Operation}, ClienteId={ClienteId}, Success={Success}, DurationMs={DurationMs}, ValorComprovante={ValorComprovante}, ValorFatura={ValorFatura}",
                    "ProcessarComprovante",
                    request.ClienteId,
                    false,
                    stopwatch.ElapsedMilliseconds,
                    analise.Valor.Value,
                    faturaPendente.Valor);

                await EnviarMensagemClienteAsync(
                    request.JidOriginal,
                    $"❌ O valor do comprovante (R$ {analise.Valor.Value:F2}) não corresponde ao valor da fatura pendente (R$ {faturaPendente.Valor:F2}).",
                    cancellationToken,
                    request.NumeroWhatsApp);

                return Result.Success(new ProcessarComprovanteResult(
                    Sucesso: false,
                    FaturaId: null,
                    Mensagem: "Valor não corresponde"
                ));
            }

            var fatura = faturaCorrespondenteResult.Value;

            // 7. Marcar fatura como paga (com transação)
            _logger.LogInformation(
                "Etapa 5: Marcando fatura como paga. Operation={Operation}, ClienteId={ClienteId}, FaturaId={FaturaId}, ValorFatura={ValorFatura}",
                "ProcessarComprovante.MarcarFaturaPaga",
                request.ClienteId,
                fatura.Id,
                fatura.Valor);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            try
            {
                var marcarPagaResult = fatura.MarcarComoPaga();
                if (!marcarPagaResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    stopwatch.Stop();
                    _logger.LogError(
                        "Erro ao marcar fatura como paga. Operation={Operation}, ClienteId={ClienteId}, FaturaId={FaturaId}, Success={Success}, DurationMs={DurationMs}, Errors={Errors}",
                        "ProcessarComprovante",
                        request.ClienteId,
                        fatura.Id,
                        false,
                        stopwatch.ElapsedMilliseconds,
                        string.Join(", ", marcarPagaResult.Errors));
                    return Result.Error($"Erro ao processar pagamento: {string.Join(", ", marcarPagaResult.Errors)}");
                }

                await _faturaRepository.UpdateAsync(fatura, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "Fatura marcada como paga com sucesso. Operation={Operation}, ClienteId={ClienteId}, FaturaId={FaturaId}",
                    "ProcessarComprovante.MarcarFaturaPaga",
                    request.ClienteId,
                    fatura.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "Erro ao processar pagamento da fatura. Operation={Operation}, ClienteId={ClienteId}, FaturaId={FaturaId}, Success={Success}, DurationMs={DurationMs}, ErrorMessage={ErrorMessage}",
                    "ProcessarComprovante",
                    request.ClienteId,
                    fatura.Id,
                    false,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                return Result.Error($"Erro ao processar pagamento: {ex.Message}");
            }

            // 8. Enviar comprovante para grupo de sócios
            _logger.LogInformation(
                "Etapa 6: Enviando comprovante para grupo de sócios. Operation={Operation}, ClienteId={ClienteId}, FaturaId={FaturaId}",
                "ProcessarComprovante.EnviarParaGrupo",
                request.ClienteId,
                fatura.Id);

            var configuracao = await _cacheService.ObterConfiguracaoAsync(cancellationToken);
            var grupoId = !string.IsNullOrWhiteSpace(configuracao?.GrupoSociosWhatsAppId)
                ? configuracao.GrupoSociosWhatsAppId
                : GRUPO_SOCIOS_PADRAO_ID;

            if (!string.IsNullOrWhiteSpace(grupoId))
            {
                var mensagemGrupo = $"✅ *Comprovante Validado*\n\n" +
                                   $"Cliente: {fatura.Cliente.NomeCompleto}\n" +
                                   $"Valor: R$ {fatura.Valor:F2}\n" +
                                   $"Fatura: #{fatura.Id.ToString().Substring(0, 8)}\n" +
                                   $"Número do Comprovante: {analise.NumeroComprovante ?? "N/A"}\n" +
                                   $"Data: {_dateTimeProvider.UtcNow:dd/MM/yyyy HH:mm}";

                var envioGrupoResult = await _evolutionApiClient.EnviarMensagemParaGrupoAsync(
                    grupoId,
                    mensagemGrupo,
                    request.Arquivo,
                    request.MimeType,
                    cancellationToken);

                if (!envioGrupoResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Erro ao enviar comprovante para grupo (não crítico). Operation={Operation}, ClienteId={ClienteId}, FaturaId={FaturaId}, GrupoId={GrupoId}, Errors={Errors}",
                        "ProcessarComprovante.EnviarParaGrupo",
                        request.ClienteId,
                        fatura.Id,
                        grupoId,
                        string.Join(", ", envioGrupoResult.Errors));
                }
                else
                {
                    _logger.LogInformation(
                        "Comprovante enviado para grupo com sucesso. Operation={Operation}, ClienteId={ClienteId}, FaturaId={FaturaId}, GrupoId={GrupoId}",
                        "ProcessarComprovante.EnviarParaGrupo",
                        request.ClienteId,
                        fatura.Id,
                        grupoId);
                }
            }

            // 9. Confirmar ao cliente
            _logger.LogInformation(
                "Etapa 7: Enviando confirmação ao cliente. Operation={Operation}, ClienteId={ClienteId}, FaturaId={FaturaId}",
                "ProcessarComprovante.ConfirmarCliente",
                request.ClienteId,
                fatura.Id);

            await EnviarMensagemClienteAsync(
                request.JidOriginal,
                $"✅ Comprovante validado com sucesso!\nA fatura de R$ {fatura.Valor:F2} foi marcada como paga.\nObrigado pelo pagamento!",
                cancellationToken,
                request.NumeroWhatsApp);

            stopwatch.Stop();
            _logger.LogInformation(
                "Comprovante processado com sucesso. Operation={Operation}, ClienteId={ClienteId}, FaturaId={FaturaId}, Success={Success}, DurationMs={DurationMs}, ValorFatura={ValorFatura}",
                "ProcessarComprovante",
                request.ClienteId,
                fatura.Id,
                true,
                stopwatch.ElapsedMilliseconds,
                fatura.Valor);

            return Result.Success(new ProcessarComprovanteResult(
                Sucesso: true,
                FaturaId: fatura.Id,
                Mensagem: "Comprovante validado e fatura marcada como paga"
            ));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Erro inesperado ao processar comprovante. Operation={Operation}, ClienteId={ClienteId}, Success={Success}, DurationMs={DurationMs}, ErrorMessage={ErrorMessage}",
                "ProcessarComprovante",
                request.ClienteId,
                false,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            return Result.Error($"Erro inesperado ao processar comprovante: {ex.Message}");
        }
    }

    /// <summary>
    /// Envia mensagem para o cliente usando o JID original recebido no webhook.
    /// A Evolution API aceita tanto JIDs @s.whatsapp.net quanto @lid diretamente.
    /// </summary>
    private async Task EnviarMensagemClienteAsync(string jidDestino, string mensagem, CancellationToken cancellationToken, string? numeroFallback = null)
    {
        _logger.LogDebug(
            "Enviando mensagem para cliente. Operation={Operation}, JidDestino={JidMascarado}",
            "EnviarMensagemCliente",
            TelefoneHelper.MascararNumero(jidDestino));

        // Usa o JID original - a Evolution API aceita ambos os formatos (@lid e @s.whatsapp.net)
        var resultado = await _evolutionApiClient.EnviarMensagemAsync(jidDestino, mensagem, cancellationToken);
        
        if (!resultado.IsSuccess)
        {
            _logger.LogWarning(
                "Erro ao enviar mensagem para cliente. Operation={Operation}, JidDestino={JidMascarado}, Errors={Errors}",
                "EnviarMensagemCliente",
                TelefoneHelper.MascararNumero(jidDestino),
                string.Join(", ", resultado.Errors));
        }
    }
}
