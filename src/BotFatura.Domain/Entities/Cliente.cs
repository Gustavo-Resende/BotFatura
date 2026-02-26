using Ardalis.GuardClauses;
using Ardalis.Result;
using BotFatura.Domain.Common;

namespace BotFatura.Domain.Entities;

public class Cliente : Entity
{
    public string NomeCompleto { get; private set; } = null!;
    public string WhatsApp { get; private set; } = null!;
    public bool Ativo { get; private set; }

    // Navegação (EF Core)
    private readonly List<Contrato> _contratos = [];
    public IReadOnlyCollection<Contrato> Contratos => _contratos.AsReadOnly();

    // Construtor protegido usado apenas via Factory ou EF Core
    protected Cliente() { }

    public Cliente(string nomeCompleto, string whatsApp)
    {
        NomeCompleto = Guard.Against.NullOrWhiteSpace(nomeCompleto, nameof(nomeCompleto));
        WhatsApp = Guard.Against.NullOrWhiteSpace(whatsApp, nameof(whatsApp));
        Ativo = true;
    }

    public Result AtualizarDados(string nomeCompleto, string whatsApp)
    {
        NomeCompleto = Guard.Against.NullOrWhiteSpace(nomeCompleto, nameof(nomeCompleto));
        WhatsApp = Guard.Against.NullOrWhiteSpace(whatsApp, nameof(whatsApp));
        return Result.Success();
    }

    public Result AtualizarTelefone(string novoWhatsApp)

    {
        WhatsApp = Guard.Against.NullOrWhiteSpace(novoWhatsApp, nameof(novoWhatsApp));
        return Result.Success();
    }

    public Result Desativar()
    {
        if (!Ativo) return Result.Error("Cliente já está desativado.");
        Ativo = false;
        return Result.Success();
    }

    public Result Ativar()
    {
        if (Ativo) return Result.Error("Cliente já está ativo.");
        Ativo = true;
        return Result.Success();
    }
}
