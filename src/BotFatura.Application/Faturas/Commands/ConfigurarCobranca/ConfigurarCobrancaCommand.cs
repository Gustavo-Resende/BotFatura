using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.ConfigurarCobranca;

/// <summary>
/// Comando para configurar uma nova cobrança (fatura) para um cliente.
/// </summary>
/// <param name="ClienteId">Identificador único do cliente.</param>
/// <param name="Valor">Valor da fatura.</param>
/// <param name="DataVencimento">Data e hora de vencimento (Ex: 2026-02-22T16:40:00).</param>
public record ConfigurarCobrancaCommand(Guid ClienteId, decimal Valor, DateTime DataVencimento) : IRequest<Result<Guid>>;

