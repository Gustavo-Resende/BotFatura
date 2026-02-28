using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BotFatura.Application.Comprovantes.Services;

/// <summary>
/// Serviço responsável por validar comprovantes de pagamento
/// </summary>
public class ComprovanteValidationService
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ComprovanteValidationService> _logger;

    public ComprovanteValidationService(
        IFaturaRepository faturaRepository,
        ICacheService cacheService,
        ILogger<ComprovanteValidationService> logger)
    {
        _faturaRepository = faturaRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Valida se o destinatário do comprovante corresponde aos dados configurados no sistema
    /// </summary>
    /// <param name="dadosDestinatario">Dados do destinatário extraídos do comprovante</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Result indicando sucesso ou falha com mensagem de erro</returns>
    public async Task<Result> ValidarDestinatarioAsync(
        DadosDestinatarioDto? dadosDestinatario,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Iniciando validação de destinatário. Operation={Operation}, ChavePixExtraida={ChavePixExtraida}, NomeExtraido={NomeExtraido}",
            "ValidarDestinatario",
            dadosDestinatario?.ChavePix ?? "(null)",
            dadosDestinatario?.Nome ?? "(null)");

        if (dadosDestinatario == null)
        {
            _logger.LogWarning(
                "Validação de destinatário falhou: dados não extraídos. Operation={Operation}, Success={Success}",
                "ValidarDestinatario",
                false);
            return Result.Error("Não foi possível extrair os dados do destinatário do comprovante.");
        }

        var configuracao = await _cacheService.ObterConfiguracaoAsync(cancellationToken);
        if (configuracao == null)
        {
            _logger.LogWarning(
                "Validação de destinatário falhou: configuração não encontrada. Operation={Operation}, Success={Success}",
                "ValidarDestinatario",
                false);
            return Result.Error("Configuração do sistema não encontrada. Entre em contato com o suporte.");
        }

        var chavePixConfigurada = NormalizarTexto(configuracao.ChavePix);
        var nomeTitularConfigurado = NormalizarTexto(configuracao.NomeTitularPix);

        var chavePixComprovante = NormalizarTexto(dadosDestinatario.ChavePix);
        var nomeComprovante = NormalizarTexto(dadosDestinatario.Nome);

        // Validação: chave PIX ou nome do titular deve corresponder
        var chavePixValida = !string.IsNullOrEmpty(chavePixComprovante) && 
                            !string.IsNullOrEmpty(chavePixConfigurada) &&
                            chavePixComprovante.Contains(chavePixConfigurada);

        var nomeValido = !string.IsNullOrEmpty(nomeComprovante) && 
                        !string.IsNullOrEmpty(nomeTitularConfigurado) &&
                        nomeComprovante.Contains(nomeTitularConfigurado);

        if (!chavePixValida && !nomeValido)
        {
            _logger.LogWarning(
                "Validação de destinatário falhou: dados não correspondem. Operation={Operation}, Success={Success}, ChavePixValida={ChavePixValida}, NomeValido={NomeValido}",
                "ValidarDestinatario",
                false,
                chavePixValida,
                nomeValido);
            return Result.Error("O comprovante não corresponde aos nossos dados cadastrados. Verifique se o pagamento foi feito para a chave PIX correta.");
        }

        _logger.LogInformation(
            "Validação de destinatário concluída com sucesso. Operation={Operation}, Success={Success}, ChavePixValida={ChavePixValida}, NomeValido={NomeValido}",
            "ValidarDestinatario",
            true,
            chavePixValida,
            nomeValido);

        return Result.Success();
    }

    /// <summary>
    /// Busca faturas pendentes para um cliente específico
    /// </summary>
    /// <param name="clienteId">ID do cliente</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de faturas pendentes ou enviadas</returns>
    public async Task<Result<List<Fatura>>> ObterFaturasPendentesAsync(
        Guid clienteId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Buscando faturas pendentes. Operation={Operation}, ClienteId={ClienteId}",
            "ObterFaturasPendentes",
            clienteId);

        var spec = new Specifications.FaturasPendentesClienteSpec(clienteId);
        var faturas = await _faturaRepository.ListAsync(spec, cancellationToken);

        _logger.LogInformation(
            "Busca de faturas pendentes concluída. Operation={Operation}, ClienteId={ClienteId}, QuantidadeEncontrada={Quantidade}",
            "ObterFaturasPendentes",
            clienteId,
            faturas.Count);

        return Result.Success(faturas);
    }

    /// <summary>
    /// Valida se o valor do comprovante corresponde a alguma fatura pendente
    /// </summary>
    /// <param name="valorComprovante">Valor extraído do comprovante</param>
    /// <param name="fatura">Fatura a ser validada</param>
    /// <returns>Result indicando se o valor corresponde</returns>
    public Result ValidarValor(decimal valorComprovante, Fatura fatura)
    {
        _logger.LogInformation(
            "Validando valor do comprovante. Operation={Operation}, ValorComprovante={ValorComprovante}, ValorFatura={ValorFatura}, FaturaId={FaturaId}",
            "ValidarValor",
            valorComprovante,
            fatura.Valor,
            fatura.Id);

        const decimal tolerancia = 0.01m;
        var diferencaValor = Math.Abs(valorComprovante - fatura.Valor);

        if (diferencaValor > tolerancia)
        {
            _logger.LogWarning(
                "Validação de valor falhou. Operation={Operation}, Success={Success}, ValorComprovante={ValorComprovante}, ValorFatura={ValorFatura}, Diferenca={Diferenca}",
                "ValidarValor",
                false,
                valorComprovante,
                fatura.Valor,
                diferencaValor);
            return Result.Error($"O valor do comprovante (R$ {valorComprovante:F2}) não corresponde ao valor da fatura pendente (R$ {fatura.Valor:F2}).");
        }

        _logger.LogInformation(
            "Validação de valor concluída com sucesso. Operation={Operation}, Success={Success}, FaturaId={FaturaId}",
            "ValidarValor",
            true,
            fatura.Id);

        return Result.Success();
    }

    /// <summary>
    /// Encontra a fatura correspondente ao valor do comprovante
    /// </summary>
    /// <param name="clienteId">ID do cliente</param>
    /// <param name="valorComprovante">Valor extraído do comprovante</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Fatura correspondente ou null se não encontrada</returns>
    public async Task<Result<Fatura?>> EncontrarFaturaCorrespondenteAsync(
        Guid clienteId,
        decimal valorComprovante,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Buscando fatura correspondente ao valor. Operation={Operation}, ClienteId={ClienteId}, ValorComprovante={ValorComprovante}",
            "EncontrarFaturaCorrespondente",
            clienteId,
            valorComprovante);

        var faturasPendentesResult = await ObterFaturasPendentesAsync(clienteId, cancellationToken);
        if (!faturasPendentesResult.IsSuccess)
        {
            return Result<Fatura?>.Error(new Ardalis.Result.ErrorList(faturasPendentesResult.Errors.Select(e => e.ToString()).ToArray()));
        }

        var faturas = faturasPendentesResult.Value;
        if (faturas == null || !faturas.Any())
        {
            _logger.LogWarning(
                "Nenhuma fatura pendente encontrada. Operation={Operation}, ClienteId={ClienteId}",
                "EncontrarFaturaCorrespondente",
                clienteId);
            return Result.Success<Fatura?>(null);
        }

        const decimal tolerancia = 0.01m;

        // Procurar fatura com valor correspondente
        var faturaCorrespondente = faturas
            .Where(f => Math.Abs(f.Valor - valorComprovante) <= tolerancia)
            .OrderByDescending(f => f.DataVencimento)
            .FirstOrDefault();

        if (faturaCorrespondente != null)
        {
            _logger.LogInformation(
                "Fatura correspondente encontrada. Operation={Operation}, ClienteId={ClienteId}, FaturaId={FaturaId}, ValorFatura={ValorFatura}",
                "EncontrarFaturaCorrespondente",
                clienteId,
                faturaCorrespondente.Id,
                faturaCorrespondente.Valor);
        }
        else
        {
            _logger.LogWarning(
                "Nenhuma fatura com valor correspondente. Operation={Operation}, ClienteId={ClienteId}, ValorComprovante={ValorComprovante}, FaturasPendentes={Quantidade}",
                "EncontrarFaturaCorrespondente",
                clienteId,
                valorComprovante,
                faturas.Count);
        }

        return Result.Success<Fatura?>(faturaCorrespondente);
    }

    /// <summary>
    /// Normaliza texto para comparação (lowercase, sem espaços extras)
    /// </summary>
    private static string NormalizarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        return texto
            .ToLowerInvariant()
            .Trim()
            .Replace("  ", " ");
    }
}
