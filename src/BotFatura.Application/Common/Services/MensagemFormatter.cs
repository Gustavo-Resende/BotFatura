using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;

namespace BotFatura.Application.Common.Services;

public class MensagemFormatter : IMensagemFormatter
{
    private readonly IRepository<Configuracao> _configRepository;

    public MensagemFormatter(IRepository<Configuracao> configRepository)
    {
        _configRepository = configRepository;
    }

    public async Task<string> FormatarMensagemAsync(string template, Cliente cliente, Fatura fatura, CancellationToken cancellationToken = default)
    {
        var configs = await _configRepository.ListAsync(cancellationToken);
        var config = configs.FirstOrDefault();

        var nomeDono = config?.NomeTitularPix ?? "[NOME NÃO CONFIGURADO]";
        var chavePix = config?.ChavePix ?? "[CHAVE NÃO CONFIGURADA]";

        return template
            .Replace("{NomeCliente}", cliente.NomeCompleto)
            .Replace("{Valor}", fatura.Valor.ToString("F2"))
            .Replace("{Vencimento}", fatura.DataVencimento.ToString("dd/MM/yyyy"))
            .Replace("{NomeDono}", nomeDono)
            .Replace("{ChavePix}", chavePix);
    }
}
