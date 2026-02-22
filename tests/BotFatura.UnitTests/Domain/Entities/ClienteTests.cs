using BotFatura.Domain.Entities;
using FluentAssertions;

namespace BotFatura.UnitTests.Domain.Entities;

public class ClienteTests
{
    [Fact]
    public void Constructor_QuandoNomeEVazio_DeveLancarArgumentException()
    {
        // Arrange
        Action action = () => new Cliente("", "123456789");

        // Act & Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*nomeCompleto*");
    }

    [Fact]
    public void Constructor_QuandoWhatsAppENulo_DeveLancarArgumentNullException()
    {
        // Arrange
        Action action = () => new Cliente("João Silva", null!);

        // Act & Assert
        action.Should().Throw<ArgumentNullException>()
            .WithMessage("*whatsApp*");
    }

    [Fact]
    public void Ativar_QuandoClienteJaEstaAtivo_DeveRetornarErroResult()
    {
        // Arrange
        var cliente = new Cliente("João Silva", "123456789");
        
        // Act
        var result = cliente.Ativar();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Cliente já está ativo"));
    }

    [Fact]
    public void Desativar_QuandoClienteEstaAtivo_DeveRetornarSuccessResultEAlterarEstado()
    {
        // Arrange
        var cliente = new Cliente("João Silva", "123456789");
        
        // Act
        var result = cliente.Desativar();

        // Assert
        result.IsSuccess.Should().BeTrue();
        cliente.Ativo.Should().BeFalse();
    }
}
