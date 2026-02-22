using Ardalis.Result;
using Ardalis.Specification;
using BotFatura.Application.Clientes.Commands.CadastrarCliente;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace BotFatura.UnitTests.Application.Commands;

public class CadastrarClienteCommandHandlerTests
{
    private readonly Mock<IClienteRepository> _clienteRepositoryMock;
    private readonly CadastrarClienteCommandHandler _handler;

    public CadastrarClienteCommandHandlerTests()
    {
        _clienteRepositoryMock = new Mock<IClienteRepository>();
        _handler = new CadastrarClienteCommandHandler(_clienteRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_QuandoWhatsAppJaExiste_DeveRetornarConflictResult()
    {
        // Arrange
        var command = new CadastrarClienteCommand("João Silva", "+5511999999999");
        var clienteExistente = new Cliente("Zezinho", "+5511999999999");

        // Simulando que o banco achou alguém com a mesma Specification de WhatsApp
        _clienteRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<ISpecification<Cliente>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clienteExistente);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Errors.Should().Contain(e => e.Contains("já está cadastrado"));
        
        // Garante que não tentou salvar se já existia
        _clienteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_QuandoDadosValidosENaoInexistente_DeveCriarERetornarId()
    {
        // Arrange
        var command = new CadastrarClienteCommand("João Silva", "+5511999999999");

        // Simulando que não há ninguém com esse celular
        _clienteRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<ISpecification<Cliente>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty(); // Pegou o Guid do Cliente gerado
        
        // Garante que tentou gravar a entidade nova
        _clienteRepositoryMock.Verify(r => r.AddAsync(It.Is<Cliente>(c => c.NomeCompleto == "João Silva"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
