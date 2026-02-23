using Ardalis.Result;
using BotFatura.Application.Faturas.Commands.ConfigurarCobranca;
using BotFatura.Application.Faturas.Commands.EnviarFaturaWhatsApp;
using BotFatura.Application.Faturas.Commands.RegistrarPagamento;
using BotFatura.Application.Faturas.Commands.CancelarFatura;
using BotFatura.Application.Faturas.Queries.ListarFaturas;
using BotFatura.Domain.Enums;
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
        var group = app.MapGroup("/api/faturas").WithTags("Faturas").RequireAuthorization();

        group.MapGet("/", async (StatusFatura? status, ISender sender) =>
        {
            var result = await sender.Send(new ListarFaturasQuery(status));
            return Results.Ok(result.Value);
        })
        .WithSummary("Lista as faturas com filtro opcional por status.");

        group.MapPost("/", async (ConfigurarCobrancaCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            if (result.IsSuccess)
            {
                return Results.Created($"/api/faturas/{result.Value}", result.Value);
            }

            if (result.Status == ResultStatus.NotFound)
            {
                return Results.NotFound(result.Errors);
            }

            if (result.Status == ResultStatus.Invalid)
            {
                return Results.BadRequest(result.ValidationErrors);
            }

            return Results.BadRequest(result.Errors);
        })
        .WithSummary("Configura uma nova cobrança.");

        group.MapPost("/{id:guid}/pagar", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new RegistrarPagamentoCommand(id));
            return result.IsSuccess 
                ? Results.Ok(new { message = "Pagamento registrado com sucesso." }) 
                : Results.BadRequest(result.Errors);
        })
        .WithSummary("Marca uma fatura como paga.");

        group.MapPost("/{id:guid}/cancelar", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new CancelarFaturaCommand(id));
            return result.IsSuccess 
                ? Results.Ok(new { message = "Fatura cancelada." }) 
                : Results.BadRequest(result.Errors);
        })
        .WithSummary("Cancela uma fatura ativa.");

        group.MapPost("/{id:guid}/enviar", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new EnviarFaturaWhatsAppCommand(id));

            if (result.IsSuccess)
            {
                return Results.Ok(new { message = "Fatura enviada com sucesso para o WhatsApp." });
            }

            return result.Status == ResultStatus.NotFound 
                ? Results.NotFound(result.Errors) 
                : Results.BadRequest(result.Errors);
        })
        .WithName("EnviarFatura")
        .WithSummary("Dispara manualmente a cobrança da fatura via WhatsApp.");
    }
}
