using Ardalis.Result;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Clientes.Commands.ExcluirCliente;

public class ExcluirClienteCommandHandler : IRequestHandler<ExcluirClienteCommand, Result>
{
    private readonly IClienteRepository _repository;

    public ExcluirClienteCommandHandler(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ExcluirClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (cliente == null)
            return Result.NotFound("Cliente não encontrado.");

        var desativarResult = cliente.Desativar();
        if (!desativarResult.IsSuccess)
            return desativarResult;

        await _repository.UpdateAsync(cliente, cancellationToken);
        return Result.Success();
    }
}
