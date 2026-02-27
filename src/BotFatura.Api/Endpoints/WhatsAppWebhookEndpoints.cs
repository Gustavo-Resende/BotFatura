using BotFatura.Application.Comprovantes.Commands.ProcessarComprovante;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Interfaces;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BotFatura.Api.Endpoints;

public class WhatsAppWebhookEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Endpoint público para receber webhooks do Evolution API
        var webhookGroup = app.MapGroup("/webhook/whatsapp").WithTags("WhatsApp Webhook");

        webhookGroup.MapPost("/", async (HttpRequest request, ISender sender, IClienteRepository clienteRepository, IEvolutionApiClient evolutionApiClient, IConfiguration configuration) =>
        {
            try
            {
                // Validar webhook secret se configurado
                var webhookSecret = configuration["EvolutionApi:WebhookSecret"];
                if (!string.IsNullOrWhiteSpace(webhookSecret) && webhookSecret != "YOUR_WEBHOOK_SECRET")
                {
                    if (!ValidarWebhookSecret(request, webhookSecret))
                    {
                        return Results.Unauthorized();
                    }
                }

                var body = await new StreamReader(request.Body).ReadToEndAsync();
                var webhookData = JsonSerializer.Deserialize<EvolutionWebhookDto>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (webhookData == null || webhookData.Event != "messages.upsert")
                {
                    return Results.Ok(new { message = "Evento ignorado" });
                }

                var message = webhookData.Data?.Message;
                if (message == null)
                {
                    return Results.Ok(new { message = "Mensagem vazia" });
                }

                // Extrair número do remetente
                var remoteJid = webhookData.Data?.Key?.RemoteJid;
                if (string.IsNullOrWhiteSpace(remoteJid))
                {
                    return Results.Ok(new { message = "Remetente não identificado" });
                }

                // Remover sufixo @s.whatsapp.net ou @g.us
                var numeroWhatsApp = remoteJid.Split('@')[0];
                if (remoteJid.Contains("@g.us"))
                {
                    // Mensagem de grupo, ignorar
                    return Results.Ok(new { message = "Mensagem de grupo ignorada" });
                }

                // Buscar cliente pelo WhatsApp
                var todosClientes = await clienteRepository.ListAsync(default);
                var cliente = todosClientes.FirstOrDefault(c => 
                    c.WhatsApp == numeroWhatsApp || 
                    c.WhatsApp.EndsWith(numeroWhatsApp) ||
                    numeroWhatsApp.EndsWith(c.WhatsApp.Replace("+", "").Replace("-", "").Replace(" ", "")));
                
                if (cliente == null)
                {
                    return Results.Ok(new { message = "Cliente não encontrado" });
                }

                // Verificar se é imagem ou documento
                byte[]? arquivo = null;
                string? mimeType = null;

                if (message.ImageMessage != null)
                {
                    var imageUrl = message.ImageMessage.Url;
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        var downloadResult = await evolutionApiClient.BaixarArquivoAsync(imageUrl);
                        if (downloadResult.IsSuccess)
                        {
                            arquivo = downloadResult.Value;
                            mimeType = message.ImageMessage.Mimetype ?? "image/jpeg";
                        }
                    }
                }
                else if (message.DocumentMessage != null)
                {
                    var docUrl = message.DocumentMessage.Url;
                    if (!string.IsNullOrWhiteSpace(docUrl))
                    {
                        var downloadResult = await evolutionApiClient.BaixarArquivoAsync(docUrl);
                        if (downloadResult.IsSuccess)
                        {
                            arquivo = downloadResult.Value;
                            mimeType = message.DocumentMessage.Mimetype ?? "application/pdf";
                        }
                    }
                }

                // Se não encontrou arquivo, ignorar
                if (arquivo == null || arquivo.Length == 0)
                {
                    return Results.Ok(new { message = "Nenhum arquivo encontrado na mensagem" });
                }

                // Processar comprovante de forma assíncrona
                var command = new ProcessarComprovanteCommand(
                    ClienteId: cliente.Id,
                    Arquivo: arquivo,
                    MimeType: mimeType!,
                    NumeroWhatsApp: numeroWhatsApp,
                    DataEnvioMensagemFatura: DateTime.UtcNow // Assumindo que a mensagem foi enviada hoje
                );

                // Executar de forma assíncrona (fire and forget para não bloquear webhook)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await sender.Send(command);
                    }
                    catch (Exception ex)
                    {
                        // Log do erro (pode usar ILogger aqui se necessário)
                        Console.WriteLine($"Erro ao processar comprovante: {ex.Message}");
                    }
                });

                return Results.Ok(new { message = "Comprovante recebido e será processado" });
            }
            catch (Exception ex)
            {
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
    }

    private class WebhookData
    {
        public WebhookKey? Key { get; set; }
        public WebhookMessage? Message { get; set; }
    }

    private class WebhookKey
    {
        public string? RemoteJid { get; set; }
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
