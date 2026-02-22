using Ardalis.Result;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Clientes.Queries.ObterClientePorId;

public class ObterClienteQueryHandler : IRequestHandler<ObterClienteQuery, Result<ClienteDto>>
{
    private readonly IClienteRepository _clienteRepository;

    public ObterClienteQueryHandler(IClienteRepository clienteRepository)
    {
        _clienteRepository = clienteRepository;
    }

    public async Task<Result<ClienteDto>> Handle(ObterClienteQuery request, CancellationToken cancellationToken)
    {
        var cliente = await _clienteRepository.GetByIdAsync(request.Id, cancellationToken);

        if (cliente == null)
            return Result.NotFound($"Cliente com ID {request.Id} não encontrado.");

        var dto = new ClienteDto(
            cliente.Id,
            cliente.NomeCompleto,
            cliente.WhatsApp,
            cliente.Ativo,
            cliente.CreatedAt
        );

        return Result.Success(dto);
    }
}
