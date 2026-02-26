using BotFatura.Domain.Entities;

namespace BotFatura.Domain.Factories;

public class LogNotificacaoFactory : ILogNotificacaoFactory
{
    public LogNotificacao Criar(Guid faturaId, string tipoNotificacao, string mensagemEnviada, string destinatario, bool sucesso, string? erro = null)
    {
        return new LogNotificacao(faturaId, tipoNotificacao, mensagemEnviada, destinatario, sucesso, erro);
    }

    public LogNotificacao CriarSucesso(Guid faturaId, string tipoNotificacao, string mensagemEnviada, string destinatario)
    {
        return Criar(faturaId, tipoNotificacao, mensagemEnviada, destinatario, sucesso: true);
    }

    public LogNotificacao CriarFalha(Guid faturaId, string tipoNotificacao, string mensagemEnviada, string destinatario, string erro)
    {
        return Criar(faturaId, tipoNotificacao, mensagemEnviada, destinatario, sucesso: false, erro);
    }
}
