using Ardalis.GuardClauses;
using Ardalis.Result;
using BotFatura.Domain.Common;

namespace BotFatura.Domain.Entities;

/// <summary>
/// Representa um contrato de cobrança recorrente com um cliente.
/// É responsável pela geração automática de faturas mensais.
/// </summary>
public class Contrato : Entity
{
    public Guid ClienteId { get; private set; }

    public decimal ValorMensal { get; private set; }

    public int DiaVencimento { get; private set; }

    public DateOnly DataInicio { get; private set; }

    public DateOnly? DataFim { get; private set; }

    public bool Ativo { get; private set; }

    // Navegação (EF Core)
    public Cliente Cliente { get; private set; } = null!;
    public ICollection<Fatura> Faturas { get; private set; } = [];

    protected Contrato() { }

    public Contrato(Guid clienteId, decimal valorMensal, int diaVencimento, DateOnly dataInicio, DateOnly? dataFim)
    {
        ClienteId     = Guard.Against.Default(clienteId, nameof(clienteId));
        ValorMensal   = Guard.Against.NegativeOrZero(valorMensal, nameof(valorMensal));
        DiaVencimento = Guard.Against.OutOfRange(diaVencimento, nameof(diaVencimento), 1, 28);
        DataInicio    = Guard.Against.Default(dataInicio, nameof(dataInicio));

        if (dataFim.HasValue && dataFim.Value <= dataInicio)
            throw new ArgumentException("A data de fim deve ser posterior à data de início.", nameof(dataFim));

        DataFim = dataFim;
        Ativo   = true;
    }

    public bool EstaVigente(DateOnly dataReferencia) =>
        Ativo && dataReferencia >= DataInicio && (DataFim == null || dataReferencia <= DataFim);

    public DateOnly CalcularVencimentoDoMes(int ano, int mes)
    {
        var diasNoMes = DateTime.DaysInMonth(ano, mes);
        var diaEfetivo = Math.Min(DiaVencimento, diasNoMes);
        return new DateOnly(ano, mes, diaEfetivo);
    }

    public Result Encerrar()
    {
        if (!Ativo)
            return Result.Error("Este contrato já está encerrado.");

        Ativo   = false;
        DataFim = DateOnly.FromDateTime(DateTime.UtcNow);
        return Result.Success();
    }
}
