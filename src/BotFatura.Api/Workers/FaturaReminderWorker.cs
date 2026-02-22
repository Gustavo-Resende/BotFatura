using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotFatura.Api.Workers;

public class FaturaReminderWorker : BackgroundService
{
    private readonly ILogger<FaturaReminderWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public FaturaReminderWorker(ILogger<FaturaReminderWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FaturaReminderWorker iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker rodando: Procurando faturas pendentes...");

            try
            {
                await ProcessarFaturasPendentesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro fatal ao processar faturas pendentes.");
            }

            // Aguarda 5 minutos antes da proxima varredura (no mundo real pode ser 1 dia, ex: Task.Delay(TimeSpan.FromHours(24)))
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task ProcessarFaturasPendentesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var faturaRepository = scope.ServiceProvider.GetRequiredService<IFaturaRepository>();
        var clienteRepository = scope.ServiceProvider.GetRequiredService<IClienteRepository>();
        var evolutionApi = scope.ServiceProvider.GetRequiredService<IEvolutionApiClient>();

        // Busca todas as faturas Pendentes (Sem Specification elaborada pra simplificar o MVP)
        var faturas = await faturaRepository.ListAsync(cancellationToken);
        var faturasPendentes = faturas.Where(f => f.Status == StatusFatura.Pendente).ToList();

        if (!faturasPendentes.Any())
        {
            _logger.LogInformation("Nenhuma fatura pendente encontrada.");
            return;
        }

        foreach (var fatura in faturasPendentes)
        {
            var cliente = await clienteRepository.GetByIdAsync(fatura.ClienteId, cancellationToken);
            
            if (cliente == null || !cliente.Ativo) continue;

            string mensagem = $"Olá {cliente.NomeCompleto}, você possui uma fatura pendente no valor de R$ {fatura.Valor:F2} com vencimento para {fatura.DataVencimento:dd/MM/yyyy}.";

            var result = await evolutionApi.EnviarMensagemAsync(cliente.WhatsApp, mensagem, cancellationToken);

            if (result.IsSuccess)
            {
                var marcarResult = fatura.MarcarComoEnviada();
                if (marcarResult.IsSuccess)
                {
                    await faturaRepository.UpdateAsync(fatura, cancellationToken);
                    _logger.LogInformation($"Fatura {fatura.Id} enviada para o WhatsApp do cliente {cliente.NomeCompleto}.");
                }
            }
            else
            {
                _logger.LogWarning($"Falha ao enviar fatura {fatura.Id} para {cliente.NomeCompleto}: {string.Join(',', result.Errors)}");
            }
        }
    }
}
