using BotFatura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotFatura.Infrastructure.Data.Configurations;

public class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> builder)
    {
        builder.ToTable("contratos");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.ClienteId)
               .HasColumnName("cliente_id")
               .IsRequired();

        builder.Property(c => c.ValorMensal)
               .HasColumnName("valor_mensal")
               .HasColumnType("numeric(18,2)")
               .IsRequired();

        builder.Property(c => c.DiaVencimento)
               .HasColumnName("dia_vencimento")
               .IsRequired();

        builder.Property(c => c.DataInicio)
               .HasColumnName("data_inicio")
               .IsRequired();

        builder.Property(c => c.DataFim)
               .HasColumnName("data_fim")
               .IsRequired(false);

        builder.Property(c => c.Ativo)
               .HasColumnName("ativo")
               .IsRequired();

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        // Relacionamento 1:N com Cliente
        builder.HasOne(c => c.Cliente)
               .WithMany(cl => cl.Contratos)
               .HasForeignKey(c => c.ClienteId)
               .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento 1:N com Fatura (via FK ContratoId nullable em Fatura)
        builder.HasMany(c => c.Faturas)
               .WithOne(f => f.Contrato)
               .HasForeignKey(f => f.ContratoId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        // Índice para consultas do worker (DiaVencimento + Ativo) — principal query de produção
        builder.HasIndex(c => new { c.DiaVencimento, c.Ativo })
               .HasDatabaseName("ix_contratos_dia_vencimento_ativo");
    }
}
