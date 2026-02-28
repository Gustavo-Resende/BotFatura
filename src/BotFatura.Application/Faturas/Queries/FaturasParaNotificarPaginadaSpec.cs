using Ardalis.Specification;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;

namespace BotFatura.Application.Faturas.Queries;

public class FaturasParaNotificarPaginadaSpec : Specification<Fatura>
{
    public FaturasParaNotificarPaginadaSpec(int skip, int take)
    {
        Query.Include(f => f.Cliente)
             .AsNoTracking()
             .Where(f => (f.Status == StatusFatura.Pendente || f.Status == StatusFatura.Enviada) && f.Cliente.Ativo)
             .OrderBy(f => f.DataVencimento)
             .Skip(skip)
             .Take(take);
    }
}
