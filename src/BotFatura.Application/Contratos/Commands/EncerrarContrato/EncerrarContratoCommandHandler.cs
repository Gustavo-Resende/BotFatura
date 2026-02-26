using Ardalis.Result;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Contratos.Commands.EncerrarContrato;

public class EncerrarContratoCommandHandler : IRequestHandler<EncerrarContratoCommand, Result>
{
    private readonly IContratoRepository _contratoRepository;

    public EncerrarContratoCommandHandler(IContratoRepository contratoRepository)
    {
        _contratoRepository = contratoRepository;
    }

    public async Task<Result> Handle(EncerrarContratoCommand request, CancellationToken cancellationToken)
    {
        var contrato = await _contratoRepository.GetByIdAsync(request.ContratoId, cancellationToken);

        if (contrato is null)
            return Result.NotFound($"O contrato informado ({request.ContratoId}) não foi encontrado.");

        var resultado = contrato.Encerrar();
        if (!resultado.IsSuccess)
            return resultado;

        await _contratoRepository.UpdateAsync(contrato, cancellationToken);
        return Result.Success();
    }
}
