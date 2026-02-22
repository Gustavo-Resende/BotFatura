using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using BotFatura.Infrastructure.Data;

namespace BotFatura.Infrastructure.Repositories;

public class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    public ClienteRepository(AppDbContext dbContext) : base(dbContext) { }
}

public class FaturaRepository : Repository<Fatura>, IFaturaRepository
{
    public FaturaRepository(AppDbContext dbContext) : base(dbContext) { }
}

public class MensagemTemplateRepository : Repository<MensagemTemplate>, IMensagemTemplateRepository
{
    public MensagemTemplateRepository(AppDbContext dbContext) : base(dbContext) { }
}
