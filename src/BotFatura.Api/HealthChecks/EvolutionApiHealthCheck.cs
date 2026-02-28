using BotFatura.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BotFatura.Api.HealthChecks;

public class EvolutionApiHealthCheck : IHealthCheck
{
    private readonly IEvolutionApiClient _evolutionApiClient;

    public EvolutionApiHealthCheck(IEvolutionApiClient evolutionApiClient)
    {
        _evolutionApiClient = evolutionApiClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statusResult = await _evolutionApiClient.ObterStatusAsync(cancellationToken);
            
            if (!statusResult.IsSuccess)
            {
                return HealthCheckResult.Unhealthy(
                    "Evolution API não está respondendo corretamente",
                    data: new Dictionary<string, object> { { "error", string.Join(", ", statusResult.Errors) } });
            }

            var status = statusResult.Value;
            var isHealthy = status == "open";

            return isHealthy
                ? HealthCheckResult.Healthy("Evolution API está conectada e operacional", 
                    data: new Dictionary<string, object> { { "status", status } })
                : HealthCheckResult.Degraded($"Evolution API está com status: {status}",
                    data: new Dictionary<string, object> { { "status", status } });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Erro ao verificar saúde da Evolution API",
                ex,
                data: new Dictionary<string, object> { { "exception", ex.Message } });
        }
    }
}
