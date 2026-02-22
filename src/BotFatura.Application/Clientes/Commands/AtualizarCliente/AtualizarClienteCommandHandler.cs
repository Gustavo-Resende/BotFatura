using Ardalis.Result;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Clientes.Commands.AtualizarCliente;

public class AtualizarClienteCommandHandler : IRequestHandler<AtualizarClienteCommand, Result>
{
    private readonly IClienteRepository _repository;

    public AtualizarClienteCommandHandler(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(AtualizarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (cliente == null)
            return Result.NotFound("Cliente não encontrado.");

        var updateResult = cliente.AtualizarDados(request.NomeCompleto, request.WhatsApp);
        if (!updateResult.IsSuccess)
            return updateResult;

        await _repository.UpdateAsync(cliente, cancellationToken);
        return Result.Success();
    }
}
