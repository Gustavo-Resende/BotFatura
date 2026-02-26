using BotFatura.Domain.Enums;

namespace BotFatura.Application.Common.Strategies;

public class ManualStrategy : INotificacaoStrategy
{
    public string ObterTextoPadrao()
    {
        return "Olá {NomeCliente}! 🤖\n\nIdentificamos uma fatura pendente no valor de *R$ {Valor}* com vencimento em *{Vencimento}*.\n\n*Pagamento via PIX:*\nTitular: {NomeDono}\nChave: {ChavePix}\n\nPor favor, efetue o pagamento para evitar suspensão do serviço.";
    }

    public string ObterTipoNotificacaoString()
    {
        return "Manual";
    }

    public TipoNotificacaoTemplate ObterTipoNotificacaoTemplate()
    {
        // Para manual, retorna Vencimento como padrão
        return TipoNotificacaoTemplate.Vencimento;
    }
}
