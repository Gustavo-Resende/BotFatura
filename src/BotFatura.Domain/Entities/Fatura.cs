using Ardalis.GuardClauses;
using Ardalis.Result;
using BotFatura.Domain.Common;
using BotFatura.Domain.Enums;

namespace BotFatura.Domain.Entities;

public class Fatura : Entity
{
    public Guid ClienteId { get; private set; }
    public decimal Valor { get; private set; }
    public DateTime DataVencimento { get; private set; }
    public StatusFatura Status { get; private set; }
    
    // Navegação
    public Cliente Cliente { get; private set; } = null!;


    protected Fatura() { }

    public Fatura(Guid clienteId, decimal valor, DateTime dataVencimento)
    {
        ClienteId = Guard.Against.Default(clienteId, nameof(clienteId));
        Valor = Guard.Against.NegativeOrZero(valor, nameof(valor));
        DataVencimento = Guard.Against.Default(dataVencimento, nameof(dataVencimento));
        
        Status = StatusFatura.Pendente;
    }

    public Result MarcarComoEnviada()
    {
        if (Status != StatusFatura.Pendente)
            return Result.Error("Apenas faturas pendentes podem ser marcadas como enviadas.");

        Status = StatusFatura.Enviada;
        return Result.Success();
    }

    public Result MarcarComoPaga()
    {
        if (Status == StatusFatura.Cancelada)
            return Result.Error("Uma fatura cancelada não pode ser paga.");

        Status = StatusFatura.Paga;
        return Result.Success();
    }

    public Result Cancelar()
    {
        if (Status == StatusFatura.Paga)
            return Result.Error("Uma fatura já paga não pode ser cancelada.");
            
        Status = StatusFatura.Cancelada;
        return Result.Success();
    }
}
