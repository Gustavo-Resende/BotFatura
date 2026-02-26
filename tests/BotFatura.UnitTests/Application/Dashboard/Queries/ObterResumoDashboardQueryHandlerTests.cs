using BotFatura.Application.Common.Models;
using BotFatura.Application.Dashboard.Queries.ObterResumoDashboard;
using BotFatura.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace BotFatura.UnitTests.Application.Dashboard.Queries;

public class ObterResumoDashboardQueryHandlerTests
{
    private readonly Mock<IFaturaRepository>   _faturaRepositoryMock;
    private readonly Mock<IClienteRepository>  _clienteRepositoryMock;
    private readonly ObterResumoDashboardQueryHandler _handler;

    public ObterResumoDashboardQueryHandlerTests()
    {
        _faturaRepositoryMock  = new Mock<IFaturaRepository>();
        _clienteRepositoryMock = new Mock<IClienteRepository>();
        _handler = new ObterResumoDashboardQueryHandler(
            _faturaRepositoryMock.Object,
            _clienteRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_QuandoExistemFaturasEClientes_DeveRetornarResumoConsolidadoCorreto()
    {
        // Arrange — o handler usa ObterDadosConsolidadosDashboardAsync, não métodos individuais
        var dadosConsolidados = new FaturaDadosConsolidados
        {
            TotalPendente         = 1500m,
            TotalVencendoHoje     = 500m,
            TotalPago             = 2000m,
            TotalAtrasado         = 300m,
            FaturasPendentesCount = 5,
            FaturasAtrasadasCount = 2
        };

        _faturaRepositoryMock
            .Setup(r => r.ObterDadosConsolidadosDashboardAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dadosConsolidados);

        _clienteRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _handler.Handle(new ObterResumoDashboardQuery(), CancellationToken.None);

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

    [Fact]
    public async Task Handle_QuandoNaoExistemFaturasNemClientes_DeveRetornarTudoZerado()
    {
        // Arrange — USB: banco vazio, zero dados
        _faturaRepositoryMock
            .Setup(r => r.ObterDadosConsolidadosDashboardAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FaturaDadosConsolidados());

        _clienteRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(new ObterResumoDashboardQuery(), CancellationToken.None);

        // Assert
        result.TotalPendente.Should().Be(0m);
        result.TotalPago.Should().Be(0m);
        result.ClientesAtivosCount.Should().Be(0);
        result.FaturasPendentesCount.Should().Be(0);
    }
}
