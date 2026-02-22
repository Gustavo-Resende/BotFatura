using Ardalis.Specification;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;

namespace BotFatura.Application.Faturas.Queries.ListarFaturas;

public class FaturasComClientesSpec : Specification<Fatura>
{
    public FaturasComClientesSpec(StatusFatura? status = null)
    {
        Query.Include(f => f.Cliente);


        if (status.HasValue)
        {
            Query.Where(f => f.Status == status.Value);
        }

        Query.OrderByDescending(f => f.DataVencimento);
    }
}
