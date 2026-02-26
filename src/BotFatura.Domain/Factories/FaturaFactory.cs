using Ardalis.Result;
using BotFatura.Domain.Entities;

namespace BotFatura.Domain.Factories;

public class FaturaFactory : IFaturaFactory
{
    public Result<Fatura> Criar(Guid clienteId, decimal valor, DateTime dataVencimento, Guid? contratoId = null)
    {
        try
        {
            var fatura = new Fatura(clienteId, valor, dataVencimento, contratoId);
            return Result<Fatura>.Success(fatura);
        }
        catch (Exception ex)
        {
            return Result<Fatura>.Error($"Erro ao criar fatura: {ex.Message}");
        }
    }

    public Result<Fatura> CriarParaContrato(Guid clienteId, decimal valor, DateTime dataVencimento, Guid contratoId)
    {
        return Criar(clienteId, valor, dataVencimento, contratoId);
    }
}
