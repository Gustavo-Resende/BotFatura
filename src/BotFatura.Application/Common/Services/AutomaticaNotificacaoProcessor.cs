using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Common.Strategies;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;

namespace BotFatura.Application.Common.Services;

public class AutomaticaNotificacaoProcessor : NotificacaoProcessorBase
{
    public AutomaticaNotificacaoProcessor(
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
        // Validações básicas - cliente já está incluído na query
        if (fatura.Cliente == null || !fatura.Cliente.Ativo)
            return Result.Error("Cliente inexistente ou desativado.");

        return Result.Success();
    }

    protected override async Task<MensagemTemplate?> ObterTemplateAsync(INotificacaoStrategy strategy, CancellationToken cancellationToken)
    {
        return await _cacheService.ObterTemplateAsync(strategy.ObterTipoNotificacaoTemplate(), cancellationToken);
    }

    protected override async Task AplicarDelayAsync(CancellationToken cancellationToken)
    {
        // Delay anti-ban para notificações automáticas (5-15 segundos)
        var delay = Random.Shared.Next(5000, 15000);
        await Task.Delay(delay, cancellationToken);
    }
}
