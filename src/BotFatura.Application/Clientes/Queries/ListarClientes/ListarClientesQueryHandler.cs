using Ardalis.Result;
using BotFatura.Application.Clientes.Queries.ObterClientePorId;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Clientes.Queries.ListarClientes;

public class ListarClientesQueryHandler : IRequestHandler<ListarClientesQuery, Result<List<ClienteDto>>>
{
    private readonly IClienteRepository _repository;

    public ListarClientesQueryHandler(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<ClienteDto>>> Handle(ListarClientesQuery request, CancellationToken cancellationToken)
    {
        var spec = new ListarClientesSpec();
        var clientes = await _repository.ListAsync(spec, cancellationToken);
        
        var dtos = clientes
            .Select(c => new ClienteDto(c.Id, c.NomeCompleto, c.WhatsApp, c.Ativo, c.CreatedAt))
            .ToList();

        return Result.Success(dtos);
    }
}
