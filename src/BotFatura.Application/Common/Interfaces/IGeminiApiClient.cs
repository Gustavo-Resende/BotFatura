using Ardalis.Result;

namespace BotFatura.Application.Common.Interfaces;

public interface IGeminiApiClient
{
    Task<Result<ComprovanteAnalisadoDto>> AnalisarComprovanteAsync(byte[] arquivo, string mimeType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dados do pagador extraídos do comprovante
/// </summary>
public record DadosPagadorDto(
    string? Nome,
    string? Documento,
    string? Banco,
    string? Agencia,
    string? Conta
);

/// <summary>
/// Dados do destinatário extraídos do comprovante
/// </summary>
public record DadosDestinatarioDto(
    string? Nome,
    string? ChavePix,
    string? Documento,
    string? Banco,
    string? Agencia,
    string? Conta
);

/// <summary>
/// DTO com dados extraídos do comprovante de pagamento via Gemini
/// </summary>
public record ComprovanteAnalisadoDto(
    bool IsComprovante,
    decimal? Valor,
    DateTime? Data,
    string? TipoPagamento,
    int Confianca,
    string? Observacoes,
    DadosPagadorDto? DadosPagador,
    DadosDestinatarioDto? DadosDestinatario,
    string? NumeroComprovante
);
