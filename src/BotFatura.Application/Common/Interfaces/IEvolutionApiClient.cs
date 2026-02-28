using Ardalis.Result;

namespace BotFatura.Application.Common.Interfaces;

public interface IEvolutionApiClient
{
    Task<Result> EnviarMensagemAsync(string numeroWhatsApp, string texto, CancellationToken cancellationToken = default);
    Task<Result> EnviarMensagemParaGrupoAsync(string grupoId, string texto, byte[]? anexo = null, string? mimeType = null, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> BaixarArquivoAsync(string mediaUrl, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> BaixarMidiaDescriptografadaAsync(object messagePayload, CancellationToken cancellationToken = default);
    Task<Result<List<GrupoWhatsAppDto>>> ListarGruposAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> ObterStatusAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> GerarQrCodeAsync(CancellationToken cancellationToken = default);
    Task<Result> CriarInstanciaAsync(CancellationToken cancellationToken = default);
    Task<Result> DesconectarAsync(CancellationToken cancellationToken = default);
}

public record GrupoWhatsAppDto(string Id, string Nome, string? Descricao, int Participantes);
