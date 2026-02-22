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
        var clientes = await _repository.ListAsync(cancellationToken);
        
        var dtos = clientes
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ClienteDto(c.Id, c.NomeCompleto, c.WhatsApp, c.Ativo, c.CreatedAt))
            .ToList();

        return Result.Success(dtos);
    }
}
