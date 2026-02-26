using BotFatura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotFatura.Infrastructure.Data.Configurations;

public class FaturaConfiguration : IEntityTypeConfiguration<Fatura>
{
    public void Configure(EntityTypeBuilder<Fatura> builder)
    {
        builder.ToTable("Faturas");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Valor)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(f => f.DataVencimento)
            .IsRequired();

        // Armazenando o ENUM como String no banco para melhor legibilidade no PgAdmin
        builder.Property(f => f.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(f => f.DataVencimento);
        builder.HasIndex(f => f.Status);

        // Índice composto para queries do worker (Status, DataVencimento)
        builder.HasIndex(f => new { f.Status, f.DataVencimento })
               .HasDatabaseName("IX_Faturas_Status_DataVencimento");

        // Índice composto para queries de cliente (ClienteId, Status)
        builder.HasIndex(f => new { f.ClienteId, f.Status })
               .HasDatabaseName("IX_Faturas_ClienteId_Status");
            
        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // Relacionamento 1:N com Cliente
        builder.HasOne(f => f.Cliente)
            .WithMany()
            .HasForeignKey(f => f.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
