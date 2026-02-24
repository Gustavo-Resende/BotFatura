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

        var totalPendenteTask = _faturaRepository.ObterSomaPorStatusAsync(pendentesOuEnviadasStatus, cancellationToken);
        var totalVencendoHojeTask = _faturaRepository.ObterSomaVencendoHojeAsync(pendentesOuEnviadasStatus, cancellationToken);
        var totalPagoTask = _faturaRepository.ObterSomaPorStatusAsync(pagasStatus, cancellationToken);
        var totalAtrasadoTask = _faturaRepository.ObterSomaAtrasadasAsync(pendentesOuEnviadasStatus, cancellationToken);
        
        // Para simplificar, assumimos que todos os clientes cadastrados são "ativos" para o contador rápido
        // ou você pode depois implementar uma especificação para isso.
        var clientesAtivosCountTask = _clienteRepository.CountAsync(cancellationToken); 
        var faturasPendentesCountTask = _faturaRepository.ObterContagemPorStatusAsync(pendentesOuEnviadasStatus, cancellationToken);
        var faturasAtrasadasCountTask = _faturaRepository.ObterContagemAtrasadasAsync(pendentesOuEnviadasStatus, cancellationToken);

        await Task.WhenAll(
            totalPendenteTask, 
            totalVencendoHojeTask, 
            totalPagoTask, 
            totalAtrasadoTask, 
            clientesAtivosCountTask, 
            faturasPendentesCountTask, 
            faturasAtrasadasCountTask);

        return new DashboardResumoDto
        {
            TotalPendente = await totalPendenteTask,
            TotalVencendoHoje = await totalVencendoHojeTask,
            TotalPago = await totalPagoTask,
            TotalAtrasado = await totalAtrasadoTask,
            ClientesAtivosCount = await clientesAtivosCountTask,
            FaturasPendentesCount = await faturasPendentesCountTask,
            FaturasAtrasadasCount = await faturasAtrasadasCountTask
        };
    }
}
