using BotFatura.Domain.Common;

namespace BotFatura.Domain.Entities;

public class LogComprovante : Entity
{
    public Guid ClienteId { get; private set; }
    public Guid? FaturaId { get; private set; }
    public decimal? ValorExtraido { get; private set; }
    public decimal? ValorEsperado { get; private set; }
    public bool Sucesso { get; private set; }
    public string? Erro { get; private set; }
    public string TipoArquivo { get; private set; } = null!;
    public int TamanhoArquivo { get; private set; }

    // Navegação
    public Cliente Cliente { get; private set; } = null!;
    public Fatura? Fatura { get; private set; }

    protected LogComprovante() { }

    public LogComprovante(
        Guid clienteId,
        Guid? faturaId,
        decimal? valorExtraido,
        decimal? valorEsperado,
        bool sucesso,
        string tipoArquivo,
        int tamanhoArquivo,
        string? erro = null)
    {
        ClienteId = clienteId;
        FaturaId = faturaId;
        ValorExtraido = valorExtraido;
        ValorEsperado = valorEsperado;
        Sucesso = sucesso;
        TipoArquivo = tipoArquivo;
        TamanhoArquivo = tamanhoArquivo;
        Erro = erro;
    }
}
