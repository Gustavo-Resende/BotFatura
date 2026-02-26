using Ardalis.Result;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BotFatura.Application.Contratos.Commands.GerarFaturasDoContrato;

public class GerarFaturasDoContratoCommandHandler : IRequestHandler<GerarFaturasDoContratoCommand, Result>
{
    private readonly IContratoRepository _contratoRepository;
    private readonly IFaturaRepository _faturaRepository;
    private readonly ILogger<GerarFaturasDoContratoCommandHandler> _logger;

    public GerarFaturasDoContratoCommandHandler(
        IContratoRepository contratoRepository,
        IFaturaRepository   faturaRepository,
        ILogger<GerarFaturasDoContratoCommandHandler> logger)
    {
        _contratoRepository = contratoRepository;
        _faturaRepository   = faturaRepository;
        _logger             = logger;
    }

    public async Task<Result> Handle(GerarFaturasDoContratoCommand request, CancellationToken cancellationToken)
    {
        // A data de referência é hoje + 3 dias, gerando a fatura com antecedência para o envio da régua.
        var dataReferencia = request.DataReferencia ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

        var contratosVigentes = await _contratoRepository
            .ListarVigentesParaGerarFaturaAsync(dataReferencia, cancellationToken);

        if (contratosVigentes.Count == 0)
        {
            _logger.LogInformation("Nenhum contrato vigente encontrado para a data {DataReferencia}.", dataReferencia);
            return Result.Success();
        }

        var statusAbertos = new[] { StatusFatura.Pendente, StatusFatura.Enviada };
        var faturasGeradas = 0;

        foreach (var contrato in contratosVigentes)
        {
            var vencimento = contrato.CalcularVencimentoDoMes(dataReferencia.Year, dataReferencia.Month);

            // Garantia de Idempotência: verifica se já existe fatura deste contrato para o mês/ano de referência.
            // A query filtra diretamente em memória a partir do navegado (já carregado via Include no repo).
            var jaExisteFaturaNoMes = contrato.Faturas.Any(f =>
                f.ContratoId == contrato.Id &&
                f.DataVencimento.Year  == vencimento.Year &&
                f.DataVencimento.Month == vencimento.Month);

            if (jaExisteFaturaNoMes)
            {
                _logger.LogDebug(
                    "Fatura já existente para contrato {ContratoId} no mês {Mes}/{Ano}. Ignorando.",
                    contrato.Id, vencimento.Month, vencimento.Year);
                continue;
            }

            // Converte DateOnly → DateTime UTC (meia-noite do dia de vencimento).
            var dataVencimentoUtc = vencimento.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var fatura = new Fatura(
                contrato.ClienteId,
                contrato.ValorMensal,
                dataVencimentoUtc,
                contratoId: contrato.Id);

            await _faturaRepository.AddAsync(fatura, cancellationToken);
            faturasGeradas++;

            _logger.LogInformation(
                "Fatura de R$ {Valor} gerada para contrato {ContratoId} (Cliente: {ClienteId}), vencimento {Vencimento}.",
                contrato.ValorMensal, contrato.Id, contrato.ClienteId, vencimento);
        }

        _logger.LogInformation("{Quantidade} fatura(s) gerada(s) pelo ciclo de recorrência.", faturasGeradas);
        return Result.Success();
    }
}
