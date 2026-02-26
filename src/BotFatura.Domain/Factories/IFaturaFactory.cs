using Ardalis.Result;
using BotFatura.Domain.Entities;

namespace BotFatura.Domain.Factories;

public interface IFaturaFactory
{
    Result<Fatura> Criar(Guid clienteId, decimal valor, DateTime dataVencimento, Guid? contratoId = null);
    Result<Fatura> CriarParaContrato(Guid clienteId, decimal valor, DateTime dataVencimento, Guid contratoId);
}
