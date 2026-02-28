using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;

namespace BotFatura.IntegrationTests.Fakes;

/// <summary>
/// Implementação fake do IEvolutionApiClient para testes de integração.
/// Registra todas as mensagens enviadas para verificação nos testes.
/// </summary>
public class FakeEvolutionApiClient : IEvolutionApiClient
{
    private readonly List<MensagemEnviada> _mensagensEnviadas = new();
    private readonly List<MensagemGrupo> _mensagensGrupo = new();
    private string _statusWhatsApp = "open";
    private bool _simularErro;
    private string? _mensagemErro;

    /// <summary>
    /// Lista de mensagens enviadas para clientes
    /// </summary>
    public IReadOnlyList<MensagemEnviada> MensagensEnviadas => _mensagensEnviadas.AsReadOnly();

    /// <summary>
    /// Lista de mensagens enviadas para grupos
    /// </summary>
    public IReadOnlyList<MensagemGrupo> MensagensGrupo => _mensagensGrupo.AsReadOnly();

    /// <summary>
    /// Configura o status do WhatsApp (open, close, etc)
    /// </summary>
    public FakeEvolutionApiClient ComStatus(string status)
    {
        _statusWhatsApp = status;
        return this;
    }

    /// <summary>
    /// Configura para simular erro em todas as operações
    /// </summary>
    public FakeEvolutionApiClient SimularErro(string mensagem = "Erro simulado")
    {
        _simularErro = true;
        _mensagemErro = mensagem;
        return this;
    }

    /// <summary>
    /// Reseta o estado do fake
    /// </summary>
    public void Reset()
    {
        _mensagensEnviadas.Clear();
        _mensagensGrupo.Clear();
        _statusWhatsApp = "open";
        _simularErro = false;
        _mensagemErro = null;
    }

    public Task<Result<string>> ObterStatusAsync(CancellationToken cancellationToken = default)
    {
        if (_simularErro)
            return Task.FromResult(Result<string>.Error(_mensagemErro ?? "Erro simulado"));
        
        return Task.FromResult(Result.Success(_statusWhatsApp));
    }

    public Task<Result> EnviarMensagemAsync(string numeroWhatsApp, string texto, CancellationToken cancellationToken = default)
    {
        if (_simularErro)
            return Task.FromResult(Result.Error(_mensagemErro ?? "Erro simulado"));

        _mensagensEnviadas.Add(new MensagemEnviada
        {
            NumeroWhatsApp = numeroWhatsApp,
            Texto = texto,
            DataHora = DateTime.UtcNow
        });

        return Task.FromResult(Result.Success());
    }

    public Task<Result> EnviarMensagemParaGrupoAsync(
        string grupoId, 
        string texto, 
        byte[]? arquivo = null, 
        string? mimeType = null, 
        CancellationToken cancellationToken = default)
    {
        if (_simularErro)
            return Task.FromResult(Result.Error(_mensagemErro ?? "Erro simulado"));

        _mensagensGrupo.Add(new MensagemGrupo
        {
            GrupoId = grupoId,
            Texto = texto,
            ArquivoTamanho = arquivo?.Length ?? 0,
            MimeType = mimeType,
            DataHora = DateTime.UtcNow
        });

        return Task.FromResult(Result.Success());
    }

    public Task<Result<string>> GerarQrCodeAsync(CancellationToken cancellationToken = default)
    {
        if (_simularErro)
            return Task.FromResult(Result<string>.Error(_mensagemErro ?? "Erro simulado"));
        
        return Task.FromResult(Result.Success("data:image/png;base64,FAKE_QRCODE"));
    }

    public Task<Result> DesconectarAsync(CancellationToken cancellationToken = default)
    {
        if (_simularErro)
            return Task.FromResult(Result.Error(_mensagemErro ?? "Erro simulado"));
        
        _statusWhatsApp = "close";
        return Task.FromResult(Result.Success());
    }

    public Task<Result> CriarInstanciaAsync(CancellationToken cancellationToken = default)
    {
        if (_simularErro)
            return Task.FromResult(Result.Error(_mensagemErro ?? "Erro simulado"));
        
        return Task.FromResult(Result.Success());
    }

    public Task<Result<byte[]>> BaixarArquivoAsync(string mediaUrl, CancellationToken cancellationToken = default)
    {
        if (_simularErro)
            return Task.FromResult(Result<byte[]>.Error(_mensagemErro ?? "Erro simulado"));
        
        // Retorna uma imagem fake
        return Task.FromResult(Result.Success(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));
    }

    public Task<Result<byte[]>> BaixarMidiaDescriptografadaAsync(object messagePayload, CancellationToken cancellationToken = default)
    {
        if (_simularErro)
            return Task.FromResult(Result<byte[]>.Error(_mensagemErro ?? "Erro simulado"));
        
        // Retorna uma imagem fake
        return Task.FromResult(Result.Success(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));
    }

    public Task<Result<List<GrupoWhatsAppDto>>> ListarGruposAsync(CancellationToken cancellationToken = default)
    {
        if (_simularErro)
            return Task.FromResult(Result<List<GrupoWhatsAppDto>>.Error(_mensagemErro ?? "Erro simulado"));
        
        return Task.FromResult(Result.Success(new List<GrupoWhatsAppDto>
        {
            new("123456789@g.us", "Grupo Teste", "Descrição do grupo", 5)
        }));
    }
}

/// <summary>
/// Registro de mensagem enviada para cliente
/// </summary>
public class MensagemEnviada
{
    public string NumeroWhatsApp { get; init; } = string.Empty;
    public string Texto { get; init; } = string.Empty;
    public DateTime DataHora { get; init; }
}

/// <summary>
/// Registro de mensagem enviada para grupo
/// </summary>
public class MensagemGrupo
{
    public string GrupoId { get; init; } = string.Empty;
    public string Texto { get; init; } = string.Empty;
    public int ArquivoTamanho { get; init; }
    public string? MimeType { get; init; }
    public DateTime DataHora { get; init; }
}

/// <summary>
/// Grupo do WhatsApp (para compatibilidade)
/// </summary>
public class GrupoWhatsApp
{
    public string Id { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
}
