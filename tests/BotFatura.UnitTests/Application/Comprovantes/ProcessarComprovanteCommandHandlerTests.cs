using Ardalis.Result;
using Ardalis.Specification;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Comprovantes.Commands.ProcessarComprovante;
using BotFatura.Application.Comprovantes.Services;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using BotFatura.TestUtils.Builders;
using BotFatura.TestUtils.Cenarios;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BotFatura.UnitTests.Application.Comprovantes;

public class ProcessarComprovanteCommandHandlerTests
{
    private readonly Mock<IGeminiApiClient> _geminiClientMock;
    private readonly Mock<IFaturaRepository> _faturaRepositoryMock;
    private readonly Mock<IEvolutionApiClient> _evolutionApiClientMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<ProcessarComprovanteCommandHandler>> _loggerMock;
    private readonly ProcessarComprovanteCommandHandler _handler;

    public ProcessarComprovanteCommandHandlerTests()
    {
        _geminiClientMock = new Mock<IGeminiApiClient>();
        _faturaRepositoryMock = new Mock<IFaturaRepository>();
        _evolutionApiClientMock = new Mock<IEvolutionApiClient>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<ProcessarComprovanteCommandHandler>>();

        // Criar ValidationService com mocks reais
        var validationServiceLoggerMock = new Mock<ILogger<ComprovanteValidationService>>();
        var validationService = new ComprovanteValidationService(
            _faturaRepositoryMock.Object,
            _cacheServiceMock.Object,
            validationServiceLoggerMock.Object);

        _handler = new ProcessarComprovanteCommandHandler(
            _geminiClientMock.Object,
            _faturaRepositoryMock.Object,
            _evolutionApiClientMock.Object,
            validationService,
            _unitOfWorkMock.Object,
            _dateTimeProviderMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);

        // Setup padrão
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _dateTimeProviderMock.Setup(x => x.Today).Returns(DateTime.Today);
        _evolutionApiClientMock.Setup(x => x.EnviarMensagemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _evolutionApiClientMock.Setup(x => x.EnviarMensagemParaGrupoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    #region Cenários de Sucesso

    [Fact]
    public async Task Handle_ComprovanteValidoValorCorreto_DeveMarcarFaturaComoPaga()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var valorFatura = 150.00m;
        var cliente = CriarClienteTeste(clienteId);
        var fatura = CriarFaturaPendente(clienteId, valorFatura);
        
        var comprovanteDto = CenariosComprovantePredefinidos.ComprovantePixValido(valorFatura);
        
        ConfigurarMocksParaSucesso(cliente, fatura, comprovanteDto);

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeTrue();
        resultado.Value.FaturaId.Should().Be(fatura.Id);
        resultado.Value.Mensagem.Should().Contain("validado");
    }

    [Fact]
    public async Task Handle_ComprovanteNoLimiteTolerancia_DeveAceitar()
    {
        // Arrange - Valor R$ 0,01 acima (dentro da tolerância)
        var clienteId = Guid.NewGuid();
        var valorFatura = 150.00m;
        var valorComprovante = 150.01m;
        
        var cliente = CriarClienteTeste(clienteId);
        var fatura = CriarFaturaPendente(clienteId, valorFatura);
        
        var comprovanteDto = CenariosComprovantePredefinidos.ComprovanteNoLimiteToleranciaSuperior(valorFatura);
        
        ConfigurarMocksParaSucesso(cliente, fatura, comprovanteDto);

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeTrue();
    }

    [Theory]
    [InlineData(10000.00)]
    [InlineData(50000.00)]
    [InlineData(99999.99)]
    public async Task Handle_ComprovanteValorAlto_DeveProcessarCorretamente(decimal valorAlto)
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var cliente = CriarClienteTeste(clienteId);
        var fatura = CriarFaturaPendente(clienteId, valorAlto);
        
        var comprovanteDto = new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(valorAlto)
            .ComDadosDestinatario(nome: "Empresa BotFatura", chavePix: "pix@botfatura.com.br")
            .Build();
        
        ConfigurarMocksParaSucesso(cliente, fatura, comprovanteDto);

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeTrue();
    }

    #endregion

    #region Cenários de Falha - Comprovante Inválido

    [Fact]
    public async Task Handle_ImagemNaoEComprovante_DeveRetornarFalha()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var comprovanteDto = CenariosComprovantePredefinidos.ImagemNaoComprovante();
        
        _geminiClientMock.Setup(x => x.AnalisarComprovanteAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(comprovanteDto));

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeFalse();
        resultado.Value.Mensagem.Should().Contain("não é um comprovante válido");
    }

