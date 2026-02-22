using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Clientes.Commands.ExcluirCliente;

public record ExcluirClienteCommand(Guid Id) : IRequest<Result>;
