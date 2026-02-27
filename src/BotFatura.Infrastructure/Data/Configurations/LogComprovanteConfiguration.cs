using BotFatura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotFatura.Infrastructure.Data.Configurations;

public class LogComprovanteConfiguration : IEntityTypeConfiguration<LogComprovante>
{
    public void Configure(EntityTypeBuilder<LogComprovante> builder)
    {
        builder.ToTable("LogsComprovante");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ValorExtraido)
            .HasColumnType("numeric(18,2)");

        builder.Property(l => l.ValorEsperado)
            .HasColumnType("numeric(18,2)");

        builder.Property(l => l.TipoArquivo)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Erro)
            .HasMaxLength(1000);

        // Relacionamento com Cliente
        builder.HasOne(l => l.Cliente)
            .WithMany()
            .HasForeignKey(l => l.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com Fatura (opcional)
        builder.HasOne(l => l.Fatura)
            .WithMany()
            .HasForeignKey(l => l.FaturaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => l.ClienteId);
        builder.HasIndex(l => l.FaturaId);
        builder.HasIndex(l => l.CreatedAt);
    }
}
