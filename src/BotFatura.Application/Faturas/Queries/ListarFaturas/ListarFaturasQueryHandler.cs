using Ardalis.Result;
using BotFatura.Application.Faturas.Common;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Faturas.Queries.ListarFaturas;

public class ListarFaturasQueryHandler : IRequestHandler<ListarFaturasQuery, Result<List<FaturaDto>>>
{
    private readonly IFaturaRepository _repository;

    public ListarFaturasQueryHandler(IFaturaRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<FaturaDto>>> Handle(ListarFaturasQuery request, CancellationToken cancellationToken)
    {
        var spec = new FaturasComClientesSpec(request.Status);
        var faturas = await _repository.ListAsync(spec, cancellationToken);

        var dtos = faturas.Select(f => new FaturaDto(
            f.Id,
            f.ClienteId,
            f.Cliente?.NomeCompleto ?? "Cliente não encontrado",
            f.Valor,
            f.DataVencimento,
            f.Status
        )).ToList();

        return Result.Success(dtos);
    }
}
