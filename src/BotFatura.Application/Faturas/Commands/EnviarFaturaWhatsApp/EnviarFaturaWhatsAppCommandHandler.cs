using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.EnviarFaturaWhatsApp;

public class EnviarFaturaWhatsAppCommandHandler : IRequestHandler<EnviarFaturaWhatsAppCommand, Result>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IMensagemTemplateRepository _templateRepository;
    private readonly IEvolutionApiClient _evolutionApi;

    public EnviarFaturaWhatsAppCommandHandler(
        IFaturaRepository faturaRepository,
        IClienteRepository clienteRepository,
        IMensagemTemplateRepository templateRepository,
        IEvolutionApiClient evolutionApi)
    {
        _faturaRepository = faturaRepository;
        _clienteRepository = clienteRepository;
        _templateRepository = templateRepository;
        _evolutionApi = evolutionApi;
    }

    public async Task<Result> Handle(EnviarFaturaWhatsAppCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar existência da fatura
        var fatura = await _faturaRepository.GetByIdAsync(request.FaturaId, cancellationToken);
        if (fatura == null)
            return Result.NotFound("Fatura não encontrada.");

        // 2. Validar cliente
        var cliente = await _clienteRepository.GetByIdAsync(fatura.ClienteId, cancellationToken);
        if (cliente == null || !cliente.Ativo)
            return Result.Error("Cliente inexistente ou desativado.");

        // 3. Verificar Status da Evolution API
        var statusResult = await _evolutionApi.ObterStatusAsync(cancellationToken);
        if (!statusResult.IsSuccess || statusResult.Value != "open")
            return Result.Error($"A instância do WhatsApp não está conectada. Status: {statusResult.Value}");

        // 4. Obter Template e Montar Mensagem
        var templates = await _templateRepository.ListAsync(cancellationToken);
        var template = templates.FirstOrDefault(t => t.IsPadrao) ?? templates.FirstOrDefault();

        string mensagem;
        if (template != null)
        {
            mensagem = template.TextoBase
                .Replace("{NomeCliente}", cliente.NomeCompleto)
                .Replace("{Valor}", fatura.Valor.ToString("F2"))
                .Replace("{Vencimento}", fatura.DataVencimento.ToString("dd/MM/yyyy"));
        }
        else
        {
            mensagem = $"Olá {cliente.NomeCompleto}! Identificamos uma fatura pendente no valor de R$ {fatura.Valor:F2} com vencimento para {fatura.DataVencimento:dd/MM/yyyy}.";
        }

        // 5. Enviar Mensagem (Com delay de segurança anti-ban de 5 a 10s)
        var delayManual = new Random().Next(5000, 10000);
        await Task.Delay(delayManual, cancellationToken);

        var sendResult = await _evolutionApi.EnviarMensagemAsync(cliente.WhatsApp, mensagem, cancellationToken);

        
        if (sendResult.IsSuccess)
        {
            fatura.MarcarComoEnviada();
            await _faturaRepository.UpdateAsync(fatura, cancellationToken);
            return Result.Success();
        }

        return Result.Error(string.Join(", ", sendResult.Errors));
    }
}
