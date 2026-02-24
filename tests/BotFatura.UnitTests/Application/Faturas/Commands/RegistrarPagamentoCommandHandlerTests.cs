using Ardalis.Result;
using BotFatura.Application.Faturas.Commands.RegistrarPagamento;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace BotFatura.UnitTests.Application.Faturas.Commands;

public class RegistrarPagamentoCommandHandlerTests
{
    private readonly Mock<IFaturaRepository> _repositoryMock;
    private readonly RegistrarPagamentoCommandHandler _handler;

    public RegistrarPagamentoCommandHandlerTests()
    {
        _repositoryMock = new Mock<IFaturaRepository>();
        _handler = new RegistrarPagamentoCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_DeveRetornarSuccess_QuandoFaturaExisteEStatusEAlterado()
    {
        // Arrange
        var faturaId = Guid.NewGuid();
        var fatura = new Fatura(Guid.NewGuid(), 100m, DateTime.UtcNow.AddDays(10));
        
        _repositoryMock.Setup(r => r.GetByIdAsync(faturaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fatura);

        var command = new RegistrarPagamentoCommand(faturaId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fatura.Status.Should().Be(StatusFatura.Paga);
        _repositoryMock.Verify(r => r.UpdateAsync(fatura, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarNotFound_QuandoFaturaNaoExiste()
    {
        // Arrange
        var faturaId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(faturaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Fatura?)null);

        var command = new RegistrarPagamentoCommand(faturaId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoFaturaJaEstaPaga()
    {
        // Arrange
        var faturaId = Guid.NewGuid();
        var fatura = new Fatura(Guid.NewGuid(), 100m, DateTime.UtcNow.AddDays(10));
        fatura.MarcarComoPaga(); // Já paga

        _repositoryMock.Setup(r => r.GetByIdAsync(faturaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fatura);

        var command = new RegistrarPagamentoCommand(faturaId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("já está paga"));
    }
}
