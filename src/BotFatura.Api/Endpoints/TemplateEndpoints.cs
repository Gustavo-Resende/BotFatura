using BotFatura.Application.Templates.Queries.ObterPreviewMensagem;
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

        group.MapPost("/preview", async (ObterPreviewMensagemQuery query, IMediator mediator) =>
        {
            var result = await mediator.Send(query);
            return result.IsSuccess ? Results.Ok(new { preview = result.Value }) : Results.BadRequest(result.Errors);
        });
    }
}
