using Ardalis.Result;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Templates.Commands.AtualizarTemplatePorTipo;

public class AtualizarTemplatePorTipoCommandHandler : IRequestHandler<AtualizarTemplatePorTipoCommand, Result>
{
    private readonly IMensagemTemplateRepository _repository;

    public AtualizarTemplatePorTipoCommandHandler(IMensagemTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(AtualizarTemplatePorTipoCommand request, CancellationToken cancellationToken)
    {
        var templates = await _repository.ListAsync(cancellationToken);
        var template = templates.FirstOrDefault(t => t.TipoNotificacao == request.Tipo);
        
        if (template == null)
            return Result.NotFound($"Template do tipo {request.Tipo} não encontrado.");

        var updateResult = template.AtualizarTexto(request.TextoBase);
        if (!updateResult.IsSuccess)
            return updateResult;

        await _repository.UpdateAsync(template, cancellationToken);
        return Result.Success();
    }
}
