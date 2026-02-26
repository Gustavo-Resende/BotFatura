using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.AtualizarFatura;

/// <summary>
/// Atualiza os dados editáveis de uma fatura (valor e data de vencimento).
/// Não é permitido editar faturas com status Paga ou Cancelada.
/// </summary>
/// <param name="FaturaId">Identificador único da fatura a ser atualizada.</param>
/// <param name="Valor">Novo valor da fatura. Deve ser maior que zero.</param>
/// <param name="DataVencimento">Nova data de vencimento.</param>
public record AtualizarFaturaCommand(Guid FaturaId, decimal Valor, DateTime DataVencimento) : IRequest<Result>;
