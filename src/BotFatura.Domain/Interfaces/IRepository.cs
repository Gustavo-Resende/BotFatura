using Ardalis.Specification;
using BotFatura.Domain.Common;

namespace BotFatura.Domain.Interfaces;

public interface IRepository<T> : IRepositoryBase<T> where T : class
{
    // A implementação usará o Ardalis.Specification.EntityFrameworkCore no projeto de Infra
}
