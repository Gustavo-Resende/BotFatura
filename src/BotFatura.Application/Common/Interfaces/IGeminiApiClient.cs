using Ardalis.Result;

namespace BotFatura.Application.Common.Interfaces;

public interface IGeminiApiClient
{
    Task<Result<ComprovanteAnalisadoDto>> AnalisarComprovanteAsync(byte[] arquivo, string mimeType, CancellationToken cancellationToken = default);
}

public record ComprovanteAnalisadoDto(
    bool IsComprovante,
    decimal? Valor,
    DateTime? Data,
    string? TipoPagamento,
    int Confianca,
    string? Observacoes
);
