using BotFatura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotFatura.Infrastructure.Data.Configurations;

public class ConfiguracaoConfiguration : IEntityTypeConfiguration<Configuracao>
{
    public void Configure(EntityTypeBuilder<Configuracao> builder)
    {
        builder.ToTable("Configuracoes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ChavePix)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.NomeTitularPix)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.GrupoSociosWhatsAppId)
            .HasMaxLength(100);

        builder.Property(c => c.DiasAntecedenciaLembrete)
            .IsRequired();

        builder.Property(c => c.DiasAposVencimentoCobranca)
            .IsRequired();
    }
}
