using Ardalis.Result;
using BotFatura.Application.Templates.Common;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Templates.Queries.ObterTemplatePorTipo;

public class ObterTemplatePorTipoQueryHandler : IRequestHandler<ObterTemplatePorTipoQuery, Result<TemplateDto>>
{
    private readonly IMensagemTemplateRepository _repository;

    public ObterTemplatePorTipoQueryHandler(IMensagemTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TemplateDto>> Handle(ObterTemplatePorTipoQuery request, CancellationToken cancellationToken)
    {
        var templates = await _repository.ListAsync(cancellationToken);
        var template = templates.FirstOrDefault(t => t.TipoNotificacao == request.Tipo);
        
        if (template == null)
            return Result.NotFound($"Template do tipo {request.Tipo} não encontrado.");

        var dto = new TemplateDto(template.Id, template.TextoBase, template.IsPadrao);
        return Result.Success(dto);
    }
}
