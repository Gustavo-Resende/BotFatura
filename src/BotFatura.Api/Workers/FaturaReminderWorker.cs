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
            // Processamento diário (simulado para rodar a cada 1 hora para testes, mas lógica baseada em datas)
            _logger.LogInformation("Iniciando processamento da régua de cobrança...");

            try
            {
                await ProcessarReguaCobrancaAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar régua de cobrança.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ProcessarReguaCobrancaAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var faturaRepository = scope.ServiceProvider.GetRequiredService<IFaturaRepository>();
        var clienteRepository = scope.ServiceProvider.GetRequiredService<IClienteRepository>();
        var evolutionApi = scope.ServiceProvider.GetRequiredService<IEvolutionApiClient>();
        var templateRepository = scope.ServiceProvider.GetRequiredService<IMensagemTemplateRepository>();
        var formatter = scope.ServiceProvider.GetRequiredService<IMensagemFormatter>();
        var logRepository = scope.ServiceProvider.GetRequiredService<IRepository<LogNotificacao>>();

        // 1. Verificar Status WhatsApp
        var statusResult = await evolutionApi.ObterStatusAsync(cancellationToken);
        if (!statusResult.IsSuccess || statusResult.Value != "open") return;

        // 2. Buscar faturas pendentes ou enviadas (que ainda não foram pagas)
        var todasFaturas = await faturaRepository.ListAsync(cancellationToken);
        var faturasAtivas = todasFaturas.Where(f => f.Status == StatusFatura.Pendente || f.Status == StatusFatura.Enviada).ToList();

        // 3. Buscar Template
        var templates = await templateRepository.ListAsync(cancellationToken);
        var template = templates.FirstOrDefault(t => t.IsPadrao) ?? templates.FirstOrDefault();
        if (template == null) return;

        var hoje = DateTime.Today;
        var random = new Random();

        foreach (var fatura in faturasAtivas)
        {
            bool deveEnviar = false;
            string tipoNotificacao = "";

            // Lógica 1: Lembrete 3 dias antes
            if (fatura.DataVencimento.Date == hoje.AddDays(3) && !fatura.Lembrete3DiasEnviado)
            {
                deveEnviar = true;
                tipoNotificacao = "Lembrete_3_Dias";
            }
            // Lógica 2: Cobrança no dia
            else if (fatura.DataVencimento.Date == hoje && !fatura.CobrancaDiaEnviada)
            {
                deveEnviar = true;
                tipoNotificacao = "Cobranca_Vencimento";
            }

            if (deveEnviar)
            {
                var cliente = await clienteRepository.GetByIdAsync(fatura.ClienteId, cancellationToken);
                if (cliente == null || !cliente.Ativo) continue;

                // Anti-ban delay
                await Task.Delay(random.Next(5000, 15000), cancellationToken);

                string mensagem = await formatter.FormatarMensagemAsync(template.TextoBase, cliente, fatura, cancellationToken);
                var sendResult = await evolutionApi.EnviarMensagemAsync(cliente.WhatsApp, mensagem, cancellationToken);

                // Registrar Log de Auditoria
                var log = new LogNotificacao(
                    fatura.Id,
                    tipoNotificacao,
                    mensagem,
                    cliente.WhatsApp,
                    sendResult.IsSuccess,
                    sendResult.IsSuccess ? null : string.Join(", ", sendResult.Errors)
                );
                await logRepository.AddAsync(log, cancellationToken);

                if (sendResult.IsSuccess)
                {
                    if (tipoNotificacao == "Lembrete_3_Dias") fatura.MarcarLembreteEnviado();
                    if (tipoNotificacao == "Cobranca_Vencimento") fatura.MarcarCobrancaDiaEnviada();
                    
                    fatura.MarcarComoEnviada(); // Atualiza o status geral também
                    await faturaRepository.UpdateAsync(fatura, cancellationToken);
                }
            }
        }
    }
}
