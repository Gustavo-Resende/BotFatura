namespace BotFatura.Application.Common.Models;

public record DashboardResumoDto
{
    public decimal TotalPendente { get; init; }
    public decimal TotalVencendoHoje { get; init; }
    public decimal TotalPago { get; init; }
    public decimal TotalAtrasado { get; init; }
    public int ClientesAtivosCount { get; init; }
    public int FaturasPendentesCount { get; init; }
    public int FaturasAtrasadasCount { get; init; }
}
