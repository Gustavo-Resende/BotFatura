using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BotFatura.Application.Common.Services;

public class CacheService : ICacheService
{
    private readonly IMensagemTemplateRepository _templateRepository;
    private readonly IRepository<Configuracao> _configRepository;
    private readonly IMemoryCache _memoryCache;
    private const int CACHE_DURATION_MINUTES = 30;

    public CacheService(
        IMensagemTemplateRepository templateRepository,
        IRepository<Configuracao> configRepository,
        IMemoryCache memoryCache)
    {
        _templateRepository = templateRepository;
        _configRepository = configRepository;
        _memoryCache = memoryCache;
    }

    public async Task<MensagemTemplate?> ObterTemplateAsync(TipoNotificacaoTemplate tipo, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"template_{tipo}";
        
        if (_memoryCache.TryGetValue(cacheKey, out MensagemTemplate? cachedTemplate))
        {
            return cachedTemplate;
        }

        var template = await _templateRepository.ObterPorTipoAsync(tipo, cancellationToken);
        
        if (template != null)
        {
            _memoryCache.Set(cacheKey, template, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
        }

        return template;
    }

    public async Task<Configuracao?> ObterConfiguracaoAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "configuracao_global";
        
        if (_memoryCache.TryGetValue(cacheKey, out Configuracao? cachedConfig))
        {
            return cachedConfig;
        }

        var configs = await _configRepository.ListAsync(cancellationToken);
        var config = configs.FirstOrDefault();
        
        if (config != null)
        {
            _memoryCache.Set(cacheKey, config, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
        }

        return config;
    }

    public void InvalidarTemplates()
    {
        // Remove todos os templates do cache
        var cacheKeys = new[]
        {
            "template_Lembrete",
            "template_Vencimento",
            "template_AposVencimento"
        };

        foreach (var key in cacheKeys)
        {
            _memoryCache.Remove(key);
        }
    }

    public void InvalidarConfiguracao()
    {
        _memoryCache.Remove("configuracao_global");
    }
}
