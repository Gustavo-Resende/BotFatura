using Ardalis.Result;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.CancelarFatura;

public class CancelarFaturaCommandHandler : IRequestHandler<CancelarFaturaCommand, Result>
{
    private readonly IFaturaRepository _repository;

    public CancelarFaturaCommandHandler(IFaturaRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(CancelarFaturaCommand request, CancellationToken cancellationToken)
    {
        var fatura = await _repository.GetByIdAsync(request.FaturaId, cancellationToken);
        if (fatura == null)
            return Result.NotFound("Fatura não encontrada.");

        var cancelarResult = fatura.Cancelar();
        if (!cancelarResult.IsSuccess)
            return cancelarResult;

        await _repository.UpdateAsync(fatura, cancellationToken);
        return Result.Success();
    }
}
