using BotFatura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotFatura.Infrastructure.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.NomeCompleto)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.WhatsApp)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(c => c.Ativo)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(c => c.CreatedAt)
            .IsRequired();
            
        builder.Property(c => c.UpdatedAt)
            .IsRequired(false);
    }
}
