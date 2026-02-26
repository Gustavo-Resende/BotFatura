using Ardalis.Result;
using BotFatura.Application.Templates.Common;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Templates.Queries.ObterTemplatesPorTipo;

public class ObterTemplatesPorTipoQueryHandler : IRequestHandler<ObterTemplatesPorTipoQuery, Result<TemplatesPorTipoDto>>
{
    private readonly IMensagemTemplateRepository _repository;

    public ObterTemplatesPorTipoQueryHandler(IMensagemTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TemplatesPorTipoDto>> Handle(ObterTemplatesPorTipoQuery request, CancellationToken cancellationToken)
    {
        var templates = await _repository.ListAsync(cancellationToken);
        
        var lembrete = templates.FirstOrDefault(t => t.TipoNotificacao == TipoNotificacaoTemplate.Lembrete);
        var vencimento = templates.FirstOrDefault(t => t.TipoNotificacao == TipoNotificacaoTemplate.Vencimento);
        var aposVencimento = templates.FirstOrDefault(t => t.TipoNotificacao == TipoNotificacaoTemplate.AposVencimento);

        var dto = new TemplatesPorTipoDto(
            Lembrete: lembrete != null ? new TemplateDto(lembrete.Id, lembrete.TextoBase, lembrete.IsPadrao) : null,
            Vencimento: vencimento != null ? new TemplateDto(vencimento.Id, vencimento.TextoBase, vencimento.IsPadrao) : null,
            AposVencimento: aposVencimento != null ? new TemplateDto(aposVencimento.Id, aposVencimento.TextoBase, aposVencimento.IsPadrao) : null
        );

        return Result.Success(dto);
    }
}
