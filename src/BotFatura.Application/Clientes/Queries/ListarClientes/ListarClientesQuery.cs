using Ardalis.Result;
using BotFatura.Application.Clientes.Queries.ObterClientePorId;
using MediatR;

namespace BotFatura.Application.Clientes.Queries.ListarClientes;

public record ListarClientesQuery() : IRequest<Result<List<ClienteDto>>>;
