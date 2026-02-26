using BotFatura.Domain.Entities;

namespace BotFatura.Domain.Factories;

public interface ILogNotificacaoFactory
{
    LogNotificacao Criar(Guid faturaId, string tipoNotificacao, string mensagemEnviada, string destinatario, bool sucesso, string? erro = null);
    LogNotificacao CriarSucesso(Guid faturaId, string tipoNotificacao, string mensagemEnviada, string destinatario);
    LogNotificacao CriarFalha(Guid faturaId, string tipoNotificacao, string mensagemEnviada, string destinatario, string erro);
}
