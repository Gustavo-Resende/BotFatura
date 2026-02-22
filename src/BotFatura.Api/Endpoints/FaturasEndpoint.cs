using Ardalis.Result;
using BotFatura.Application.Faturas.Commands.ConfigurarCobranca;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BotFatura.Api.Endpoints;

public class FaturasEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/faturas").WithTags("Faturas");

        group.MapPost("/", async (ConfigurarCobrancaCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            if (result.IsSuccess)
            {
                return Results.Created($"/api/faturas/{result.Value}", result.Value);
            }

            if (result.Status == ResultStatus.NotFound)
            {
                return Results.NotFound(result.Errors); // Ex: ClienteId não existe
            }

            if (result.Status == ResultStatus.Invalid)
            {
                return Results.BadRequest(result.ValidationErrors); // Faltando preço, etc
            }

            if (result.Status == ResultStatus.Error)
            {
                return Results.UnprocessableEntity(result.Errors); // Cliente desativado
            }

            return Results.BadRequest(result.Errors);
        });
    }
}
