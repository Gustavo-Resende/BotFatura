using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Dashboard.Queries.ObterClientesAtrasados;

public record ObterClientesAtrasadosQuery : IRequest<IEnumerable<ClienteAtrasadoDto>>;

public class ObterClientesAtrasadosQueryHandler : IRequestHandler<ObterClientesAtrasadosQuery, IEnumerable<ClienteAtrasadoDto>>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IClienteRepository _clienteRepository;

    public ObterClientesAtrasadosQueryHandler(IFaturaRepository faturaRepository, IClienteRepository clienteRepository)
    {
        _faturaRepository = faturaRepository;
        _clienteRepository = clienteRepository;
    }

    public async Task<IEnumerable<ClienteAtrasadoDto>> Handle(ObterClientesAtrasadosQuery request, CancellationToken cancellationToken)
    {
        var hoje = DateTime.UtcNow.Date;
        var faturas = await _faturaRepository.ListAsync(cancellationToken);
        
        var faturasAtrasadas = faturas
            .Where(f => f.Status == StatusFatura.Pendente && f.DataVencimento.Date < hoje)
            .GroupBy(f => f.ClienteId)
            .ToList();

        var result = new List<ClienteAtrasadoDto>();

        foreach (var grupo in faturasAtrasadas)
        {
            var cliente = await _clienteRepository.GetByIdAsync(grupo.Key, cancellationToken);
            if (cliente == null) continue;

            result.Add(new ClienteAtrasadoDto
            {
                ClienteId = cliente.Id,
                Nome = cliente.NomeCompleto,
                WhatsApp = cliente.WhatsApp,
                FaturasAtrasadas = grupo.Count(),
                ValorTotalAtrasado = grupo.Sum(f => f.Valor)
            });
        }

        return result;
    }
}
