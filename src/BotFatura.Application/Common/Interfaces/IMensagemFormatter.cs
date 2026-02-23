using BotFatura.Domain.Entities;

namespace BotFatura.Application.Common.Interfaces;

public interface IMensagemFormatter
{
    Task<string> FormatarMensagemAsync(string template, Cliente cliente, Fatura fatura, CancellationToken cancellationToken = default);
}
