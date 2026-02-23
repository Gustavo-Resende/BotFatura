using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Entities;
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

            // Aguarda 5 minutos antes da proxima varredura
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task ProcessarFaturasPendentesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var faturaRepository = scope.ServiceProvider.GetRequiredService<IFaturaRepository>();
        var clienteRepository = scope.ServiceProvider.GetRequiredService<IClienteRepository>();
        var evolutionApi = scope.ServiceProvider.GetRequiredService<IEvolutionApiClient>();
        var templateRepository = scope.ServiceProvider.GetRequiredService<IMensagemTemplateRepository>();
        var formatter = scope.ServiceProvider.GetRequiredService<IMensagemFormatter>();

        // 1. Verificar Status da Instância antes de tudo
        var statusResult = await evolutionApi.ObterStatusAsync(cancellationToken);
        if (!statusResult.IsSuccess || statusResult.Value != "open")
        {
            _logger.LogWarning($"[WHATSAPP] Os disparos foram ignorados porque a instância não está conectada (Status atual: {statusResult.Value ?? "Não Encontrada"}).");
            return;
        }

        // 2. Busca todas as faturas Pendentes
        var faturas = await faturaRepository.ListAsync(cancellationToken);
        var faturasPendentes = faturas.Where(f => f.Status == StatusFatura.Pendente).ToList();

        if (!faturasPendentes.Any())
        {
            _logger.LogInformation("Nenhuma fatura pendente encontrada.");
            return;
        }

        // 3. Buscar Template Padrão
        var templates = await templateRepository.ListAsync(cancellationToken);
        var template = templates.FirstOrDefault(t => t.IsPadrao) 
            ?? new MensagemTemplate("Olá {NomeCliente}, sua fatura de R$ {Valor} vence em {Vencimento}. PIX: {ChavePix}", true);

        var random = new Random();
        foreach (var fatura in faturasPendentes)
        {
            // Proteção Anti-Ban: Delay aleatório entre 10 e 25 segundos
            var delay = random.Next(10000, 25000);
            _logger.LogInformation($"[ANTI-BAN] Aguardando {delay/1000}s antes de enviar fatura {fatura.Id}...");
            await Task.Delay(delay, cancellationToken);

            var cliente = await clienteRepository.GetByIdAsync(fatura.ClienteId, cancellationToken);
            
            if (cliente == null || !cliente.Ativo) continue;

            string mensagem = await formatter.FormatarMensagemAsync(template.TextoBase, cliente, fatura, cancellationToken);

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
