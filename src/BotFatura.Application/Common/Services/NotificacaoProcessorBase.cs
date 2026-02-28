using Ardalis.Result;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Common.Strategies;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Factories;
using BotFatura.Domain.Interfaces;

namespace BotFatura.Application.Common.Services;

public abstract class NotificacaoProcessorBase
{
    protected readonly IFaturaRepository _faturaRepository;
    protected readonly IClienteRepository _clienteRepository;
    protected readonly IMensagemTemplateRepository _templateRepository;
    protected readonly IEvolutionApiClient _evolutionApi;
    protected readonly IMensagemFormatter _formatter;
    protected readonly IRepository<LogNotificacao> _logRepository;
    protected readonly ILogNotificacaoFactory _logFactory;
    protected readonly Domain.Interfaces.IUnitOfWork _unitOfWork;
    protected readonly ICacheService _cacheService;

    protected NotificacaoProcessorBase(
        IFaturaRepository faturaRepository,
        IClienteRepository clienteRepository,
        IMensagemTemplateRepository templateRepository,
        IEvolutionApiClient evolutionApi,
        IMensagemFormatter formatter,
        IRepository<LogNotificacao> logRepository,
        ILogNotificacaoFactory logFactory,
        Domain.Interfaces.IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _faturaRepository = faturaRepository;
        _clienteRepository = clienteRepository;
        _templateRepository = templateRepository;
        _evolutionApi = evolutionApi;
        _formatter = formatter;
        _logRepository = logRepository;
        _logFactory = logFactory;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    // Template Method - define o algoritmo
    public async Task<Result> ProcessarAsync(
        Fatura fatura,
        INotificacaoStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        // 1. Validar pré-condições
        var validacaoResult = await ValidarPreCondicoesAsync(fatura, cancellationToken);
        if (!validacaoResult.IsSuccess)
            return validacaoResult;

        // 2. Obter template
        var template = await ObterTemplateAsync(strategy, cancellationToken);
        if (template == null)
            return Result.NotFound($"Template do tipo {strategy.ObterTipoNotificacaoTemplate()} não encontrado.");

        // 3. Formatar mensagem
        var cliente = await ObterClienteAsync(fatura.ClienteId, cancellationToken);
        var mensagem = await FormatarMensagemAsync(template.TextoBase, cliente!, fatura, cancellationToken);

        // 4. Aplicar delay (se necessário)
        await AplicarDelayAsync(cancellationToken);

        // 5. Enviar mensagem
        var sendResult = await EnviarMensagemAsync(cliente!.WhatsApp, mensagem, cancellationToken);

        // 6. Registrar log e atualizar fatura (com transação)
        if (sendResult.IsSuccess)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                
                await RegistrarLogAsync(fatura, strategy, mensagem, cliente.WhatsApp, sendResult, cancellationToken);
                await AtualizarFaturaAsync(fatura, strategy, cancellationToken);
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Error($"Erro ao salvar log e atualizar fatura: {ex.Message}");
            }
        }
        else
        {
            // Mesmo em caso de falha, registrar o log
            await RegistrarLogAsync(fatura, strategy, mensagem, cliente.WhatsApp, sendResult, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return sendResult.IsSuccess 
            ? Result.Success() 
            : Result.Error(string.Join(", ", sendResult.Errors));
    }

    // Métodos abstratos - devem ser implementados pelas classes filhas
    protected abstract Task<Result> ValidarPreCondicoesAsync(Fatura fatura, CancellationToken cancellationToken);
    protected abstract Task<MensagemTemplate?> ObterTemplateAsync(INotificacaoStrategy strategy, CancellationToken cancellationToken);
    protected abstract Task AplicarDelayAsync(CancellationToken cancellationToken);

    // Métodos protegidos - podem ser sobrescritos se necessário
    protected virtual async Task<Cliente?> ObterClienteAsync(Guid clienteId, CancellationToken cancellationToken)
    {
        return await _clienteRepository.GetByIdAsync(clienteId, cancellationToken);
    }

    protected virtual async Task<string> FormatarMensagemAsync(
        string templateTexto, 
        Cliente cliente, 
        Fatura fatura, 
        CancellationToken cancellationToken)
    {
        return await _formatter.FormatarMensagemAsync(templateTexto, cliente, fatura, cancellationToken);
    }

    protected virtual async Task<Result> EnviarMensagemAsync(
        string whatsApp, 
        string mensagem, 
        CancellationToken cancellationToken)
    {
        return await _evolutionApi.EnviarMensagemAsync(whatsApp, mensagem, cancellationToken);
    }

    protected virtual async Task RegistrarLogAsync(
        Fatura fatura,
        INotificacaoStrategy strategy,
        string mensagem,
        string destinatario,
        Result sendResult,
        CancellationToken cancellationToken)
    {
        var log = sendResult.IsSuccess
            ? _logFactory.CriarSucesso(
                fatura.Id,
                strategy.ObterTipoNotificacaoString(),
                mensagem,
                destinatario)
            : _logFactory.CriarFalha(
                fatura.Id,
                strategy.ObterTipoNotificacaoString(),
                mensagem,
                destinatario,
                string.Join(", ", sendResult.Errors));

        await _logRepository.AddAsync(log, cancellationToken);
    }

    protected virtual async Task AtualizarFaturaAsync(
        Fatura fatura,
        INotificacaoStrategy strategy,
        CancellationToken cancellationToken)
    {
        var tipoString = strategy.ObterTipoNotificacaoString();
        if (tipoString == "Lembrete_3_Dias")
            fatura.MarcarLembreteEnviado();
        if (tipoString == "Cobranca_Vencimento")
            fatura.MarcarCobrancaDiaEnviada();
        if (tipoString == "Cobranca_Apos_Vencimento")
            fatura.MarcarCobrancaAposVencimentoEnviada();

        fatura.MarcarComoEnviada();
        await _faturaRepository.UpdateAsync(fatura, cancellationToken);
    }
}
