using System.Text.Json;
using BotFatura.Application.Common.Interfaces;
using BotFatura.Application.Comprovantes.Commands.ProcessarComprovante;
using BotFatura.Application.Comprovantes.Services;
using BotFatura.Domain.Entities;
using BotFatura.Domain.Interfaces;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;

namespace BotFatura.Api.Endpoints;

public class TestEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/test").WithTags("Test");

        group.MapPost("/gemini/url", async (
            [FromBody] TestGeminiUrlRequest request,
            IGeminiApiClient geminiApiClient,
            ILogger<IGeminiApiClient> logger) =>
        {
            try
            {
                logger.LogInformation("Testando Gemini com URL: {Url}", request.ImageUrl);

                // Baixar imagem da URL
                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(request.ImageUrl);

                logger.LogInformation("Imagem baixada. Tamanho: {Size} KB", imageBytes.Length / 1024.0);

                // Analisar com Gemini
                var resultado = await geminiApiClient.AnalisarComprovanteAsync(imageBytes, "image/jpeg", default);

                if (resultado.IsSuccess)
                {
                    return Results.Ok(new
                    {
                        success = true,
                        data = resultado.Value,
                        message = "Análise realizada com sucesso!"
                    });
                }
                else
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        errors = resultado.Errors,
                        message = "Falha na análise"
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao testar Gemini");
                return Results.BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    message = "Erro ao processar"
                });
            }
        })
        .WithSummary("Testa o Gemini com uma imagem via URL")
        .WithDescription("Baixa uma imagem da URL fornecida e envia para análise no Gemini");

        group.MapPost("/gemini/base64", async (
            [FromBody] TestGeminiBase64Request request,
            IGeminiApiClient geminiApiClient,
            ILogger<IGeminiApiClient> logger) =>
        {
            try
            {
                logger.LogInformation("Testando Gemini com base64. MimeType: {MimeType}", request.MimeType);

                // Converter base64 para bytes
                var imageBytes = Convert.FromBase64String(request.Base64Image);

                logger.LogInformation("Imagem convertida. Tamanho: {Size} KB", imageBytes.Length / 1024.0);

                // Analisar com Gemini
                var resultado = await geminiApiClient.AnalisarComprovanteAsync(
                    imageBytes, 
                    request.MimeType ?? "image/jpeg", 
                    default);

                if (resultado.IsSuccess)
                {
                    return Results.Ok(new
                    {
                        success = true,
                        data = resultado.Value,
                        message = "Análise realizada com sucesso!"
                    });
                }
                else
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        errors = resultado.Errors,
                        message = "Falha na análise"
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao testar Gemini");
                return Results.BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    message = "Erro ao processar"
                });
            }
        })
        .WithSummary("Testa o Gemini com uma imagem via base64")
        .WithDescription("Recebe uma imagem em base64 e envia para análise no Gemini");

        // Endpoint para gerar comprovante sintético
        group.MapPost("/comprovante/gerar", (
            [FromBody] GerarComprovanteRequest request,
            ILogger<TestEndpoints> logger) =>
        {
            try
            {
                logger.LogInformation("Gerando comprovante sintético. Valor: {Valor}", request.Valor);

                var imagemBytes = GerarComprovanteSintetico(request);
                var base64 = Convert.ToBase64String(imagemBytes);

                return Results.Ok(new
                {
                    success = true,
                    base64Image = base64,
                    mimeType = "image/png",
                    tamanhoKb = imagemBytes.Length / 1024.0,
                    parametros = request,
                    message = "Comprovante sintético gerado com sucesso!"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao gerar comprovante sintético");
                return Results.BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        })
        .WithSummary("Gera um comprovante PIX sintético para testes")
        .WithDescription("Gera uma imagem PNG de comprovante com os parâmetros especificados");

        // Endpoint para simular processamento completo com mock
        group.MapPost("/comprovante/simular", async (
            [FromBody] SimularProcessamentoRequest request,
            IFaturaRepository faturaRepository,
            ICacheService cacheService,
            ILogger<TestEndpoints> logger) =>
        {
            try
            {
                logger.LogInformation("Simulando processamento de comprovante. ClienteId: {ClienteId}", request.ClienteId);

                // Simular análise do Gemini (resposta mockada)
                var comprovanteAnalisado = new ComprovanteAnalisadoDto(
                    IsComprovante: true,
                    Valor: request.ValorComprovante,
                    Data: DateTime.Now,
                    TipoPagamento: "PIX",
                    Confianca: 95,
                    Observacoes: "Comprovante simulado para testes",
                    DadosPagador: new DadosPagadorDto(
                        Nome: request.NomePagador ?? "Pagador Teste",
                        Documento: "123.456.789-00",
                        Banco: "Nubank",
                        Agencia: null,
                        Conta: null
                    ),
                    DadosDestinatario: new DadosDestinatarioDto(
                        Nome: request.NomeDestinatario ?? "Empresa BotFatura",
                        ChavePix: request.ChavePixDestinatario ?? "pix@botfatura.com.br",
                        Documento: null,
                        Banco: "Banco do Brasil",
                        Agencia: null,
                        Conta: null
                    ),
                    NumeroComprovante: $"E{Guid.NewGuid():N}"[..32]
                );

                // Validar destinatário
                var configuracao = await cacheService.ObterConfiguracaoAsync();
                var destinatarioValido = configuracao != null && 
                    (comprovanteAnalisado.DadosDestinatario?.ChavePix?.Contains(configuracao.ChavePix ?? "") == true ||
                     comprovanteAnalisado.DadosDestinatario?.Nome?.Contains(configuracao.NomeTitularPix ?? "") == true);

                // Buscar fatura correspondente
                Fatura? faturaCorrespondente = null;
                if (request.ClienteId.HasValue)
                {
                    var spec = new BotFatura.Application.Comprovantes.Specifications.FaturasPendentesClienteSpec(request.ClienteId.Value);
                    var faturas = await faturaRepository.ListAsync(spec);
                    faturaCorrespondente = faturas
                        .Where(f => Math.Abs(f.Valor - request.ValorComprovante) <= 0.01m)
                        .OrderByDescending(f => f.DataVencimento)
                        .FirstOrDefault();
                }

                var resultado = new
                {
                    success = true,
                    etapas = new
                    {
                        analiseGemini = new { sucesso = true, dados = comprovanteAnalisado },
                        validacaoDestinatario = new { sucesso = destinatarioValido, configuracao = new { chavePix = configuracao?.ChavePix, nomeTitular = configuracao?.NomeTitularPix } },
                        validacaoValor = new { sucesso = faturaCorrespondente != null, faturaEncontrada = faturaCorrespondente != null ? new { id = faturaCorrespondente.Id, valor = faturaCorrespondente.Valor } : null }
                    },
                    resultadoFinal = destinatarioValido && faturaCorrespondente != null 
                        ? "APROVADO - Comprovante seria validado com sucesso"
                        : $"REJEITADO - {(destinatarioValido ? "Valor não corresponde" : "Destinatário incorreto")}",
                    message = "Simulação concluída (nenhuma alteração foi feita no banco)"
                };

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro na simulação de processamento");
                return Results.BadRequest(new { success = false, error = ex.Message });
            }
        })
        .WithSummary("Simula o processamento completo de um comprovante")
        .WithDescription("Executa todas as validações sem chamar a API Gemini nem alterar dados");

        // Endpoint para listar cenários de teste disponíveis
        group.MapGet("/comprovante/cenarios", (ILogger<TestEndpoints> logger) =>
        {
            var cenarios = new[]
            {
                new { nome = "ComprovantePixValido", descricao = "Comprovante PIX válido com todos os dados corretos", valor = 150.00m },
                new { nome = "ComprovanteValorIncorreto", descricao = "Comprovante com valor diferente da fatura", valor = 200.00m },
                new { nome = "ComprovanteDestinatarioErrado", descricao = "Comprovante para outra chave PIX", valor = 150.00m },
                new { nome = "ImagemNaoComprovante", descricao = "Imagem que não é um comprovante válido", valor = 0m },
                new { nome = "ComprovanteValorAlto", descricao = "Comprovante com valor de R$ 10.000", valor = 10000.00m },
                new { nome = "ComprovanteValorMuitoAlto", descricao = "Comprovante com valor de R$ 50.000", valor = 50000.00m },
                new { nome = "ComprovanteTransferencia", descricao = "Comprovante de transferência bancária", valor = 150.00m },
                new { nome = "ComprovanteBoleto", descricao = "Comprovante de pagamento de boleto", valor = 150.00m },
                new { nome = "ComprovanteLimiteTolerancia", descricao = "Valor R$ 0,01 acima (no limite)", valor = 150.01m },
                new { nome = "ComprovanteForaTolerancia", descricao = "Valor R$ 0,02 acima (fora da tolerância)", valor = 150.02m },
            };

            return Results.Ok(new
            {
                success = true,
                cenarios,
                instrucoes = new
                {
                    gerarComprovante = "POST /api/test/comprovante/gerar com os parâmetros desejados",
                    simularProcessamento = "POST /api/test/comprovante/simular para testar sem chamar Gemini",
                    testarComGemini = "POST /api/test/gemini/base64 para testar com Gemini real"
                }
            });
        })
        .WithSummary("Lista cenários de teste disponíveis")
        .WithDescription("Retorna lista de cenários predefinidos para testes de comprovantes");

        // Endpoint para gerar e testar em um único passo
        group.MapPost("/comprovante/gerar-e-testar", async (
            [FromBody] GerarETestarRequest request,
            IGeminiApiClient geminiApiClient,
            ILogger<TestEndpoints> logger) =>
        {
            try
            {
                logger.LogInformation("Gerando e testando comprovante. Valor: {Valor}", request.Valor);

                // Gerar comprovante sintético
                var gerarRequest = new GerarComprovanteRequest
                {
                    Valor = request.Valor,
                    NomePagador = request.NomePagador ?? "Cliente Teste",
                    NomeDestinatario = request.NomeDestinatario ?? "Empresa BotFatura",
                    ChavePixDestinatario = request.ChavePixDestinatario ?? "pix@botfatura.com.br",
                    TipoPagamento = "PIX"
                };

                var imagemBytes = GerarComprovanteSintetico(gerarRequest);

                // Analisar com Gemini (se usarGeminiReal = true)
                object? analiseGemini = null;
                if (request.UsarGeminiReal)
                {
                    logger.LogInformation("Enviando para análise no Gemini real...");
                    var resultado = await geminiApiClient.AnalisarComprovanteAsync(imagemBytes, "image/png", default);
                    analiseGemini = resultado.IsSuccess 
                        ? new { sucesso = true, dados = resultado.Value }
                        : new { sucesso = false, erros = resultado.Errors };
                }

                return Results.Ok(new
                {
                    success = true,
                    comprovanteGerado = new
                    {
                        base64Image = Convert.ToBase64String(imagemBytes),
                        mimeType = "image/png",
                        tamanhoKb = imagemBytes.Length / 1024.0,
                        parametros = gerarRequest
                    },
                    analiseGemini = analiseGemini ?? new { mensagem = "Gemini não foi chamado (usarGeminiReal = false)" },
                    message = request.UsarGeminiReal 
                        ? "Comprovante gerado e analisado pelo Gemini real" 
                        : "Comprovante gerado (para testar com Gemini, defina usarGeminiReal = true)"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao gerar e testar comprovante");
                return Results.BadRequest(new { success = false, error = ex.Message });
            }
        })
        .WithSummary("Gera um comprovante e opcionalmente testa com Gemini")
        .WithDescription("Útil para testes rápidos: gera imagem sintética e pode enviar para Gemini em um único passo");
    }

    #region Métodos Auxiliares

    private static byte[] GerarComprovanteSintetico(GerarComprovanteRequest request)
    {
        const int largura = 400;
        const int altura = 600;

        using var surface = SKSurface.Create(new SKImageInfo(largura, altura));
        var canvas = surface.Canvas;

        // Fundo branco
        canvas.Clear(SKColors.White);

        // Fontes
        using var fonteTitulo = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 20,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        using var fonteNormal = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        using var fonteValor = new SKPaint
        {
            Color = new SKColor(0, 128, 0),
            TextSize = 28,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        using var fonteCinza = new SKPaint
        {
            Color = SKColors.Gray,
            TextSize = 12,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        // Barra verde superior
        using var linhaPaint = new SKPaint
        {
            Color = new SKColor(0, 128, 0),
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRect(0, 0, largura, 60, linhaPaint);

        // Título branco
        using var fonteTituloBranca = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 18,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        canvas.DrawText("Comprovante de Pagamento", 80, 38, fonteTituloBranca);

        var posY = 100f;

        // Círculo de sucesso
        using var circuloPaint = new SKPaint
        {
            Color = new SKColor(0, 200, 0),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawCircle(largura / 2, posY, 30, circuloPaint);

        // Check mark
        using var checkPaint = new SKPaint
        {
            Color = SKColors.White,
            StrokeWidth = 4,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };
        canvas.DrawLine(largura / 2 - 12, posY, largura / 2 - 2, posY + 10, checkPaint);
        canvas.DrawLine(largura / 2 - 2, posY + 10, largura / 2 + 15, posY - 10, checkPaint);

        posY += 60;

        // Status
        var textoStatus = "Transferência realizada";
        var larguraTexto = fonteTitulo.MeasureText(textoStatus);
        canvas.DrawText(textoStatus, (largura - larguraTexto) / 2, posY, fonteTitulo);
        posY += 30;

        // Valor
        var valorTexto = $"R$ {request.Valor:N2}";
        var larguraValor = fonteValor.MeasureText(valorTexto);
        canvas.DrawText(valorTexto, (largura - larguraValor) / 2, posY, fonteValor);
        posY += 50;

        // Linha separadora
        using var linhaSeparadora = new SKPaint { Color = SKColors.LightGray, StrokeWidth = 1 };
        canvas.DrawLine(20, posY, largura - 20, posY, linhaSeparadora);
        posY += 30;

        // Destinatário
        canvas.DrawText("Para", 30, posY, fonteCinza);
        posY += 20;
        canvas.DrawText(request.NomeDestinatario ?? "Empresa BotFatura", 30, posY, fonteNormal);
        posY += 20;
        canvas.DrawText($"Chave PIX: {request.ChavePixDestinatario ?? "pix@botfatura.com.br"}", 30, posY, fonteCinza);
        posY += 40;

        // Linha separadora
        canvas.DrawLine(20, posY, largura - 20, posY, linhaSeparadora);
        posY += 30;

        // Pagador
        canvas.DrawText("De", 30, posY, fonteCinza);
        posY += 20;
        canvas.DrawText(request.NomePagador ?? "Cliente Teste", 30, posY, fonteNormal);
        posY += 20;
        canvas.DrawText("CPF: 123.456.789-00", 30, posY, fonteCinza);
        posY += 40;

        // Linha separadora
        canvas.DrawLine(20, posY, largura - 20, posY, linhaSeparadora);
        posY += 30;

        // Informações da transação
        canvas.DrawText("Informações da transação", 30, posY, fonteCinza);
        posY += 25;
        canvas.DrawText($"Data: {DateTime.Now:dd/MM/yyyy HH:mm}", 30, posY, fonteNormal);
        posY += 20;
        canvas.DrawText($"Tipo: {request.TipoPagamento ?? "PIX"}", 30, posY, fonteNormal);
        posY += 20;
        canvas.DrawText($"ID: E{Guid.NewGuid():N}"[..40], 30, posY, fonteNormal);

        // Converter para PNG
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    #endregion

    #region Records

    public record TestGeminiUrlRequest(string ImageUrl);
    public record TestGeminiBase64Request(string Base64Image, string? MimeType);

    public record GerarComprovanteRequest
    {
        public decimal Valor { get; init; } = 150.00m;
        public string? NomePagador { get; init; } = "Cliente Teste";
        public string? NomeDestinatario { get; init; } = "Empresa BotFatura";
        public string? ChavePixDestinatario { get; init; } = "pix@botfatura.com.br";
        public string? TipoPagamento { get; init; } = "PIX";
    }

    public record SimularProcessamentoRequest
    {
        public Guid? ClienteId { get; init; }
        public decimal ValorComprovante { get; init; } = 150.00m;
        public string? NomePagador { get; init; }
        public string? NomeDestinatario { get; init; }
        public string? ChavePixDestinatario { get; init; }
    }

    public record GerarETestarRequest
    {
        public decimal Valor { get; init; } = 150.00m;
        public string? NomePagador { get; init; }
        public string? NomeDestinatario { get; init; }
        public string? ChavePixDestinatario { get; init; }
        public bool UsarGeminiReal { get; init; } = false;
    }

    #endregion
}
