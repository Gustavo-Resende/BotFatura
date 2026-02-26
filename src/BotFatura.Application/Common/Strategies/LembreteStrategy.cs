using BotFatura.Domain.Enums;

namespace BotFatura.Application.Common.Strategies;

public class LembreteStrategy : INotificacaoStrategy
{
    public string ObterTextoPadrao()
    {
        return "Olá {NomeCliente}!\n\nLembramos que você tem uma fatura no valor de *R$ {Valor}* com vencimento em *{Vencimento}*.\n\n*Pagamento via PIX:*\nTitular: {NomeDono}\nChave: {ChavePix}\n\nPor favor, efetue o pagamento até a data de vencimento.";
    }

    public string ObterTipoNotificacaoString()
    {
        return "Lembrete_3_Dias";
    }

    public TipoNotificacaoTemplate ObterTipoNotificacaoTemplate()
    {
        return TipoNotificacaoTemplate.Lembrete;
    }
}
