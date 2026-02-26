using BotFatura.Application.Templates.Queries.ObterPreviewMensagem;
using BotFatura.Application.Templates.Queries.ListarTemplates;
using BotFatura.Application.Templates.Commands.AtualizarTemplate;
using BotFatura.Application.Templates.Queries.ObterTemplatesPorTipo;
using BotFatura.Application.Templates.Queries.ObterTemplatePorTipo;
using BotFatura.Application.Templates.Commands.AtualizarTemplatePorTipo;
using BotFatura.Application.Templates.Commands.ResetarTemplatePorTipo;
using BotFatura.Domain.Enums;
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
        var group = app.MapGroup("/api/templates").WithTags("Templates").RequireAuthorization();

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

        // Novos endpoints para templates por tipo
        group.MapGet("/tipos", async (ISender sender) =>
        {
            var result = await sender.Send(new ObterTemplatesPorTipoQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        })
        .WithSummary("Retorna todos os templates agrupados por tipo (Lembrete, Vencimento, AposVencimento).");

        group.MapGet("/tipos/{tipo:int}", async (int tipo, ISender sender) =>
        {
            if (!Enum.IsDefined(typeof(TipoNotificacaoTemplate), tipo))
                return Results.BadRequest("Tipo de notificação inválido.");

            var result = await sender.Send(new ObterTemplatePorTipoQuery((TipoNotificacaoTemplate)tipo));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        })
        .WithSummary("Retorna template específico por tipo de notificação.");

        group.MapPut("/tipos/{tipo:int}", async (int tipo, AtualizarTemplatePorTipoCommand command, ISender sender) =>
        {
            if (!Enum.IsDefined(typeof(TipoNotificacaoTemplate), tipo))
                return Results.BadRequest("Tipo de notificação inválido.");

            if (tipo != (int)command.Tipo)
                return Results.BadRequest("Tipo no corpo difere do tipo da rota.");

            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors);
        })
        .WithSummary("Atualiza o texto de um template por tipo de notificação.");

        group.MapPost("/tipos/{tipo:int}/reset", async (int tipo, ISender sender) =>
        {
            if (!Enum.IsDefined(typeof(TipoNotificacaoTemplate), tipo))
                return Results.BadRequest("Tipo de notificação inválido.");

            var result = await sender.Send(new ResetarTemplatePorTipoCommand((TipoNotificacaoTemplate)tipo));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors);
        })
        .WithSummary("Reseta um template para o texto padrão do sistema.");
    }
}
