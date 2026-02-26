using Ardalis.Specification;
using BotFatura.Domain.Entities;

namespace BotFatura.Application.Clientes.Queries.ListarClientes;

public class ListarClientesSpec : Specification<Cliente>
{
    public ListarClientesSpec()
    {
        Query.AsNoTracking()
             .OrderByDescending(c => c.CreatedAt);
    }
}
