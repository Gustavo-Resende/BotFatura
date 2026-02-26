using BotFatura.Domain.Entities;
using FluentAssertions;

namespace BotFatura.UnitTests.Domain.Entities;

public class ContratoTests
{
    // ─── Construtor ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_QuandoDiaVencimentoMaiorQue28_DeveLancarArgumentException()
    {
        // Arrange
        Action action = () => new Contrato(
            Guid.NewGuid(), valorMensal: 500m,
            diaVencimento: 29,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: null);

        // Act & Assert
        action.Should().Throw<ArgumentException>()
              .WithMessage("*diaVencimento*");
    }

    [Fact]
    public void Constructor_QuandoDiaVencimentoZero_DeveLancarArgumentException()
    {
        // Arrange
        Action action = () => new Contrato(
            Guid.NewGuid(), valorMensal: 500m,
            diaVencimento: 0,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: null);

        // Act & Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_QuandoValorMensalEZero_DeveLancarArgumentException()
    {
        // Arrange
        Action action = () => new Contrato(
            Guid.NewGuid(), valorMensal: 0m,
            diaVencimento: 15,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: null);

        // Act & Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_QuandoDataFimAnteriorADataInicio_DeveLancarArgumentException()
    {
        // Arrange — usuário tenta criar contrato com data fim no passado em relação ao início
        Action action = () => new Contrato(
            Guid.NewGuid(), valorMensal: 300m,
            diaVencimento: 10,
            dataInicio: new DateOnly(2026, 6, 1),
            dataFim: new DateOnly(2026, 5, 31));

        // Act & Assert
        action.Should().Throw<ArgumentException>()
              .WithMessage("*data de fim*");
    }

    [Fact]
    public void Constructor_QuandoDadosValidos_DeveIniciarComAtivoVerdadeiro()
    {
        // Arrange & Act
        var contrato = new Contrato(
            Guid.NewGuid(), valorMensal: 750m,
            diaVencimento: 20,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: new DateOnly(2026, 6, 30));

        // Assert
        contrato.Ativo.Should().BeTrue();
        contrato.ValorMensal.Should().Be(750m);
        contrato.DiaVencimento.Should().Be(20);
    }

    // ─── EstaVigente ───────────────────────────────────────────────────────────

    [Fact]
    public void EstaVigente_QuandoContratoAtivoEDataDentroDoIntervalo_DeveRetornarVerdadeiro()
    {
        // Arrange
        var contrato = new Contrato(
            Guid.NewGuid(), 500m, 15,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: new DateOnly(2026, 6, 30));

        // Act & Assert
        contrato.EstaVigente(new DateOnly(2026, 3, 15)).Should().BeTrue();
    }

    [Fact]
    public void EstaVigente_QuandoDataReferenciaAntesDoInicio_DeveRetornarFalso()
    {
        // Arrange — contrato começa no futuro
        var contrato = new Contrato(
            Guid.NewGuid(), 500m, 15,
            dataInicio: new DateOnly(2026, 6, 1),
            dataFim: null);

        // Act & Assert
        contrato.EstaVigente(new DateOnly(2026, 3, 15)).Should().BeFalse();
    }

    [Fact]
    public void EstaVigente_QuandoDataReferenciaDepoisDoFim_DeveRetornarFalso()
    {
        // Arrange — contrato de 6 meses que já expirou
        var contrato = new Contrato(
            Guid.NewGuid(), 500m, 15,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: new DateOnly(2026, 6, 30));

        // Act & Assert
        contrato.EstaVigente(new DateOnly(2026, 7, 1)).Should().BeFalse();
    }

    [Fact]
    public void EstaVigente_QuandoContratoEncerradoProgramaticamente_DeveRetornarFalso()
    {
        // Arrange
        var contrato = new Contrato(
            Guid.NewGuid(), 500m, 15,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: null);

        contrato.Encerrar();

        // Act & Assert
        contrato.EstaVigente(new DateOnly(2026, 3, 15)).Should().BeFalse();
    }

    [Fact]
    public void EstaVigente_ContratoPorPrazoIndeterminado_DeveRetornarVerdadeiroParaFuturoLonginquo()
    {
        // Arrange — sem DataFim = vigente para sempre
        var contrato = new Contrato(
            Guid.NewGuid(), 200m, 5,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: null);

        // Act & Assert
        contrato.EstaVigente(new DateOnly(2030, 12, 31)).Should().BeTrue();
    }

    // ─── CalcularVencimentoDoMes ────────────────────────────────────────────────

    [Theory]
    [InlineData(2026, 3, 20, 2026, 3, 20)]  // mês normal — dia exato
    [InlineData(2026, 2, 28, 2026, 2, 28)]  // fevereiro com dia 28 — sem perda
    [InlineData(2026, 4, 28, 2026, 4, 28)]  // abril com 30 dias — dia 28 existe
    public void CalcularVencimentoDoMes_QuandoDiaExisteNoMes_DeveRetornarDiaExato(
        int anoEsperado, int mesEsperado, int diaEsperado,
        int anoCalculo, int mesCalculo, int diaVencimento)
    {
        // Arrange
        var contrato = new Contrato(
            Guid.NewGuid(), 300m,
            diaVencimento: diaVencimento,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: null);

        // Act
        var vencimento = contrato.CalcularVencimentoDoMes(anoCalculo, mesCalculo);

        // Assert
        vencimento.Should().Be(new DateOnly(anoEsperado, mesEsperado, diaEsperado));
    }

    [Fact]
    public void CalcularVencimentoDoMes_ContratoComDia28_EmFevereiro_DeveRetornarDia28()
    {
        // Arrange — situação crítica: DiaVencimento no limite (28) em fevereiro
        var contrato = new Contrato(
            Guid.NewGuid(), 500m,
            diaVencimento: 28,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: null);

        // Act
        var vencimento = contrato.CalcularVencimentoDoMes(ano: 2026, mes: 2);

        // Assert
        vencimento.Should().Be(new DateOnly(2026, 2, 28));
        vencimento.Month.Should().Be(2);
    }

    // ─── Encerrar ──────────────────────────────────────────────────────────────

    [Fact]
    public void Encerrar_QuandoContratoAtivo_DeveMarcarComoInativo()
    {
        // Arrange
        var contrato = new Contrato(
            Guid.NewGuid(), 500m, 15,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: null);

        // Act
        var resultado = contrato.Encerrar();

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        contrato.Ativo.Should().BeFalse();
        contrato.DataFim.Should().NotBeNull();
    }

    [Fact]
    public void Encerrar_QuandoContratoJaEncerrado_DeveRetornarErroSemModificarEstado()
    {
        // Arrange — USB: usuário clica em encerrar duas vezes
        var contrato = new Contrato(
            Guid.NewGuid(), 500m, 15,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: null);

        contrato.Encerrar(); // primeiro encerramento

        // Act
        var segundoEncerramento = contrato.Encerrar();

        // Assert
        segundoEncerramento.IsSuccess.Should().BeFalse();
        segundoEncerramento.Errors.Should().Contain(e => e.Contains("já está encerrado"));
    }
}
