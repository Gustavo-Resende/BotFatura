using Ardalis.Result;
using BotFatura.Application.Common.Strategies;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Templates.Commands.ResetarTemplatePorTipo;

public class ResetarTemplatePorTipoCommandHandler : IRequestHandler<ResetarTemplatePorTipoCommand, Result>
{
    private readonly IMensagemTemplateRepository _repository;

    public ResetarTemplatePorTipoCommandHandler(IMensagemTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ResetarTemplatePorTipoCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.ObterPorTipoAsync(request.Tipo, cancellationToken);
        
        if (template == null)
            return Result.NotFound($"Template do tipo {request.Tipo} não encontrado.");

        // Usar Strategy Pattern para obter texto padrão
        var strategy = NotificacaoStrategyFactory.Criar(request.Tipo);
        var textoPadrao = strategy.ObterTextoPadrao();

        var resetResult = template.ResetarParaPadrao(textoPadrao);
        if (!resetResult.IsSuccess)
            return resetResult;

        await _repository.UpdateAsync(template, cancellationToken);
        return Result.Success();
    }
}
