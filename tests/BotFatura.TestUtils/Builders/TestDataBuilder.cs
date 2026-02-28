using BotFatura.Application.Common.Interfaces;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.TestUtils.Geradores;

namespace BotFatura.TestUtils.Builders;

/// <summary>
/// Builder fluente para criar cenários de teste completos
/// </summary>
public class TestScenarioBuilder
{
    private Cliente? _cliente;
    private Fatura? _fatura;
    private ComprovanteAnalisadoDto? _comprovanteAnalisado;
    private byte[]? _imagemComprovante;
    private readonly ComprovanteGenerator _comprovanteGenerator = new();

    /// <summary>
    /// Configura um cliente para o cenário
    /// </summary>
    public TestScenarioBuilder ComCliente(string nome, string whatsApp, string? whatsAppJid = null)
    {
        _cliente = new Cliente(
            nomeCompleto: nome,
            whatsApp: whatsApp,
            whatsAppJid: whatsAppJid
        );
        return this;
    }

    /// <summary>
    /// Configura um cliente existente
    /// </summary>
    public TestScenarioBuilder ComCliente(Cliente cliente)
    {
        _cliente = cliente;
        return this;
    }

    /// <summary>
    /// Adiciona uma fatura pendente ao cenário
    /// </summary>
    public TestScenarioBuilder ComFaturaPendente(decimal valor, DateTime vencimento)
    {
        if (_cliente == null)
            throw new InvalidOperationException("Configure um cliente antes de adicionar uma fatura.");

        _fatura = new Fatura(_cliente.Id, valor, vencimento);
        return this;
    }

    /// <summary>
    /// Adiciona uma fatura existente
    /// </summary>
    public TestScenarioBuilder ComFatura(Fatura fatura)
    {
        _fatura = fatura;
        return this;
    }

    /// <summary>
    /// Configura um comprovante válido que corresponde à fatura
    /// </summary>
    public TestScenarioBuilder ComComprovanteValido(
        string? chavePixDestinatario = null,
        string? nomeDestinatario = null)
    {
        if (_fatura == null)
            throw new InvalidOperationException("Configure uma fatura antes de adicionar um comprovante.");

        var parametros = new ComprovanteParametros
        {
            Valor = _fatura.Valor,
            NomePagador = _cliente?.NomeCompleto ?? "Pagador Teste",
            DocumentoPagador = "123.456.789-00",
            NomeDestinatario = nomeDestinatario ?? "Empresa BotFatura",
            ChavePixDestinatario = chavePixDestinatario ?? "pix@botfatura.com.br",
            Data = DateTime.Now,
            TipoPagamento = "PIX",
            NumeroComprovante = $"E{Guid.NewGuid():N}"[..32]
        };

        _imagemComprovante = _comprovanteGenerator.GerarComprovantePix(parametros);
        _comprovanteAnalisado = CriarComprovanteAnalisado(parametros, isComprovante: true, confianca: 95);
        
        return this;
    }

    /// <summary>
    /// Configura um comprovante com valor incorreto
    /// </summary>
    public TestScenarioBuilder ComComprovanteValorIncorreto(decimal valorIncorreto)
    {
        if (_fatura == null)
            throw new InvalidOperationException("Configure uma fatura antes de adicionar um comprovante.");

        var parametros = new ComprovanteParametros
        {
            Valor = valorIncorreto, // Valor diferente da fatura
            NomePagador = _cliente?.NomeCompleto ?? "Pagador Teste",
            DocumentoPagador = "123.456.789-00",
            NomeDestinatario = "Empresa BotFatura",
            ChavePixDestinatario = "pix@botfatura.com.br",
            Data = DateTime.Now,
            TipoPagamento = "PIX",
            NumeroComprovante = $"E{Guid.NewGuid():N}"[..32]
        };

        _imagemComprovante = _comprovanteGenerator.GerarComprovantePix(parametros);
        _comprovanteAnalisado = CriarComprovanteAnalisado(parametros, isComprovante: true, confianca: 95);
        
        return this;
    }

