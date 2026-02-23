using BotFatura.Application.Configuracoes.Commands.AtualizarConfiguracao;
using BotFatura.Application.Configuracoes.Queries.ObterConfiguracao;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BotFatura.Api.Endpoints;

public class ConfiguracaoEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/configuracoes").WithTags("Configuracoes").RequireAuthorization();

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new ObterConfiguracaoQuery());
            return Results.Ok(result.Value);
        })
        .WithSummary("Obtém as configurações globais (PIX, etc).");

        group.MapPost("/", async (AtualizarConfiguracaoCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(new { message = "Configurações atualizadas com sucesso." }) : Results.BadRequest(result.Errors);
        })
        .WithSummary("Atualiza as configurações globais (PIX, etc).");
    }
}
