using Ardalis.Result;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Configuracoes.Commands.AtualizarConfiguracao;

public class AtualizarConfiguracaoCommandHandler : IRequestHandler<AtualizarConfiguracaoCommand, Result>
{
    private readonly IRepository<Configuracao> _repository;

    public AtualizarConfiguracaoCommandHandler(IRepository<Configuracao> repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(AtualizarConfiguracaoCommand request, CancellationToken cancellationToken)
    {
        var configs = await _repository.ListAsync(cancellationToken);
        var config = configs.FirstOrDefault();

        if (config == null)
        {
            config = new Configuracao(request.ChavePix, request.NomeTitularPix);
            await _repository.AddAsync(config, cancellationToken);
        }
        else
        {
            config.AtualizarPix(request.ChavePix, request.NomeTitularPix);
            await _repository.UpdateAsync(config, cancellationToken);
        }

        return Result.Success();
    }
}
