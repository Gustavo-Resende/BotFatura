using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Clientes.Commands.CadastrarCliente;

public record CadastrarClienteCommand(string NomeCompleto, string WhatsApp) : IRequest<Result<Guid>>;
