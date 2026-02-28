using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using BotFatura.Application.Faturas.Queries;
using MediatR;

namespace BotFatura.Application.Dashboard.Queries.ObterClientesAtrasados;

public record ObterClientesAtrasadosQuery : IRequest<IEnumerable<ClienteAtrasadoDto>>;

public class ObterClientesAtrasadosQueryHandler : IRequestHandler<ObterClientesAtrasadosQuery, IEnumerable<ClienteAtrasadoDto>>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ObterClientesAtrasadosQueryHandler(IFaturaRepository faturaRepository, IDateTimeProvider dateTimeProvider)
    {
        _faturaRepository = faturaRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IEnumerable<ClienteAtrasadoDto>> Handle(ObterClientesAtrasadosQuery request, CancellationToken cancellationToken)
    {
        var hoje = _dateTimeProvider.Today;
        var spec = new FaturasParaNotificarSpec();
        var faturas = await _faturaRepository.ListAsync(spec, cancellationToken);
        
        var faturasAtrasadas = faturas
            .Where(f => f.Status == StatusFatura.Pendente && f.DataVencimento.Date < hoje)
            .GroupBy(f => f.ClienteId)
            .ToList();

        var result = new List<ClienteAtrasadoDto>();

        foreach (var grupo in faturasAtrasadas)
        {
            var cliente = grupo.First().Cliente;
            if (cliente == null || !cliente.Ativo) continue;

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
