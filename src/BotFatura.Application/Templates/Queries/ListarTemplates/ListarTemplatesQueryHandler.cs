using Ardalis.Result;
using BotFatura.Application.Templates.Common;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Templates.Queries.ListarTemplates;

public class ListarTemplatesQueryHandler : IRequestHandler<ListarTemplatesQuery, Result<List<TemplateDto>>>
{
    private readonly IMensagemTemplateRepository _repository;

    public ListarTemplatesQueryHandler(IMensagemTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<TemplateDto>>> Handle(ListarTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _repository.ListAsync(cancellationToken);
        
        var dtos = templates
            .Select(t => new TemplateDto(t.Id, t.TextoBase, t.IsPadrao))
            .ToList();

        return Result.Success(dtos);
    }
}
