using BotFatura.Domain.Entities;
using FluentAssertions;

namespace BotFatura.UnitTests.Domain.Entities;

public class FaturaTests
{
    [Fact]
    public void Constructor_QuandoValorENegativo_DeveLancarArgumentException()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        Action action = () => new Fatura(clienteId, -10.50m, DateTime.UtcNow.AddDays(10));

        // Act & Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_QuandoValorEZero_DeveLancarArgumentException()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        Action action = () => new Fatura(clienteId, 0m, DateTime.UtcNow.AddDays(10));

        // Act & Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Cancelar_QuandoFaturaJaEstaPaga_DeveRetornarErroResult()
    {
        // Arrange
        var fatura = new Fatura(Guid.NewGuid(), 100m, DateTime.UtcNow.AddDays(10));
        fatura.MarcarComoPaga(); // Forçando estado

        // Act
        var result = fatura.Cancelar();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("já paga não pode ser cancelada"));
    }
}
