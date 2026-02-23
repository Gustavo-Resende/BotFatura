using Ardalis.GuardClauses;
using BotFatura.Domain.Common;

namespace BotFatura.Domain.Entities;

public class Configuracao : Entity
{
    public string ChavePix { get; private set; } = null!;
    public string NomeTitularPix { get; private set; } = null!;

    // Para o EF
    protected Configuracao() { }

    public Configuracao(string chavePix, string nomeTitularPix)
    {
        ChavePix = Guard.Against.NullOrWhiteSpace(chavePix, nameof(chavePix));
        NomeTitularPix = Guard.Against.NullOrWhiteSpace(nomeTitularPix, nameof(nomeTitularPix));
    }

    public void AtualizarPix(string chavePix, string nomeTitularPix)
    {
        ChavePix = Guard.Against.NullOrWhiteSpace(chavePix, nameof(chavePix));
        NomeTitularPix = Guard.Against.NullOrWhiteSpace(nomeTitularPix, nameof(nomeTitularPix));
    }
}
