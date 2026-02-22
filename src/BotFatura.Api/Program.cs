using BotFatura.Application;
using BotFatura.Infrastructure;
using Carter;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
            "Olá {NomeCliente}! 🤖\n\nIdentificamos uma fatura pendente no valor de *R$ {Valor}* com vencimento em *{Vencimento}*.\n\nPor favor, efetue o pagamento para evitar suspensão do serviço.",
            isPadrao: true));
        context.SaveChanges();
    }
}

app.Run();
