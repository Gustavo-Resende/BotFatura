using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using BotFatura.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BotFatura.Infrastructure.Repositories;

public class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    public ClienteRepository(AppDbContext dbContext) : base(dbContext) { }

    /// <summary>
    /// Busca cliente pelo JID do WhatsApp (Evolution API).
    /// Primeiro tenta buscar pelo JID exato armazenado, depois pelo número base.
    /// </summary>
    public async Task<Cliente?> BuscarPorWhatsAppJidAsync(string jid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jid))
            return null;

        // Extrair apenas a parte numérica do JID (antes do @)
        var numeroJid = jid.Split('@')[0];

        // Primeiro, tentar buscar pelo JID completo armazenado
        var cliente = await _dbContext.Clientes
            .FirstOrDefaultAsync(c => c.WhatsAppJid == jid && c.Ativo, cancellationToken);

        if (cliente != null)
            return cliente;

        // Se não encontrou, tentar pelo número base do JID
        cliente = await _dbContext.Clientes
            .FirstOrDefaultAsync(c => 
                c.Ativo && 
                (c.WhatsAppJid != null && c.WhatsAppJid.StartsWith(numeroJid + "@")), 
                cancellationToken);

        if (cliente != null)
            return cliente;

        // Fallback: tentar pelo número do WhatsApp tradicional
        cliente = await _dbContext.Clientes
            .FirstOrDefaultAsync(c => 
                c.Ativo &&
                (c.WhatsApp == numeroJid || 
                 c.WhatsApp.EndsWith(numeroJid) ||
                 numeroJid.EndsWith(c.WhatsApp.Replace("+", "").Replace("-", "").Replace(" ", ""))),
                cancellationToken);

        return cliente;
    }
}

public class FaturaRepository : Repository<Fatura>, IFaturaRepository
{
    public FaturaRepository(AppDbContext dbContext) : base(dbContext) { }
    
    public async Task<FaturaDadosConsolidados> ObterDadosConsolidadosDashboardAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow.Date;
        var pendentesOuEnviadas = new[] { StatusFatura.Pendente, StatusFatura.Enviada };

        var faturas = _dbContext.Faturas.AsNoTracking();

        var totalPendente = await faturas
            .Where(f => pendentesOuEnviadas.Contains(f.Status))
            .SumAsync(f => f.Valor, cancellationToken);

        var totalVencendoHoje = await faturas
            .Where(f => pendentesOuEnviadas.Contains(f.Status) && f.DataVencimento.Date == hoje)
            .SumAsync(f => f.Valor, cancellationToken);

        var totalPago = await faturas
            .Where(f => f.Status == StatusFatura.Paga)
            .SumAsync(f => f.Valor, cancellationToken);

        var totalAtrasado = await faturas
            .Where(f => pendentesOuEnviadas.Contains(f.Status) && f.DataVencimento.Date < hoje)
            .SumAsync(f => f.Valor, cancellationToken);

        var faturasPendentesCount = await faturas
            .CountAsync(f => pendentesOuEnviadas.Contains(f.Status), cancellationToken);

        var faturasAtrasadasCount = await faturas
            .CountAsync(f => pendentesOuEnviadas.Contains(f.Status) && f.DataVencimento.Date < hoje, cancellationToken);

        return new FaturaDadosConsolidados
        {
            TotalPendente = totalPendente,
            TotalVencendoHoje = totalVencendoHoje,
            TotalPago = totalPago,
            TotalAtrasado = totalAtrasado,
            FaturasPendentesCount = faturasPendentesCount,
            FaturasAtrasadasCount = faturasAtrasadasCount
        };
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

    public async Task<List<Application.Dashboard.Queries.ObterClientesAtrasados.ClienteAtrasadoDto>> ObterClientesAtrasadosAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow.Date;
        
        return await _dbContext.Faturas
            .AsNoTracking()
            .Include(f => f.Cliente)
            .Where(f => f.Status == StatusFatura.Pendente && 
                       f.DataVencimento.Date < hoje && 
                       f.Cliente.Ativo)
            .GroupBy(f => new { f.ClienteId, f.Cliente.NomeCompleto, f.Cliente.WhatsApp })
            .Select(g => new Application.Dashboard.Queries.ObterClientesAtrasados.ClienteAtrasadoDto
            {
                ClienteId = g.Key.ClienteId,
                Nome = g.Key.NomeCompleto,
                WhatsApp = g.Key.WhatsApp,
                FaturasAtrasadas = g.Count(),
                ValorTotalAtrasado = g.Sum(f => f.Valor)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Application.Faturas.Common.FaturaDto>> ListarFaturasComProjecaoAsync(StatusFatura? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Faturas
            .AsNoTracking()
            .Include(f => f.Cliente)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        return await query
            .OrderByDescending(f => f.DataVencimento)
            .Select(f => new Application.Faturas.Common.FaturaDto(
                f.Id,
                f.ClienteId,
                f.Cliente != null ? f.Cliente.NomeCompleto : "Cliente não encontrado",
                f.Valor,
                f.DataVencimento,
                f.Status
            ))
            .ToListAsync(cancellationToken);
    }
}

public class MensagemTemplateRepository : Repository<MensagemTemplate>, IMensagemTemplateRepository
{
    public MensagemTemplateRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<MensagemTemplate?> ObterPorTipoAsync(Domain.Enums.TipoNotificacaoTemplate tipo, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MensagensTemplate
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TipoNotificacao == tipo, cancellationToken);
    }
}
