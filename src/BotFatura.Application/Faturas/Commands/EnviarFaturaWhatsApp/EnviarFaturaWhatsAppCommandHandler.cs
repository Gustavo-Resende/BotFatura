using Ardalis.Result;
using BotFatura.Application.Common.Services;
using BotFatura.Application.Common.Strategies;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.EnviarFaturaWhatsApp;

public class EnviarFaturaWhatsAppCommandHandler : IRequestHandler<EnviarFaturaWhatsAppCommand, Result>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly ManualNotificacaoProcessor _notificacaoProcessor;

    public EnviarFaturaWhatsAppCommandHandler(
        IFaturaRepository faturaRepository,
        ManualNotificacaoProcessor notificacaoProcessor)
    {
        _faturaRepository = faturaRepository;
        _notificacaoProcessor = notificacaoProcessor;
    }

    public async Task<Result> Handle(EnviarFaturaWhatsAppCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar existência da fatura
        var fatura = await _faturaRepository.GetByIdAsync(request.FaturaId, cancellationToken);
        if (fatura == null)
            return Result.NotFound("Fatura não encontrada.");

        // 2. Usar Template Method Pattern para processar notificação manual
        var strategy = new ManualStrategy();
        var result = await _notificacaoProcessor.ProcessarAsync(fatura, strategy, cancellationToken);

        return result;
    }
}
