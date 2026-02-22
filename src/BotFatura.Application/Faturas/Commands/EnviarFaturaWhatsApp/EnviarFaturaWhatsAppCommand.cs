using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.EnviarFaturaWhatsApp;

/// <summary>
/// Comando para disparar manualmente uma fatura para o WhatsApp do cliente.
/// </summary>
/// <param name="FaturaId">ID da fatura que será enviada.</param>
public record EnviarFaturaWhatsAppCommand(Guid FaturaId) : IRequest<Result>;
