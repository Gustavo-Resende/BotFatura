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

// Adicionando suporte a Autenticação e Autorização
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Injecting Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddIdentityApiEndpoints<Microsoft.AspNetCore.Identity.IdentityUser>()
    .AddEntityFrameworkStores<BotFatura.Infrastructure.Data.AppDbContext>();

// Add Carter for Minimal APIs
builder.Services.AddCarter();

// Register the Background Worker for Faturas
builder.Services.AddHostedService<BotFatura.Api.Workers.FaturaReminderWorker>();

builder.Services.AddSwaggerGen(c =>
{
    var apiXmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXmlFile);
    c.IncludeXmlComments(apiXmlPath);

    var appXmlFile = "BotFatura.Application.xml";
    var appXmlPath = Path.Combine(AppContext.BaseDirectory, appXmlFile);
    c.IncludeXmlComments(appXmlPath);

    // Configuração para suportar o Token no Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Token JWT. Exemplo: Bearer {seu_token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

Console.WriteLine($"[DEBUG] Usando Connection String: {app.Configuration.GetConnectionString("DefaultConnection")}");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Mapeia rotas de login/registro nativas do .NET 8 Identity
app.MapGroup("/api/auth").MapIdentityApi<Microsoft.AspNetCore.Identity.IdentityUser>().WithTags("Auth");

// Map Carter Endpoints
app.MapCarter();

// Executar Migrações e Seeder
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<BotFatura.Infrastructure.Data.AppDbContext>();
    var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
    
    // Aplica as migrações e cria o banco se não existir
    context.Database.Migrate();

    // Criar Usuário Admin Padrão
    if (!userManager.Users.Any())
    {
        var adminUser = new Microsoft.AspNetCore.Identity.IdentityUser 
        { 
            UserName = "admin@botfatura.com.br", 
            Email = "admin@botfatura.com.br",
            EmailConfirmed = true 
        };
        userManager.CreateAsync(adminUser, "Admin@123").GetAwaiter().GetResult();
    }

    if (!context.MensagensTemplate.Any())
    {
        context.MensagensTemplate.Add(new BotFatura.Domain.Entities.MensagemTemplate(
            "Olá {NomeCliente}! 🤖\n\nIdentificamos uma fatura pendente no valor de *R$ {Valor}* com vencimento em *{Vencimento}*.\n\n*Pagamento via PIX:*\nTitular: {NomeDono}\nChave: {ChavePix}\n\nPor favor, efetue o pagamento para evitar suspensão do serviço.",
            isPadrao: true));
        context.SaveChanges();
    }
}

app.Run();
