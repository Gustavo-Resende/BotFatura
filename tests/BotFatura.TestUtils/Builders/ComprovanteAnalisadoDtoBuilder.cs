using BotFatura.Application.Common.Interfaces;

namespace BotFatura.TestUtils.Builders;

/// <summary>
/// Builder fluente para criar objetos ComprovanteAnalisadoDto para testes
/// </summary>
public class ComprovanteAnalisadoDtoBuilder
{
    private bool _isComprovante = true;
    private decimal? _valor = 100.00m;
    private DateTime? _data = DateTime.Now;
    private string? _tipoPagamento = "PIX";
    private int _confianca = 95;
    private string? _observacoes = "Comprovante analisado com sucesso";
    private DadosPagadorDto? _dadosPagador;
    private DadosDestinatarioDto? _dadosDestinatario;
    private string? _numeroComprovante = "E00000000202402271234567890123456";

    public ComprovanteAnalisadoDtoBuilder()
    {
        // Valores padrão para pagador
        _dadosPagador = new DadosPagadorDto(
            Nome: "João da Silva",
            Documento: "123.456.789-00",
            Banco: "Nubank",
            Agencia: null,
            Conta: null
        );

        // Valores padrão para destinatário
        _dadosDestinatario = new DadosDestinatarioDto(
            Nome: "Empresa BotFatura",
            ChavePix: "pix@botfatura.com.br",
            Documento: "12.345.678/0001-90",
            Banco: "Banco do Brasil",
            Agencia: null,
            Conta: null
        );
    }

    /// <summary>
    /// Define se é um comprovante válido
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComIsComprovante(bool isComprovante)
    {
        _isComprovante = isComprovante;
        return this;
    }

    /// <summary>
    /// Define o valor do comprovante
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComValor(decimal? valor)
    {
        _valor = valor;
        return this;
    }

    /// <summary>
    /// Define a data do comprovante
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComData(DateTime? data)
    {
        _data = data;
        return this;
    }

    /// <summary>
    /// Define o tipo de pagamento
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComTipoPagamento(string? tipoPagamento)
    {
        _tipoPagamento = tipoPagamento;
        return this;
    }

    /// <summary>
    /// Define a confiança da análise (0-100)
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComConfianca(int confianca)
    {
        _confianca = confianca;
        return this;
    }

    /// <summary>
    /// Define observações adicionais
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComObservacoes(string? observacoes)
    {
        _observacoes = observacoes;
        return this;
    }

    /// <summary>
    /// Define o número/ID do comprovante
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComNumeroComprovante(string? numeroComprovante)
    {
        _numeroComprovante = numeroComprovante;
        return this;
    }

    /// <summary>
    /// Define os dados do pagador
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComDadosPagador(
        string? nome = null,
        string? documento = null,
        string? banco = null,
        string? agencia = null,
        string? conta = null)
    {
        _dadosPagador = new DadosPagadorDto(
            Nome: nome ?? "João da Silva",
            Documento: documento ?? "123.456.789-00",
            Banco: banco ?? "Nubank",
            Agencia: agencia,
            Conta: conta
        );
        return this;
    }

    /// <summary>
    /// Define os dados do destinatário
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComDadosDestinatario(
        string? nome = null,
        string? chavePix = null,
        string? documento = null,
        string? banco = null,
        string? agencia = null,
        string? conta = null)
    {
        _dadosDestinatario = new DadosDestinatarioDto(
            Nome: nome ?? "Empresa BotFatura",
            ChavePix: chavePix ?? "pix@botfatura.com.br",
            Documento: documento ?? "12.345.678/0001-90",
            Banco: banco ?? "Banco do Brasil",
            Agencia: agencia,
            Conta: conta
        );
        return this;
    }

    /// <summary>
    /// Remove os dados do pagador (simula extração falha)
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder SemDadosPagador()
    {
        _dadosPagador = null;
        return this;
    }

    /// <summary>
    /// Remove os dados do destinatário (simula extração falha)
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder SemDadosDestinatario()
    {
        _dadosDestinatario = null;
        return this;
    }

    /// <summary>
    /// Configura como uma imagem que não é comprovante
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComoNaoComprovante()
    {
        _isComprovante = false;
        _valor = null;
        _data = null;
        _tipoPagamento = null;
        _confianca = 10;
        _observacoes = "A imagem não aparenta ser um comprovante de pagamento";
        _dadosPagador = null;
        _dadosDestinatario = null;
        _numeroComprovante = null;
        return this;
    }

    /// <summary>
    /// Configura como um comprovante PIX padrão
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComoComprovantePix(decimal valor)
    {
        _isComprovante = true;
        _valor = valor;
        _tipoPagamento = "PIX";
        _confianca = 95;
        return this;
    }

    /// <summary>
    /// Configura como um comprovante de transferência
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComoComprovanteTransferencia(decimal valor)
    {
        _isComprovante = true;
        _valor = valor;
        _tipoPagamento = "Transferencia";
        _confianca = 90;
        return this;
    }

    /// <summary>
    /// Configura como um comprovante de boleto
    /// </summary>
    public ComprovanteAnalisadoDtoBuilder ComoComprovanteBoleto(decimal valor)
    {
        _isComprovante = true;
        _valor = valor;
        _tipoPagamento = "Boleto";
        _confianca = 85;
        return this;
    }

    /// <summary>
    /// Constrói o objeto ComprovanteAnalisadoDto
    /// </summary>
    public ComprovanteAnalisadoDto Build()
    {
        return new ComprovanteAnalisadoDto(
            IsComprovante: _isComprovante,
            Valor: _valor,
            Data: _data,
            TipoPagamento: _tipoPagamento,
            Confianca: _confianca,
            Observacoes: _observacoes,
            DadosPagador: _dadosPagador,
            DadosDestinatario: _dadosDestinatario,
            NumeroComprovante: _numeroComprovante
        );
    }
}
