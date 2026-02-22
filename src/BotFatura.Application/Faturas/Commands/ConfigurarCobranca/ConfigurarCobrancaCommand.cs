using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.ConfigurarCobranca;

public record ConfigurarCobrancaCommand(Guid ClienteId, decimal Valor, DateTime DataVencimento) : IRequest<Result<Guid>>;
