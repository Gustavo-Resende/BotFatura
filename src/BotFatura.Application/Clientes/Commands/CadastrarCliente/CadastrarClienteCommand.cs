using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Clientes.Commands.CadastrarCliente;

/// <summary>
/// Comando para cadastrar um novo cliente.
/// </summary>
/// <param name="NomeCompleto">Nome completo do cliente.</param>
/// <param name="WhatsApp">Número do WhatsApp com DDD e DDI (Ex: 5511999999999).</param>
public record CadastrarClienteCommand(string NomeCompleto, string WhatsApp) : IRequest<Result<Guid>>;

