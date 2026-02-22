using Ardalis.GuardClauses;
using Ardalis.Result;
using BotFatura.Domain.Common;

namespace BotFatura.Domain.Entities;

public class MensagemTemplate : Entity
{
    public string TextoBase { get; private set; } = null!;
    
    // Indica se é o template padrão do sistema
    public bool IsPadrao { get; private set; }

    protected MensagemTemplate() { }

    public MensagemTemplate(string textoBase, bool isPadrao = false)
    {
        TextoBase = Guard.Against.NullOrWhiteSpace(textoBase, nameof(textoBase));
        IsPadrao = isPadrao;
    }

    public Result AtualizarTexto(string novoTexto)
    {
        TextoBase = Guard.Against.NullOrWhiteSpace(novoTexto, nameof(novoTexto));
        return Result.Success();
    }
    
    public Result DefinirComoPadrao()
    {
        IsPadrao = true;
        return Result.Success();
    }
}
