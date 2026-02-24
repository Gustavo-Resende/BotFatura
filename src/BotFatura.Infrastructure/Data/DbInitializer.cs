using BotFatura.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BotFatura.Infrastructure.Data;

public interface IDbInitializer
{
    Task InitializeAsync();
}

public class DbInitializer : IDbInitializer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<DbInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            _logger.LogInformation("Iniciando migrações de banco de dados...");
            await context.Database.MigrateAsync();

            await SeedDefaultTemplatesAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao inicializar o banco de dados.");
            throw;
        }
    }

    private async Task SeedDefaultTemplatesAsync(AppDbContext context)
    {
        if (!await context.MensagensTemplate.AnyAsync())
        {
            _logger.LogInformation("Semeando templates padrão...");
            context.MensagensTemplate.Add(new MensagemTemplate(
                "Olá {NomeCliente}! 🤖\n\nIdentificamos uma fatura pendente no valor de *R$ {Valor}* com vencimento em *{Vencimento}*.\n\n*Pagamento via PIX:*\nTitular: {NomeDono}\nChave: {ChavePix}\n\nPor favor, efetue o pagamento para evitar suspensão do serviço.",
                isPadrao: true));
            await context.SaveChangesAsync();
        }
    }
}
