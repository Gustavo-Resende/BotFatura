using BotFatura.Domain.Enums;

namespace BotFatura.Application.Faturas.Common;

public record FaturaDto(
    Guid Id, 
    Guid ClienteId, 
    string NomeCliente,
    decimal Valor, 
    DateTime DataVencimento, 
    StatusFatura Status);
