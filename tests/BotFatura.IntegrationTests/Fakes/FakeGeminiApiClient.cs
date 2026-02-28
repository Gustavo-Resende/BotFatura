using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.TestUtils.Builders;
using BotFatura.TestUtils.Cenarios;
using System.Security.Cryptography;

namespace BotFatura.IntegrationTests.Fakes;

/// <summary>
/// Implementação fake do IGeminiApiClient para testes de integração.
/// Retorna respostas pré-programadas baseadas em metadados da imagem.
/// </summary>
public class FakeGeminiApiClient : IGeminiApiClient
{
    private readonly Dictionary<string, ComprovanteAnalisadoDto> _respostasProgramadas = new();
    private readonly List<ChamadaGemini> _historicosChamadas = new();
    private ComprovanteAnalisadoDto? _respostaPadrao;
    private bool _simularErro;
    private string? _mensagemErro;
    private int _latenciaMs;
    private double _taxaFalha;
    private readonly Random _random = new();

    /// <summary>
    /// Histórico de chamadas realizadas ao fake client
    /// </summary>
    public IReadOnlyList<ChamadaGemini> HistoricoChamadas => _historicosChamadas.AsReadOnly();

    /// <summary>
    /// Quantidade de chamadas realizadas
    /// </summary>
    public int QuantidadeChamadas => _historicosChamadas.Count;

    /// <summary>
    /// Configura uma resposta padrão para qualquer imagem
    /// </summary>
    public FakeGeminiApiClient ComRespostaPadrao(ComprovanteAnalisadoDto resposta)
    {
        _respostaPadrao = resposta;
        return this;
    }

    /// <summary>
    /// Configura uma resposta específica para uma imagem (baseado no hash)
    /// </summary>
    public FakeGeminiApiClient ComRespostaParaImagem(byte[] imagem, ComprovanteAnalisadoDto resposta)
    {
        var hash = CalcularHash(imagem);
        _respostasProgramadas[hash] = resposta;
        return this;
    }

    /// <summary>
    /// Configura o fake para simular um erro na API
    /// </summary>
    public FakeGeminiApiClient SimularErro(string mensagem = "Erro simulado na API Gemini")
    {
        _simularErro = true;
        _mensagemErro = mensagem;
        return this;
    }

    /// <summary>
    /// Configura uma latência artificial para simular tempo de resposta da API
    /// </summary>
    public FakeGeminiApiClient ComLatencia(int milliseconds)
    {
        _latenciaMs = milliseconds;
        return this;
    }

    /// <summary>
    /// Configura uma taxa de falha aleatória (0.0 a 1.0)
    /// </summary>
    public FakeGeminiApiClient ComTaxaFalha(double taxa)
    {
        _taxaFalha = Math.Clamp(taxa, 0.0, 1.0);
        return this;
    }

    /// <summary>
    /// Reseta o estado do fake client
    /// </summary>
    public void Reset()
    {
        _respostasProgramadas.Clear();
        _historicosChamadas.Clear();
        _respostaPadrao = null;
        _simularErro = false;
        _mensagemErro = null;
        _latenciaMs = 0;
        _taxaFalha = 0;
    }

    public async Task<Result<ComprovanteAnalisadoDto>> AnalisarComprovanteAsync(
        byte[] arquivo, 
        string mimeType, 
        CancellationToken cancellationToken = default)
    {
        // Registrar chamada
        var chamada = new ChamadaGemini
        {
            ArquivoTamanho = arquivo.Length,
            MimeType = mimeType,
            DataHora = DateTime.UtcNow,
            Hash = CalcularHash(arquivo)
        };
        _historicosChamadas.Add(chamada);

        // Simular latência
        if (_latenciaMs > 0)
        {
            await Task.Delay(_latenciaMs, cancellationToken);
        }

        // Simular erro configurado
        if (_simularErro)
        {
            return Result.Error(_mensagemErro ?? "Erro simulado");
        }

        // Simular falha aleatória
        if (_taxaFalha > 0 && _random.NextDouble() < _taxaFalha)
        {
            return Result.Error("Falha aleatória simulada na API Gemini");
        }

        // Buscar resposta programada para a imagem específica
        var hash = CalcularHash(arquivo);
        if (_respostasProgramadas.TryGetValue(hash, out var respostaProgramada))
        {
            return Result.Success(respostaProgramada);
        }

        // Usar resposta padrão se configurada
        if (_respostaPadrao != null)
        {
            return Result.Success(_respostaPadrao);
        }

        // Resposta inteligente baseada no tamanho da imagem
        return Result.Success(GerarRespostaInteligente(arquivo, mimeType));
    }

    /// <summary>
    /// Gera uma resposta "inteligente" baseada em características da imagem.
    /// Para testes, interpreta certas características como indicadores do tipo de comprovante.
    /// </summary>
    private ComprovanteAnalisadoDto GerarRespostaInteligente(byte[] arquivo, string mimeType)
    {
        var tamanho = arquivo.Length;

        // Imagens muito pequenas (< 1KB) - provavelmente não são comprovantes
        if (tamanho < 1024)
        {
            return CenariosComprovantePredefinidos.ImagemNaoComprovante();
        }

        // Imagens grandes (> 100KB) - provavelmente são comprovantes
        if (tamanho > 100 * 1024)
        {
            // Usar o tamanho para gerar um valor "aleatório mas determinístico"
            var valorBase = (tamanho % 1000) + 100; // Valor entre 100 e 1099
            return new ComprovanteAnalisadoDtoBuilder()
                .ComoComprovantePix(valorBase)
                .ComDadosDestinatario(nome: "Empresa BotFatura", chavePix: "pix@botfatura.com.br")
                .Build();
        }

        // Imagens médias - comprovante padrão
        return CenariosComprovantePredefinidos.ComprovantePixValido(150.00m);
    }

    private static string CalcularHash(byte[] dados)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(dados);
        return Convert.ToHexString(hashBytes);
    }
}

/// <summary>
/// Registro de uma chamada ao fake Gemini client
/// </summary>
public class ChamadaGemini
{
    public int ArquivoTamanho { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public DateTime DataHora { get; init; }
    public string Hash { get; init; } = string.Empty;
}
