using Ardalis.Result;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Templates.Commands.AtualizarTemplate;

public class AtualizarTemplateCommandHandler : IRequestHandler<AtualizarTemplateCommand, Result>
{
    private readonly IMensagemTemplateRepository _repository;

    public AtualizarTemplateCommandHandler(IMensagemTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(AtualizarTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (template == null)
            return Result.NotFound("Template não encontrado.");

        var updateResult = template.AtualizarTexto(request.TextoBase);
        if (!updateResult.IsSuccess)
            return updateResult;

        await _repository.UpdateAsync(template, cancellationToken);
        return Result.Success();
    }
}
