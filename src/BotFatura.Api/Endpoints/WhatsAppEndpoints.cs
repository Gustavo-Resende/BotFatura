using BotFatura.Application.Common.Interfaces;
using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BotFatura.Api.Endpoints;

public class WhatsAppEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/whatsapp").WithTags("WhatsApp").RequireAuthorization();

        group.MapGet("/status", async (IEvolutionApiClient client) =>
        {
            var result = await client.ObterStatusAsync();
            return result.IsSuccess ? Results.Ok(new { status = result.Value }) : Results.BadRequest(result.Errors);
        });

        // Rota principal para conexão (pode ser usada via POST ou GET para facilitar polling)
        group.MapGet("/conectar", async (IEvolutionApiClient client) =>
        {
            var statusResult = await client.ObterStatusAsync();
            
            if (statusResult.IsSuccess && statusResult.Value == "INSTANCE_NOT_FOUND")
            {
                await client.CriarInstanciaAsync();
                // Aguardar mais tempo para a instância ser criada completamente
                await Task.Delay(5000); 
                statusResult = await client.ObterStatusAsync();
            }

            if (statusResult.IsSuccess && statusResult.Value == "open")
            {
                return Results.Ok(new { status = "connected", message = "WhatsApp já está conectado." });
            }

            // Aguardar antes de gerar QR Code para evitar requisições muito rápidas
            await Task.Delay(3000);

            var qrResult = await client.GerarQrCodeAsync();
            if (qrResult.IsSuccess)
            {
                return Results.Ok(new 
                { 
                    status = "awaiting_qrcode", 
                    message = "QR Code gerado. Escaneie no seu WhatsApp. Você tem 40 segundos.",
                    qrcodeBase64 = qrResult.Value,
                    expiresIn = 40,
                    warning = "Aguarde o QR Code expirar antes de gerar um novo. Múltiplas tentativas podem causar bloqueio."
                });
            }

            return Results.BadRequest(new { message = "Erro ao obter QR Code", details = qrResult.Errors });
        });

        group.MapPost("/conectar", async (IEvolutionApiClient client) =>
        {
            // Reaproveita a lógica do GET para evitar duplicação
            return await client.ObterStatusAsync() switch
            {
                { IsSuccess: true, Value: "open" } => Results.Ok(new { status = "connected" }),
                _ => Results.Redirect("/api/whatsapp/conectar", true) // Redireciona para o GET que tem a lógica completa
            };
        });


        group.MapDelete("/desconectar", async (IEvolutionApiClient client) =>
        {
            var result = await client.DesconectarAsync();
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors);
        });

        group.MapGet("/grupos", async (IEvolutionApiClient client) =>
        {
            // Validar se WhatsApp está conectado antes de listar grupos
            var statusResult = await client.ObterStatusAsync();
            if (!statusResult.IsSuccess || statusResult.Value != "open")
            {
                return Results.BadRequest(new { 
                    message = "WhatsApp não está conectado. Conecte o WhatsApp primeiro antes de listar grupos.",
                    status = statusResult.Value ?? "unknown"
                });
            }

            // Aguardar mais tempo para garantir que a conexão está completamente estável
            // Especialmente importante após uma conexão recente
            await Task.Delay(2000);

            var result = await client.ListarGruposAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(result.Errors);
        })
        .WithSummary("Lista todos os grupos do WhatsApp que o bot participa. Use com moderação para evitar bloqueios.");
    }
}
