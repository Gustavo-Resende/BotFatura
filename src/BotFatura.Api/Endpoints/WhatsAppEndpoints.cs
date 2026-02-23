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
                await Task.Delay(2000); 
                statusResult = await client.ObterStatusAsync();
            }

            if (statusResult.IsSuccess && statusResult.Value == "open")
            {
                return Results.Ok(new { status = "connected", message = "WhatsApp já está conectado." });
            }

            var qrResult = await client.GerarQrCodeAsync();
            if (qrResult.IsSuccess)
            {
                return Results.Ok(new 
                { 
                    status = "awaiting_qrcode", 
                    message = "QR Code gerado. Escaneie no seu WhatsApp.",
                    qrcodeBase64 = qrResult.Value,
                    expiresIn = 40 // Evolution API geralmente expira em 40s a 1m
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
    }
}
