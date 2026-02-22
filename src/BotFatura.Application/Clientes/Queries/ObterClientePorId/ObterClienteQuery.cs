using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Clientes.Queries.ObterClientePorId;

public record ObterClienteQuery(Guid Id) : IRequest<Result<ClienteDto>>;
