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

    public async Task<Result<string>> ObterStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/instance/connectionState/{_instanceName}", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result.Success("INSTANCE_NOT_FOUND");

            if (!response.IsSuccessStatusCode)
                return Result.Error("Ocorreu um erro ao consultar o status na Evolution API.");

            var data = await response.Content.ReadFromJsonAsync<InstanceStatusResponse>(cancellationToken);
            return Result.Success(data?.Instance?.State ?? "UNKNOWN");
        }
        catch (Exception ex)
        {
            return Result.Error($"Erro de comunicação: {ex.Message}");
        }
    }

    public async Task<Result<string>> GerarQrCodeAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/instance/connect/{_instanceName}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
            return Result.Error("Não foi possível gerar o QR Code. Verifique se a instância está ativa.");

        var data = await response.Content.ReadFromJsonAsync<QrCodeResponse>(cancellationToken);
        return Result.Success(data?.Base64 ?? string.Empty);
    }

    public async Task<Result> CriarInstanciaAsync(CancellationToken cancellationToken = default)
    {
        var payload = new 
        {
            instanceName = _instanceName,
            token = _apiKey,
            qrcode = true,
            integration = "WHATSAPP-BAILEYS"
        };

        var response = await _httpClient.PostAsJsonAsync("/instance/create", payload, cancellationToken);
        
        if (response.IsSuccessStatusCode) return Result.Success();

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result.Error($"Erro ao criar instância: {error}");
    }

    public async Task<Result> DesconectarAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/instance/logout/{_instanceName}", cancellationToken);
        return response.IsSuccessStatusCode ? Result.Success() : Result.Error("Erro ao desconectar instância.");
    }

    // Classes auxiliares para desserialização
    private class InstanceStatusResponse
    {
        public InstanceData? Instance { get; set; }
    }

    private class InstanceData
    {
        public string? State { get; set; }
    }

    private class QrCodeResponse
    {
        public string? Base64 { get; set; }
    }
}
