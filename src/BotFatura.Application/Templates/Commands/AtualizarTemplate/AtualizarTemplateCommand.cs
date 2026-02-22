using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Templates.Commands.AtualizarTemplate;

public record AtualizarTemplateCommand(Guid Id, string TextoBase) : IRequest<Result>;
