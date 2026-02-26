using Ardalis.GuardClauses;
using BotFatura.Domain.Common;

namespace BotFatura.Domain.Entities;

public class Configuracao : Entity
{
    public string ChavePix { get; private set; } = null!;
    public string NomeTitularPix { get; private set; } = null!;
    public int DiasAntecedenciaLembrete { get; private set; }
    public int DiasAposVencimentoCobranca { get; private set; }

    // Para o EF
    protected Configuracao() { }

    public Configuracao(string chavePix, string nomeTitularPix, int diasAntecedenciaLembrete = 3, int diasAposVencimentoCobranca = 7)
    {
        ChavePix = Guard.Against.NullOrWhiteSpace(chavePix, nameof(chavePix));
        NomeTitularPix = Guard.Against.NullOrWhiteSpace(nomeTitularPix, nameof(nomeTitularPix));
        DiasAntecedenciaLembrete = Guard.Against.OutOfRange(diasAntecedenciaLembrete, nameof(diasAntecedenciaLembrete), 1, 30);
        DiasAposVencimentoCobranca = Guard.Against.OutOfRange(diasAposVencimentoCobranca, nameof(diasAposVencimentoCobranca), 1, 60);
    }

    public void AtualizarConfiguracao(string chavePix, string nomeTitularPix, int diasAntecedenciaLembrete, int diasAposVencimentoCobranca)
    {
        ChavePix = Guard.Against.NullOrWhiteSpace(chavePix, nameof(chavePix));
        NomeTitularPix = Guard.Against.NullOrWhiteSpace(nomeTitularPix, nameof(nomeTitularPix));
        DiasAntecedenciaLembrete = Guard.Against.OutOfRange(diasAntecedenciaLembrete, nameof(diasAntecedenciaLembrete), 1, 30);
        DiasAposVencimentoCobranca = Guard.Against.OutOfRange(diasAposVencimentoCobranca, nameof(diasAposVencimentoCobranca), 1, 60);
    }
}
