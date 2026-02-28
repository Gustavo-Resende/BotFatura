using Ardalis.Specification;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Comprovantes.Commands.ProcessarComprovante;
using BotFatura.Application.Comprovantes.Services;
using BotFatura.Domain.Entities;
using BotFatura.IntegrationTests.Fakes;
using BotFatura.IntegrationTests.Fixtures;
using BotFatura.TestUtils.Builders;
using BotFatura.TestUtils.Cenarios;
using BotFatura.TestUtils.Geradores;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace BotFatura.IntegrationTests.Comprovantes;

/// <summary>
/// Testes de integração que verificam o fluxo completo de processamento de comprovantes
/// usando FakeGeminiApiClient e FakeEvolutionApiClient
/// </summary>
public class ComprovanteFluxoCompletoTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ComprovanteGenerator _comprovanteGenerator;
    private readonly ProcessarComprovanteCommandHandler _handler;

    public ComprovanteFluxoCompletoTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _comprovanteGenerator = new ComprovanteGenerator();

        // Criar handler com todas as dependências
        var loggerMock = new Moq.Mock<ILogger<ProcessarComprovanteCommandHandler>>();
        
        _handler = new ProcessarComprovanteCommandHandler(
            _fixture.ServiceProvider.GetRequiredService<IGeminiApiClient>(),
            _fixture.FaturaRepositoryMock.Object,
            _fixture.ServiceProvider.GetRequiredService<IEvolutionApiClient>(),
            _fixture.ServiceProvider.GetRequiredService<ComprovanteValidationService>(),
            _fixture.UnitOfWorkMock.Object,
            _fixture.DateTimeProviderMock.Object,
            _fixture.CacheServiceMock.Object,
            loggerMock.Object);
    }

    #region Fluxo Completo de Sucesso

    [Fact]
    public async Task FluxoCompleto_ComprovantePixValido_DeveProcessarENotificar()
    {
        // Arrange
        var valorFatura = 150.00m;
        
        // Configurar cenário
        _fixture.ConfigurarConfiguracao();
        var cliente = _fixture.ConfigurarCliente("João da Silva", "11999999999");
        var fatura = _fixture.ConfigurarFaturaPendente(cliente.Id, valorFatura);
        
        // Gerar comprovante sintético
        var parametros = new ComprovanteParametros
        {
            Valor = valorFatura,
            NomePagador = "João da Silva",
            NomeDestinatario = "Empresa BotFatura",
            ChavePixDestinatario = "pix@botfatura.com.br"
        };
        var imagemComprovante = _comprovanteGenerator.GerarComprovantePix(parametros);
        
        // Configurar resposta do fake Gemini
        _fixture.FakeGemini.ComRespostaParaImagem(
            imagemComprovante, 
            CenariosComprovantePredefinidos.ComprovantePixValido(valorFatura));
        
        var command = CriarCommand(cliente.Id, imagemComprovante);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeTrue();
        resultado.Value.FaturaId.Should().Be(fatura.Id);
        
        // Verificar que mensagens foram enviadas
        _fixture.FakeEvolution.MensagensEnviadas.Should().HaveCountGreaterThan(0);
        _fixture.FakeEvolution.MensagensEnviadas.Last().Texto.Should().Contain("validado com sucesso");
        
        // Verificar que grupo foi notificado
        _fixture.FakeEvolution.MensagensGrupo.Should().HaveCountGreaterThan(0);
        _fixture.FakeEvolution.MensagensGrupo.Last().Texto.Should().Contain("Comprovante Validado");
    }

    [Theory]
    [InlineData(100.00)]
    [InlineData(500.00)]
    [InlineData(1000.00)]
    [InlineData(5000.00)]
    [InlineData(10000.00)]
    public async Task FluxoCompleto_DiferentesValores_DeveProcessarCorretamente(decimal valor)
    {
        // Arrange
        _fixture.Reset();
        _fixture.ConfigurarConfiguracao();
        var cliente = _fixture.ConfigurarCliente();
        var fatura = _fixture.ConfigurarFaturaPendente(cliente.Id, valor);
        
        var parametros = new ComprovanteParametros { Valor = valor };
        var imagemComprovante = _comprovanteGenerator.GerarComprovantePix(parametros);
        
        _fixture.FakeGemini.ComRespostaPadrao(CenariosComprovantePredefinidos.ComprovantePixValido(valor));
        
        var command = CriarCommand(cliente.Id, imagemComprovante);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeTrue();
    }

    #endregion

    #region Fluxo de Falha - Validações

    [Fact]
    public async Task FluxoCompleto_ImagemNaoEComprovante_DeveEnviarMensagemErro()
    {
        // Arrange
        _fixture.ConfigurarConfiguracao();
        var cliente = _fixture.ConfigurarCliente();
        
        var imagemNaoComprovante = _comprovanteGenerator.GerarImagemNaoComprovante();
        
        _fixture.FakeGemini.ComRespostaPadrao(CenariosComprovantePredefinidos.ImagemNaoComprovante());
        
        var command = CriarCommand(cliente.Id, imagemNaoComprovante);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeFalse();
        
        // Verificar que mensagem de erro foi enviada ao cliente
        _fixture.FakeEvolution.MensagensEnviadas.Should().HaveCountGreaterThan(0);
        _fixture.FakeEvolution.MensagensEnviadas.Last().Texto.Should().Contain("não é um comprovante");
    }

    [Fact]
    public async Task FluxoCompleto_ValorIncorreto_DeveEnviarMensagemErro()
    {
        // Arrange
        var valorFatura = 150.00m;
        var valorComprovante = 200.00m;
        
        _fixture.ConfigurarConfiguracao();
        var cliente = _fixture.ConfigurarCliente();
        _fixture.ConfigurarFaturaPendente(cliente.Id, valorFatura);
        
        var imagemComprovante = _comprovanteGenerator.GerarComprovantePix(new ComprovanteParametros { Valor = valorComprovante });
        
        _fixture.FakeGemini.ComRespostaPadrao(CenariosComprovantePredefinidos.ComprovanteValorIncorreto(valorComprovante));
        
        var command = CriarCommand(cliente.Id, imagemComprovante);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeFalse();
        resultado.Value.Mensagem.Should().Contain("Valor não corresponde");
        
        // Verificar que mensagem de erro foi enviada
        _fixture.FakeEvolution.MensagensEnviadas.Should().HaveCountGreaterThan(0);
        _fixture.FakeEvolution.MensagensEnviadas.Last().Texto.Should().Contain("não corresponde");
    }

    [Fact]
    public async Task FluxoCompleto_DestinatarioIncorreto_DeveEnviarMensagemErro()
    {
        // Arrange
        var valor = 150.00m;
        
        _fixture.ConfigurarConfiguracao();
        var cliente = _fixture.ConfigurarCliente();
        _fixture.ConfigurarFaturaPendente(cliente.Id, valor);
        
        var imagemComprovante = _comprovanteGenerator.GerarComprovantePix(new ComprovanteParametros { Valor = valor });
        
        _fixture.FakeGemini.ComRespostaPadrao(CenariosComprovantePredefinidos.ComprovanteDestinatarioErrado(valor));
        
        var command = CriarCommand(cliente.Id, imagemComprovante);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeFalse();
        resultado.Value.Mensagem.Should().Contain("Destinatário");
    }

    [Fact]
    public async Task FluxoCompleto_SemFaturaPendente_DeveEnviarMensagemErro()
    {
        // Arrange
        var valor = 150.00m;
        
        _fixture.ConfigurarConfiguracao();
        var cliente = _fixture.ConfigurarCliente();
        // Não configura fatura pendente
        _fixture.FaturaRepositoryMock.Setup(x => x.ListAsync(It.IsAny<ISpecification<Fatura>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Fatura>());
        
        var imagemComprovante = _comprovanteGenerator.GerarComprovantePix(new ComprovanteParametros { Valor = valor });
        
        _fixture.FakeGemini.ComRespostaPadrao(CenariosComprovantePredefinidos.ComprovantePixValido(valor));
        
        var command = CriarCommand(cliente.Id, imagemComprovante);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeFalse();
        resultado.Value.Mensagem.Should().Contain("fatura pendente");
    }

    #endregion

    #region Testes de Tolerância de Valor

    [Theory]
    [InlineData(150.00, 150.01, true)]  // R$ 0,01 acima - deve aceitar
    [InlineData(150.00, 149.99, true)]  // R$ 0,01 abaixo - deve aceitar
    [InlineData(150.00, 150.02, false)] // R$ 0,02 acima - deve rejeitar
    [InlineData(150.00, 149.98, false)] // R$ 0,02 abaixo - deve rejeitar
    public async Task FluxoCompleto_TesteToleranciaValor_DeveValidarCorretamente(
        decimal valorFatura, decimal valorComprovante, bool deveAceitar)
    {
        // Arrange
        _fixture.Reset();
        _fixture.ConfigurarConfiguracao();
        var cliente = _fixture.ConfigurarCliente();
        _fixture.ConfigurarFaturaPendente(cliente.Id, valorFatura);
        
        var imagemComprovante = _comprovanteGenerator.GerarComprovantePix(new ComprovanteParametros { Valor = valorComprovante });
        
        _fixture.FakeGemini.ComRespostaPadrao(new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(valorComprovante)
            .ComDadosDestinatario(nome: "Empresa BotFatura", chavePix: "pix@botfatura.com.br")
            .Build());
        
        var command = CriarCommand(cliente.Id, imagemComprovante);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Value.Sucesso.Should().Be(deveAceitar);
    }

    #endregion

    #region Testes de Resiliência

    [Fact]
    public async Task FluxoCompleto_GeminiRetornaErro_DeveRetornarFalhaGraciosamente()
    {
        // Arrange
        _fixture.ConfigurarConfiguracao();
        var cliente = _fixture.ConfigurarCliente();
        
        _fixture.FakeGemini.SimularErro("Serviço temporariamente indisponível");
        
        var imagemComprovante = _comprovanteGenerator.GerarComprovantePix(new ComprovanteParametros());
        var command = CriarCommand(cliente.Id, imagemComprovante);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.Contains("analisar comprovante"));
    }

    [Fact]
    public async Task FluxoCompleto_ComLatencia_DeveProcessarCorretamente()
    {
        // Arrange
        var valor = 150.00m;
        
        _fixture.ConfigurarConfiguracao();
        var cliente = _fixture.ConfigurarCliente();
        _fixture.ConfigurarFaturaPendente(cliente.Id, valor);
        
        // Simular latência de 100ms na API Gemini
        _fixture.FakeGemini
            .ComLatencia(100)
            .ComRespostaPadrao(CenariosComprovantePredefinidos.ComprovantePixValido(valor));
        
        var imagemComprovante = _comprovanteGenerator.GerarComprovantePix(new ComprovanteParametros { Valor = valor });
        var command = CriarCommand(cliente.Id, imagemComprovante);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var resultado = await _handler.Handle(command, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(100);
    }

    #endregion

    #region Testes de Auditoria

    [Fact]
    public async Task FluxoCompleto_Sucesso_DeveRegistrarChamadaGemini()
    {
        // Arrange
        var valor = 150.00m;
        
        _fixture.ConfigurarConfiguracao();
        var cliente = _fixture.ConfigurarCliente();
        _fixture.ConfigurarFaturaPendente(cliente.Id, valor);
        
        _fixture.FakeGemini.ComRespostaPadrao(CenariosComprovantePredefinidos.ComprovantePixValido(valor));
        
        var imagemComprovante = _comprovanteGenerator.GerarComprovantePix(new ComprovanteParametros { Valor = valor });
        var command = CriarCommand(cliente.Id, imagemComprovante);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Verificar que a chamada ao Gemini foi registrada
        _fixture.FakeGemini.QuantidadeChamadas.Should().Be(1);
        _fixture.FakeGemini.HistoricoChamadas.First().MimeType.Should().Be("image/jpeg");
        _fixture.FakeGemini.HistoricoChamadas.First().ArquivoTamanho.Should().Be(imagemComprovante.Length);
    }

    [Fact]
    public async Task FluxoCompleto_Sucesso_DeveEnviarMensagemParaGrupoSocios()
    {
        // Arrange
        var valor = 150.00m;
        
        _fixture.ConfigurarConfiguracao();
        var cliente = _fixture.ConfigurarCliente("Maria Santos");
        _fixture.ConfigurarFaturaPendente(cliente.Id, valor);
        
        _fixture.FakeGemini.ComRespostaPadrao(CenariosComprovantePredefinidos.ComprovantePixValido(valor));
        
        var imagemComprovante = _comprovanteGenerator.GerarComprovantePix(new ComprovanteParametros { Valor = valor });
        var command = CriarCommand(cliente.Id, imagemComprovante);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Verificar mensagem para grupo de sócios
        _fixture.FakeEvolution.MensagensGrupo.Should().HaveCount(1);
        var mensagemGrupo = _fixture.FakeEvolution.MensagensGrupo.First();
        mensagemGrupo.Texto.Should().Contain("Comprovante Validado");
        mensagemGrupo.Texto.Should().Contain("Maria Santos");
        mensagemGrupo.Texto.Should().Contain($"R$ {valor:F2}");
    }

    #endregion

    #region Helpers

    private static ProcessarComprovanteCommand CriarCommand(Guid clienteId, byte[] arquivo)
    {
        return new ProcessarComprovanteCommand(
            ClienteId: clienteId,
            Arquivo: arquivo,
            MimeType: "image/jpeg",
            NumeroWhatsApp: "11999999999",
            JidOriginal: "5511999999999@s.whatsapp.net",
            DataEnvioMensagemFatura: DateTime.UtcNow
        );
    }

    #endregion
}
