using Ardalis.Result;
using BotFatura.Application.Faturas.Common;
using BotFatura.Domain.Enums;
using MediatR;

namespace BotFatura.Application.Faturas.Queries.ListarFaturas;

public record ListarFaturasQuery(StatusFatura? Status = null) : IRequest<Result<List<FaturaDto>>>;
