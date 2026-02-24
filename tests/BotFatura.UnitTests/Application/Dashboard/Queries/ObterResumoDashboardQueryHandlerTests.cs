using BotFatura.Application.Common.Models;
using BotFatura.Application.Dashboard.Queries.ObterResumoDashboard;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Enums;
using BotFatura.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace BotFatura.UnitTests.Application.Dashboard.Queries;

public class ObterResumoDashboardQueryHandlerTests
{
    private readonly Mock<IFaturaRepository> _faturaRepositoryMock;
    private readonly Mock<IClienteRepository> _clienteRepositoryMock;
    private readonly ObterResumoDashboardQueryHandler _handler;

    public ObterResumoDashboardQueryHandlerTests()
    {
        _faturaRepositoryMock = new Mock<IFaturaRepository>();
        _clienteRepositoryMock = new Mock<IClienteRepository>();
        _handler = new ObterResumoDashboardQueryHandler(_faturaRepositoryMock.Object, _clienteRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_DeveRetornarResumoCorreto_QuandoExistemFaturasEClientes()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var pendentesOuEnviadasStatus = new[] { StatusFatura.Pendente, StatusFatura.Enviada };
        var pagasStatus = new[] { StatusFatura.Paga };

        _faturaRepositoryMock.Setup(r => r.ObterSomaPorStatusAsync(pendentesOuEnviadasStatus, cancellationToken))
            .ReturnsAsync(1500m);
        _faturaRepositoryMock.Setup(r => r.ObterSomaVencendoHojeAsync(pendentesOuEnviadasStatus, cancellationToken))
            .ReturnsAsync(500m);
        _faturaRepositoryMock.Setup(r => r.ObterSomaPorStatusAsync(pagasStatus, cancellationToken))
            .ReturnsAsync(2000m);
        _faturaRepositoryMock.Setup(r => r.ObterSomaAtrasadasAsync(pendentesOuEnviadasStatus, cancellationToken))
            .ReturnsAsync(300m);
        
        _clienteRepositoryMock.Setup(r => r.CountAsync(cancellationToken))
            .ReturnsAsync(10);
        _faturaRepositoryMock.Setup(r => r.ObterContagemPorStatusAsync(pendentesOuEnviadasStatus, cancellationToken))
            .ReturnsAsync(5);
        _faturaRepositoryMock.Setup(r => r.ObterContagemAtrasadasAsync(pendentesOuEnviadasStatus, cancellationToken))
            .ReturnsAsync(2);

        var query = new ObterResumoDashboardQuery();

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.TotalPendente.Should().Be(1500m);
        result.TotalVencendoHoje.Should().Be(500m);
        result.TotalPago.Should().Be(2000m);
        result.TotalAtrasado.Should().Be(300m);
        result.ClientesAtivosCount.Should().Be(10);
        result.FaturasPendentesCount.Should().Be(5);
        result.FaturasAtrasadasCount.Should().Be(2);
    }
}
