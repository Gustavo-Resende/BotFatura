using BotFatura.Domain.Entities;
using Microsoft.AspNetCore.Identity;
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        try
        {
            _logger.LogInformation("Iniciando migrações de banco de dados...");
            await context.Database.MigrateAsync();

            await SeedAdminUserAsync(userManager);
            await SeedDefaultTemplatesAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao inicializar o banco de dados.");
            throw;
        }
    }

    private async Task SeedAdminUserAsync(UserManager<IdentityUser> userManager)
    {
        var adminEmail = _configuration["DefaultAdmin:Email"] ?? "admin@botfatura.com.br";
        var adminPassword = _configuration["DefaultAdmin:Password"] ?? "BF_P@ss_9932_*xZ";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            _logger.LogInformation("Criando usuário administrador padrão: {Email}", adminEmail);
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Falha ao criar usuário admin: {Errors}", errors);
            }
        }
        else
        {
            // Opcional: Garantir que a senha está sincronizada com o config durante o desenvolvimento
            _logger.LogInformation("Usuário admin já existe. Resetando senha para garantir sincronia com config.");
            var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
            await userManager.ResetPasswordAsync(adminUser, token, adminPassword);
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
