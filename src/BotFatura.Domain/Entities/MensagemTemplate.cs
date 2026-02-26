using Ardalis.GuardClauses;
using Ardalis.Result;
using BotFatura.Domain.Common;
using BotFatura.Domain.Enums;

namespace BotFatura.Domain.Entities;

public class MensagemTemplate : Entity
{
    public string TextoBase { get; private set; } = null!;
    
    // Indica se é o template padrão do sistema
    public bool IsPadrao { get; private set; }
    
    public TipoNotificacaoTemplate TipoNotificacao { get; private set; }

    protected MensagemTemplate() { }

    public MensagemTemplate(string textoBase, TipoNotificacaoTemplate tipoNotificacao, bool isPadrao = false)
    {
        TextoBase = Guard.Against.NullOrWhiteSpace(textoBase, nameof(textoBase));
        TipoNotificacao = tipoNotificacao;
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
    
    public Result ResetarParaPadrao(string textoPadrao)
    {
        TextoBase = Guard.Against.NullOrWhiteSpace(textoPadrao, nameof(textoPadrao));
        return Result.Success();
    }
}
