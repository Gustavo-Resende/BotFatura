using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Comprovantes.Commands.ProcessarComprovante;

public record ProcessarComprovanteCommand(
    Guid ClienteId,
    byte[] Arquivo,
    string MimeType,
    string NumeroWhatsApp,
    string JidOriginal, // JID completo do WhatsApp (ex: 123@lid ou 123@s.whatsapp.net)
    DateTime DataEnvioMensagemFatura
) : IRequest<Result<ProcessarComprovanteResult>>;

public record ProcessarComprovanteResult(
    bool Sucesso,
    Guid? FaturaId,
    string Mensagem
);
