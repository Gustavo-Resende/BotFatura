using Ardalis.Result;
using BotFatura.Application.Dashboard.Common;
using MediatR;

namespace BotFatura.Application.Dashboard.Queries.ObterHistoricoPagamentos;

public enum TipoPeriodoChart
{
    MesCompleto = 1,
    AnoCompleto = 2
}

public record ObterHistoricoPagamentosQuery(TipoPeriodoChart Tipo = TipoPeriodoChart.MesCompleto, DateTime? Data = null) 
    : IRequest<Result<List<HistoricoPagamentoDto>>>;
