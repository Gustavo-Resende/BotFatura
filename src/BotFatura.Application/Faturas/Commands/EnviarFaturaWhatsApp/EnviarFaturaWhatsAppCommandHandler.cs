using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using MediatR;

namespace BotFatura.Application.Faturas.Commands.EnviarFaturaWhatsApp;

public class EnviarFaturaWhatsAppCommandHandler : IRequestHandler<EnviarFaturaWhatsAppCommand, Result>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IMensagemTemplateRepository _templateRepository;
    private readonly IEvolutionApiClient _evolutionApi;
    private readonly IMensagemFormatter _formatter;
    private readonly IRepository<LogNotificacao> _logRepository;

    public EnviarFaturaWhatsAppCommandHandler(
        IFaturaRepository faturaRepository,
        IClienteRepository clienteRepository,
        IMensagemTemplateRepository templateRepository,
        IEvolutionApiClient evolutionApi,
        IMensagemFormatter formatter,
        IRepository<LogNotificacao> logRepository)
    {
        _faturaRepository = faturaRepository;
        _clienteRepository = clienteRepository;
        _templateRepository = templateRepository;
        _evolutionApi = evolutionApi;
        _formatter = formatter;
        _logRepository = logRepository;
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

        // 4. Obter Template e Montar Mensagem via Formatter
        var templates = await _templateRepository.ListAsync(cancellationToken);
        var template = templates.FirstOrDefault(t => t.IsPadrao) ?? templates.FirstOrDefault();

        string templateTexto = template?.TextoBase ?? "Olá {NomeCliente}! 🤖\n\nIdentificamos uma fatura pendente no valor de *R$ {Valor}* com vencimento em *{Vencimento}*.\n\n*Pagamento via PIX:*\nTitular: {NomeDono}\nChave: {ChavePix}\n\nPor favor, efetue o pagamento para evitar suspensão do serviço.";
        string mensagem = await _formatter.FormatarMensagemAsync(templateTexto, cliente, fatura, cancellationToken);

        // 5. Enviar Mensagem (Com delay de segurança anti-ban de 5 a 10s)
        var delayManual = new Random().Next(5000, 10000);
        await Task.Delay(delayManual, cancellationToken);

        var sendResult = await _evolutionApi.EnviarMensagemAsync(cliente.WhatsApp, mensagem, cancellationToken);
        
        // 6. Registro de Auditoria
        var log = new LogNotificacao(
            fatura.Id,
            "Manual",
            mensagem,
            cliente.WhatsApp,
            sendResult.IsSuccess,
            sendResult.IsSuccess ? null : string.Join(", ", sendResult.Errors)
        );
        await _logRepository.AddAsync(log, cancellationToken);

        if (sendResult.IsSuccess)
        {
            fatura.MarcarComoEnviada();
            await _faturaRepository.UpdateAsync(fatura, cancellationToken);
            return Result.Success();
        }

        return Result.Error(string.Join(", ", sendResult.Errors));
    }
}
