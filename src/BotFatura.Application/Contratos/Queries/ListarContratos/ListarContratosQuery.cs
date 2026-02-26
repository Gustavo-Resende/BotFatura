using Ardalis.Result;
using BotFatura.Application.Contratos.Common;
using MediatR;

namespace BotFatura.Application.Contratos.Queries.ListarContratos;

/// <summary>
/// Retorna a lista de contratos, com filtro opcional por cliente.
/// </summary>
/// <param name="ClienteId">Quando informado, filtra os contratos de um cliente específico.</param>
public record ListarContratosQuery(Guid? ClienteId = null) : IRequest<Result<List<ContratoDto>>>;
