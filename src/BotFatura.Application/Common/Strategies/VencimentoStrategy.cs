using BotFatura.Domain.Enums;

namespace BotFatura.Application.Common.Strategies;

public class VencimentoStrategy : INotificacaoStrategy
{
    public string ObterTextoPadrao()
    {
        return "Olá {NomeCliente}!\n\nIdentificamos uma fatura pendente no valor de *R$ {Valor}* com vencimento em *{Vencimento}*.\n\n*Pagamento via PIX:*\nTitular: {NomeDono}\nChave: {ChavePix}\n\nPor favor, efetue o pagamento para evitar suspensão do serviço.";
    }

    public string ObterTipoNotificacaoString()
    {
        return "Cobranca_Vencimento";
    }

    public TipoNotificacaoTemplate ObterTipoNotificacaoTemplate()
    {
        return TipoNotificacaoTemplate.Vencimento;
    }
}
