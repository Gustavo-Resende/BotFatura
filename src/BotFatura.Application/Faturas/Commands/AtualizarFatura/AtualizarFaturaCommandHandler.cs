using Ardalis.Result;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.AtualizarFatura;

public class AtualizarFaturaCommandHandler : IRequestHandler<AtualizarFaturaCommand, Result>
{
    private readonly IFaturaRepository _faturaRepository;

    public AtualizarFaturaCommandHandler(IFaturaRepository faturaRepository)
    {
        _faturaRepository = faturaRepository;
    }

    public async Task<Result> Handle(AtualizarFaturaCommand request, CancellationToken cancellationToken)
    {
        var fatura = await _faturaRepository.GetByIdAsync(request.FaturaId, cancellationToken);

        if (fatura is null)
            return Result.NotFound($"Fatura ({request.FaturaId}) não encontrada.");

        var resultado = fatura.AtualizarDados(request.Valor, request.DataVencimento);
        if (!resultado.IsSuccess)
            return resultado;

        await _faturaRepository.UpdateAsync(fatura, cancellationToken);
        return Result.Success();
    }
}
