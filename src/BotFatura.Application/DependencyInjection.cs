using System.Reflection;
using BotFatura.Application.Common.Behaviors;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Common.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BotFatura.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddScoped<IMensagemFormatter, MensagemFormatter>();
        services.AddScoped<IReguaCobrancaService, ReguaCobrancaService>();
        
        // Cache Service
        services.AddMemoryCache();
        services.AddScoped<ICacheService, CacheService>();

        return services;
    }
}
