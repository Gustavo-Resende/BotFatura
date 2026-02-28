using BotFatura.Application.Common.Interfaces;
using Carter;
using Microsoft.AspNetCore.Mvc;

namespace BotFatura.Api.Endpoints;

public class TestEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/test").WithTags("Test");

        group.MapPost("/gemini/url", async (
            [FromBody] TestGeminiUrlRequest request,
            IGeminiApiClient geminiApiClient,
            ILogger<IGeminiApiClient> logger) =>
        {
            try
            {
                logger.LogInformation("Testando Gemini com URL: {Url}", request.ImageUrl);

                // Baixar imagem da URL
                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(request.ImageUrl);

                logger.LogInformation("Imagem baixada. Tamanho: {Size} KB", imageBytes.Length / 1024.0);

                // Analisar com Gemini
                var resultado = await geminiApiClient.AnalisarComprovanteAsync(imageBytes, "image/jpeg", default);

                if (resultado.IsSuccess)
                {
                    return Results.Ok(new
                    {
                        success = true,
                        data = resultado.Value,
                        message = "Análise realizada com sucesso!"
                    });
                }
                else
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        errors = resultado.Errors,
                        message = "Falha na análise"
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao testar Gemini");
                return Results.BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    message = "Erro ao processar"
                });
            }
        })
        .WithSummary("Testa o Gemini com uma imagem via URL")
        .WithDescription("Baixa uma imagem da URL fornecida e envia para análise no Gemini");

        group.MapPost("/gemini/base64", async (
            [FromBody] TestGeminiBase64Request request,
            IGeminiApiClient geminiApiClient,
            ILogger<IGeminiApiClient> logger) =>
        {
            try
            {
                logger.LogInformation("Testando Gemini com base64. MimeType: {MimeType}", request.MimeType);

                // Converter base64 para bytes
                var imageBytes = Convert.FromBase64String(request.Base64Image);

                logger.LogInformation("Imagem convertida. Tamanho: {Size} KB", imageBytes.Length / 1024.0);

                // Analisar com Gemini
                var resultado = await geminiApiClient.AnalisarComprovanteAsync(
                    imageBytes, 
                    request.MimeType ?? "image/jpeg", 
                    default);

                if (resultado.IsSuccess)
                {
                    return Results.Ok(new
                    {
                        success = true,
                        data = resultado.Value,
                        message = "Análise realizada com sucesso!"
                    });
                }
                else
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        errors = resultado.Errors,
                        message = "Falha na análise"
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao testar Gemini");
                return Results.BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    message = "Erro ao processar"
                });
            }
        })
        .WithSummary("Testa o Gemini com uma imagem via base64")
        .WithDescription("Recebe uma imagem em base64 e envia para análise no Gemini");
    }

    public record TestGeminiUrlRequest(string ImageUrl);
    public record TestGeminiBase64Request(string Base64Image, string? MimeType);
}
