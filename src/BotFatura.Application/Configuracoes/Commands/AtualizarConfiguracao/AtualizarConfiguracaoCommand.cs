using Ardalis.Result;
using MediatR;

namespace BotFatura.Application.Configuracoes.Commands.AtualizarConfiguracao;

public record AtualizarConfiguracaoCommand(string ChavePix, string NomeTitularPix, int DiasAntecedenciaLembrete, int DiasAposVencimentoCobranca) : IRequest<Result>;
