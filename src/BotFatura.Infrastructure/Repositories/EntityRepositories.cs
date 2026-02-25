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
    
    public async Task<FaturaDadosConsolidados> ObterDadosConsolidadosDashboardAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow.Date;
        var pendentesOuEnviadas = new[] { StatusFatura.Pendente, StatusFatura.Enviada };

        return await _dbContext.Faturas
            .GroupBy(_ => 1)
            .Select(g => new FaturaDadosConsolidados
            {
                TotalPendente = g.Where(f => pendentesOuEnviadas.Contains(f.Status)).Sum(f => f.Valor),
                TotalVencendoHoje = g.Where(f => pendentesOuEnviadas.Contains(f.Status) && f.DataVencimento.Date == hoje).Sum(f => f.Valor),
                TotalPago = g.Where(f => f.Status == StatusFatura.Paga).Sum(f => f.Valor),
                TotalAtrasado = g.Where(f => pendentesOuEnviadas.Contains(f.Status) && f.DataVencimento.Date < hoje).Sum(f => f.Valor),
                FaturasPendentesCount = g.Count(f => pendentesOuEnviadas.Contains(f.Status)),
                FaturasAtrasadasCount = g.Count(f => pendentesOuEnviadas.Contains(f.Status) && f.DataVencimento.Date < hoje)
            })
            .FirstOrDefaultAsync(cancellationToken) ?? new FaturaDadosConsolidados();
    }

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
}

public class MensagemTemplateRepository : Repository<MensagemTemplate>, IMensagemTemplateRepository
{
    public MensagemTemplateRepository(AppDbContext dbContext) : base(dbContext) { }
}
