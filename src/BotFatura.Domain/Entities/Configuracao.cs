using Ardalis.GuardClauses;
using BotFatura.Domain.Common;

namespace BotFatura.Domain.Entities;

public class Configuracao : Entity
{
    public string ChavePix { get; private set; } = null!;
    public string NomeTitularPix { get; private set; } = null!;
    public int DiasAntecedenciaLembrete { get; private set; }

    // Para o EF
    protected Configuracao() { }

    public Configuracao(string chavePix, string nomeTitularPix, int diasAntecedenciaLembrete = 3)
    {
        ChavePix = Guard.Against.NullOrWhiteSpace(chavePix, nameof(chavePix));
        NomeTitularPix = Guard.Against.NullOrWhiteSpace(nomeTitularPix, nameof(nomeTitularPix));
        DiasAntecedenciaLembrete = Guard.Against.OutOfRange(diasAntecedenciaLembrete, nameof(diasAntecedenciaLembrete), 1, 30);
    }

    public void AtualizarConfiguracao(string chavePix, string nomeTitularPix, int diasAntecedenciaLembrete)
    {
        ChavePix = Guard.Against.NullOrWhiteSpace(chavePix, nameof(chavePix));
        NomeTitularPix = Guard.Against.NullOrWhiteSpace(nomeTitularPix, nameof(nomeTitularPix));
        DiasAntecedenciaLembrete = Guard.Against.OutOfRange(diasAntecedenciaLembrete, nameof(diasAntecedenciaLembrete), 1, 30);
    }
}
