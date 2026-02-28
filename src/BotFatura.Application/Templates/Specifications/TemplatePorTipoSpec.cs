using Ardalis.Specification;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;

namespace BotFatura.Application.Templates.Specifications;

public sealed class TemplatePorTipoSpec : Specification<MensagemTemplate>
{
    public TemplatePorTipoSpec(TipoNotificacaoTemplate tipo)
    {
        Query.Where(t => t.TipoNotificacao == tipo);
    }
}
