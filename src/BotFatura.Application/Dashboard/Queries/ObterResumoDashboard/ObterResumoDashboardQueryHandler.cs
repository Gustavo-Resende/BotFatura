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
        var pendentesOuEnviadasStatus = new[] { StatusFatura.Pendente, StatusFatura.Enviada };
        var pagasStatus = new[] { StatusFatura.Paga };

        var totalPendente = await _faturaRepository.ObterSomaPorStatusAsync(pendentesOuEnviadasStatus, cancellationToken);
        var totalVencendoHoje = await _faturaRepository.ObterSomaVencendoHojeAsync(pendentesOuEnviadasStatus, cancellationToken);
        var totalPago = await _faturaRepository.ObterSomaPorStatusAsync(pagasStatus, cancellationToken);
        var totalAtrasado = await _faturaRepository.ObterSomaAtrasadasAsync(pendentesOuEnviadasStatus, cancellationToken);
        
        var clientesAtivosCount = await _clienteRepository.CountAsync(cancellationToken); 
        var faturasPendentesCount = await _faturaRepository.ObterContagemPorStatusAsync(pendentesOuEnviadasStatus, cancellationToken);
        var faturasAtrasadasCount = await _faturaRepository.ObterContagemAtrasadasAsync(pendentesOuEnviadasStatus, cancellationToken);

        return new DashboardResumoDto
        {
            TotalPendente = totalPendente,
            TotalVencendoHoje = totalVencendoHoje,
            TotalPago = totalPago,
            TotalAtrasado = totalAtrasado,
            ClientesAtivosCount = clientesAtivosCount,
            FaturasPendentesCount = faturasPendentesCount,
            FaturasAtrasadasCount = faturasAtrasadasCount
        };
    }
}
