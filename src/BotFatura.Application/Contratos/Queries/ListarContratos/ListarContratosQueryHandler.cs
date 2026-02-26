using Ardalis.Result;
using Ardalis.Specification;
using BotFatura.Application.Contratos.Common;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Contratos.Queries.ListarContratos;

internal sealed class ContratosComClienteSpec : Specification<Contrato>
{
    public ContratosComClienteSpec(Guid? clienteId)
    {
        Query.Include(c => c.Cliente)
             .AsNoTracking()
             .OrderByDescending(c => c.Ativo)
             .ThenBy(c => c.DataInicio);

        if (clienteId.HasValue)
            Query.Where(c => c.ClienteId == clienteId.Value);
    }
}

public class ListarContratosQueryHandler : IRequestHandler<ListarContratosQuery, Result<List<ContratoDto>>>
{
    private readonly IContratoRepository _contratoRepository;

    public ListarContratosQueryHandler(IContratoRepository contratoRepository)
    {
        _contratoRepository = contratoRepository;
    }

    public async Task<Result<List<ContratoDto>>> Handle(ListarContratosQuery request, CancellationToken cancellationToken)
    {
        var spec = new ContratosComClienteSpec(request.ClienteId);
        var contratos = await _contratoRepository.ListAsync(spec, cancellationToken);

        var dtos = contratos.Select(c => new ContratoDto(
            c.Id,
            c.ClienteId,
            c.Cliente?.NomeCompleto ?? "Cliente não encontrado",
            c.ValorMensal,
            c.DiaVencimento,
            c.DataInicio,
            c.DataFim,
            c.Ativo)).ToList();

        return Result.Success(dtos);
    }
}