    /// <summary>
    /// Configura um comprovante com destinatário errado
    /// </summary>
    public TestScenarioBuilder ComComprovanteDestinatarioErrado()
    {
        if (_fatura == null)
            throw new InvalidOperationException("Configure uma fatura antes de adicionar um comprovante.");

        var parametros = new ComprovanteParametros
        {
            Valor = _fatura.Valor,
            NomePagador = _cliente?.NomeCompleto ?? "Pagador Teste",
            DocumentoPagador = "123.456.789-00",
            NomeDestinatario = "Outra Empresa LTDA",
            ChavePixDestinatario = "outra@empresa.com",
            Data = DateTime.Now,
            TipoPagamento = "PIX",
            NumeroComprovante = $"E{Guid.NewGuid():N}"[..32]
        };

        _imagemComprovante = _comprovanteGenerator.GerarComprovantePix(parametros);
        _comprovanteAnalisado = CriarComprovanteAnalisado(parametros, isComprovante: true, confianca: 95);
        
        return this;
    }

    /// <summary>
    /// Configura uma imagem que não é comprovante
    /// </summary>
    public TestScenarioBuilder ComImagemNaoComprovante()
    {
        _imagemComprovante = _comprovanteGenerator.GerarImagemNaoComprovante();
        _comprovanteAnalisado = new ComprovanteAnalisadoDto(
            IsComprovante: false,
            Valor: null,
            Data: null,
            TipoPagamento: null,
            Confianca: 10,
            Observacoes: "A imagem não aparenta ser um comprovante de pagamento",
            DadosPagador: null,
            DadosDestinatario: null,
            NumeroComprovante: null
        );
        
        return this;
    }

    /// <summary>
    /// Configura um comprovante com valor alto para testes de edge case
    /// </summary>
    public TestScenarioBuilder ComComprovanteValorAlto(decimal valorAlto)
    {
        var parametros = new ComprovanteParametros
        {
            Valor = valorAlto,
            NomePagador = _cliente?.NomeCompleto ?? "Pagador Teste",
            DocumentoPagador = "123.456.789-00",
            NomeDestinatario = "Empresa BotFatura",
            ChavePixDestinatario = "pix@botfatura.com.br",
            Data = DateTime.Now,
            TipoPagamento = "PIX",
            NumeroComprovante = $"E{Guid.NewGuid():N}"[..32]
        };

        _imagemComprovante = _comprovanteGenerator.GerarComprovantePix(parametros);
        _comprovanteAnalisado = CriarComprovanteAnalisado(parametros, isComprovante: true, confianca: 95);
        
        return this;
    }

    /// <summary>
    /// Constrói o cenário de teste
    /// </summary>
    public TestScenario Build()
    {
        return new TestScenario
        {
            Cliente = _cliente,
            Fatura = _fatura,
            ComprovanteAnalisado = _comprovanteAnalisado,
            ImagemComprovante = _imagemComprovante
        };
    }

    private static ComprovanteAnalisadoDto CriarComprovanteAnalisado(
        ComprovanteParametros parametros, 
        bool isComprovante, 
        int confianca)
    {
        return new ComprovanteAnalisadoDto(
            IsComprovante: isComprovante,
            Valor: parametros.Valor,
            Data: parametros.Data,
            TipoPagamento: parametros.TipoPagamento,
            Confianca: confianca,
            Observacoes: "Comprovante analisado com sucesso",
            DadosPagador: new DadosPagadorDto(
                Nome: parametros.NomePagador,
                Documento: parametros.DocumentoPagador,
                Banco: parametros.BancoPagador,
                Agencia: null,
                Conta: null
            ),
            DadosDestinatario: new DadosDestinatarioDto(
                Nome: parametros.NomeDestinatario,
                ChavePix: parametros.ChavePixDestinatario,
                Documento: parametros.DocumentoDestinatario,
                Banco: parametros.BancoDestinatario,
                Agencia: null,
                Conta: null
            ),
            NumeroComprovante: parametros.NumeroComprovante
        );
    }

}

/// <summary>
/// Representa um cenário de teste completo
/// </summary>
public class TestScenario
{
    public Cliente? Cliente { get; init; }
    public Fatura? Fatura { get; init; }
    public ComprovanteAnalisadoDto? ComprovanteAnalisado { get; init; }
    public byte[]? ImagemComprovante { get; init; }
}
