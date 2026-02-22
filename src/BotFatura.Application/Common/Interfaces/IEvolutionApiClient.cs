using Ardalis.Result;

namespace BotFatura.Application.Common.Interfaces;

public interface IEvolutionApiClient
{
    Task<Result> EnviarMensagemAsync(string numeroWhatsApp, string texto, CancellationToken cancellationToken = default);
    Task<Result<string>> ObterStatusAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> GerarQrCodeAsync(CancellationToken cancellationToken = default);
    Task<Result> CriarInstanciaAsync(CancellationToken cancellationToken = default);
    Task<Result> DesconectarAsync(CancellationToken cancellationToken = default);
}
