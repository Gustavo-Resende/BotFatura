using BotFatura.Application;
using BotFatura.Infrastructure;
using Carter;
using Microsoft.EntityFrameworkCore;

// Corrigir erro de DateTime no Npgsql/PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);


// Adicionar suporte a configurações locais (Local.json) que não vão para o Git
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var apiXmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXmlFile);
    c.IncludeXmlComments(apiXmlPath);

    var appXmlFile = "BotFatura.Application.xml";
    var appXmlPath = Path.Combine(AppContext.BaseDirectory, appXmlFile);
    c.IncludeXmlComments(appXmlPath);
});

// Injecting Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

Console.WriteLine($"[DEBUG] Usando Connection String: {builder.Configuration.GetConnectionString("DefaultConnection")}");

// Add Carter for Minimal APIs
builder.Services.AddCarter();

// Register the Background Worker for Faturas
builder.Services.AddHostedService<BotFatura.Api.Workers.FaturaReminderWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map Carter Endpoints
app.MapCarter();

// Executar Migrações e Seeder
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<BotFatura.Infrastructure.Data.AppDbContext>();
    
    // Aplica as migrações e cria o banco se não existir (O erro anterior era por falta de tabelas)
    context.Database.Migrate();

    if (!context.MensagensTemplate.Any())
    {
        context.MensagensTemplate.Add(new BotFatura.Domain.Entities.MensagemTemplate(
            "Olá {NomeCliente}! 🤖\n\nIdentificamos uma fatura pendente no valor de *R$ {Valor}* com vencimento em *{Vencimento}*.\n\n*Pagamento via PIX:*\nTitular: {NomeDono}\nChave: {ChavePix}\n\nPor favor, efetue o pagamento para evitar suspensão do serviço.",
            isPadrao: true));
        context.SaveChanges();
    }
}

app.Run();
