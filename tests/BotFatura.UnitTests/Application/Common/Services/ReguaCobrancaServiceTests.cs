using BotFatura.Application.Common.Services;
using BotFatura.Domain.Entities;
using FluentAssertions;

namespace BotFatura.UnitTests.Application.Common.Services;

public class ReguaCobrancaServiceTests
{
    private readonly ReguaCobrancaService _service;
    private readonly DateTime _hoje;

    public ReguaCobrancaServiceTests()
    {
        _service = new ReguaCobrancaService();
        _hoje = new DateTime(2026, 02, 24);
    }

    [Fact]
    public void Processar_DeveRetornarLembrete_QuandoFaltar3DiasParaVencimento()
    {
        // Arrange
        var faturas = new List<Fatura>
        {
            new Fatura(Guid.NewGuid(), 100, _hoje.AddDays(3))
        };

        // Act
        var resultado = _service.Processar(faturas, _hoje, 3).ToList();

        // Assert
        resultado.Should().HaveCount(1);
        resultado[0].TipoNotificacao.Should().Be("Lembrete_3_Dias");
        resultado[0].Fatura.Should().Be(faturas[0]);
    }

    [Fact]
    public void Processar_DeveRetornarCobranca_QuandoForDiaDoVencimento()
    {
        // Arrange
        var faturas = new List<Fatura>
        {
            new Fatura(Guid.NewGuid(), 100, _hoje)
        };

        // Act
        var resultado = _service.Processar(faturas, _hoje, 3).ToList();

        // Assert
        resultado.Should().HaveCount(1);
        resultado[0].TipoNotificacao.Should().Be("Cobranca_Vencimento");
        resultado[0].Fatura.Should().Be(faturas[0]);
    }

    [Fact]
    public void Processar_NaoDeveRetornarLembrete_SeJaFoiEnviado()
    {
        // Arrange
        var fatura = new Fatura(Guid.NewGuid(), 100, _hoje.AddDays(3));
        fatura.MarcarLembreteEnviado();
        var faturas = new List<Fatura> { fatura };

        // Act
        var resultado = _service.Processar(faturas, _hoje, 3).ToList();

        // Assert
        resultado.Should().BeEmpty();
    }

    [Fact]
    public void Processar_NaoDeveRetornarCobranca_SeJaFoiEnviada()
    {
        // Arrange
        var fatura = new Fatura(Guid.NewGuid(), 100, _hoje);
        fatura.MarcarCobrancaDiaEnviada();
        var faturas = new List<Fatura> { fatura };

        // Act
        var resultado = _service.Processar(faturas, _hoje, 3).ToList();

        // Assert
        resultado.Should().BeEmpty();
    }

    [Fact]
    public void Processar_NaoDeveRetornarNada_QuandoVencimentoForLonge()
    {
        // Arrange
        var faturas = new List<Fatura>
        {
            new Fatura(Guid.NewGuid(), 100, _hoje.AddDays(10)),
            new Fatura(Guid.NewGuid(), 100, _hoje.AddDays(-1))
        };

        // Act
        var resultado = _service.Processar(faturas, _hoje, 3).ToList();

        // Assert
        resultado.Should().BeEmpty();
    }
}
