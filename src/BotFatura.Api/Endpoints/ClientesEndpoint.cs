using Ardalis.Result;
using BotFatura.Application.Clientes.Commands.CadastrarCliente;
using BotFatura.Application.Clientes.Queries.ObterClientePorId;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BotFatura.Api.Endpoints;

public class ClientesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes").WithTags("Clientes");

        group.MapPost("/", async (CadastrarClienteCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            if (result.IsSuccess)
            {
                // Retorna 201 Created com a URL para possível busca futura
                return Results.Created($"/api/clientes/{result.Value}", result.Value);
            }

            if (result.Status == ResultStatus.Conflict)
            {
                return Results.Conflict(result.Errors);
            }

            if (result.Status == ResultStatus.Invalid)
            {
                return Results.BadRequest(result.ValidationErrors);
            }

            return Results.BadRequest(result.Errors);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var query = new ObterClienteQuery(id);
            var result = await sender.Send(query);

            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }

            if (result.Status == ResultStatus.NotFound)
            {
                return Results.NotFound(result.Errors);
            }

            return Results.BadRequest(result.Errors);
        });
    }
}
