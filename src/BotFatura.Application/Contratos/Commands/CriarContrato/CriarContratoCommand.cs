using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Contratos.Commands.CriarContrato;

/// <summary>
/// Cria um novo contrato de cobrança recorrente para um cliente.
/// </summary>
/// <param name="ClienteId">Identificador único do cliente.</param>
/// <param name="ValorMensal">Valor fixo cobrado a cada ciclo mensal.</param>
/// <param name="DiaVencimento">Dia do mês em que a fatura vence (1 a 28).</param>
/// <param name="DataInicio">Data de início da vigência do contrato.</param>
/// <param name="DataFim">Data de encerramento. Nulo para contratos por prazo indeterminado.</param>
public record CriarContratoCommand(
    Guid      ClienteId,
    decimal   ValorMensal,
    int       DiaVencimento,
    DateOnly  DataInicio,
    DateOnly? DataFim) : IRequest<Result<Guid>>;
