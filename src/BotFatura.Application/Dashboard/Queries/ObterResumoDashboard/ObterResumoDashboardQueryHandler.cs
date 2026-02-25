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
        var dadosFaturas = await _faturaRepository.ObterDadosConsolidadosDashboardAsync(cancellationToken);
        var clientesAtivosCount = await _clienteRepository.CountAsync(cancellationToken);

        return new DashboardResumoDto
        {
            TotalPendente = dadosFaturas.TotalPendente,
            TotalVencendoHoje = dadosFaturas.TotalVencendoHoje,
            TotalPago = dadosFaturas.TotalPago,
            TotalAtrasado = dadosFaturas.TotalAtrasado,
            ClientesAtivosCount = clientesAtivosCount,
            FaturasPendentesCount = dadosFaturas.FaturasPendentesCount,
            FaturasAtrasadasCount = dadosFaturas.FaturasAtrasadasCount
        };
    }
}
