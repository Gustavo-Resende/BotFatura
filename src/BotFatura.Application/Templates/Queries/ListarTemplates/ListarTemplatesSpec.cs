using Ardalis.Specification;
using BotFatura.Domain.Entities;

namespace BotFatura.Application.Templates.Queries.ListarTemplates;

public class ListarTemplatesSpec : Specification<MensagemTemplate>
{
    public ListarTemplatesSpec()
    {
        Query.AsNoTracking();
    }
}
