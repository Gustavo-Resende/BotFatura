using Ardalis.Result;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.ConfigurarCobranca;

public class ConfigurarCobrancaCommandHandler : IRequestHandler<ConfigurarCobrancaCommand, Result<Guid>>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IClienteRepository _clienteRepository;

    public ConfigurarCobrancaCommandHandler(IFaturaRepository faturaRepository, IClienteRepository clienteRepository)
    {
        _faturaRepository = faturaRepository;
        _clienteRepository = clienteRepository;
    }

    public async Task<Result<Guid>> Handle(ConfigurarCobrancaCommand request, CancellationToken cancellationToken)
    {
        // Regra de Validação 2: Verificar Existência de Vínculo no banco (Regra de Negócio na Aplicação)
        var cliente = await _clienteRepository.GetByIdAsync(request.ClienteId, cancellationToken);
        
        if (cliente == null)
        {
            return Result.NotFound($"O Cliente informado ({request.ClienteId}) não existe no banco de dados.");
        }

        if (!cliente.Ativo)
        {
            return Result.Error("Não é possível gerar uma fatura para um cliente desativado.");
        }

        // Tenta gerar a entidade Fatura. O FluentValidator já checou se o valor e a data são lógicos.
        var fatura = new Fatura(request.ClienteId, request.Valor, request.DataVencimento);
        
        await _faturaRepository.AddAsync(fatura, cancellationToken);
        
        return Result.Success(fatura.Id);
    }
}
