using Ardalis.Result;
using BotFatura.Domain.Enums;
using MediatR;

namespace BotFatura.Application.Templates.Commands.AtualizarTemplatePorTipo;

public record AtualizarTemplatePorTipoCommand(TipoNotificacaoTemplate Tipo, string TextoBase) : IRequest<Result>;
