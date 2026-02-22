using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Interfaces;
using BotFatura.Infrastructure.Data;
using BotFatura.Infrastructure.Repositories;
using BotFatura.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // Registrando o cliente HTTP da Evolution API
        services.AddHttpClient<IEvolutionApiClient, EvolutionApiClient>();

        return services;
    }
}
