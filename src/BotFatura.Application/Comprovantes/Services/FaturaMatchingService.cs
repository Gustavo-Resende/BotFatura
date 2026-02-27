using Ardalis.Specification;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;

namespace BotFatura.Application.Comprovantes.Services;

public class FaturaMatchingService
{
    private readonly IFaturaRepository _faturaRepository;

    public FaturaMatchingService(IFaturaRepository faturaRepository)
    {
        _faturaRepository = faturaRepository;
    }

    public async Task<Fatura?> EncontrarFaturaCorrespondenteAsync(
        Guid clienteId, 
        decimal valorComprovante, 
        DateTime dataEnvioMensagemFatura,
        CancellationToken cancellationToken = default)
    {
        // Buscar todas as faturas pendentes ou enviadas do cliente com Cliente incluído
        var spec = new FaturasParaMatchingSpec(clienteId);
        var faturas = await _faturaRepository.ListAsync(spec, cancellationToken);

        // Filtrar faturas com valor correspondente (tolerância de R$ 0,01)
        var faturasComValorCorrespondente = faturas
            .Where(f => Math.Abs(f.Valor - valorComprovante) <= 0.01m)
            .ToList();

        if (!faturasComValorCorrespondente.Any())
        {
            return null;
        }

        // Filtrar faturas que estavam vencidas ou vencendo no dia do envio da mensagem de fatura
        // Consideramos uma margem de 3 dias antes e depois da data de vencimento
        var faturasVencidasOuVencendo = faturasComValorCorrespondente
            .Where(f => 
                f.DataVencimento.Date <= dataEnvioMensagemFatura.Date.AddDays(3) &&
                f.DataVencimento.Date >= dataEnvioMensagemFatura.Date.AddDays(-3))
            .OrderByDescending(f => f.DataVencimento) // Priorizar a mais recente
            .ToList();

        // Se encontrar exatamente 1 fatura, retornar
        if (faturasVencidasOuVencendo.Count == 1)
        {
            return faturasVencidasOuVencendo.First();
        }

        // Se não encontrar nenhuma na margem, tentar com a mais próxima do valor
        if (!faturasVencidasOuVencendo.Any() && faturasComValorCorrespondente.Any())
        {
            return faturasComValorCorrespondente
                .OrderByDescending(f => f.DataVencimento)
                .First();
        }

        // Se encontrar múltiplas, retornar null (precisa de intervenção manual)
        return null;
    }

    // Specification para buscar faturas com Cliente incluído
    private class FaturasParaMatchingSpec : Specification<Fatura>
    {
        public FaturasParaMatchingSpec(Guid clienteId)
        {
            Query
                .Where(f => f.ClienteId == clienteId 
                    && (f.Status == StatusFatura.Pendente || f.Status == StatusFatura.Enviada))
                .Include(f => f.Cliente);
        }
    }
}
