using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BotFatura.Infrastructure.Services;

public class GeminiApiClient : IGeminiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiApiClient> _logger;

    public GeminiApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _apiKey = configuration["GeminiApi:ApiKey"] ?? throw new ArgumentNullException("GeminiApi:ApiKey");
        _model = configuration["GeminiApi:Model"] ?? "gemini-2.5-flash";
        
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
    }

    public async Task<Result<ComprovanteAnalisadoDto>> AnalisarComprovanteAsync(byte[] arquivo, string mimeType, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var arquivoTamanhoKb = arquivo.Length / 1024.0;

        _logger.LogInformation(
            "Iniciando análise de comprovante via Gemini API. Operation={Operation}, MimeType={MimeType}, ArquivoTamanhoKB={ArquivoTamanhoKB:F2}, Model={Model}",
            "AnalisarComprovante",
            mimeType,
            arquivoTamanhoKb,
            _model);

        try
        {
            var base64Content = Convert.ToBase64String(arquivo);
            
            _logger.LogInformation(
                "Imagem convertida para base64. Operation={Operation}, Base64Length={Base64Length}, PrimeirosBytes={PrimeirosBytes}",
                "AnalisarComprovante",
                base64Content.Length,
                BitConverter.ToString(arquivo.Take(10).ToArray()));
            
            var prompt = @"Analise este comprovante de pagamento e retorne APENAS um JSON válido com a seguinte estrutura:
{
  ""isComprovante"": boolean,
  ""valor"": number (apenas números, sem R$ ou símbolos),
  ""data"": ""YYYY-MM-DD"",
  ""tipoPagamento"": ""PIX|Transferencia|Boleto|Cartao"",
  ""confianca"": number (0-100),
  ""observacoes"": string,
  ""dadosPagador"": {
    ""nome"": string ou null,
    ""documento"": string ou null (CPF/CNPJ),
    ""banco"": string ou null,
    ""agencia"": string ou null,
    ""conta"": string ou null
  },
  ""dadosDestinatario"": {
    ""nome"": string ou null,
    ""chavePix"": string ou null (chave PIX completa se for PIX),
    ""documento"": string ou null (CPF/CNPJ),
    ""banco"": string ou null,
    ""agencia"": string ou null,
    ""conta"": string ou null
  },
  ""numeroComprovante"": string ou null (ID da transação, endToEndId, código de autenticação)
}

IMPORTANTE:
- Se não for um comprovante válido, retorne isComprovante: false e os demais campos como null.
- Se for um comprovante, extraia TODOS os dados possíveis do documento.
- O campo chavePix deve conter a chave PIX exata do destinatário (email, telefone, CPF, CNPJ ou chave aleatória).
- O campo numeroComprovante deve conter o código único da transação (endToEndId para PIX, NSU para cartão, etc).
- Para o campo valor, use apenas números (ex: 150.00, não R$ 150,00).";

            var requestBody = new
            {
                contents = new object[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                text = prompt
                            },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Content
                                }
                            }
                        }
                    }
                }
            };

            var url = $"models/{_model}:generateContent?key={_apiKey}";
            
            _logger.LogDebug(
                "Enviando requisição para Gemini API. Operation={Operation}, Url={Url}",
                "AnalisarComprovante",
                $"models/{_model}:generateContent");

            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Erro na chamada Gemini API. Operation={Operation}, Success={Success}, StatusCode={StatusCode}, DurationMs={DurationMs}, Error={Error}",
                    "AnalisarComprovante",
                    false,
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    errorContent);
                return Result.Error($"Erro ao analisar comprovante: {response.StatusCode}");
            }

            var responseData = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
            
            if (responseData?.Candidates == null || !responseData.Candidates.Any())
            {
                _logger.LogWarning(
                    "Resposta inválida do Gemini API (sem candidates). Operation={Operation}, Success={Success}, DurationMs={DurationMs}",
                    "AnalisarComprovante",
                    false,
                    stopwatch.ElapsedMilliseconds);
                return Result.Error("Resposta inválida do Gemini API");
            }

            var textResponse = responseData.Candidates[0].Content?.Parts?[0]?.Text;
            if (string.IsNullOrWhiteSpace(textResponse))
            {
                _logger.LogWarning(
                    "Resposta vazia do Gemini API. Operation={Operation}, Success={Success}, DurationMs={DurationMs}",
                    "AnalisarComprovante",
                    false,
                    stopwatch.ElapsedMilliseconds);
                return Result.Error("Resposta vazia do Gemini API");
            }

            // Extrair JSON da resposta (pode vir com markdown code blocks)
            var jsonText = ExtrairJsonDaResposta(textResponse);
            
            var resultado = JsonSerializer.Deserialize<ComprovanteAnalisadoDto>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (resultado == null)
            {
                _logger.LogWarning(
                    "Não foi possível deserializar resposta do Gemini. Operation={Operation}, Success={Success}, DurationMs={DurationMs}, JsonResponse={JsonResponse}",
                    "AnalisarComprovante",
                    false,
                    stopwatch.ElapsedMilliseconds,
                    jsonText.Length > 500 ? jsonText[..500] + "..." : jsonText);
                return Result.Error("Não foi possível deserializar a resposta do Gemini");
            }

            _logger.LogInformation(
                "Análise de comprovante concluída com sucesso. Operation={Operation}, Success={Success}, DurationMs={DurationMs}, IsComprovante={IsComprovante}, Valor={Valor}, Confianca={Confianca}",
                "AnalisarComprovante",
                true,
                stopwatch.ElapsedMilliseconds,
                resultado.IsComprovante,
                resultado.Valor,
                resultado.Confianca);

            return Result.Success(resultado);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Erro ao analisar comprovante com Gemini. Operation={Operation}, Success={Success}, DurationMs={DurationMs}, MimeType={MimeType}, ArquivoTamanhoKB={ArquivoTamanhoKB:F2}, ErrorMessage={ErrorMessage}",
                "AnalisarComprovante",
                false,
                stopwatch.ElapsedMilliseconds,
                mimeType,
                arquivoTamanhoKb,
                ex.Message);
            return Result.Error($"Erro ao processar comprovante: {ex.Message}");
        }
    }

    private string ExtrairJsonDaResposta(string resposta)
    {
        // Remove markdown code blocks se existirem
        var json = resposta.Trim();
        
        if (json.StartsWith("```json"))
        {
            json = json.Substring(7);
        }
        else if (json.StartsWith("```"))
        {
            json = json.Substring(3);
        }
        
        if (json.EndsWith("```"))
        {
            json = json.Substring(0, json.Length - 3);
        }
        
        return json.Trim();
    }

    private class GeminiResponse
    {
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        public Content? Content { get; set; }
    }

    private class Content
    {
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        public string? Text { get; set; }
    }
}
