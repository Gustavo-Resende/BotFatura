using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;

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
        var textoLembrete = "Olá {NomeCliente}!\n\nLembramos que você tem uma fatura no valor de *R$ {Valor}* com vencimento em *{Vencimento}*.\n\n*Pagamento via PIX:*\nTitular: {NomeDono}\nChave: {ChavePix}\n\nPor favor, efetue o pagamento até a data de vencimento.";
        var textoVencimento = "Olá {NomeCliente}!\n\nIdentificamos uma fatura pendente no valor de *R$ {Valor}* com vencimento em *{Vencimento}*.\n\n*Pagamento via PIX:*\nTitular: {NomeDono}\nChave: {ChavePix}\n\nPor favor, efetue o pagamento para evitar suspensão do serviço.";
        var textoAposVencimento = "Olá {NomeCliente}!\n\nSua fatura no valor de *R$ {Valor}* com vencimento em *{Vencimento}* ainda não foi paga.\n\n*Pagamento via PIX:*\nTitular: {NomeDono}\nChave: {ChavePix}\n\nPor favor, regularize sua situação o quanto antes para evitar interrupção do serviço.";
        
        // Verificar e criar template de Lembrete
        var templateLembrete = await context.MensagensTemplate.FirstOrDefaultAsync(t => t.TipoNotificacao == TipoNotificacaoTemplate.Lembrete);
        if (templateLembrete == null)
        {
            _logger.LogInformation("Criando template de Lembrete...");
            context.MensagensTemplate.Add(new MensagemTemplate(textoLembrete, TipoNotificacaoTemplate.Lembrete, isPadrao: true));
        }
        else if (!templateLembrete.TextoBase.Contains("{ChavePix}"))
        {
            _logger.LogInformation("Atualizando template de Lembrete com informações de PIX...");
            templateLembrete.AtualizarTexto(textoLembrete);
        }

        // Verificar e criar template de Vencimento
        var templateVencimento = await context.MensagensTemplate.FirstOrDefaultAsync(t => t.TipoNotificacao == TipoNotificacaoTemplate.Vencimento);
        if (templateVencimento == null)
        {
            _logger.LogInformation("Criando template de Vencimento...");
            context.MensagensTemplate.Add(new MensagemTemplate(textoVencimento, TipoNotificacaoTemplate.Vencimento, isPadrao: true));
        }
        else if (!templateVencimento.TextoBase.Contains("{ChavePix}"))
        {
            _logger.LogInformation("Atualizando template de Vencimento com informações de PIX...");
            templateVencimento.AtualizarTexto(textoVencimento);
        }

        // Verificar e criar template de Pós-Vencimento
        var templateAposVencimento = await context.MensagensTemplate.FirstOrDefaultAsync(t => t.TipoNotificacao == TipoNotificacaoTemplate.AposVencimento);
        if (templateAposVencimento == null)
        {
            _logger.LogInformation("Criando template de Pós-Vencimento...");
            context.MensagensTemplate.Add(new MensagemTemplate(textoAposVencimento, TipoNotificacaoTemplate.AposVencimento, isPadrao: true));
        }
        else if (!templateAposVencimento.TextoBase.Contains("{ChavePix}"))
        {
            _logger.LogInformation("Atualizando template de Pós-Vencimento com informações de PIX...");
            templateAposVencimento.AtualizarTexto(textoAposVencimento);
        }

        await context.SaveChangesAsync();
    }
}
