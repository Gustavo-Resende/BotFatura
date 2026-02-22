using BotFatura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotFatura.Infrastructure.Data.Configurations;

public class MensagemTemplateConfiguration : IEntityTypeConfiguration<MensagemTemplate>
{
    public void Configure(EntityTypeBuilder<MensagemTemplate> builder)
    {
        builder.ToTable("MensagensTemplate");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TextoBase)
            .IsRequired()
            .HasMaxLength(2000); // Texto do WhatsApp pode ser longo

        builder.Property(m => m.IsPadrao)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(m => m.CreatedAt)
            .IsRequired();
    }
}
