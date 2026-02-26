using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using BotFatura.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BotFatura.Infrastructure.Repositories;

public class ContratoRepository : Repository<Contrato>, IContratoRepository
{
    public ContratoRepository(AppDbContext dbContext) : base(dbContext) { }

    /// <inheritdoc />
    public async Task<List<Contrato>> ListarVigentesParaGerarFaturaAsync(
        DateOnly dataReferencia,
        CancellationToken cancellationToken = default)
    {
        // Filtra contratos vigentes cujo DiaVencimento bate com o dia de referência.
        // Usa filtered Include (EF Core 5+) para carregar apenas as faturas do mês alvo,
        // evitando carregar toda a coleção histórica de faturas por contrato.
        return await _dbContext.Contratos
            .Where(c =>
                c.Ativo &&
                c.DataInicio <= dataReferencia &&
                (c.DataFim == null || c.DataFim >= dataReferencia) &&
                c.DiaVencimento == dataReferencia.Day)
            .Include(c => c.Cliente)
            .Include(c => c.Faturas.Where(f =>
                f.DataVencimento.Year  == dataReferencia.Year &&
                f.DataVencimento.Month == dataReferencia.Month))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
