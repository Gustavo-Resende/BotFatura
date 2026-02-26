using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Common.Strategies;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;

namespace BotFatura.Application.Common.Services;

public class AutomaticaNotificacaoProcessor : NotificacaoProcessorBase
{
    private readonly Random _random;

    public AutomaticaNotificacaoProcessor(
        IFaturaRepository faturaRepository,
        IClienteRepository clienteRepository,
        IMensagemTemplateRepository templateRepository,
        IEvolutionApiClient evolutionApi,
        IMensagemFormatter formatter,
        IRepository<LogNotificacao> logRepository,
        Domain.Factories.ILogNotificacaoFactory logFactory)
        : base(faturaRepository, clienteRepository, templateRepository, evolutionApi, formatter, logRepository, logFactory)
    {
        _random = new Random();
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
        return await _templateRepository.ObterPorTipoAsync(strategy.ObterTipoNotificacaoTemplate(), cancellationToken);
    }

    protected override async Task AplicarDelayAsync(CancellationToken cancellationToken)
    {
        // Delay anti-ban para notificações automáticas (5-15 segundos)
        var delay = _random.Next(5000, 15000);
        await Task.Delay(delay, cancellationToken);
    }
}
