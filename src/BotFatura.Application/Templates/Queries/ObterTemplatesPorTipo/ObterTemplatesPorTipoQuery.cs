using Ardalis.Result;
using BotFatura.Application.Templates.Common;
using BotFatura.Domain.Enums;
using MediatR;

namespace BotFatura.Application.Templates.Queries.ObterTemplatesPorTipo;

public record TemplatesPorTipoDto(
    TemplateDto? Lembrete,
    TemplateDto? Vencimento,
    TemplateDto? AposVencimento
);

public record ObterTemplatesPorTipoQuery() : IRequest<Result<TemplatesPorTipoDto>>;
