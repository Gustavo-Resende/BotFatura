using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Contratos.Commands.EncerrarContrato;

/// <summary>
/// Encerra um contrato ativo, impedindo a geração de novas faturas.
/// </summary>
/// <param name="ContratoId">Identificador único do contrato a ser encerrado.</param>
public record EncerrarContratoCommand(Guid ContratoId) : IRequest<Result>;
