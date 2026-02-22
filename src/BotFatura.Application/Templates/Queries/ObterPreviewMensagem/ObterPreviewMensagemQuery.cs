using BotFatura.Domain.Interfaces;
using MediatR;
using Ardalis.Result;

namespace BotFatura.Application.Templates.Queries.ObterPreviewMensagem;

public record ObterPreviewMensagemQuery(Guid ClienteId, string? TextoCustomizado = null) : IRequest<Result<string>>;

public class ObterPreviewMensagemQueryHandler : IRequestHandler<ObterPreviewMensagemQuery, Result<string>>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IMensagemTemplateRepository _templateRepository;

    public ObterPreviewMensagemQueryHandler(IClienteRepository clienteRepository, IMensagemTemplateRepository templateRepository)
    {
        _clienteRepository = clienteRepository;
        _templateRepository = templateRepository;
    }

    public async Task<Result<string>> Handle(ObterPreviewMensagemQuery request, CancellationToken cancellationToken)
    {
        var cliente = await _clienteRepository.GetByIdAsync(request.ClienteId, cancellationToken);
        if (cliente == null) return Result.NotFound("Cliente não encontrado.");

        string? textoBase = request.TextoCustomizado;

        if (string.IsNullOrWhiteSpace(textoBase))
        {
            var faturas = await _templateRepository.ListAsync(cancellationToken);
            var template = faturas.FirstOrDefault(t => t.IsPadrao);
            textoBase = template?.TextoBase ?? "Olá {NomeCliente}, sua fatura de R$ {Valor} vence em {Vencimento}.";
        }

        // Agora garantimos que textoBase não é nulo para o Replace
        string mensagem = textoBase!
            .Replace("{NomeCliente}", cliente.NomeCompleto)
            .Replace("{Valor}", "150,00")
            .Replace("{Vencimento}", DateTime.UtcNow.AddDays(5).ToString("dd/MM/yyyy"));

        return Result.Success(mensagem);
    }
}
