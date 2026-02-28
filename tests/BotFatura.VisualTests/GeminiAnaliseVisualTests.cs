using BotFatura.Application.Common.Interfaces;
using BotFatura.Infrastructure.Services;
using BotFatura.TestUtils.Cenarios;
using BotFatura.TestUtils.Geradores;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BotFatura.VisualTests;

/// <summary>
/// Testes visuais que chamam a API Gemini real com comprovantes sintéticos.
/// 
/// IMPORTANTE:
/// - Estes testes têm custo ($) - execute apenas quando necessário
/// - Use a categoria "Visual" para filtrar: dotnet test --filter "Category=Visual"
/// - Certifique-se de ter a variável GEMINI_API_KEY configurada
/// - Custo estimado: ~R$ 0,05 - 0,10 por teste
/// </summary>
[Trait("Category", "Visual")]
public class GeminiAnaliseVisualTests : IClassFixture<GeminiTestFixture>
{
    private readonly GeminiTestFixture _fixture;
    private readonly ComprovanteGenerator _generator;

    public GeminiAnaliseVisualTests(GeminiTestFixture fixture)
    {
        _fixture = fixture;
        _generator = new ComprovanteGenerator();
    }

    [Fact(Skip = "Executar apenas sob demanda - tem custo")]
    public async Task AnalisarComprovante_ComprovantePixValido_DeveRetornarDadosCorretos()
    {
        // Arrange
        var parametros = new ComprovanteParametros
        {
            Valor = 150.00m,
            NomePagador = "João da Silva",
            NomeDestinatario = "Empresa BotFatura LTDA",
            ChavePixDestinatario = "pix@botfatura.com.br",
            TipoPagamento = "PIX"
        };
        var imagemComprovante = _generator.GerarComprovantePix(parametros);

        // Act
        var resultado = await _fixture.GeminiClient.AnalisarComprovanteAsync(
            imagemComprovante, 
            "image/png", 
            CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue("A análise deve ser bem-sucedida");
        resultado.Value.IsComprovante.Should().BeTrue("Deve reconhecer como comprovante");
        resultado.Value.Valor.Should().BeApproximately(150.00m, 0.10m, "Valor deve estar próximo de R$ 150,00");
        resultado.Value.TipoPagamento.Should().NotBeNullOrEmpty();
        resultado.Value.Confianca.Should().BeGreaterThan(50);
    }

    [Fact(Skip = "Executar apenas sob demanda - tem custo")]
    public async Task AnalisarComprovante_ImagemNaoComprovante_DeveRetornarFalse()
    {
        // Arrange
        var imagemNaoComprovante = _generator.GerarImagemNaoComprovante();

        // Act
        var resultado = await _fixture.GeminiClient.AnalisarComprovanteAsync(
            imagemNaoComprovante, 
            "image/png", 
            CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue("A chamada à API deve ser bem-sucedida");
        resultado.Value.IsComprovante.Should().BeFalse("Não deve reconhecer como comprovante");
    }

    [Theory(Skip = "Executar apenas sob demanda - tem custo")]
    [InlineData(100.00)]
    [InlineData(500.00)]
    [InlineData(1000.00)]
    [InlineData(5000.00)]
    [InlineData(10000.00)]
    public async Task AnalisarComprovante_DiferentesValores_DeveExtrairValorCorreto(decimal valor)
    {
        // Arrange
        var parametros = new ComprovanteParametros
        {
            Valor = valor,
            NomePagador = "Cliente Teste",
            NomeDestinatario = "Empresa BotFatura",
            ChavePixDestinatario = "pix@botfatura.com.br"
        };
        var imagemComprovante = _generator.GerarComprovantePix(parametros);

        // Act
        var resultado = await _fixture.GeminiClient.AnalisarComprovanteAsync(
            imagemComprovante, 
            "image/png", 
            CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.IsComprovante.Should().BeTrue();
        resultado.Value.Valor.Should().BeApproximately(valor, valor * 0.01m, 
            $"Valor extraído deve estar dentro de 1% de tolerância de {valor:C}");
    }

    [Theory(Skip = "Executar apenas sob demanda - tem custo")]
    [InlineData("João da Silva")]
    [InlineData("Maria Santos Oliveira")]
    [InlineData("EMPRESA TESTE LTDA ME")]
    [InlineData("José Carlos Pereira Junior")]
    public async Task AnalisarComprovante_DiferentesNomes_DeveExtrairNomeCorreto(string nomePagador)
    {
        // Arrange
        var parametros = new ComprovanteParametros
        {
            Valor = 150.00m,
            NomePagador = nomePagador,
            NomeDestinatario = "Empresa BotFatura",
            ChavePixDestinatario = "pix@botfatura.com.br"
        };
        var imagemComprovante = _generator.GerarComprovantePix(parametros);

        // Act
        var resultado = await _fixture.GeminiClient.AnalisarComprovanteAsync(
            imagemComprovante, 
            "image/png", 
            CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.DadosPagador.Should().NotBeNull();
        resultado.Value.DadosPagador!.Nome.Should().NotBeNullOrEmpty();
        // Verificar que o nome extraído contém pelo menos parte do nome original
        resultado.Value.DadosPagador.Nome!.ToLower().Should().ContainAny(
            nomePagador.ToLower().Split(' ').Where(p => p.Length > 2).ToArray());
    }

    [Fact(Skip = "Executar apenas sob demanda - tem custo")]
    public async Task AnalisarComprovante_DeveExtrairChavePix()
    {
        // Arrange
        var chavePixEsperada = "teste@empresa.com.br";
        var parametros = new ComprovanteParametros
        {
            Valor = 150.00m,
            NomePagador = "Cliente Teste",
            NomeDestinatario = "Empresa BotFatura",
            ChavePixDestinatario = chavePixEsperada
        };
        var imagemComprovante = _generator.GerarComprovantePix(parametros);

        // Act
        var resultado = await _fixture.GeminiClient.AnalisarComprovanteAsync(
            imagemComprovante, 
            "image/png", 
            CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.DadosDestinatario.Should().NotBeNull();
        resultado.Value.DadosDestinatario!.ChavePix.Should().NotBeNullOrEmpty();
        resultado.Value.DadosDestinatario.ChavePix!.ToLower()
            .Should().Contain(chavePixEsperada.ToLower());
    }

    [Fact(Skip = "Executar apenas sob demanda - tem custo")]
    public async Task SuiteCompleta_ExecutarTodosCenarios_DevePassarTodos()
    {
        // Arrange - Todos os cenários de teste
        var cenarios = new List<(string Nome, ComprovanteParametros Parametros, bool DeveSerComprovante)>
        {
            ("PIX R$ 100", new ComprovanteParametros { Valor = 100.00m }, true),
            ("PIX R$ 500", new ComprovanteParametros { Valor = 500.00m }, true),
            ("PIX R$ 1.000", new ComprovanteParametros { Valor = 1000.00m }, true),
            ("PIX R$ 10.000", new ComprovanteParametros { Valor = 10000.00m }, true),
            ("PIX R$ 50.000", new ComprovanteParametros { Valor = 50000.00m }, true),
            ("Nome simples", new ComprovanteParametros { NomePagador = "João" }, true),
            ("Nome composto", new ComprovanteParametros { NomePagador = "João da Silva Santos" }, true),
            ("Empresa", new ComprovanteParametros { NomePagador = "EMPRESA TESTE LTDA" }, true),
        };

        var resultados = new List<(string Cenario, bool Sucesso, string? Erro)>();

        // Act
        foreach (var (nome, parametros, deveSerComprovante) in cenarios)
        {
            try
            {
                var imagem = _generator.GerarComprovantePix(parametros);
                var resultado = await _fixture.GeminiClient.AnalisarComprovanteAsync(
                    imagem, "image/png", CancellationToken.None);

                if (resultado.IsSuccess)
                {
                    var passou = resultado.Value.IsComprovante == deveSerComprovante;
                    resultados.Add((nome, passou, passou ? null : $"IsComprovante={resultado.Value.IsComprovante}, esperado={deveSerComprovante}"));
                }
                else
                {
                    resultados.Add((nome, false, string.Join(", ", resultado.Errors)));
                }

                // Delay para evitar rate limiting
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                resultados.Add((nome, false, ex.Message));
            }
        }

        // Assert
        var falhas = resultados.Where(r => !r.Sucesso).ToList();
        falhas.Should().BeEmpty(
            $"Todos os cenários devem passar. Falhas: {string.Join("; ", falhas.Select(f => $"{f.Cenario}: {f.Erro}"))}");
    }
}

/// <summary>
/// Fixture que configura o GeminiApiClient real para os testes visuais
/// </summary>
public class GeminiTestFixture : IDisposable
{
    public IGeminiApiClient GeminiClient { get; }
    private readonly ServiceProvider _serviceProvider;

    public GeminiTestFixture()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddDebug());
        services.AddHttpClient<IGeminiApiClient, GeminiApiClient>();

        _serviceProvider = services.BuildServiceProvider();
        GeminiClient = _serviceProvider.GetRequiredService<IGeminiApiClient>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}

/// <summary>
/// Extensões para asserções mais expressivas
/// </summary>
internal static class FluentAssertionsExtensions
{
    public static void ContainAny(this FluentAssertions.Primitives.StringAssertions assertions, string[] substrings)
    {
        var subject = assertions.Subject?.ToLower() ?? "";
        var contains = substrings.Any(s => subject.Contains(s.ToLower()));
        contains.Should().BeTrue($"A string '{assertions.Subject}' deveria conter pelo menos um de: {string.Join(", ", substrings)}");
    }
}
