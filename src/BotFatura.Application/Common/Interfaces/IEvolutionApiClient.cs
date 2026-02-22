using Ardalis.Result;

namespace BotFatura.Application.Common.Interfaces;

public interface IEvolutionApiClient
{
    Task<Result> EnviarMensagemAsync(string numeroWhatsApp, string texto, CancellationToken cancellationToken = default);
}
