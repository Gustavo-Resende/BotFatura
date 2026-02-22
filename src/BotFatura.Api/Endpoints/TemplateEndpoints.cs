using BotFatura.Application.Templates.Queries.ObterPreviewMensagem;
using BotFatura.Application.Templates.Queries.ListarTemplates;
using BotFatura.Application.Templates.Commands.AtualizarTemplate;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BotFatura.Api.Endpoints;

public class TemplateEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/templates").WithTags("Templates");

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new ListarTemplatesQuery());
            return Results.Ok(result.Value);
        })
        .WithSummary("Lista todos os templates de mensagem.");

        group.MapPut("/{id:guid}", async (Guid id, AtualizarTemplateCommand command, ISender sender) =>
        {
            if (id != command.Id) return Results.BadRequest("ID no corpo difere do ID da rota.");
            
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors);
        })
        .WithSummary("Atualiza o texto de um template.");

        group.MapPost("/preview", async (ObterPreviewMensagemQuery query, ISender sender) =>
        {
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(new { preview = result.Value }) : Results.BadRequest(result.Errors);
        })
        .WithSummary("Gera uma prévia da mensagem com dados fictícios.");
    }
}
