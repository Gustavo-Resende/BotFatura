using BotFatura.Domain.Entities;

namespace BotFatura.Domain.Interfaces;

public interface IClienteRepository : IRepository<Cliente>
{
}

public interface IFaturaRepository : IRepository<Fatura>
{
}

public interface IMensagemTemplateRepository : IRepository<MensagemTemplate>
{
}