    [Fact]
    public async Task Handle_GeminiRetornaErro_DeveRetornarFalha()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        
        _geminiClientMock.Setup(x => x.AnalisarComprovanteAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Error("Erro na API Gemini"));

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.Contains("analisar comprovante"));
    }

    #endregion

    #region Cenários de Falha - Valor Incorreto

    [Fact]
    public async Task Handle_ComprovanteValorDiferente_DeveRetornarFalha()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var valorFatura = 150.00m;
        var valorComprovante = 200.00m;
        
        var cliente = CriarClienteTeste(clienteId);
        var fatura = CriarFaturaPendente(clienteId, valorFatura);
        
        var comprovanteDto = CenariosComprovantePredefinidos.ComprovanteValorIncorreto(valorComprovante);
        
        ConfigurarMocksParaValidacaoDestinatario(cliente, fatura, comprovanteDto);

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeFalse();
        resultado.Value.Mensagem.Should().Contain("Valor não corresponde");
    }

    [Fact]
    public async Task Handle_ComprovanteForaTolerancia_DeveRetornarFalha()
    {
        // Arrange - Valor R$ 0,02 acima (fora da tolerância)
        var clienteId = Guid.NewGuid();
        var valorFatura = 150.00m;
        
        var cliente = CriarClienteTeste(clienteId);
        var fatura = CriarFaturaPendente(clienteId, valorFatura);
        
        var comprovanteDto = CenariosComprovantePredefinidos.ComprovanteForaToleranciaSuperior(valorFatura);
        
        ConfigurarMocksParaValidacaoDestinatario(cliente, fatura, comprovanteDto);

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ComprovanteSemValor_DeveRetornarFalha()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var cliente = CriarClienteTeste(clienteId);
        var fatura = CriarFaturaPendente(clienteId, 150.00m);
        
        var comprovanteDto = CenariosComprovantePredefinidos.ComprovanteSemValor();
        
        _geminiClientMock.Setup(x => x.AnalisarComprovanteAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(comprovanteDto));
        
        ConfigurarCacheServiceComConfiguracao();

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeFalse();
        resultado.Value.Mensagem.Should().Contain("Valor não identificado");
    }

    [Fact]
    public async Task Handle_ComprovanteValorZero_DeveRetornarFalha()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var cliente = CriarClienteTeste(clienteId);
        var fatura = CriarFaturaPendente(clienteId, 150.00m);
        
        var comprovanteDto = CenariosComprovantePredefinidos.ComprovanteValorZero();
        
        _geminiClientMock.Setup(x => x.AnalisarComprovanteAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(comprovanteDto));
        
        ConfigurarCacheServiceComConfiguracao();

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeFalse();
    }

    #endregion

    #region Cenários de Falha - Destinatário Incorreto

    [Fact]
    public async Task Handle_ComprovanteDestinatarioErrado_DeveRetornarFalha()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var valorFatura = 150.00m;
        
        var comprovanteDto = CenariosComprovantePredefinidos.ComprovanteDestinatarioErrado(valorFatura);
        
        _geminiClientMock.Setup(x => x.AnalisarComprovanteAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(comprovanteDto));
        
        ConfigurarCacheServiceComConfiguracao();

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeFalse();
        resultado.Value.Mensagem.Should().Contain("Destinatário");
    }

    #endregion

    #region Cenários de Falha - Sem Fatura Pendente

    [Fact]
    public async Task Handle_SemFaturaPendente_DeveRetornarFalha()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var valorFatura = 150.00m;
        
        var comprovanteDto = CenariosComprovantePredefinidos.ComprovantePixValido(valorFatura);
        
        _geminiClientMock.Setup(x => x.AnalisarComprovanteAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(comprovanteDto));
        
        ConfigurarCacheServiceComConfiguracao();
        
        // Não configura faturas - lista vazia
        _faturaRepositoryMock.Setup(x => x.ListAsync(It.IsAny<ISpecification<Fatura>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fatura>());

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeFalse();
        resultado.Value.Mensagem.Should().Contain("fatura pendente");
    }

    #endregion

    #region Testes de Diferentes Tipos de Pagamento

    [Theory]
    [InlineData("PIX")]
    [InlineData("Transferencia")]
    [InlineData("Boleto")]
    public async Task Handle_DiferentesTiposPagamento_DeveProcessarCorretamente(string tipoPagamento)
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var valorFatura = 150.00m;
        var cliente = CriarClienteTeste(clienteId);
        var fatura = CriarFaturaPendente(clienteId, valorFatura);
        
        var comprovanteDto = new ComprovanteAnalisadoDtoBuilder()
            .ComValor(valorFatura)
            .ComTipoPagamento(tipoPagamento)
            .ComDadosDestinatario(nome: "Empresa BotFatura", chavePix: "pix@botfatura.com.br")
            .Build();
        
        ConfigurarMocksParaSucesso(cliente, fatura, comprovanteDto);

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeTrue();
    }

    #endregion

    #region Testes de Nomes Diversos

    [Theory]
    [InlineData("João da Silva")]
    [InlineData("Maria de Souza Santos")]
    [InlineData("José Carlos Pereira Junior")]
    [InlineData("EMPRESA TESTE LTDA ME")]
    public async Task Handle_DiferentesNomesPagador_DeveProcessarCorretamente(string nomePagador)
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var valorFatura = 150.00m;
        var cliente = CriarClienteTeste(clienteId, nomePagador);
        var fatura = CriarFaturaPendente(clienteId, valorFatura);
        
        var comprovanteDto = new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(valorFatura)
            .ComDadosPagador(nome: nomePagador)
            .ComDadosDestinatario(nome: "Empresa BotFatura", chavePix: "pix@botfatura.com.br")
            .Build();
        
        ConfigurarMocksParaSucesso(cliente, fatura, comprovanteDto);

        var command = CriarCommand(clienteId);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Sucesso.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private static Cliente CriarClienteTeste(Guid clienteId, string nome = "Cliente Teste")
    {
        var cliente = new Cliente(nome, "11999999999", "5511999999999@s.whatsapp.net");
        // Usar reflection para setar o Id
        typeof(Cliente).GetProperty("Id")!.SetValue(cliente, clienteId);
        return cliente;
    }

    private static Fatura CriarFaturaPendente(Guid clienteId, decimal valor)
    {
        var fatura = new Fatura(clienteId, valor, DateTime.UtcNow.AddDays(10));
        return fatura;
    }

    private ProcessarComprovanteCommand CriarCommand(Guid clienteId)
    {
        return new ProcessarComprovanteCommand(
            ClienteId: clienteId,
            Arquivo: new byte[] { 1, 2, 3 },
            MimeType: "image/jpeg",
            NumeroWhatsApp: "11999999999",
            JidOriginal: "5511999999999@s.whatsapp.net",
            DataEnvioMensagemFatura: DateTime.UtcNow
        );
    }

    private void ConfigurarMocksParaSucesso(Cliente cliente, Fatura fatura, ComprovanteAnalisadoDto comprovanteDto)
    {
        _geminiClientMock.Setup(x => x.AnalisarComprovanteAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(comprovanteDto));
        
        ConfigurarCacheServiceComConfiguracao();
        
        // Configurar repository para retornar a fatura
        _faturaRepositoryMock.Setup(x => x.ListAsync(It.IsAny<ISpecification<Fatura>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fatura> { fatura });
        
        // Configurar UnitOfWork
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void ConfigurarMocksParaValidacaoDestinatario(Cliente cliente, Fatura fatura, ComprovanteAnalisadoDto comprovanteDto)
    {
        _geminiClientMock.Setup(x => x.AnalisarComprovanteAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(comprovanteDto));
        
        ConfigurarCacheServiceComConfiguracao();
        
        // Configurar repository para retornar a fatura
        _faturaRepositoryMock.Setup(x => x.ListAsync(It.IsAny<ISpecification<Fatura>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fatura> { fatura });
    }

    private void ConfigurarCacheServiceComConfiguracao()
    {
        var configuracao = new Configuracao("pix@botfatura.com.br", "Empresa BotFatura");
        
        _cacheServiceMock.Setup(x => x.ObterConfiguracaoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuracao);
    }

    #endregion
}
