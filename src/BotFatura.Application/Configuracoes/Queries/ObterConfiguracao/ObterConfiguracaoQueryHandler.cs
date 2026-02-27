using Ardalis.Result;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Configuracoes.Queries.ObterConfiguracao;

public class ObterConfiguracaoQueryHandler : IRequestHandler<ObterConfiguracaoQuery, Result<ConfiguracaoDto>>
{
    private readonly IRepository<Configuracao> _repository;

    public ObterConfiguracaoQueryHandler(IRepository<Configuracao> repository)
    {
        _repository = repository;
    }

    public async Task<Result<ConfiguracaoDto>> Handle(ObterConfiguracaoQuery request, CancellationToken cancellationToken)
    {
        var configs = await _repository.ListAsync(cancellationToken);
        var config = configs.FirstOrDefault();

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
