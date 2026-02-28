using Ardalis.Specification;
using BotFatura.Domain.Entities;

namespace BotFatura.Application.Configuracoes.Specifications;

public sealed class ConfiguracaoUnicaSpec : Specification<Configuracao>
{
    public ConfiguracaoUnicaSpec()
    {
        Query.OrderBy(c => c.CreatedAt).Take(1);
    }
}
