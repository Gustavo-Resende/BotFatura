using BotFatura.Application.Dashboard.Queries.ObterResumoDashboard;
using BotFatura.Application.Dashboard.Queries.ObterClientesAtrasados;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BotFatura.Api.Endpoints;

public class DashboardEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

        group.MapGet("/resumo", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new ObterResumoDashboardQuery());
            return Results.Ok(result);
        });

        group.MapGet("/atrasados", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new ObterClientesAtrasadosQuery());
            return Results.Ok(result);
        });
    }
}
