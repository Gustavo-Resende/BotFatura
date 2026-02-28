using System.Diagnostics;
using System.Text.Json;
using BotFatura.Application.Common.Helpers;
using BotFatura.Application.Comprovantes.Commands.ProcessarComprovante;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Interfaces;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace BotFatura.Api.Endpoints;

public class WhatsAppWebhookEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Endpoint público para receber webhooks do Evolution API
        var webhookGroup = app.MapGroup("/webhook/whatsapp").WithTags("WhatsApp Webhook");

        webhookGroup.MapPost("/", async (
            HttpRequest request, 
            ISender sender, 
            IClienteRepository clienteRepository, 
            IEvolutionApiClient evolutionApiClient, 
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<WhatsAppWebhookEndpoints> logger) =>
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString("N")[..8];

            logger.LogInformation(
                "Webhook recebido. Operation={Operation}, CorrelationId={CorrelationId}",
                "WebhookRecebido",
                correlationId);

            try
            {
                // Validar webhook secret se configurado
                var webhookSecret = configuration["EvolutionApi:WebhookSecret"];
                if (!string.IsNullOrWhiteSpace(webhookSecret) && webhookSecret != "YOUR_WEBHOOK_SECRET")
                {
                    if (!ValidarWebhookSecret(request, webhookSecret))
                    {
                        stopwatch.Stop();
                        logger.LogWarning(
                            "Webhook secret inválido. Operation={Operation}, CorrelationId={CorrelationId}, Success={Success}, DurationMs={DurationMs}",
                            "WebhookRecebido",
                            correlationId,
                            false,
                            stopwatch.ElapsedMilliseconds);
                        return Results.Unauthorized();
                    }
                }

                var body = await new StreamReader(request.Body).ReadToEndAsync();
                
                // Log do payload resumido (sem dados sensíveis)
                var payloadResumo = body.Length > 500 ? body[..500] + "..." : body;
                logger.LogDebug(
                    "Payload recebido. Operation={Operation}, CorrelationId={CorrelationId}, PayloadTamanho={PayloadTamanho}",
                    "WebhookRecebido",
                    correlationId,
                    body.Length);

                // Parsear JSON bruto para extrair a mensagem original (necessário para download de mídia)
                JsonElement? mensagemOriginalJson = null;
                try
                {
                    using var jsonDoc = JsonDocument.Parse(body);
                    if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        mensagemOriginalJson = dataElement.Clone();
                    }
                }
                catch { /* Ignora erros de parse do JSON bruto */ }

                var webhookData = JsonSerializer.Deserialize<EvolutionWebhookDto>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // A Evolution API pode enviar um único objeto ou um array de eventos
                List<EvolutionWebhookDto> eventos;

                if (webhookData != null)
                {
                    eventos = new List<EvolutionWebhookDto> { webhookData };
                }
                else
                {
                    var lista = JsonSerializer.Deserialize<List<EvolutionWebhookDto>>(body, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (lista == null || lista.Count == 0)
                    {
                        stopwatch.Stop();
                        logger.LogInformation(
                            "Webhook sem eventos para processar. Operation={Operation}, CorrelationId={CorrelationId}, DurationMs={DurationMs}",
                            "WebhookRecebido",
                            correlationId,
                            stopwatch.ElapsedMilliseconds);
                        return Results.Ok(new { message = "Webhook sem eventos" });
                    }

                    eventos = lista;
                }

                var eventoMensagem = eventos.FirstOrDefault(e => string.Equals(e.Event, "messages.upsert", StringComparison.OrdinalIgnoreCase));
                if (eventoMensagem == null)
                {
                    stopwatch.Stop();
                    logger.LogInformation(
                        "Nenhum evento messages.upsert encontrado. Operation={Operation}, CorrelationId={CorrelationId}, EventosRecebidos={EventosRecebidos}, DurationMs={DurationMs}",
                        "WebhookRecebido",
                        correlationId,
                        string.Join(", ", eventos.Select(e => e.Event)),
                        stopwatch.ElapsedMilliseconds);
                    return Results.Ok(new { message = "Evento ignorado" });
                }

                var message = eventoMensagem.Data?.Message;
                if (message == null)
                {
                    stopwatch.Stop();
                    logger.LogInformation(
                        "Mensagem vazia no evento messages.upsert. Operation={Operation}, CorrelationId={CorrelationId}, DurationMs={DurationMs}",
                        "WebhookRecebido",
                        correlationId,
                        stopwatch.ElapsedMilliseconds);
                    return Results.Ok(new { message = "Mensagem vazia" });
                }

                // Extrair JID completo do remetente (identificador único do Evolution API)
                var jidCompleto = eventoMensagem.Data?.Key?.RemoteJid;

                if (string.IsNullOrWhiteSpace(jidCompleto))
                {
                    stopwatch.Stop();
                    logger.LogWarning(
                        "Nenhum JID informado no webhook. Operation={Operation}, CorrelationId={CorrelationId}, DurationMs={DurationMs}",
                        "WebhookRecebido",
                        correlationId,
                        stopwatch.ElapsedMilliseconds);
                    return Results.Ok(new { message = "Remetente não identificado" });
                }

                // Extrair apenas a parte numérica do JID para logs
                var numeroBase = jidCompleto.Split('@')[0];
                
                if (jidCompleto.Contains("@g.us"))
                {
                    // Mensagem de grupo, ignorar
                    stopwatch.Stop();
                    logger.LogInformation(
                        "Mensagem de grupo ignorada. Operation={Operation}, CorrelationId={CorrelationId}, GrupoId={GrupoId}, DurationMs={DurationMs}",
                        "WebhookRecebido",
                        correlationId,
                        numeroBase,
                        stopwatch.ElapsedMilliseconds);
                    return Results.Ok(new { message = "Mensagem de grupo ignorada" });
                }

                // Buscar cliente pelo JID do WhatsApp (usa o JID completo para busca mais precisa)
                logger.LogInformation(
                    "Buscando cliente pelo JID. Operation={Operation}, CorrelationId={CorrelationId}, JidMascarado={JidMascarado}",
                    "WebhookRecebido.IdentificarCliente",
                    correlationId,
                    TelefoneHelper.MascararNumero(jidCompleto));

                var cliente = await clienteRepository.BuscarPorWhatsAppJidAsync(jidCompleto, default);
                
                if (cliente == null)
                {
                    stopwatch.Stop();
                    logger.LogInformation(
                        "Cliente não encontrado para o JID. Operation={Operation}, CorrelationId={CorrelationId}, JidMascarado={JidMascarado}, DurationMs={DurationMs}",
                        "WebhookRecebido",
                        correlationId,
                        TelefoneHelper.MascararNumero(jidCompleto),
                        stopwatch.ElapsedMilliseconds);
                    return Results.Ok(new { message = "Cliente não encontrado" });
                }

                // Atualizar o JID do cliente se for diferente do cadastrado (vincula o JID ao cliente automaticamente)
                if (cliente.WhatsAppJid != jidCompleto)
                {
                    cliente.AtualizarWhatsAppJid(jidCompleto);
                    await clienteRepository.UpdateAsync(cliente, default);
                    logger.LogInformation(
                        "JID do cliente atualizado automaticamente. Operation={Operation}, ClienteId={ClienteId}, NovoJid={JidMascarado}",
                        "WebhookRecebido.AtualizarJid",
                        cliente.Id,
                        TelefoneHelper.MascararNumero(jidCompleto));
                }

                logger.LogInformation(
                    "Cliente identificado. Operation={Operation}, CorrelationId={CorrelationId}, ClienteId={ClienteId}, ClienteNome={ClienteNome}",
                    "WebhookRecebido.IdentificarCliente",
                    correlationId,
                    cliente.Id,
                    cliente.NomeCompleto);

                // Verificar se é imagem ou documento
                byte[]? arquivo = null;
                string? mimeType = null;

                if (message.ImageMessage != null)
                {
                    logger.LogInformation(
                        "Baixando imagem via Evolution API (descriptografada). Operation={Operation}, CorrelationId={CorrelationId}, ClienteId={ClienteId}",
                        "WebhookRecebido.BaixarArquivo",
                        correlationId,
                        cliente.Id);

                    // Usar JSON original para preservar todas as propriedades necessárias para descriptografia
                    object? messagePayload = mensagemOriginalJson.HasValue 
                        ? mensagemOriginalJson.Value 
                        : new { key = eventoMensagem.Data?.Key, message = eventoMensagem.Data?.Message };

                    var downloadResult = await evolutionApiClient.BaixarMidiaDescriptografadaAsync(messagePayload);
                    if (downloadResult.IsSuccess)
                    {
                        arquivo = downloadResult.Value;
                        mimeType = message.ImageMessage.Mimetype ?? "image/jpeg";
                        logger.LogInformation(
                            "Imagem baixada com sucesso. Operation={Operation}, CorrelationId={CorrelationId}, ArquivoTamanhoKB={ArquivoTamanhoKB:F2}, MimeType={MimeType}",
                            "WebhookRecebido.BaixarArquivo",
                            correlationId,
                            arquivo.Length / 1024.0,
                            mimeType);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Erro ao baixar imagem. Operation={Operation}, CorrelationId={CorrelationId}, ClienteId={ClienteId}, Errors={Errors}",
                            "WebhookRecebido.BaixarArquivo",
                            correlationId,
                            cliente.Id,
                            string.Join(", ", downloadResult.Errors));
                    }
                }
                else if (message.DocumentMessage != null)
                {
                    logger.LogInformation(
                        "Baixando documento via Evolution API (descriptografado). Operation={Operation}, CorrelationId={CorrelationId}, ClienteId={ClienteId}",
                        "WebhookRecebido.BaixarArquivo",
                        correlationId,
                        cliente.Id);

                    // Usar JSON original para preservar todas as propriedades necessárias para descriptografia
                    object? messagePayload = mensagemOriginalJson.HasValue 
                        ? mensagemOriginalJson.Value 
                        : new { key = eventoMensagem.Data?.Key, message = eventoMensagem.Data?.Message };

                    var downloadResult = await evolutionApiClient.BaixarMidiaDescriptografadaAsync(messagePayload);
                    if (downloadResult.IsSuccess)
                    {
                        arquivo = downloadResult.Value;
                        mimeType = message.DocumentMessage.Mimetype ?? "application/pdf";
                        logger.LogInformation(
                            "Documento baixado com sucesso. Operation={Operation}, CorrelationId={CorrelationId}, ArquivoTamanhoKB={ArquivoTamanhoKB:F2}, MimeType={MimeType}",
                            "WebhookRecebido.BaixarArquivo",
                            correlationId,
                            arquivo.Length / 1024.0,
                            mimeType);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Erro ao baixar documento. Operation={Operation}, CorrelationId={CorrelationId}, ClienteId={ClienteId}, Errors={Errors}",
                            "WebhookRecebido.BaixarArquivo",
                            correlationId,
                            cliente.Id,
                            string.Join(", ", downloadResult.Errors));
                    }
                }

                // Se não encontrou arquivo, ignorar
                if (arquivo == null || arquivo.Length == 0)
                {
                    stopwatch.Stop();
                    logger.LogInformation(
                        "Nenhum arquivo encontrado na mensagem. Operation={Operation}, CorrelationId={CorrelationId}, ClienteId={ClienteId}, DurationMs={DurationMs}",
                        "WebhookRecebido",
                        correlationId,
                        cliente.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Results.Ok(new { message = "Nenhum arquivo encontrado na mensagem" });
                }

                // Processar comprovante de forma assíncrona
                var command = new ProcessarComprovanteCommand(
                    ClienteId: cliente.Id,
                    Arquivo: arquivo,
                    MimeType: mimeType!,
                    NumeroWhatsApp: numeroBase,
                    JidOriginal: jidCompleto, // JID completo para responder (pode ser @lid ou @s.whatsapp.net)
                    DataEnvioMensagemFatura: DateTime.UtcNow
                );

                logger.LogInformation(
                    "Iniciando processamento de comprovante em background. Operation={Operation}, CorrelationId={CorrelationId}, ClienteId={ClienteId}, ArquivoTamanhoKB={ArquivoTamanhoKB:F2}, MimeType={MimeType}",
                    "WebhookRecebido.IniciarProcessamento",
                    correlationId,
                    cliente.Id,
                    arquivo.Length / 1024.0,
                    mimeType);

                // Executar de forma assíncrona (fire and forget para não bloquear webhook)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Criar um novo scope para o processamento em background
                        using var scope = serviceProvider.CreateScope();
                        var scopedSender = scope.ServiceProvider.GetRequiredService<ISender>();
                        await scopedSender.Send(command);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Erro ao processar comprovante em background. Operation={Operation}, CorrelationId={CorrelationId}, ClienteId={ClienteId}, ErrorMessage={ErrorMessage}",
                            "WebhookRecebido.ProcessarComprovante",
                            correlationId,
                            cliente.Id,
                            ex.Message);
                    }
                });

                stopwatch.Stop();
                logger.LogInformation(
                    "Webhook processado, comprovante em processamento. Operation={Operation}, CorrelationId={CorrelationId}, ClienteId={ClienteId}, Success={Success}, DurationMs={DurationMs}",
                    "WebhookRecebido",
                    correlationId,
                    cliente.Id,
                    true,
                    stopwatch.ElapsedMilliseconds);

                return Results.Ok(new { message = "Comprovante recebido e será processado", correlationId });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(
                    ex,
                    "Erro ao processar webhook. Operation={Operation}, CorrelationId={CorrelationId}, Success={Success}, DurationMs={DurationMs}, ErrorMessage={ErrorMessage}",
                    "WebhookRecebido",
                    correlationId,
                    false,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithSummary("Endpoint público para receber webhooks do Evolution API")
        .AllowAnonymous(); // Endpoint público, sem autenticação
    }

    // DTOs para deserialização do webhook
    private class EvolutionWebhookDto
    {
        public string? Event { get; set; }
        public WebhookData? Data { get; set; }
        public string? Sender { get; set; }
    }

    private class WebhookData
    {
        public WebhookKey? Key { get; set; }
        public WebhookMessage? Message { get; set; }
    }

    private class WebhookKey
    {
        public string? RemoteJid { get; set; }
        public bool? FromMe { get; set; }
    }

    private class WebhookMessage
    {
        public ImageMessage? ImageMessage { get; set; }
        public DocumentMessage? DocumentMessage { get; set; }
    }

    private class ImageMessage
    {
        public string? Url { get; set; }
        public string? Mimetype { get; set; }
    }

    private class DocumentMessage
    {
        public string? Url { get; set; }
        public string? Mimetype { get; set; }
    }

    private static bool ValidarWebhookSecret(HttpRequest request, string expectedSecret)
    {
        // Evolution API pode enviar o secret em um header ou no body
        // Verificar header primeiro
        if (request.Headers.TryGetValue("X-Webhook-Secret", out var headerSecret))
        {
            return headerSecret == expectedSecret;
        }

        // Se não estiver no header, pode estar em um campo do body
        // Por enquanto, retornamos true se o secret estiver configurado
        // Você pode implementar validação adicional conforme a documentação da Evolution API
        return true;
    }
}
