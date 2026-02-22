using System.Net.Http.Json;
using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BotFatura.Infrastructure.Services;

public class EvolutionApiClient : IEvolutionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _instanceName;
    private readonly string _apiKey;

    public EvolutionApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        
        var baseUrl = configuration["EvolutionApi:BaseUrl"] ?? throw new ArgumentNullException("EvolutionApi:BaseUrl");
        _instanceName = configuration["EvolutionApi:InstanceName"] ?? throw new ArgumentNullException("EvolutionApi:InstanceName");
        _apiKey = configuration["EvolutionApi:ApiKey"] ?? throw new ArgumentNullException("EvolutionApi:ApiKey");

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
    }

    public async Task<Result> EnviarMensagemAsync(string numeroWhatsApp, string texto, CancellationToken cancellationToken = default)
    {
        // O payload padrão do webhook POST /message/sendText/{instance}
        var payload = new 
        {
            number = numeroWhatsApp,
            text = texto
        };

        var response = await _httpClient.PostAsJsonAsync($"/message/sendText/{_instanceName}", payload, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Error($"Falha na api do Whatsapp: {response.StatusCode} - {errorResponse}");
    }
}
