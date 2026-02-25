using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using BotFatura.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BotFatura.Infrastructure.Repositories;

public class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    public ClienteRepository(AppDbContext dbContext) : base(dbContext) { }
}

public class FaturaRepository : Repository<Fatura>, IFaturaRepository
{
    public FaturaRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<decimal> ObterSomaPorStatusAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Faturas
            .Where(f => status.Contains(f.Status))
            .SumAsync(f => f.Valor, cancellationToken);
    }

    public async Task<decimal> ObterSomaVencendoHojeAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow.Date;
        return await _dbContext.Faturas
            .Where(f => status.Contains(f.Status) && f.DataVencimento.Date == hoje)
            .SumAsync(f => f.Valor, cancellationToken);
    }

    public async Task<int> ObterContagemPorStatusAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Faturas
            .CountAsync(f => status.Contains(f.Status), cancellationToken);
    }

    public async Task<decimal> ObterSomaAtrasadasAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow.Date;
        return await _dbContext.Faturas
            .Where(f => status.Contains(f.Status) && f.DataVencimento.Date < hoje)
            .SumAsync(f => f.Valor, cancellationToken);
    }

    public async Task<int> ObterContagemAtrasadasAsync(IEnumerable<StatusFatura> status, CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow.Date;
        return await _dbContext.Faturas
            .CountAsync(f => status.Contains(f.Status) && f.DataVencimento.Date < hoje, cancellationToken);
    }
    
    public async Task<List<(DateTime Data, decimal Total)>> ObterHistoricoPagamentosAsync(DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
    {
        var faturasPagas = await _dbContext.Faturas
            .Where(f => f.Status == StatusFatura.Paga &&
                        f.DataVencimento.Date >= inicio.Date &&
                        f.DataVencimento.Date <= fim.Date)
            .GroupBy(f => f.DataVencimento.Date)
            .Select(g => new { Data = g.Key, Total = g.Sum(f => f.Valor) })
            .OrderBy(g => g.Data)
            .ToListAsync(cancellationToken);

        return faturasPagas.Select(x => (x.Data, x.Total)).ToList();
    }
}

public class MensagemTemplateRepository : Repository<MensagemTemplate>, IMensagemTemplateRepository
{
    public MensagemTemplateRepository(AppDbContext dbContext) : base(dbContext) { }
}
