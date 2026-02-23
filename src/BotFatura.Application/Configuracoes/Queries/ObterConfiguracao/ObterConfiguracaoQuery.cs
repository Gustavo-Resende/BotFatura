using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Configuracoes.Queries.ObterConfiguracao;

public record ConfiguracaoDto(string ChavePix, string NomeTitularPix);

public record ObterConfiguracaoQuery() : IRequest<Result<ConfiguracaoDto>>;
