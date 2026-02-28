using Ardalis.Specification;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;

namespace BotFatura.Application.Comprovantes.Specifications;

/// <summary>
/// Specification para buscar faturas pendentes ou enviadas de um cliente específico
/// </summary>
public class FaturasPendentesClienteSpec : Specification<Fatura>
{
    public FaturasPendentesClienteSpec(Guid clienteId)
    {
        Query
            .Where(f => f.ClienteId == clienteId && 
                       (f.Status == StatusFatura.Pendente || f.Status == StatusFatura.Enviada))
            .Include(f => f.Cliente)
            .OrderByDescending(f => f.DataVencimento);
    }
}
