using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;

namespace BotFatura.Application.Common.Interfaces;

public interface ICacheService
{
    Task<MensagemTemplate?> ObterTemplateAsync(TipoNotificacaoTemplate tipo, CancellationToken cancellationToken = default);
    Task<Configuracao?> ObterConfiguracaoAsync(CancellationToken cancellationToken = default);
    void InvalidarTemplates();
    void InvalidarConfiguracao();
}

