using SkiaSharp;

namespace BotFatura.TestUtils.Geradores;

/// <summary>
/// Gerador de comprovantes sintéticos para testes.
/// Cria imagens PNG que simulam comprovantes de pagamento PIX.
/// </summary>
public class ComprovanteGenerator
{
    private const int LARGURA = 400;
    private const int ALTURA = 600;
    
    /// <summary>
    /// Gera um comprovante de pagamento PIX sintético
    /// </summary>
    public byte[] GerarComprovantePix(ComprovanteParametros parametros)
    {
        using var surface = SKSurface.Create(new SKImageInfo(LARGURA, ALTURA));
        var canvas = surface.Canvas;
        
        // Fundo branco
        canvas.Clear(SKColors.White);
        
        // Configurar fontes
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
            Color = new SKColor(0, 128, 0), // Verde
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
        
        // Desenhar linha superior verde
        using var linhaPaint = new SKPaint
        {
            Color = new SKColor(0, 128, 0),
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRect(0, 0, LARGURA, 60, linhaPaint);
        
        // Título no topo
        using var fonteTituloBranca = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 18,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        canvas.DrawText("Comprovante de Pagamento", 80, 38, fonteTituloBranca);
        
        var posY = 100f;
        
        // Ícone de sucesso (círculo verde com checkmark simulado)
        using var circuloPaint = new SKPaint
        {
            Color = new SKColor(0, 200, 0),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawCircle(LARGURA / 2, posY, 30, circuloPaint);
        
        using var checkPaint = new SKPaint
        {
            Color = SKColors.White,
            StrokeWidth = 4,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };
        canvas.DrawLine(LARGURA / 2 - 12, posY, LARGURA / 2 - 2, posY + 10, checkPaint);
        canvas.DrawLine(LARGURA / 2 - 2, posY + 10, LARGURA / 2 + 15, posY - 10, checkPaint);
        
        posY += 60;
        
        // Status
        canvas.DrawText("Transferência realizada", CentralizarTexto("Transferência realizada", fonteTitulo), posY, fonteTitulo);
        posY += 30;
        
        // Valor
        var valorTexto = $"R$ {parametros.Valor:N2}";
        canvas.DrawText(valorTexto, CentralizarTexto(valorTexto, fonteValor), posY, fonteValor);
        posY += 50;
        
        // Linha separadora
        using var linhaSeparadora = new SKPaint
        {
            Color = SKColors.LightGray,
            StrokeWidth = 1
        };
        canvas.DrawLine(20, posY, LARGURA - 20, posY, linhaSeparadora);
        posY += 30;
        
        // Dados do destinatário
        canvas.DrawText("Para", 30, posY, fonteCinza);
        posY += 20;
        canvas.DrawText(parametros.NomeDestinatario, 30, posY, fonteNormal);
        posY += 20;
        
        if (!string.IsNullOrEmpty(parametros.ChavePixDestinatario))
        {
            canvas.DrawText($"Chave PIX: {parametros.ChavePixDestinatario}", 30, posY, fonteCinza);
            posY += 20;
        }
        
        if (!string.IsNullOrEmpty(parametros.DocumentoDestinatario))
        {
            canvas.DrawText($"CPF/CNPJ: {parametros.DocumentoDestinatario}", 30, posY, fonteCinza);
            posY += 20;
        }
        
        if (!string.IsNullOrEmpty(parametros.BancoDestinatario))
        {
            canvas.DrawText($"Banco: {parametros.BancoDestinatario}", 30, posY, fonteCinza);
            posY += 20;
        }
        
        posY += 20;
        
        // Linha separadora
        canvas.DrawLine(20, posY, LARGURA - 20, posY, linhaSeparadora);
        posY += 30;
        
        // Dados do pagador
        canvas.DrawText("De", 30, posY, fonteCinza);
        posY += 20;
        canvas.DrawText(parametros.NomePagador, 30, posY, fonteNormal);
        posY += 20;
        
        if (!string.IsNullOrEmpty(parametros.DocumentoPagador))
        {
            canvas.DrawText($"CPF/CNPJ: {parametros.DocumentoPagador}", 30, posY, fonteCinza);
            posY += 20;
        }
        
        if (!string.IsNullOrEmpty(parametros.BancoPagador))
        {
            canvas.DrawText($"Banco: {parametros.BancoPagador}", 30, posY, fonteCinza);
            posY += 20;
        }
        
        posY += 20;
        
        // Linha separadora
        canvas.DrawLine(20, posY, LARGURA - 20, posY, linhaSeparadora);
        posY += 30;
        
        // Informações adicionais
        canvas.DrawText("Informações da transação", 30, posY, fonteCinza);
        posY += 25;
        
        canvas.DrawText($"Data: {parametros.Data:dd/MM/yyyy HH:mm}", 30, posY, fonteNormal);
        posY += 20;
        
        canvas.DrawText($"Tipo: {parametros.TipoPagamento}", 30, posY, fonteNormal);
        posY += 20;
        
        if (!string.IsNullOrEmpty(parametros.NumeroComprovante))
        {
            canvas.DrawText($"ID: {parametros.NumeroComprovante}", 30, posY, fonteNormal);
            posY += 20;
        }
        
        // Converter para PNG
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
    
    /// <summary>
    /// Gera um comprovante de transferência bancária sintético
    /// </summary>
    public byte[] GerarComprovanteTransferencia(ComprovanteParametros parametros)
    {
        parametros = parametros with { TipoPagamento = "Transferência" };
        return GerarComprovantePix(parametros);
    }
    
    /// <summary>
    /// Gera uma imagem que NÃO é um comprovante (para testes de rejeição)
    /// </summary>
    public byte[] GerarImagemNaoComprovante()
    {
        using var surface = SKSurface.Create(new SKImageInfo(400, 300));
        var canvas = surface.Canvas;
        
        // Fundo colorido aleatório
        canvas.Clear(new SKColor(200, 220, 255));
        
        using var paint = new SKPaint
        {
            Color = SKColors.DarkBlue,
            TextSize = 24,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        
        canvas.DrawText("Foto de paisagem", 100, 150, paint);
        
        // Desenhar algumas formas aleatórias para simular uma foto
        using var circlePaint = new SKPaint
        {
            Color = SKColors.Yellow,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(320, 60, 40, circlePaint);
        
        using var mountainPaint = new SKPaint
        {
            Color = new SKColor(100, 150, 100),
            Style = SKPaintStyle.Fill
        };
        
        var path = new SKPath();
        path.MoveTo(0, 300);
        path.LineTo(100, 180);
        path.LineTo(200, 250);
        path.LineTo(300, 150);
        path.LineTo(400, 220);
        path.LineTo(400, 300);
        path.Close();
        canvas.DrawPath(path, mountainPaint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
    
    /// <summary>
    /// Gera um comprovante com dados parciais (legibilidade ruim)
    /// </summary>
    public byte[] GerarComprovanteParcialmenteLegivel(ComprovanteParametros parametros)
    {
        using var surface = SKSurface.Create(new SKImageInfo(LARGURA, ALTURA));
        var canvas = surface.Canvas;
        
        // Fundo com "ruído"
        canvas.Clear(new SKColor(240, 240, 235));
        
        using var fonteNormal = new SKPaint
        {
            Color = new SKColor(180, 180, 180), // Texto bem claro
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };
        
        var posY = 100f;
        
        canvas.DrawText("Comprovante de Pagamento", 80, posY, fonteNormal);
        posY += 50;
        
        // Valor parcialmente visível
        var valorTexto = $"R$ {parametros.Valor:N2}";
        canvas.DrawText(valorTexto, 100, posY, fonteNormal);
        posY += 40;
        
        canvas.DrawText($"Para: {parametros.NomeDestinatario}", 30, posY, fonteNormal);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
    
    private float CentralizarTexto(string texto, SKPaint paint)
    {
        var larguraTexto = paint.MeasureText(texto);
        return (LARGURA - larguraTexto) / 2;
    }
}

/// <summary>
/// Parâmetros para geração de comprovante sintético
/// </summary>
public record ComprovanteParametros
{
    public decimal Valor { get; init; } = 100.00m;
    public string NomeDestinatario { get; init; } = "Empresa Teste LTDA";
    public string? ChavePixDestinatario { get; init; } = "email@empresa.com.br";
    public string? DocumentoDestinatario { get; init; } = "12.345.678/0001-90";
    public string? BancoDestinatario { get; init; } = "Banco do Brasil";
    public string NomePagador { get; init; } = "João da Silva";
    public string? DocumentoPagador { get; init; } = "123.456.789-00";
    public string? BancoPagador { get; init; } = "Nubank";
    public DateTime Data { get; init; } = DateTime.Now;
    public string TipoPagamento { get; init; } = "PIX";
    public string? NumeroComprovante { get; init; } = "E00000000202402271234567890123456";
}
