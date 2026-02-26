using Ardalis.Result;
using BotFatura.Domain.Factories;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.ConfigurarCobranca;

public class ConfigurarCobrancaCommandHandler : IRequestHandler<ConfigurarCobrancaCommand, Result<Guid>>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IFaturaFactory _faturaFactory;

    public ConfigurarCobrancaCommandHandler(
        IFaturaRepository faturaRepository, 
        IClienteRepository clienteRepository,
        IFaturaFactory faturaFactory)
    {
        _faturaRepository = faturaRepository;
        _clienteRepository = clienteRepository;
        _faturaFactory = faturaFactory;
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

        // Usar Factory Pattern para criar a entidade Fatura
        var faturaResult = _faturaFactory.Criar(request.ClienteId, request.Valor, request.DataVencimento);
        if (!faturaResult.IsSuccess)
        {
            return Result<Guid>.Error(string.Join(", ", faturaResult.Errors));
        }
        
        await _faturaRepository.AddAsync(faturaResult.Value, cancellationToken);
        
        return Result.Success(faturaResult.Value.Id);
    }
}
