namespace BotFatura.Application.Dashboard.Queries.ObterClientesAtrasados;

public record ClienteAtrasadoDto
{
    public Guid ClienteId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string WhatsApp { get; init; } = string.Empty;
    public int FaturasAtrasadas { get; init; }
    public decimal ValorTotalAtrasado { get; init; }
}
