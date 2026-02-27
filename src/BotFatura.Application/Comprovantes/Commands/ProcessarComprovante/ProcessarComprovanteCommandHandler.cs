using Ardalis.Result;
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
    private readonly IRepository<Domain.Entities.Configuracao> _configuracaoRepository;
    private readonly FaturaMatchingService _faturaMatchingService;
    private readonly ILogger<ProcessarComprovanteCommandHandler> _logger;

    public ProcessarComprovanteCommandHandler(
        IGeminiApiClient geminiClient,
        IFaturaRepository faturaRepository,
        IEvolutionApiClient evolutionApiClient,
        IRepository<Domain.Entities.Configuracao> configuracaoRepository,
        FaturaMatchingService faturaMatchingService,
        ILogger<ProcessarComprovanteCommandHandler> logger)
    {
        _geminiClient = geminiClient;
        _faturaRepository = faturaRepository;
        _evolutionApiClient = evolutionApiClient;
        _configuracaoRepository = configuracaoRepository;
        _faturaMatchingService = faturaMatchingService;
        _logger = logger;
    }

    public async Task<Result<ProcessarComprovanteResult>> Handle(ProcessarComprovanteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Analisar comprovante com Gemini
            _logger.LogInformation("Analisando comprovante do cliente {ClienteId}", request.ClienteId);
            var analiseResult = await _geminiClient.AnalisarComprovanteAsync(request.Arquivo, request.MimeType, cancellationToken);

            if (!analiseResult.IsSuccess)
            {
                return Result.Error($"Erro ao analisar comprovante: {string.Join(", ", analiseResult.Errors)}");
            }

            var analise = analiseResult.Value;

            // 2. Verificar se é um comprovante válido
            if (!analise.IsComprovante)
            {
                await EnviarMensagemClienteAsync(
                    request.NumeroWhatsApp,
                    "❌ O arquivo enviado não é um comprovante de pagamento válido. Por favor, envie uma imagem ou PDF do comprovante.",
                    cancellationToken);

                return Result.Success(new ProcessarComprovanteResult(
                    Sucesso: false,
                    FaturaId: null,
                    Mensagem: "Arquivo não é um comprovante válido"
                ));
            }

            // 3. Verificar se o valor foi extraído
            if (!analise.Valor.HasValue || analise.Valor.Value <= 0)
            {
                await EnviarMensagemClienteAsync(
                    request.NumeroWhatsApp,
                    "❌ Não foi possível identificar o valor do comprovante. Por favor, envie um comprovante mais legível.",
                    cancellationToken);

                return Result.Success(new ProcessarComprovanteResult(
                    Sucesso: false,
                    FaturaId: null,
                    Mensagem: "Valor não identificado no comprovante"
                ));
            }

            // 4. Buscar fatura correspondente
            var fatura = await _faturaMatchingService.EncontrarFaturaCorrespondenteAsync(
                request.ClienteId,
                analise.Valor.Value,
                request.DataEnvioMensagemFatura,
                cancellationToken);

            if (fatura == null)
            {
                await EnviarMensagemClienteAsync(
                    request.NumeroWhatsApp,
                    $"❌ Não foi possível encontrar uma fatura pendente com o valor de R$ {analise.Valor.Value:F2}. Por favor, verifique o valor e tente novamente.",
                    cancellationToken);

                return Result.Success(new ProcessarComprovanteResult(
                    Sucesso: false,
                    FaturaId: null,
                    Mensagem: "Fatura não encontrada"
                ));
            }

            // 5. Marcar fatura como paga
            var marcarPagaResult = fatura.MarcarComoPaga();
            if (!marcarPagaResult.IsSuccess)
            {
                _logger.LogError("Erro ao marcar fatura {FaturaId} como paga: {Erro}", fatura.Id, string.Join(", ", marcarPagaResult.Errors));
                return Result.Error($"Erro ao processar pagamento: {string.Join(", ", marcarPagaResult.Errors)}");
            }

            await _faturaRepository.UpdateAsync(fatura, cancellationToken);

            // 6. Enviar comprovante para grupo de sócios
            var configs = await _configuracaoRepository.ListAsync(cancellationToken);
            var configuracao = configs.FirstOrDefault();
            if (configuracao != null && !string.IsNullOrWhiteSpace(configuracao.GrupoSociosWhatsAppId))
            {
                var mensagemGrupo = $"✅ *Comprovante Validado*\n\n" +
                                   $"Cliente: {fatura.Cliente.NomeCompleto}\n" +
                                   $"Valor: R$ {fatura.Valor:F2}\n" +
                                   $"Fatura: #{fatura.Id.ToString().Substring(0, 8)}\n" +
                                   $"Data: {DateTime.Now:dd/MM/yyyy HH:mm}";

                var envioGrupoResult = await _evolutionApiClient.EnviarMensagemParaGrupoAsync(
                    configuracao.GrupoSociosWhatsAppId,
                    mensagemGrupo,
                    request.Arquivo,
                    request.MimeType,
                    cancellationToken);

                if (!envioGrupoResult.IsSuccess)
                {
                    _logger.LogWarning("Erro ao enviar comprovante para grupo: {Erro}", string.Join(", ", envioGrupoResult.Errors));
                }
            }

            // 7. Confirmar ao cliente
            await EnviarMensagemClienteAsync(
                request.NumeroWhatsApp,
                $"✅ Comprovante validado com sucesso! A fatura de R$ {fatura.Valor:F2} foi marcada como paga.",
                cancellationToken);

            _logger.LogInformation("Comprovante processado com sucesso. Fatura {FaturaId} marcada como paga.", fatura.Id);

            return Result.Success(new ProcessarComprovanteResult(
                Sucesso: true,
                FaturaId: fatura.Id,
                Mensagem: "Comprovante validado e fatura marcada como paga"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar comprovante do cliente {ClienteId}", request.ClienteId);
            return Result.Error($"Erro inesperado ao processar comprovante: {ex.Message}");
        }
    }

    private async Task EnviarMensagemClienteAsync(string numeroWhatsApp, string mensagem, CancellationToken cancellationToken)
    {
        var resultado = await _evolutionApiClient.EnviarMensagemAsync(numeroWhatsApp, mensagem, cancellationToken);
        if (!resultado.IsSuccess)
        {
            _logger.LogWarning("Erro ao enviar mensagem para cliente {Numero}: {Erro}", numeroWhatsApp, string.Join(", ", resultado.Errors));
        }
    }
}
