using BotFatura.Domain.Enums;

namespace BotFatura.Application.Common.Strategies;

public class AposVencimentoStrategy : INotificacaoStrategy
{
    public string ObterTextoPadrao()
    {
        return "Olá {NomeCliente}!\n\nSua fatura no valor de *R$ {Valor}* com vencimento em *{Vencimento}* ainda não foi paga.\n\n*Pagamento via PIX:*\nTitular: {NomeDono}\nChave: {ChavePix}\n\nPor favor, regularize sua situação o quanto antes para evitar interrupção do serviço.";
    }

    public string ObterTipoNotificacaoString()
    {
        return "Cobranca_Apos_Vencimento";
    }

    public TipoNotificacaoTemplate ObterTipoNotificacaoTemplate()
    {
        return TipoNotificacaoTemplate.AposVencimento;
    }
}
