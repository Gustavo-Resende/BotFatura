using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;

namespace BotFatura.Domain.Interfaces;

public interface IClienteRepository : IRepository<Cliente>
{
}

public record FaturaDadosConsolidados
{
    public decimal TotalPendente { get; init; }
    public decimal TotalVencendoHoje { get; init; }
    public decimal TotalPago { get; init; }
    public decimal TotalAtrasado { get; init; }
    public int FaturasPendentesCount { get; init; }
    public int FaturasAtrasadasCount { get; init; }
}

public interface IFaturaRepository : IRepository<Fatura>
{
    Task<FaturaDadosConsolidados> ObterDadosConsolidadosDashboardAsync(CancellationToken cancellationToken = default);
    Task<decimal> ObterSomaPorStatusAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default);
    Task<decimal> ObterSomaVencendoHojeAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default);
    Task<int> ObterContagemPorStatusAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default);
    Task<decimal> ObterSomaAtrasadasAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default);
    Task<int> ObterContagemAtrasadasAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default);
    Task<List<(DateTime Data, decimal Total)>> ObterHistoricoPagamentosAsync(DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);
}

public interface IMensagemTemplateRepository : IRepository<MensagemTemplate>
{
}
