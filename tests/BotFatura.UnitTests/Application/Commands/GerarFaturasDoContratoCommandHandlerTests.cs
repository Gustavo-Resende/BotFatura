using BotFatura.Application.Contratos.Commands.GerarFaturasDoContrato;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BotFatura.UnitTests.Application.Commands;

public class GerarFaturasDoContratoCommandHandlerTests
{
    private readonly Mock<IContratoRepository> _contratoRepositoryMock;
    private readonly Mock<IFaturaRepository>   _faturaRepositoryMock;
    private readonly GerarFaturasDoContratoCommandHandler _handler;

    public GerarFaturasDoContratoCommandHandlerTests()
    {
        _contratoRepositoryMock = new Mock<IContratoRepository>();
        _faturaRepositoryMock   = new Mock<IFaturaRepository>();

        _handler = new GerarFaturasDoContratoCommandHandler(
            _contratoRepositoryMock.Object,
            _faturaRepositoryMock.Object,
            NullLogger<GerarFaturasDoContratoCommandHandler>.Instance);
    }

    // ─── Cenário: sem contratos vigentes ────────────────────────────────

    [Fact]
    public async Task Handle_QuandoNenhumContratoVigente_NaoDeveCriarNenhumaFatura()
    {
        // Arrange
        _contratoRepositoryMock
            .Setup(r => r.ListarVigentesParaGerarFaturaAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var resultado = await _handler.Handle(new GerarFaturasDoContratoCommand(), CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _faturaRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Fatura>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── Cenário: idempotência ────────────────────────────────────────────

    [Fact]
    public async Task Handle_QuandoFaturaDoMesJaExisteParaOContrato_NaoDeveCriarNovamente()
    {
        // Arrange — simula contrato com uma fatura já gerada para o mês de referência
        var dataReferencia  = new DateOnly(2026, 3, 20);
        var clienteId       = Guid.NewGuid();
        var contratoId      = Guid.NewGuid();

        // A fatura "fantasma" representa a que já foi criada no ciclo anterior
        var faturaExistente = new Fatura(clienteId, 500m,
            new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            contratoId: contratoId);

        var contrato = CriarContratoComFatura(
            contratoId, clienteId,
            diaVencimento: 20,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: new DateOnly(2026, 6, 30),
            faturas: [faturaExistente]);

        _contratoRepositoryMock
            .Setup(r => r.ListarVigentesParaGerarFaturaAsync(dataReferencia, It.IsAny<CancellationToken>()))
            .ReturnsAsync([contrato]);

        // Act
        await _handler.Handle(new GerarFaturasDoContratoCommand(dataReferencia), CancellationToken.None);

        // Assert — idempotência: não criou segunda fatura
        _faturaRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Fatura>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Não deve gerar duplicata quando já existe fatura para o mês/ano do contrato.");
    }

    // ─── Cenário: geração bem-sucedida ────────────────────────────────────

    [Fact]
    public async Task Handle_QuandoContratoVigenteESemFaturaDoMes_DeveCriarUmaFatura()
    {
        // Arrange
        var dataReferencia = new DateOnly(2026, 3, 20);
        var clienteId      = Guid.NewGuid();
        var contratoId     = Guid.NewGuid();

        // Contrato sem faturas carregadas = ainda não gerou para este mês
        var contrato = CriarContratoComFatura(
            contratoId, clienteId,
            diaVencimento: 20,
            dataInicio: new DateOnly(2026, 1, 1),
            dataFim: null,
            faturas: []);

        _contratoRepositoryMock
            .Setup(r => r.ListarVigentesParaGerarFaturaAsync(dataReferencia, It.IsAny<CancellationToken>()))
            .ReturnsAsync([contrato]);

        // Act
        var resultado = await _handler.Handle(new GerarFaturasDoContratoCommand(dataReferencia), CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _faturaRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<Fatura>(f =>
                    f.ClienteId  == clienteId  &&
                    f.ContratoId == contratoId &&
                    f.DataVencimento.Month == 3 &&
                    f.DataVencimento.Year  == 2026),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Deve criar exatamente uma fatura com os dados corretos do contrato.");
    }

    // ─── Cenário: dois contratos, apenas um sem fatura ────────────────────

    [Fact]
    public async Task Handle_QuandoDoisContratosMasApenasUmSemFatura_DeveCriarApenasFaltante()
    {
        // Arrange — USB: worker rodou com atraso, um contrato já tem fatura, outro não
        var dataReferencia = new DateOnly(2026, 3, 20);
        var clienteId1     = Guid.NewGuid();
        var clienteId2     = Guid.NewGuid();
        var contratoId1    = Guid.NewGuid();
        var contratoId2    = Guid.NewGuid();

        var faturaJaGerada = new Fatura(clienteId1, 400m,
            new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            contratoId: contratoId1);

        var contratoComFatura   = CriarContratoComFatura(contratoId1, clienteId1, 20, new DateOnly(2026, 1, 1), null, [faturaJaGerada]);
        var contratoSemFatura   = CriarContratoComFatura(contratoId2, clienteId2, 20, new DateOnly(2026, 1, 1), null, []);

        _contratoRepositoryMock
            .Setup(r => r.ListarVigentesParaGerarFaturaAsync(dataReferencia, It.IsAny<CancellationToken>()))
            .ReturnsAsync([contratoComFatura, contratoSemFatura]);

        // Act
        await _handler.Handle(new GerarFaturasDoContratoCommand(dataReferencia), CancellationToken.None);

        // Assert — apenas a fatura do contrato sem cobertura deve ser criada
        _faturaRepositoryMock.Verify(
            r => r.AddAsync(It.Is<Fatura>(f => f.ContratoId == contratoId2), It.IsAny<CancellationToken>()),
            Times.Once);
        _faturaRepositoryMock.Verify(
            r => r.AddAsync(It.Is<Fatura>(f => f.ContratoId == contratoId1), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── Cenário: DataReferencia customizada (override manual) ────────────

    [Fact]
    public async Task Handle_QuandoDataReferenciaEInformadaManualmente_DeveUsarEssaDataEmVezDeHojeMais3()
    {
        // Arrange — permite forçar geração para uma data específica (útil para backfill)
        var dataForcada = new DateOnly(2026, 1, 10);
        var contratoId  = Guid.NewGuid();
        var clienteId   = Guid.NewGuid();

        var contrato = CriarContratoComFatura(contratoId, clienteId, diaVencimento: 10,
            dataInicio: new DateOnly(2026, 1, 1), dataFim: null, faturas: []);

        _contratoRepositoryMock
            .Setup(r => r.ListarVigentesParaGerarFaturaAsync(dataForcada, It.IsAny<CancellationToken>()))
            .ReturnsAsync([contrato]);

        // Act
        await _handler.Handle(new GerarFaturasDoContratoCommand(dataForcada), CancellationToken.None);

        // Assert — deve ter consultado o repo com a data EXATA passada, e não hoje+3
        _contratoRepositoryMock.Verify(
            r => r.ListarVigentesParaGerarFaturaAsync(dataForcada, It.IsAny<CancellationToken>()),
            Times.Once);
        _faturaRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Fatura>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria um Contrato e injeta a coleção de Faturas e o Id via reflection,
    /// simulando o que o EF Core faria ao carregar do banco com Include filtrado.
    /// </summary>
    private static Contrato CriarContratoComFatura(
        Guid contratoId, Guid clienteId, int diaVencimento,
        DateOnly dataInicio, DateOnly? dataFim, IEnumerable<Fatura> faturas)
    {
        var contrato = new Contrato(clienteId, 500m, diaVencimento, dataInicio, dataFim);

        // Força o Id (protected setter via Entity base class)
        var idProperty = typeof(Contrato).BaseType!
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        idProperty!.SetValue(contrato, contratoId);

        // Injeta a coleção de Faturas (ICollection<Fatura> com setter privado)
        var faturasProperty = typeof(Contrato)
            .GetProperty("Faturas", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        faturasProperty!.SetValue(contrato, new List<Fatura>(faturas));

        return contrato;
    }
}
