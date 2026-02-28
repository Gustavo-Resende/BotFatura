using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Ardalis.Result;
using BotFatura.Application.Common.Helpers;
using BotFatura.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BotFatura.Infrastructure.Services;

public class EvolutionApiClient : IEvolutionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<EvolutionApiClient> _logger;
    private readonly string _instanceName;
    private readonly string _apiKey;
    private const string CACHE_KEY_STATUS = "evolution_rate_limit_status";
    private const string CACHE_KEY_GRUPOS = "evolution_rate_limit_grupos";

    public EvolutionApiClient(HttpClient httpClient, IConfiguration configuration, IMemoryCache memoryCache, ILogger<EvolutionApiClient> logger)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        _logger = logger;
        
        var baseUrl = configuration["EvolutionApi:BaseUrl"] ?? throw new ArgumentNullException("EvolutionApi:BaseUrl");
        _instanceName = configuration["EvolutionApi:InstanceName"] ?? throw new ArgumentNullException("EvolutionApi:InstanceName");
        _apiKey = configuration["EvolutionApi:ApiKey"] ?? throw new ArgumentNullException("EvolutionApi:ApiKey");

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
    }

    private async Task AguardarRateLimit(string tipo, int delayMinimoMs = 1000)
    {
        var cacheKey = tipo switch
        {
            "status" => CACHE_KEY_STATUS,
            "grupos" => CACHE_KEY_GRUPOS,
            _ => CACHE_KEY_STATUS
        };

        if (_memoryCache.TryGetValue(cacheKey, out DateTime ultimaRequisicao))
        {
            var tempoDecorrido = (DateTime.UtcNow - ultimaRequisicao).TotalMilliseconds;
            if (tempoDecorrido < delayMinimoMs)
            {
                var delayRestante = (int)(delayMinimoMs - tempoDecorrido);
                await Task.Delay(delayRestante);
            }
        }

        // Armazenar timestamp da última requisição com expiração de 1 minuto
        _memoryCache.Set(cacheKey, DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    public async Task<Result> EnviarMensagemAsync(string numeroWhatsApp, string texto, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var mensagemTamanho = texto?.Length ?? 0;

        _logger.LogInformation(
            "Iniciando envio de mensagem via Evolution API. Operation={Operation}, NumeroDestino={NumeroDestino}, MensagemTamanho={MensagemTamanho}",
            "EnviarMensagem",
            TelefoneHelper.MascararNumero(numeroWhatsApp),
            mensagemTamanho);

        // Validar status antes de enviar mensagem
        var statusResult = await ObterStatusAsync(cancellationToken);
        if (!statusResult.IsSuccess || statusResult.Value != "open")
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Envio de mensagem falhou: WhatsApp desconectado. Operation={Operation}, Success={Success}, DurationMs={DurationMs}",
                "EnviarMensagem",
                false,
                stopwatch.ElapsedMilliseconds);
            return Result.Error("WhatsApp não está conectado. Conecte o WhatsApp primeiro.");
        }

        // Rate limiting: mínimo 2 segundos entre envios de mensagem
        await AguardarRateLimit("status", 2000);

        // Se o destino já for um JID completo (contém @), usar diretamente
        // Caso contrário, a Evolution API vai formatar automaticamente
        var destinoFinal = numeroWhatsApp;
        
        var payload = new 
        {
            number = destinoFinal,
            text = texto
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/message/sendText/{_instanceName}", payload, cancellationToken);
            stopwatch.Stop();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Mensagem enviada com sucesso. Operation={Operation}, Success={Success}, NumeroDestino={NumeroDestino}, DurationMs={DurationMs}",
                    "EnviarMensagem",
                    true,
                    TelefoneHelper.MascararNumero(numeroWhatsApp),
                    stopwatch.ElapsedMilliseconds);
                return Result.Success();
            }

            var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Falha ao enviar mensagem. Operation={Operation}, Success={Success}, StatusCode={StatusCode}, DurationMs={DurationMs}, Error={Error}",
                "EnviarMensagem",
                false,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                errorResponse);
            return Result.Error($"Falha na api do Whatsapp: {response.StatusCode} - {errorResponse}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Erro ao enviar mensagem. Operation={Operation}, Success={Success}, DurationMs={DurationMs}, ErrorMessage={ErrorMessage}",
                "EnviarMensagem",
                false,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
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

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            List<GrupoData>? gruposRaw = null;
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // A Evolution API pode retornar um array direto ou um wrapper com propriedade "groups"
            try
            {
                gruposRaw = JsonSerializer.Deserialize<List<GrupoData>>(json, jsonOptions);
            }
            catch
            {
                try
                {
                    var dataWrapper = JsonSerializer.Deserialize<GruposResponse>(json, jsonOptions);
                    gruposRaw = dataWrapper?.Groups;
                }
                catch (Exception ex)
                {
                    return Result.Error($"Erro ao listar grupos: resposta inesperada da Evolution API. Detalhes: {ex.Message}");
                }
            }

            if (gruposRaw == null)
            {
                return Result.Success(new List<GrupoWhatsAppDto>());
            }

            var grupos = gruposRaw.Select(g => new GrupoWhatsAppDto(
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
        var stopwatch = Stopwatch.StartNew();
        var anexoTamanhoKb = anexo != null ? anexo.Length / 1024.0 : 0;
        var mensagemTamanho = texto?.Length ?? 0;

        _logger.LogInformation(
            "Iniciando envio para grupo via Evolution API. Operation={Operation}, GrupoId={GrupoId}, MensagemTamanho={MensagemTamanho}, TemAnexo={TemAnexo}, AnexoTamanhoKB={AnexoTamanhoKB:F2}, MimeType={MimeType}",
            "EnviarMensagemParaGrupo",
            grupoId,
            mensagemTamanho,
            anexo != null,
            anexoTamanhoKb,
            mimeType ?? "(sem anexo)");

        try
        {
            // Validar status antes de enviar para grupo
            var statusResult = await ObterStatusAsync(cancellationToken);
            if (!statusResult.IsSuccess || statusResult.Value != "open")
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "Envio para grupo falhou: WhatsApp desconectado. Operation={Operation}, Success={Success}, GrupoId={GrupoId}, DurationMs={DurationMs}",
                    "EnviarMensagemParaGrupo",
                    false,
                    grupoId,
                    stopwatch.ElapsedMilliseconds);
                return Result.Error("WhatsApp não está conectado. Conecte o WhatsApp primeiro.");
            }

            // Rate limiting: mínimo 3 segundos entre envios para grupos
            await AguardarRateLimit("status", 3000);

            if (anexo != null && !string.IsNullOrWhiteSpace(mimeType))
            {
                // Enviar mensagem com anexo para grupo
                var base64Content = Convert.ToBase64String(anexo);
                var mediaType = mimeType.StartsWith("image/") ? "image" : 
                               mimeType.StartsWith("video/") ? "video" : 
                               mimeType.StartsWith("audio/") ? "audio" : "document";
                
                // Evolution API v2 espera mediatype no nível raiz
                // Para imagens, não enviar fileName (só para documentos)
                object payload;
                if (mediaType == "document")
                {
                    payload = new
                    {
                        number = grupoId,
                        mediatype = mediaType,
                        mimetype = mimeType,
                        media = base64Content, // Base64 puro, sem prefixo data:
                        caption = texto,
                        fileName = "comprovante.pdf"
                    };
                }
                else
                {
                    payload = new
                    {
                        number = grupoId,
                        mediatype = mediaType,
                        mimetype = mimeType,
                        media = base64Content, // Base64 puro, sem prefixo data:
                        caption = texto
                    };
                }

                var response = await _httpClient.PostAsJsonAsync($"/message/sendMedia/{_instanceName}", payload, cancellationToken);
                stopwatch.Stop();
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Mídia enviada para grupo com sucesso. Operation={Operation}, Success={Success}, GrupoId={GrupoId}, DurationMs={DurationMs}, AnexoTamanhoKB={AnexoTamanhoKB:F2}",
                        "EnviarMensagemParaGrupo",
                        true,
                        grupoId,
                        stopwatch.ElapsedMilliseconds,
                        anexoTamanhoKb);
                    return Result.Success();
                }

                var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Falha ao enviar mídia para grupo. Operation={Operation}, Success={Success}, GrupoId={GrupoId}, StatusCode={StatusCode}, DurationMs={DurationMs}, Error={Error}",
                    "EnviarMensagemParaGrupo",
                    false,
                    grupoId,
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    errorResponse);
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
                stopwatch.Stop();
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Mensagem enviada para grupo com sucesso. Operation={Operation}, Success={Success}, GrupoId={GrupoId}, DurationMs={DurationMs}",
                        "EnviarMensagemParaGrupo",
                        true,
                        grupoId,
                        stopwatch.ElapsedMilliseconds);
                    return Result.Success();
                }

                var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Falha ao enviar mensagem para grupo. Operation={Operation}, Success={Success}, GrupoId={GrupoId}, StatusCode={StatusCode}, DurationMs={DurationMs}, Error={Error}",
                    "EnviarMensagemParaGrupo",
                    false,
                    grupoId,
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    errorResponse);
                return Result.Error($"Falha ao enviar mensagem para grupo: {response.StatusCode} - {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Erro ao enviar mensagem para grupo. Operation={Operation}, Success={Success}, GrupoId={GrupoId}, DurationMs={DurationMs}, ErrorMessage={ErrorMessage}",
                "EnviarMensagemParaGrupo",
                false,
                grupoId,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            return Result.Error($"Erro ao enviar mensagem para grupo: {ex.Message}");
        }
    }

    public async Task<Result<byte[]>> BaixarArquivoAsync(string mediaUrl, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var urlMascarada = mediaUrl.Length > 50 ? mediaUrl[..50] + "..." : mediaUrl;

        _logger.LogInformation(
            "Iniciando download de arquivo via Evolution API. Operation={Operation}, MediaUrl={MediaUrl}",
            "BaixarArquivo",
            urlMascarada);

        try
        {
            // Se a URL já for completa, usar diretamente
            if (mediaUrl.StartsWith("http://") || mediaUrl.StartsWith("https://"))
            {
                var response = await _httpClient.GetAsync(mediaUrl, cancellationToken);
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    _logger.LogInformation(
                        "Arquivo baixado com sucesso (URL direta). Operation={Operation}, Success={Success}, DurationMs={DurationMs}, ArquivoTamanhoKB={ArquivoTamanhoKB:F2}",
                        "BaixarArquivo",
                        true,
                        stopwatch.ElapsedMilliseconds,
                        bytes.Length / 1024.0);
                    return Result.Success(bytes);
                }

                _logger.LogError(
                    "Falha ao baixar arquivo (URL direta). Operation={Operation}, Success={Success}, StatusCode={StatusCode}, DurationMs={DurationMs}",
                    "BaixarArquivo",
                    false,
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
                return Result.Error($"Erro ao baixar arquivo: {response.StatusCode}");
            }

            // Se for apenas o ID da mídia, usar endpoint da Evolution API
            var downloadResponse = await _httpClient.GetAsync($"/chat/fetchMedia/{_instanceName}?messageId={mediaUrl}", cancellationToken);
            stopwatch.Stop();
            
            if (downloadResponse.IsSuccessStatusCode)
            {
                var bytes = await downloadResponse.Content.ReadAsByteArrayAsync(cancellationToken);
                _logger.LogInformation(
                    "Arquivo baixado com sucesso (Evolution API). Operation={Operation}, Success={Success}, DurationMs={DurationMs}, ArquivoTamanhoKB={ArquivoTamanhoKB:F2}",
                    "BaixarArquivo",
                    true,
                    stopwatch.ElapsedMilliseconds,
                    bytes.Length / 1024.0);
                return Result.Success(bytes);
            }

            _logger.LogError(
                "Falha ao baixar arquivo (Evolution API). Operation={Operation}, Success={Success}, StatusCode={StatusCode}, DurationMs={DurationMs}",
                "BaixarArquivo",
                false,
                (int)downloadResponse.StatusCode,
                stopwatch.ElapsedMilliseconds);
            return Result.Error($"Erro ao baixar arquivo da Evolution API: {downloadResponse.StatusCode}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Erro ao baixar arquivo. Operation={Operation}, Success={Success}, DurationMs={DurationMs}, ErrorMessage={ErrorMessage}",
                "BaixarArquivo",
                false,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            return Result.Error($"Erro ao baixar arquivo: {ex.Message}");
        }
    }

    /// <summary>
    /// Baixa mídia usando o endpoint da Evolution API que descriptografa automaticamente
    /// </summary>
    public async Task<Result<byte[]>> BaixarMidiaDescriptografadaAsync(object messagePayload, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Iniciando download de mídia via Evolution API (getBase64FromMediaMessage). Operation={Operation}",
            "BaixarMidiaDescriptografada");

        try
        {
            var requestBody = new
            {
                message = messagePayload,
                convertToMp4 = false
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/chat/getBase64FromMediaMessage/{_instanceName}", 
                requestBody, 
                cancellationToken);
            
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                
                _logger.LogInformation(
                    "Resposta da Evolution API recebida. Operation={Operation}, ResponseLength={ResponseLength}",
                    "BaixarMidiaDescriptografada",
                    jsonResponse.Length);

                // A resposta pode ser um JSON com base64 ou o base64 diretamente
                string? base64Content = null;
                
                try
                {
                    using var doc = JsonDocument.Parse(jsonResponse);
                    if (doc.RootElement.TryGetProperty("base64", out var base64Prop))
                    {
                        base64Content = base64Prop.GetString();
                    }
                }
                catch
                {
                    // Se não for JSON, assume que é o base64 diretamente
                    base64Content = jsonResponse.Trim('"');
                }

                if (!string.IsNullOrEmpty(base64Content))
                {
                    // Remover prefixo data:image/...;base64, se existir
                    if (base64Content.Contains(","))
                    {
                        base64Content = base64Content.Split(',')[1];
                    }

                    var bytes = Convert.FromBase64String(base64Content);
                    
                    _logger.LogInformation(
                        "Mídia descriptografada com sucesso. Operation={Operation}, Success={Success}, DurationMs={DurationMs}, ArquivoTamanhoKB={ArquivoTamanhoKB:F2}, PrimeirosBytes={PrimeirosBytes}",
                        "BaixarMidiaDescriptografada",
                        true,
                        stopwatch.ElapsedMilliseconds,
                        bytes.Length / 1024.0,
                        BitConverter.ToString(bytes.Take(10).ToArray()));
                    
                    return Result.Success(bytes);
                }

                _logger.LogError(
                    "Resposta da Evolution API não contém base64 válido. Operation={Operation}, Response={Response}",
                    "BaixarMidiaDescriptografada",
                    jsonResponse.Length > 200 ? jsonResponse[..200] + "..." : jsonResponse);
                return Result.Error("Resposta da Evolution API não contém mídia válida");
            }

            var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Falha ao baixar mídia (Evolution API). Operation={Operation}, Success={Success}, StatusCode={StatusCode}, DurationMs={DurationMs}, Error={Error}",
                "BaixarMidiaDescriptografada",
                false,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                errorResponse.Length > 300 ? errorResponse[..300] : errorResponse);
            return Result.Error($"Erro ao baixar mídia da Evolution API: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Erro ao baixar mídia. Operation={Operation}, Success={Success}, DurationMs={DurationMs}, ErrorMessage={ErrorMessage}",
                "BaixarMidiaDescriptografada",
                false,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            return Result.Error($"Erro ao baixar mídia: {ex.Message}");
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
