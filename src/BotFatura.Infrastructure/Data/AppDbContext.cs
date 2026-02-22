using BotFatura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotFatura.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Fatura> Faturas { get; set; }
    public DbSet<MensagemTemplate> MensagensTemplate { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Aplica todas as classes que herdam de IEntityTypeConfiguration automaticamente
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
