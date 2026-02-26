using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Interfaces;
using BotFatura.Infrastructure.Data;
using BotFatura.Infrastructure.Repositories;
using BotFatura.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace BotFatura.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IFaturaRepository, FaturaRepository>();
        services.AddScoped<IMensagemTemplateRepository, MensagemTemplateRepository>();
        services.AddScoped<IContratoRepository, ContratoRepository>();

        // Registrando o inicializador do banco
        services.AddScoped<IDbInitializer, DbInitializer>();

        // Registrando o cliente HTTP da Evolution API com resiliência
        services.AddHttpClient<IEvolutionApiClient, EvolutionApiClient>()
            .AddTransientHttpErrorPolicy(policy => 
                policy.WaitAndRetryAsync(3, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) 
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000))));

        return services;
    }
}
