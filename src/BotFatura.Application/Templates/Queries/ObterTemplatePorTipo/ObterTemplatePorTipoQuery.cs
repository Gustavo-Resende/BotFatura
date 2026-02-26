using Ardalis.Result;
using BotFatura.Application.Templates.Common;
using BotFatura.Domain.Enums;
using MediatR;

namespace BotFatura.Application.Templates.Queries.ObterTemplatePorTipo;

public record ObterTemplatePorTipoQuery(TipoNotificacaoTemplate Tipo) : IRequest<Result<TemplateDto>>;
