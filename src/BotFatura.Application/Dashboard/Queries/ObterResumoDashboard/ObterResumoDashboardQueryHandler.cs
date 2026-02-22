using BotFatura.Application.Common.Models;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Dashboard.Queries.ObterResumoDashboard;

public class ObterResumoDashboardQueryHandler : IRequestHandler<ObterResumoDashboardQuery, DashboardResumoDto>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IClienteRepository _clienteRepository;

    public ObterResumoDashboardQueryHandler(IFaturaRepository faturaRepository, IClienteRepository clienteRepository)
    {
        _faturaRepository = faturaRepository;
        _clienteRepository = clienteRepository;
    }

    public async Task<DashboardResumoDto> Handle(ObterResumoDashboardQuery request, CancellationToken cancellationToken)
    {
        var hoje = DateTime.UtcNow.Date;

        var faturas = await _faturaRepository.ListAsync(cancellationToken);
        var clientes = await _clienteRepository.ListAsync(cancellationToken);

        var pendentes = faturas.Where(f => f.Status == StatusFatura.Pendente).ToList();

        return new DashboardResumoDto
        {
            TotalPendente = pendentes.Sum(f => f.Valor),
            TotalVencendoHoje = pendentes.Where(f => f.DataVencimento.Date == hoje).Sum(f => f.Valor),
            ClientesAtivosCount = clientes.Count(c => c.Ativo),
            FaturasPendentesCount = pendentes.Count
        };
    }
}
