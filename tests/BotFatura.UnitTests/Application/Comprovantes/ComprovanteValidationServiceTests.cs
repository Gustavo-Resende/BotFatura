using Ardalis.Result;
using Ardalis.Specification;
using BotFatura.Application.Common.Interfaces;
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

public class ComprovanteValidationServiceTests
{
    private readonly Mock<IFaturaRepository> _faturaRepositoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<ComprovanteValidationService>> _loggerMock;
    private readonly ComprovanteValidationService _service;

    public ComprovanteValidationServiceTests()
    {
        _faturaRepositoryMock = new Mock<IFaturaRepository>();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<ComprovanteValidationService>>();

        _service = new ComprovanteValidationService(
            _faturaRepositoryMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }

    #region ValidarDestinatarioAsync

    [Fact]
    public async Task ValidarDestinatarioAsync_ChavePixCorreta_DeveRetornarSucesso()
    {
        // Arrange
        ConfigurarConfiguracao("pix@botfatura.com.br", "Empresa BotFatura");
        
        var dadosDestinatario = new DadosDestinatarioDto(
            Nome: "Empresa BotFatura LTDA",
            ChavePix: "pix@botfatura.com.br",
            Documento: null,
            Banco: null,
            Agencia: null,
            Conta: null);

        // Act
        var resultado = await _service.ValidarDestinatarioAsync(dadosDestinatario);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidarDestinatarioAsync_NomeCorreto_DeveRetornarSucesso()
    {
        // Arrange
        ConfigurarConfiguracao("pix@botfatura.com.br", "Empresa BotFatura");
        
        var dadosDestinatario = new DadosDestinatarioDto(
            Nome: "Empresa BotFatura LTDA ME",
            ChavePix: null, // Sem chave PIX
            Documento: null,
            Banco: null,
            Agencia: null,
            Conta: null);

        // Act
        var resultado = await _service.ValidarDestinatarioAsync(dadosDestinatario);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidarDestinatarioAsync_ChavePixErrrada_DeveRetornarFalha()
    {
        // Arrange
        ConfigurarConfiguracao("pix@botfatura.com.br", "Empresa BotFatura");
        
        var dadosDestinatario = new DadosDestinatarioDto(
            Nome: "Outra Empresa",
            ChavePix: "outra@empresa.com",
            Documento: null,
            Banco: null,
            Agencia: null,
            Conta: null);

        // Act
        var resultado = await _service.ValidarDestinatarioAsync(dadosDestinatario);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidarDestinatarioAsync_DadosNull_DeveRetornarFalha()
    {
        // Arrange
        ConfigurarConfiguracao("pix@botfatura.com.br", "Empresa BotFatura");

        // Act
        var resultado = await _service.ValidarDestinatarioAsync(null);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidarDestinatarioAsync_SemConfiguracao_DeveRetornarFalha()
    {
        // Arrange
        _cacheServiceMock.Setup(x => x.ObterConfiguracaoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Configuracao?)null);
        
        var dadosDestinatario = new DadosDestinatarioDto(
            Nome: "Empresa BotFatura",
            ChavePix: "pix@botfatura.com.br",
            Documento: null,
            Banco: null,
            Agencia: null,
            Conta: null);

        // Act
        var resultado = await _service.ValidarDestinatarioAsync(dadosDestinatario);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region ValidarValor

    [Fact]
    public void ValidarValor_ValorExato_DeveRetornarSucesso()
    {
        // Arrange
        var fatura = CriarFatura(150.00m);

        // Act
        var resultado = _service.ValidarValor(150.00m, fatura);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(150.00, 150.01)] // R$ 0,01 acima - deve aceitar
    [InlineData(150.00, 149.99)] // R$ 0,01 abaixo - deve aceitar
    public void ValidarValor_DentroTolerancia_DeveRetornarSucesso(decimal valorFatura, decimal valorComprovante)
    {
        // Arrange
        var fatura = CriarFatura(valorFatura);

        // Act
        var resultado = _service.ValidarValor(valorComprovante, fatura);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(150.00, 150.02)] // R$ 0,02 acima - deve rejeitar
    [InlineData(150.00, 149.98)] // R$ 0,02 abaixo - deve rejeitar
    [InlineData(150.00, 200.00)] // Valor bem diferente
    [InlineData(150.00, 100.00)] // Valor bem diferente
    public void ValidarValor_ForaTolerancia_DeveRetornarFalha(decimal valorFatura, decimal valorComprovante)
    {
        // Arrange
        var fatura = CriarFatura(valorFatura);

        // Act
        var resultado = _service.ValidarValor(valorComprovante, fatura);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(10000.00)]
    [InlineData(50000.00)]
    [InlineData(99999.99)]
    public void ValidarValor_ValoresAltos_DeveValidarCorretamente(decimal valor)
    {
        // Arrange
        var fatura = CriarFatura(valor);

        // Act
        var resultado = _service.ValidarValor(valor, fatura);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region EncontrarFaturaCorrespondenteAsync

    [Fact]
    public async Task EncontrarFaturaCorrespondenteAsync_FaturaExiste_DeveRetornarFatura()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var valorFatura = 150.00m;
        var fatura = CriarFatura(valorFatura, clienteId);
        
        _faturaRepositoryMock.Setup(x => x.ListAsync(It.IsAny<ISpecification<Fatura>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fatura> { fatura });

        // Act
        var resultado = await _service.EncontrarFaturaCorrespondenteAsync(clienteId, valorFatura);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value!.Valor.Should().Be(valorFatura);
    }

    [Fact]
    public async Task EncontrarFaturaCorrespondenteAsync_SemFaturaPendente_DeveRetornarNull()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        
        _faturaRepositoryMock.Setup(x => x.ListAsync(It.IsAny<ISpecification<Fatura>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fatura>());

        // Act
        var resultado = await _service.EncontrarFaturaCorrespondenteAsync(clienteId, 150.00m);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeNull();
    }

    [Fact]
    public async Task EncontrarFaturaCorrespondenteAsync_ValorNaoCorresponde_DeveRetornarNull()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var fatura = CriarFatura(150.00m, clienteId);
        
        _faturaRepositoryMock.Setup(x => x.ListAsync(It.IsAny<ISpecification<Fatura>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fatura> { fatura });

        // Act
        var resultado = await _service.EncontrarFaturaCorrespondenteAsync(clienteId, 200.00m);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeNull();
    }

    [Fact]
    public async Task EncontrarFaturaCorrespondenteAsync_MultiplasFaturas_DeveRetornarMaisRecente()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var fatura1 = CriarFatura(150.00m, clienteId, DateTime.UtcNow.AddDays(-10));
        var fatura2 = CriarFatura(150.00m, clienteId, DateTime.UtcNow.AddDays(5));
        var fatura3 = CriarFatura(150.00m, clienteId, DateTime.UtcNow);
        
        _faturaRepositoryMock.Setup(x => x.ListAsync(It.IsAny<ISpecification<Fatura>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fatura> { fatura1, fatura2, fatura3 });

        // Act
        var resultado = await _service.EncontrarFaturaCorrespondenteAsync(clienteId, 150.00m);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value!.DataVencimento.Should().Be(fatura2.DataVencimento);
    }

    #endregion

    #region Helpers

    private void ConfigurarConfiguracao(string chavePix, string nomeTitular)
    {
        var configuracao = new Configuracao(chavePix, nomeTitular);
        
        _cacheServiceMock.Setup(x => x.ObterConfiguracaoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuracao);
    }

    private static Fatura CriarFatura(decimal valor, Guid? clienteId = null, DateTime? vencimento = null)
    {
        return new Fatura(
            clienteId ?? Guid.NewGuid(), 
            valor, 
            vencimento ?? DateTime.UtcNow.AddDays(10));
    }

    #endregion
}
