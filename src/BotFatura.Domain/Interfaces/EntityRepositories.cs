using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;

namespace BotFatura.Domain.Interfaces;

public interface IClienteRepository : IRepository<Cliente>
{
}

public interface IFaturaRepository : IRepository<Fatura>
{
    Task<decimal> ObterSomaPorStatusAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default);
    Task<decimal> ObterSomaVencendoHojeAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default);
    Task<int> ObterContagemPorStatusAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default);
    Task<decimal> ObterSomaAtrasadasAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default);
    Task<int> ObterContagemAtrasadasAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default);
}

public interface IMensagemTemplateRepository : IRepository<MensagemTemplate>
{
}
