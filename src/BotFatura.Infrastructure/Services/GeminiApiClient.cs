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
        try
        {
            var base64Content = Convert.ToBase64String(arquivo);
            
            var prompt = @"Analise este comprovante de pagamento e retorne APENAS um JSON válido com a seguinte estrutura:
{
  ""isComprovante"": boolean,
  ""valor"": number (apenas números, sem R$ ou símbolos),
  ""data"": ""YYYY-MM-DD"",
  ""tipoPagamento"": ""PIX|Transferencia|Boleto|Cartao"",
  ""confianca"": number (0-100),
  ""observacoes"": string
}

Se não for um comprovante válido, retorne isComprovante: false.
Se for um comprovante, extraia o valor e a data do pagamento.";

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
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erro ao chamar Gemini API: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return Result.Error($"Erro ao analisar comprovante: {response.StatusCode}");
            }

            var responseData = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
            
            if (responseData?.Candidates == null || !responseData.Candidates.Any())
            {
                return Result.Error("Resposta inválida do Gemini API");
            }

            var textResponse = responseData.Candidates[0].Content?.Parts?[0]?.Text;
            if (string.IsNullOrWhiteSpace(textResponse))
            {
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
                return Result.Error("Não foi possível deserializar a resposta do Gemini");
            }

            return Result.Success(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar comprovante com Gemini");
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
