using BotFatura.Application.Common.Models;
using BotFatura.Domain.Entities;

namespace BotFatura.Application.Common.Interfaces;

public interface IReguaCobrancaService
{
    IEnumerable<ReguaCobrancaItem> Processar(IEnumerable<Fatura> faturas, DateTime hoje, int diasAntecedenciaLembrete);
}
