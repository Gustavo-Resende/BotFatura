namespace BotFatura.Application.Contratos.Common;

public record ContratoDto(
    Guid         Id,
    Guid         ClienteId,
    string       NomeCliente,
    decimal      ValorMensal,
    int          DiaVencimento,
    DateOnly     DataInicio,
    DateOnly?    DataFim,
    bool         Ativo);
