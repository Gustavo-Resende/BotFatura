using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Common.Models;
using BotFatura.Domain.Entities;

namespace BotFatura.Application.Common.Services;

public class ReguaCobrancaService : IReguaCobrancaService
{
    public IEnumerable<ReguaCobrancaItem> Processar(IEnumerable<Fatura> faturas, DateTime hoje, int diasAntecedenciaLembrete)
    {
        var resultados = new List<ReguaCobrancaItem>();

        foreach (var fatura in faturas)
        {
            // Lógica 1: Lembrete N dias antes (configurável)
            if (fatura.DataVencimento.Date == hoje.AddDays(diasAntecedenciaLembrete).Date && !fatura.Lembrete3DiasEnviado)
            {
                resultados.Add(new ReguaCobrancaItem(fatura, "Lembrete_3_Dias"));
            }
            // Lógica 2: Cobrança no dia
            else if (fatura.DataVencimento.Date == hoje.Date && !fatura.CobrancaDiaEnviada)
            {
                resultados.Add(new ReguaCobrancaItem(fatura, "Cobranca_Vencimento"));
            }
        }

        return resultados;
    }
}
