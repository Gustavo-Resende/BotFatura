using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Configuracoes.Queries.ObterConfiguracao;

public class ObterConfiguracaoQueryHandler : IRequestHandler<ObterConfiguracaoQuery, Result<ConfiguracaoDto>>
{
    private readonly ICacheService _cacheService;

    public ObterConfiguracaoQueryHandler(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<Result<ConfiguracaoDto>> Handle(ObterConfiguracaoQuery request, CancellationToken cancellationToken)
    {
        var config = await _cacheService.ObterConfiguracaoAsync(cancellationToken);

        if (config == null)
            return Result.Success(new ConfiguracaoDto("", "", 3, 7, null));

        return Result.Success(new ConfiguracaoDto(
            config.ChavePix,
            config.NomeTitularPix,
            config.DiasAntecedenciaLembrete,
            config.DiasAposVencimentoCobranca,
            config.GrupoSociosWhatsAppId));
    }
}
