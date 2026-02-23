using BotFatura.Domain.Common;

namespace BotFatura.Domain.Entities;

public class LogNotificacao : Entity
{
    public Guid FaturaId { get; private set; }
    public string TipoNotificacao { get; private set; } = null!; // Ex: "Lembrete_3_Dias", "Cobranca_Vencimento", "Manual"
    public string MensagemEnviada { get; private set; } = null!;
    public string Destinatario { get; private set; } = null!;
    public bool Sucesso { get; private set; }
    public string? Erro { get; private set; }

    protected LogNotificacao() { }

    public LogNotificacao(Guid faturaId, string tipoNotificacao, string mensagemEnviada, string destinatario, bool sucesso, string? erro = null)
    {
        FaturaId = faturaId;
        TipoNotificacao = tipoNotificacao;
        MensagemEnviada = mensagemEnviada;
        Destinatario = destinatario;
        Sucesso = sucesso;
        Erro = erro;
    }
}
