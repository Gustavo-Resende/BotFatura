using Ardalis.Result;
using BotFatura.Domain.Enums;
using MediatR;

namespace BotFatura.Application.Templates.Commands.ResetarTemplatePorTipo;

public record ResetarTemplatePorTipoCommand(TipoNotificacaoTemplate Tipo) : IRequest<Result>;
