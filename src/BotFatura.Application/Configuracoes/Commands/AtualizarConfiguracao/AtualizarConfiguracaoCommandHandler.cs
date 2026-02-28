using Ardalis.Result;
using Ardalis.Specification;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Configuracoes.Specifications;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Configuracoes.Commands.AtualizarConfiguracao;

public class AtualizarConfiguracaoCommandHandler : IRequestHandler<AtualizarConfiguracaoCommand, Result>
{
    private readonly IRepository<Configuracao> _repository;
    private readonly ICacheService _cacheService;

    public AtualizarConfiguracaoCommandHandler(IRepository<Configuracao> repository, ICacheService cacheService)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(AtualizarConfiguracaoCommand request, CancellationToken cancellationToken)
    {
        var spec = new ConfiguracaoUnicaSpec();
        var config = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (config == null)
        {
            config = new Configuracao(request.ChavePix, request.NomeTitularPix, request.DiasAntecedenciaLembrete, request.DiasAposVencimentoCobranca);
            config.AtualizarConfiguracao(request.ChavePix, request.NomeTitularPix, request.DiasAntecedenciaLembrete, request.DiasAposVencimentoCobranca, request.GrupoSociosWhatsAppId);
            await _repository.AddAsync(config, cancellationToken);
        }
        else
        {
            config.AtualizarConfiguracao(request.ChavePix, request.NomeTitularPix, request.DiasAntecedenciaLembrete, request.DiasAposVencimentoCobranca, request.GrupoSociosWhatsAppId);
            await _repository.UpdateAsync(config, cancellationToken);
        }

        // Invalidar cache após atualização
        _cacheService.InvalidarConfiguracao();

        return Result.Success();
    }
}
