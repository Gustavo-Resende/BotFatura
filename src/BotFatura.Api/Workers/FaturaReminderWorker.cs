using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Common.Services;
using BotFatura.Application.Common.Strategies;
using BotFatura.Application.Contratos.Commands.GerarFaturasDoContrato;
using BotFatura.Application.Faturas.Queries;
using BotFatura.Application.Configuracoes.Queries.ObterConfiguracao;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using MediatR;
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
        _logger          = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FaturaReminderWorker iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
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

        var faturaRepository   = scope.ServiceProvider.GetRequiredService<IFaturaRepository>();
        var mediator           = scope.ServiceProvider.GetRequiredService<ISender>();
        var notificacaoProcessor = scope.ServiceProvider.GetRequiredService<AutomaticaNotificacaoProcessor>();

        // Passo 0: Gerar faturas de contratos recorrentes com antecedência de 3 dias.
        // Deve ocorrer antes da régua para que as faturas recém-geradas entrem no mesmo ciclo de notificação.
        try
        {
            await mediator.Send(new GerarFaturasDoContratoCommand(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar faturas de contratos recorrentes.");
        }

        // Passo 1: Verificar se o WhatsApp está conectado antes de qualquer envio.
        var evolutionApi = scope.ServiceProvider.GetRequiredService<IEvolutionApiClient>();
        var statusResult = await evolutionApi.ObterStatusAsync(cancellationToken);
        if (!statusResult.IsSuccess || statusResult.Value != "open") return;

        var reguaService = scope.ServiceProvider.GetRequiredService<IReguaCobrancaService>();

        // Passo 1.1: Obter configuracao global para saber quantos dias antes enviar o lembrete
        //            e quantos dias após o vencimento enviar a cobrança de atraso.
        var configResult = await mediator.Send(new ObterConfiguracaoQuery(), cancellationToken);
        var diasAntecedenciaLembrete = configResult.Value?.DiasAntecedenciaLembrete > 0
            ? configResult.Value.DiasAntecedenciaLembrete
            : 3;
        var diasAposVencimentoCobranca = configResult.Value?.DiasAposVencimentoCobranca > 0
            ? configResult.Value.DiasAposVencimentoCobranca
            : 7;

        // Passo 2: Processar faturas em batches para evitar sobrecarga de memória
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        const int batchSize = 50;
        int skip = 0;
        int totalProcessadas = 0;

        while (true)
        {
            var specPaginada = new FaturasParaNotificarPaginadaSpec(skip, batchSize);
            var faturasBatch = await faturaRepository.ListAsync(specPaginada, cancellationToken);

            if (!faturasBatch.Any())
                break;

            // Passo 3: Processar Régua de Cobrança para este batch
            var itensParaNotificar = reguaService.Processar(faturasBatch, dateTimeProvider.Today, diasAntecedenciaLembrete, diasAposVencimentoCobranca);

            foreach (var item in itensParaNotificar)
            {
                var fatura          = item.Fatura;
                var tipoNotificacao = item.TipoNotificacao;

                // Usar Strategy Pattern para mapear tipo de notificação
                INotificacaoStrategy strategy;
                try
                {
                    strategy = NotificacaoStrategyFactory.CriarPorString(tipoNotificacao);
                }
                catch (ArgumentException)
                {
                    _logger.LogWarning($"Tipo de notificação inválido: {tipoNotificacao}. Usando Vencimento como fallback.");
                    strategy = NotificacaoStrategyFactory.Criar(TipoNotificacaoTemplate.Vencimento);
                }

                // Usar Template Method Pattern para processar notificação
                var result = await notificacaoProcessor.ProcessarAsync(fatura, strategy, cancellationToken);
                
                if (!result.IsSuccess)
                {
                    _logger.LogWarning($"Erro ao processar notificação para fatura {fatura.Id}: {string.Join(", ", result.Errors)}");
                }
                else
                {
                    totalProcessadas++;
                }
            }

            skip += batchSize;

            // Se retornou menos que o batch size, chegamos ao fim
            if (faturasBatch.Count < batchSize)
                break;
        }

        _logger.LogInformation("Processamento da régua de cobrança concluído. {Total} notificação(ões) processada(s).", totalProcessadas);
    }
}
