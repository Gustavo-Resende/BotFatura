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
    private static DateTime? _ultimaRequisicaoStatus;
    private static DateTime? _ultimaRequisicaoGrupos;
    private static readonly object _lockStatus = new object();
    private static readonly object _lockGrupos = new object();

    public EvolutionApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        
        var baseUrl = configuration["EvolutionApi:BaseUrl"] ?? throw new ArgumentNullException("EvolutionApi:BaseUrl");
        _instanceName = configuration["EvolutionApi:InstanceName"] ?? throw new ArgumentNullException("EvolutionApi:InstanceName");
        _apiKey = configuration["EvolutionApi:ApiKey"] ?? throw new ArgumentNullException("EvolutionApi:ApiKey");

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
    }

    private async Task AguardarRateLimit(string tipo, int delayMinimoMs = 1000)
    {
        DateTime? ultimaRequisicao = tipo switch
        {
            "status" => _ultimaRequisicaoStatus,
            "grupos" => _ultimaRequisicaoGrupos,
            _ => null
        };

        if (ultimaRequisicao.HasValue)
        {
            var tempoDecorrido = (DateTime.UtcNow - ultimaRequisicao.Value).TotalMilliseconds;
            if (tempoDecorrido < delayMinimoMs)
            {
                var delayRestante = (int)(delayMinimoMs - tempoDecorrido);
                await Task.Delay(delayRestante);
            }
        }

        var lockObject = tipo switch
        {
            "status" => _lockStatus,
            "grupos" => _lockGrupos,
            _ => _lockStatus
        };

        lock (lockObject)
        {
            if (tipo == "status")
                _ultimaRequisicaoStatus = DateTime.UtcNow;
            else if (tipo == "grupos")
                _ultimaRequisicaoGrupos = DateTime.UtcNow;
        }
    }

    public async Task<Result> EnviarMensagemAsync(string numeroWhatsApp, string texto, CancellationToken cancellationToken = default)
    {
        // Validar status antes de enviar mensagem
        var statusResult = await ObterStatusAsync(cancellationToken);
        if (!statusResult.IsSuccess || statusResult.Value != "open")
        {
            return Result.Error("WhatsApp não está conectado. Conecte o WhatsApp primeiro.");
        }

        // Rate limiting: mínimo 2 segundos entre envios de mensagem
        await AguardarRateLimit("status", 2000);

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
            // Rate limiting: mínimo 2 segundos entre requisições de status
            await AguardarRateLimit("status", 2000);
            
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
        // Rate limiting: mínimo 5 segundos entre gerações de QR Code
        await AguardarRateLimit("status", 5000);
        
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

    public async Task<Result<List<GrupoWhatsAppDto>>> ListarGruposAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Validar status antes de listar grupos para evitar requisições desnecessárias
            var statusResult = await ObterStatusAsync(cancellationToken);
            if (!statusResult.IsSuccess || statusResult.Value != "open")
            {
                return Result.Error("WhatsApp não está conectado. Conecte o WhatsApp primeiro.");
            }

            // Rate limiting rigoroso: mínimo 10 segundos entre listagens de grupos
            await AguardarRateLimit("grupos", 10000);

            // Aguardar delay adicional para garantir que a conexão está estável
            await Task.Delay(1000, cancellationToken);

            var response = await _httpClient.GetAsync($"/group/fetchAllGroups/{_instanceName}?getParticipants=false", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Error($"Erro ao listar grupos: {response.StatusCode} - {error}");
            }

            var data = await response.Content.ReadFromJsonAsync<GruposResponse>(cancellationToken: cancellationToken);
            
            if (data?.Groups == null)
            {
                return Result.Success(new List<GrupoWhatsAppDto>());
            }

            var grupos = data.Groups.Select(g => new GrupoWhatsAppDto(
                Id: g.Id ?? "",
                Nome: g.Subject ?? "Sem nome",
                Descricao: g.Description,
                Participantes: g.Participants?.Count ?? 0
            )).ToList();

            return Result.Success(grupos);
        }
        catch (Exception ex)
        {
            return Result.Error($"Erro ao listar grupos: {ex.Message}");
        }
    }

    private class GruposResponse
    {
        public List<GrupoData>? Groups { get; set; }
    }

    private class GrupoData
    {
        public string? Id { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public List<object>? Participants { get; set; }
    }

    public async Task<Result> EnviarMensagemParaGrupoAsync(string grupoId, string texto, byte[]? anexo = null, string? mimeType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validar status antes de enviar para grupo
            var statusResult = await ObterStatusAsync(cancellationToken);
            if (!statusResult.IsSuccess || statusResult.Value != "open")
            {
                return Result.Error("WhatsApp não está conectado. Conecte o WhatsApp primeiro.");
            }

            // Rate limiting: mínimo 3 segundos entre envios para grupos
            await AguardarRateLimit("status", 3000);

            if (anexo != null && !string.IsNullOrWhiteSpace(mimeType))
            {
                // Enviar mensagem com anexo para grupo
                var base64Content = Convert.ToBase64String(anexo);
                
                var payload = new
                {
                    number = grupoId,
                    mediaMessage = new
                    {
                        mediatype = mimeType.StartsWith("image/") ? "image" : "document",
                        media = base64Content,
                        caption = texto
                    }
                };

                var response = await _httpClient.PostAsJsonAsync($"/message/sendMedia/{_instanceName}", payload, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    return Result.Success();
                }

                var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Error($"Falha ao enviar mídia para grupo: {response.StatusCode} - {errorResponse}");
            }
            else
            {
                // Enviar apenas texto para grupo
                var payload = new
                {
                    number = grupoId,
                    text = texto
                };

                var response = await _httpClient.PostAsJsonAsync($"/message/sendText/{_instanceName}", payload, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    return Result.Success();
                }

                var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Error($"Falha ao enviar mensagem para grupo: {response.StatusCode} - {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            return Result.Error($"Erro ao enviar mensagem para grupo: {ex.Message}");
        }
    }

    public async Task<Result<byte[]>> BaixarArquivoAsync(string mediaUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            // Se a URL já for completa, usar diretamente
            if (mediaUrl.StartsWith("http://") || mediaUrl.StartsWith("https://"))
            {
                var response = await _httpClient.GetAsync(mediaUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    return Result.Success(bytes);
                }
                return Result.Error($"Erro ao baixar arquivo: {response.StatusCode}");
            }

            // Se for apenas o ID da mídia, usar endpoint da Evolution API
            var downloadResponse = await _httpClient.GetAsync($"/chat/fetchMedia/{_instanceName}?messageId={mediaUrl}", cancellationToken);
            
            if (downloadResponse.IsSuccessStatusCode)
            {
                var bytes = await downloadResponse.Content.ReadAsByteArrayAsync(cancellationToken);
                return Result.Success(bytes);
            }

            return Result.Error($"Erro ao baixar arquivo da Evolution API: {downloadResponse.StatusCode}");
        }
        catch (Exception ex)
        {
            return Result.Error($"Erro ao baixar arquivo: {ex.Message}");
        }
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
