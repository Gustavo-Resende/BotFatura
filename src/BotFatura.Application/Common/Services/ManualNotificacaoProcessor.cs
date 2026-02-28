using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Common.Strategies;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;

namespace BotFatura.Application.Common.Services;

public class ManualNotificacaoProcessor : NotificacaoProcessorBase
{
    public ManualNotificacaoProcessor(
        IFaturaRepository faturaRepository,
        IClienteRepository clienteRepository,
        IMensagemTemplateRepository templateRepository,
        IEvolutionApiClient evolutionApi,
        IMensagemFormatter formatter,
        IRepository<LogNotificacao> logRepository,
        Domain.Factories.ILogNotificacaoFactory logFactory,
        Domain.Interfaces.IUnitOfWork unitOfWork,
        ICacheService cacheService)
        : base(faturaRepository, clienteRepository, templateRepository, evolutionApi, formatter, logRepository, logFactory, unitOfWork, cacheService)
    {
    }

    protected override async Task<Result> ValidarPreCondicoesAsync(Fatura fatura, CancellationToken cancellationToken)
    {
        // Validar existência da fatura
        if (fatura == null)
            return Result.NotFound("Fatura não encontrada.");

        // Validar cliente
        var cliente = await _clienteRepository.GetByIdAsync(fatura.ClienteId, cancellationToken);
        if (cliente == null || !cliente.Ativo)
            return Result.Error("Cliente inexistente ou desativado.");

        // Verificar Status da Evolution API
        var statusResult = await _evolutionApi.ObterStatusAsync(cancellationToken);
        if (!statusResult.IsSuccess || statusResult.Value != "open")
            return Result.Error($"A instância do WhatsApp não está conectada. Status: {statusResult.Value}");

        return Result.Success();
    }

    protected override async Task<MensagemTemplate?> ObterTemplateAsync(INotificacaoStrategy strategy, CancellationToken cancellationToken)
    {
        // Para envio manual, busca template padrão ou primeiro disponível
        var templates = await _templateRepository.ListAsync(cancellationToken);
        return templates.FirstOrDefault(t => t.IsPadrao) ?? templates.FirstOrDefault();
    }

    protected override async Task RegistrarLogAsync(
        Domain.Entities.Fatura fatura,
        INotificacaoStrategy strategy,
        string mensagem,
        string destinatario,
        Ardalis.Result.Result sendResult,
        CancellationToken cancellationToken)
    {
        // Para manual, sempre usa "Manual" como tipo
        var log = new Domain.Entities.LogNotificacao(
            fatura.Id,
            "Manual",
            mensagem,
            destinatario,
            sendResult.IsSuccess,
            sendResult.IsSuccess ? null : string.Join(", ", sendResult.Errors));

        await _logRepository.AddAsync(log, cancellationToken);
    }

    protected override async Task AplicarDelayAsync(CancellationToken cancellationToken)
    {
        // Delay menor para envio manual (5-10 segundos)
        var delay = Random.Shared.Next(5000, 10000);
        await Task.Delay(delay, cancellationToken);
    }
}
