using BotFatura.Application;
using BotFatura.Infrastructure;
using Carter;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Adicionar suporte a configurações locais (Local.json) que não vão para o Git
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

// Configurando CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Adicionando suporte a Autenticação e Autorização
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = System.Text.Encoding.ASCII.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(secretKey),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

// Injetando as camadas
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddScoped<BotFatura.Application.Common.Interfaces.IAuthService, BotFatura.Api.Services.AuthService>();

// Adicionando Carter para Minimal APIs
builder.Services.AddCarter();

// Registrando o Background Worker para Faturas
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

// Configurando o pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

// Carter Endpoints irão lidar com a autenticação

// Mapeando os endpoints do Carter
app.MapCarter();

// Inicializar o Banco de Dados (Migrações e Seeding)
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<BotFatura.Infrastructure.Data.IDbInitializer>();
    await initializer.InitializeAsync();
}

app.Run();
