using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Templates.Commands.AtualizarTemplate;

public class AtualizarTemplateCommandHandler : IRequestHandler<AtualizarTemplateCommand, Result>
{
    private readonly IMensagemTemplateRepository _repository;
    private readonly ICacheService _cacheService;

    public AtualizarTemplateCommandHandler(IMensagemTemplateRepository repository, ICacheService cacheService)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(AtualizarTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (template == null)
            return Result.NotFound("Template não encontrado.");

        var updateResult = template.AtualizarTexto(request.TextoBase);
        if (!updateResult.IsSuccess)
            return updateResult;

        await _repository.UpdateAsync(template, cancellationToken);
        
        // Invalidar cache após atualização
        _cacheService.InvalidarTemplates();
        
        return Result.Success();
    }
}
