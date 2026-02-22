using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.CancelarFatura;

public record CancelarFaturaCommand(Guid FaturaId) : IRequest<Result>;
