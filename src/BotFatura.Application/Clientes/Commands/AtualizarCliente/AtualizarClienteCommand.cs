using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Clientes.Commands.AtualizarCliente;

public record AtualizarClienteCommand(Guid Id, string NomeCompleto, string WhatsApp) : IRequest<Result>;
