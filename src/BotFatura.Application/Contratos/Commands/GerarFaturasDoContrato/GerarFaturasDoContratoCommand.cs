using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Contratos.Commands.GerarFaturasDoContrato;

/// <summary>
/// Dispara a geração automática de faturas para todos os contratos vigentes
/// cujo vencimento cai na data de referência. Operação idempotente.
/// </summary>
/// <param name="DataReferencia">Data base para calcular quais contratos devem gerar fatura hoje. Padrão: hoje + 3 dias.</param>
public record GerarFaturasDoContratoCommand(DateOnly? DataReferencia = null) : IRequest<Result>;
