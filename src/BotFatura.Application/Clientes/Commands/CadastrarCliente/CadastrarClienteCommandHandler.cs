using Ardalis.Result;
using BotFatura.Application.Clientes.Queries.ObterClientePorWhatsApp;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Clientes.Commands.CadastrarCliente;

public class CadastrarClienteCommandHandler : IRequestHandler<CadastrarClienteCommand, Result<Guid>>
{
    private readonly IClienteRepository _clienteRepository;

    public CadastrarClienteCommandHandler(IClienteRepository clienteRepository)
    {
        _clienteRepository = clienteRepository;
    }

    public async Task<Result<Guid>> Handle(CadastrarClienteCommand request, CancellationToken cancellationToken)
    {
        // Regra de Validação 2: Verificar Unicidade no banco (Regra de Negócio na Aplicação)
        var spec = new ClientePorWhatsAppSpec(request.WhatsApp);
        var existingCliente = await _clienteRepository.FirstOrDefaultAsync(spec, cancellationToken);
        
        if (existingCliente != null)
        {
            return Result.Conflict($"O WhatsApp {request.WhatsApp} já está cadastrado.");
        }

        // Tenta gerar a entidade. O pipeline do FluentValidator já cuidou das entradas vazias,
        // E o Ardalis no Repository bloqueia o conflito real.
        var cliente = new Cliente(request.NomeCompleto, request.WhatsApp);
        
        await _clienteRepository.AddAsync(cliente, cancellationToken);
        
        return Result.Success(cliente.Id);
    }
}
