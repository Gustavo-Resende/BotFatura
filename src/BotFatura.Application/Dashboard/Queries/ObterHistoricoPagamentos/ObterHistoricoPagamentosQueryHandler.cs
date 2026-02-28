using System.Globalization;
using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Dashboard.Common;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Dashboard.Queries.ObterHistoricoPagamentos;

public class ObterHistoricoPagamentosQueryHandler 
    : IRequestHandler<ObterHistoricoPagamentosQuery, Result<List<HistoricoPagamentoDto>>>
{
    private readonly IFaturaRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ObterHistoricoPagamentosQueryHandler(IFaturaRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<List<HistoricoPagamentoDto>>> Handle(ObterHistoricoPagamentosQuery request, CancellationToken cancellationToken)
    {
        DateTime inicio, fim;

        var dataRef = request.Data ?? _dateTimeProvider.UtcNow;

        if (request.Tipo == TipoPeriodoChart.MesCompleto)
        {
            inicio = new DateTime(dataRef.Year, dataRef.Month, 1);
            fim = inicio.AddMonths(1).AddDays(-1);
        }
        else
        {
            inicio = new DateTime(dataRef.Year, 1, 1);
            fim = new DateTime(dataRef.Year, 12, 31);
        }

        var dadosBrutos = await _repository.ObterHistoricoPagamentosAsync(inicio, fim, cancellationToken);
        var result = new List<HistoricoPagamentoDto>();

        if (request.Tipo == TipoPeriodoChart.MesCompleto)
        {
            // Preencher todos os dias do mês com zero se não houver registros
            for (var d = inicio; d <= fim; d = d.AddDays(1))
            {
                var label = d.Day.ToString("D2");
                var valor = dadosBrutos.Where(x => x.Data.Date == d.Date).Sum(x => x.Total);
                result.Add(new HistoricoPagamentoDto(label, valor));
            }
        }
        else
        {
            // Preencher todos os meses do ano
            for (int m = 1; m <= 12; m++)
            {
                var label = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m);
                var valor = dadosBrutos.Where(x => x.Data.Month == m).Sum(x => x.Total);
                result.Add(new HistoricoPagamentoDto(label, valor));
            }
        }

        return Result.Success(result);
    }
}
