using Ardalis.Result;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.RegistrarPagamento;

public class RegistrarPagamentoCommandHandler : IRequestHandler<RegistrarPagamentoCommand, Result>
{
    private readonly IFaturaRepository _repository;

    public RegistrarPagamentoCommandHandler(IFaturaRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RegistrarPagamentoCommand request, CancellationToken cancellationToken)
    {
        var fatura = await _repository.GetByIdAsync(request.FaturaId, cancellationToken);
        if (fatura == null)
            return Result.NotFound("Fatura não encontrada.");

        var pagarResult = fatura.MarcarComoPaga();
        if (!pagarResult.IsSuccess)
            return pagarResult;

        await _repository.UpdateAsync(fatura, cancellationToken);
        return Result.Success();
    }
}
