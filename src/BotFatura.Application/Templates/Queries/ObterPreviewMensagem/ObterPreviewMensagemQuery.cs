using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Interfaces;
using MediatR;
using Ardalis.Result;

namespace BotFatura.Application.Templates.Queries.ObterPreviewMensagem;

public record ObterPreviewMensagemQuery(Guid ClienteId, string? TextoCustomizado = null) : IRequest<Result<string>>;

public class ObterPreviewMensagemQueryHandler : IRequestHandler<ObterPreviewMensagemQuery, Result<string>>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IMensagemTemplateRepository _templateRepository;
    private readonly IMensagemFormatter _formatter;

    public ObterPreviewMensagemQueryHandler(
        IClienteRepository clienteRepository, 
        IMensagemTemplateRepository templateRepository,
        IMensagemFormatter formatter)
    {
        _clienteRepository = clienteRepository;
        _templateRepository = templateRepository;
        _formatter = formatter;
    }

    public async Task<Result<string>> Handle(ObterPreviewMensagemQuery request, CancellationToken cancellationToken)
    {
        var cliente = await _clienteRepository.GetByIdAsync(request.ClienteId, cancellationToken);
        if (cliente == null) return Result.NotFound("Cliente não encontrado.");

        string? textoBase = request.TextoCustomizado;

        if (string.IsNullOrWhiteSpace(textoBase))
        {
            var templates = await _templateRepository.ListAsync(cancellationToken);
            var template = templates.FirstOrDefault(t => t.IsPadrao) ?? templates.FirstOrDefault();
            textoBase = template?.TextoBase ?? "Olá {NomeCliente}, sua fatura de R$ {Valor} vence em {Vencimento}.";
        }

        // Fatura fake para o preview
        var faturaFake = new BotFatura.Domain.Entities.Fatura(cliente.Id, 150.00m, DateTime.UtcNow.AddDays(5));

        string mensagem = await _formatter.FormatarMensagemAsync(textoBase, cliente, faturaFake, cancellationToken);

        return Result.Success(mensagem);
    }
}
