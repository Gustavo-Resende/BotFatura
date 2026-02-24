using BotFatura.Application.Common.Interfaces;
using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BotFatura.Api.Endpoints;

public class AuthEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", (LoginRequest request, IAuthService authService) =>
        {
            var token = authService.Authenticate(request.Email, request.Password);
            
            if (token == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new { token });
        })
        .WithSummary("Realiza o login e retorna um token JWT.");
    }
}

public record LoginRequest(string Email, string Password);
