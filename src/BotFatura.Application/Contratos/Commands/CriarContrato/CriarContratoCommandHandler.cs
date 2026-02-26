using Ardalis.Result;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Contratos.Commands.CriarContrato;

public class CriarContratoCommandHandler : IRequestHandler<CriarContratoCommand, Result<Guid>>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IContratoRepository _contratoRepository;

    public CriarContratoCommandHandler(
        IClienteRepository clienteRepository,
        IContratoRepository contratoRepository)
    {
        _clienteRepository  = clienteRepository;
        _contratoRepository = contratoRepository;
    }

    public async Task<Result<Guid>> Handle(CriarContratoCommand request, CancellationToken cancellationToken)
    {
        var cliente = await _clienteRepository.GetByIdAsync(request.ClienteId, cancellationToken);

        if (cliente is null)
            return Result.NotFound($"O cliente informado ({request.ClienteId}) não foi encontrado.");

        if (!cliente.Ativo)
            return Result.Error("Não é possível criar um contrato para um cliente desativado.");

        var contrato = new Contrato(
            request.ClienteId,
            request.ValorMensal,
            request.DiaVencimento,
            request.DataInicio,
            request.DataFim);

        await _contratoRepository.AddAsync(contrato, cancellationToken);

        return Result.Success(contrato.Id);
    }
}
