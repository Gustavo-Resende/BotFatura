using Ardalis.GuardClauses;
using Ardalis.Result;
using BotFatura.Domain.Common;

namespace BotFatura.Domain.Entities;

public class Cliente : Entity
{
    public string NomeCompleto { get; private set; } = null!;
    public string WhatsApp { get; private set; } = null!;
    
    /// <summary>
    /// JID completo do WhatsApp (Evolution API).
    /// Exemplo: "5571987699693@s.whatsapp.net" ou "154687642832914@lid"
    /// </summary>
    public string? WhatsAppJid { get; private set; }
    
    public bool Ativo { get; private set; }

    // Navegação (EF Core)
    private readonly List<Contrato> _contratos = [];
    public IReadOnlyCollection<Contrato> Contratos => _contratos.AsReadOnly();

    // Construtor protegido usado apenas via Factory ou EF Core
    protected Cliente() { }

    public Cliente(string nomeCompleto, string whatsApp, string? whatsAppJid = null)
    {
        NomeCompleto = Guard.Against.NullOrWhiteSpace(nomeCompleto, nameof(nomeCompleto));
        WhatsApp = Guard.Against.NullOrWhiteSpace(whatsApp, nameof(whatsApp));
        WhatsAppJid = whatsAppJid;
        Ativo = true;
    }

    public Result AtualizarDados(string nomeCompleto, string whatsApp, string? whatsAppJid = null)
    {
        NomeCompleto = Guard.Against.NullOrWhiteSpace(nomeCompleto, nameof(nomeCompleto));
        WhatsApp = Guard.Against.NullOrWhiteSpace(whatsApp, nameof(whatsApp));
        WhatsAppJid = whatsAppJid;
        return Result.Success();
    }

    public Result AtualizarTelefone(string novoWhatsApp)
    {
        WhatsApp = Guard.Against.NullOrWhiteSpace(novoWhatsApp, nameof(novoWhatsApp));
        return Result.Success();
    }

    /// <summary>
    /// Atualiza o JID do WhatsApp (identificador único do Evolution API).
    /// Este método é chamado automaticamente quando o cliente envia uma mensagem.
    /// </summary>
    public Result AtualizarWhatsAppJid(string novoJid)
    {
        if (string.IsNullOrWhiteSpace(novoJid))
            return Result.Error("JID não pode ser vazio.");

        WhatsAppJid = novoJid;
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
