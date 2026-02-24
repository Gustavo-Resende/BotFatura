using Ardalis.Specification;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;

namespace BotFatura.Application.Faturas.Queries;

public class FaturasParaNotificarSpec : Specification<Fatura>
{
    public FaturasParaNotificarSpec()
    {
        Query.Include(f => f.Cliente)
             .Where(f => (f.Status == StatusFatura.Pendente || f.Status == StatusFatura.Enviada) && f.Cliente.Ativo);
    }
}
