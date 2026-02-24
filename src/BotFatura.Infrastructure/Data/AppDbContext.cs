using BotFatura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotFatura.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Fatura> Faturas { get; set; }
    public DbSet<MensagemTemplate> MensagensTemplate { get; set; }
    public DbSet<Configuracao> Configuracoes { get; set; }
    public DbSet<LogNotificacao> LogsNotificacao { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Aplica todas as classes que herdam de IEntityTypeConfiguration automaticamente
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Configuração global para tratar DateTime como UTC (PostgreSQL compatibility)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

            foreach (var property in properties)
            {
                property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                    v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
            }
        }
    }
}
