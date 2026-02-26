using BotFatura.Domain.Enums;

namespace BotFatura.Application.Common.Strategies;

public static class NotificacaoStrategyFactory
{
    public static INotificacaoStrategy Criar(TipoNotificacaoTemplate tipo)
    {
        return tipo switch
        {
            TipoNotificacaoTemplate.Lembrete => new LembreteStrategy(),
            TipoNotificacaoTemplate.Vencimento => new VencimentoStrategy(),
            TipoNotificacaoTemplate.AposVencimento => new AposVencimentoStrategy(),
            _ => throw new ArgumentException($"Tipo de notificação inválido: {tipo}")
        };
    }

    public static INotificacaoStrategy CriarPorString(string tipoNotificacao)
    {
        return tipoNotificacao switch
        {
            "Lembrete_3_Dias" => new LembreteStrategy(),
            "Cobranca_Vencimento" => new VencimentoStrategy(),
            "Cobranca_Apos_Vencimento" => new AposVencimentoStrategy(),
            "Manual" => new ManualStrategy(),
            _ => throw new ArgumentException($"Tipo de notificação inválido: {tipoNotificacao}")
        };
    }
}
