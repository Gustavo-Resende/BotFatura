using Ardalis.Result;
using BotFatura.Application.Contratos.Commands.CriarContrato;
using BotFatura.Application.Contratos.Commands.EncerrarContrato;
using BotFatura.Application.Contratos.Queries.ListarContratos;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BotFatura.Api.Endpoints;

public class ContratosEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/contratos").WithTags("Contratos").RequireAuthorization();

        group.MapGet("/", async (Guid? clienteId, ISender sender) =>
        {
            var result = await sender.Send(new ListarContratosQuery(clienteId));
            return Results.Ok(result.Value);
        })
        .WithSummary("Lista todos os contratos de recorrência, com filtro opcional por cliente.");

        group.MapPost("/", async (CriarContratoCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            if (result.IsSuccess)
                return Results.Created($"/api/contratos/{result.Value}", result.Value);

            if (result.Status == ResultStatus.NotFound)
                return Results.NotFound(result.Errors);

            if (result.Status == ResultStatus.Invalid)
                return Results.BadRequest(result.ValidationErrors);

            return Results.BadRequest(result.Errors);
        })
        .WithSummary("Cria um novo contrato de cobrança recorrente para um cliente.");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new EncerrarContratoCommand(id));

            if (result.IsSuccess)
                return Results.Ok(new { message = "Contrato encerrado com sucesso." });

            return result.Status == ResultStatus.NotFound
                ? Results.NotFound(result.Errors)
                : Results.BadRequest(result.Errors);
        })
        .WithSummary("Encerra um contrato ativo, impedindo a geração de novas faturas.");
    }
}
