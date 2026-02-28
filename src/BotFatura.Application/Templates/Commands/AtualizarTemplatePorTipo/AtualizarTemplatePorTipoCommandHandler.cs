using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Templates.Specifications;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Templates.Commands.AtualizarTemplatePorTipo;

public class AtualizarTemplatePorTipoCommandHandler : IRequestHandler<AtualizarTemplatePorTipoCommand, Result>
{
    private readonly IMensagemTemplateRepository _repository;
    private readonly ICacheService _cacheService;

    public AtualizarTemplatePorTipoCommandHandler(IMensagemTemplateRepository repository, ICacheService cacheService)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(AtualizarTemplatePorTipoCommand request, CancellationToken cancellationToken)
    {
        var spec = new TemplatePorTipoSpec(request.Tipo);
        var template = await _repository.FirstOrDefaultAsync(spec, cancellationToken);
        
        if (template == null)
            return Result.NotFound($"Template do tipo {request.Tipo} não encontrado.");

        var updateResult = template.AtualizarTexto(request.TextoBase);
        if (!updateResult.IsSuccess)
            return updateResult;

        await _repository.UpdateAsync(template, cancellationToken);
        
        // Invalidar cache após atualização
        _cacheService.InvalidarTemplates();
        
        return Result.Success();
    }
}
