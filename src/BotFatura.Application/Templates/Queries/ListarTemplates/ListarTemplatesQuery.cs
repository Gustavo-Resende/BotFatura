using Ardalis.Result;
using BotFatura.Application.Templates.Common;
using MediatR;

namespace BotFatura.Application.Templates.Queries.ListarTemplates;

public record ListarTemplatesQuery() : IRequest<Result<List<TemplateDto>>>;
