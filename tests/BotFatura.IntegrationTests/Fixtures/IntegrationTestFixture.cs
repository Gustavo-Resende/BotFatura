using Ardalis.Specification;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Comprovantes.Services;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using BotFatura.IntegrationTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace BotFatura.IntegrationTests.Fixtures;

/// <summary>
/// Fixture para testes de integração que configura todas as dependências necessárias
/// </summary>
public class IntegrationTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    public FakeGeminiApiClient FakeGemini { get; }
    public FakeEvolutionApiClient FakeEvolution { get; }
    public Mock<IFaturaRepository> FaturaRepositoryMock { get; }
    public Mock<IClienteRepository> ClienteRepositoryMock { get; }
    public Mock<ICacheService> CacheServiceMock { get; }
    public Mock<IDateTimeProvider> DateTimeProviderMock { get; }
    public Mock<IUnitOfWork> UnitOfWorkMock { get; }

    public IntegrationTestFixture()
    {
        FakeGemini = new FakeGeminiApiClient();
        FakeEvolution = new FakeEvolutionApiClient();
        FaturaRepositoryMock = new Mock<IFaturaRepository>();
        ClienteRepositoryMock = new Mock<IClienteRepository>();
        CacheServiceMock = new Mock<ICacheService>();
        DateTimeProviderMock = new Mock<IDateTimeProvider>();
        UnitOfWorkMock = new Mock<IUnitOfWork>();

        var services = new ServiceCollection();

        // Registrar fakes
        services.AddSingleton<IGeminiApiClient>(FakeGemini);
        services.AddSingleton<IEvolutionApiClient>(FakeEvolution);

        // Registrar mocks
        services.AddSingleton(FaturaRepositoryMock.Object);
        services.AddSingleton(ClienteRepositoryMock.Object);
        services.AddSingleton(CacheServiceMock.Object);
        services.AddSingleton(DateTimeProviderMock.Object);
        services.AddSingleton(UnitOfWorkMock.Object);

        // Registrar loggers
        services.AddLogging(builder => builder.AddDebug());

        // Registrar serviços reais
        services.AddScoped<ComprovanteValidationService>();

        ServiceProvider = services.BuildServiceProvider();

        // Configurações padrão
        ConfigurarPadrao();
    }

    /// <summary>
    /// Configura os mocks com valores padrão para cenários comuns
    /// </summary>
    private void ConfigurarPadrao()
    {
        // DateTime
        DateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        DateTimeProviderMock.Setup(x => x.Today).Returns(DateTime.Today);

        // UnitOfWork
        UnitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        UnitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        UnitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        UnitOfWorkMock.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Configura uma configuração do sistema para os testes
    /// </summary>
    public void ConfigurarConfiguracao(string chavePix = "pix@botfatura.com.br", string nomeTitular = "Empresa BotFatura")
    {
        var configuracao = new Configuracao(chavePix, nomeTitular);
        
        CacheServiceMock.Setup(x => x.ObterConfiguracaoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuracao);
    }

    /// <summary>
    /// Configura um cliente para os testes
    /// </summary>
    public Cliente ConfigurarCliente(string nome = "Cliente Teste", string whatsApp = "11999999999")
    {
        var cliente = new Cliente(nome, whatsApp, $"55{whatsApp}@s.whatsapp.net");
        
        ClienteRepositoryMock.Setup(x => x.BuscarPorWhatsAppJidAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);
        
        return cliente;
    }

    /// <summary>
    /// Configura uma fatura pendente para os testes
    /// </summary>
    public Fatura ConfigurarFaturaPendente(Guid clienteId, decimal valor, DateTime? vencimento = null)
    {
        var fatura = new Fatura(clienteId, valor, vencimento ?? DateTime.UtcNow.AddDays(10));
        
        FaturaRepositoryMock.Setup(x => x.ListAsync(It.IsAny<ISpecification<Fatura>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fatura> { fatura });
        
        return fatura;
    }

    /// <summary>
    /// Reseta todos os fakes e mocks para o estado inicial
    /// </summary>
    public void Reset()
    {
        FakeGemini.Reset();
        FakeEvolution.Reset();
        FaturaRepositoryMock.Reset();
        ClienteRepositoryMock.Reset();
        CacheServiceMock.Reset();
        ConfigurarPadrao();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
