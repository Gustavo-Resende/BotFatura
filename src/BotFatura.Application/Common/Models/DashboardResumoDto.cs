namespace BotFatura.Application.Common.Models;

public record DashboardResumoDto
{
    public decimal TotalPendente { get; init; }
    public decimal TotalVencendoHoje { get; init; }
    public int ClientesAtivosCount { get; init; }
    public int FaturasPendentesCount { get; init; }
}
