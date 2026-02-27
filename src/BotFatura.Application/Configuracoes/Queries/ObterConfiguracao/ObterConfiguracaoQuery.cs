using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Configuracoes.Queries.ObterConfiguracao;

public record ConfiguracaoDto(string ChavePix, string NomeTitularPix, int DiasAntecedenciaLembrete, int DiasAposVencimentoCobranca, string? GrupoSociosWhatsAppId);

public record ObterConfiguracaoQuery() : IRequest<Result<ConfiguracaoDto>>;
