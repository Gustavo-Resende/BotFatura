using Ardalis.Specification.EntityFrameworkCore;
using BotFatura.Domain.Interfaces;
using BotFatura.Infrastructure.Data;

namespace BotFatura.Infrastructure.Repositories;

public class Repository<T> : RepositoryBase<T>, IRepository<T> where T : class
{
    private readonly AppDbContext _dbContext;

    public Repository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}
