namespace BotFatura.Application.Common.Strategies;

public interface INotificacaoStrategy
{
    string ObterTextoPadrao();
    string ObterTipoNotificacaoString();
    Domain.Enums.TipoNotificacaoTemplate ObterTipoNotificacaoTemplate();
}
