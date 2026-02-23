using Ardalis.Result;
using BotFatura.Application.Clientes.Commands.CadastrarCliente;
using BotFatura.Application.Clientes.Commands.AtualizarCliente;
using BotFatura.Application.Clientes.Commands.ExcluirCliente;
using BotFatura.Application.Clientes.Queries.ObterClientePorId;
using BotFatura.Application.Clientes.Queries.ListarClientes;

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
        var group = app.MapGroup("/api/clientes").WithTags("Clientes").RequireAuthorization();

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

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new ListarClientesQuery());
            return Results.Ok(result.Value);
        })
        .WithSummary("Lista todos os clientes cadastrados.");

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
        })
        .WithSummary("Obtém os detalhes de um cliente específico.");

        group.MapPut("/{id:guid}", async (Guid id, AtualizarClienteCommand command, ISender sender) =>
        {
            if (id != command.Id) return Results.BadRequest("ID no corpo difere do ID da rota.");
            
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors);
        })
        .WithSummary("Atualiza os dados de um cliente.");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ExcluirClienteCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors);
        })
        .WithSummary("Desativa um cliente do sistema.");

    }
}
