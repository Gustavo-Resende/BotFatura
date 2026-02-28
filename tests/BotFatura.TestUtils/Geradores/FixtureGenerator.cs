namespace BotFatura.TestUtils.Geradores;

/// <summary>
/// Utilitário para gerar e salvar fixtures de comprovantes para testes.
/// Execute como: dotnet run --project tests/BotFatura.TestUtils -- generate-fixtures
/// </summary>
public static class FixtureGenerator
{
    private static readonly ComprovanteGenerator _generator = new();

    /// <summary>
    /// Gera todos os fixtures de comprovantes e salva em disco
    /// </summary>
    public static void GerarTodosFixtures(string outputPath)
    {
        Directory.CreateDirectory(outputPath);

        // 1. Comprovantes PIX válidos com diferentes valores
        var valores = new[] { 50.00m, 100.00m, 150.00m, 200.00m, 500.00m, 1000.00m, 5000.00m, 10000.00m, 50000.00m };
        foreach (var valor in valores)
        {
            var parametros = new ComprovanteParametros
            {
                Valor = valor,
                NomePagador = "João da Silva",
                NomeDestinatario = "Empresa BotFatura",
                ChavePixDestinatario = "pix@botfatura.com.br"
            };
            var bytes = _generator.GerarComprovantePix(parametros);
            var nomeArquivo = $"pix_valido_{valor:F0}.png";
            File.WriteAllBytes(Path.Combine(outputPath, nomeArquivo), bytes);
            Console.WriteLine($"Gerado: {nomeArquivo}");
        }

        // 2. Comprovantes com diferentes nomes de pagador
        var nomes = new[] { "Maria Santos", "José Carlos Pereira", "EMPRESA LTDA ME", "Ana Paula Oliveira da Costa" };
        foreach (var nome in nomes)
        {
            var parametros = new ComprovanteParametros
            {
                Valor = 150.00m,
                NomePagador = nome,
                NomeDestinatario = "Empresa BotFatura",
                ChavePixDestinatario = "pix@botfatura.com.br"
            };
            var bytes = _generator.GerarComprovantePix(parametros);
            var nomeArquivo = $"pix_nome_{nome.Replace(" ", "_").ToLower()}.png";
            File.WriteAllBytes(Path.Combine(outputPath, nomeArquivo), bytes);
            Console.WriteLine($"Gerado: {nomeArquivo}");
        }

        // 3. Comprovante com destinatário incorreto
        {
            var parametros = new ComprovanteParametros
            {
                Valor = 150.00m,
                NomePagador = "João da Silva",
                NomeDestinatario = "Outra Empresa LTDA",
                ChavePixDestinatario = "outra@empresa.com"
            };
            var bytes = _generator.GerarComprovantePix(parametros);
            File.WriteAllBytes(Path.Combine(outputPath, "pix_destinatario_errado.png"), bytes);
            Console.WriteLine("Gerado: pix_destinatario_errado.png");
        }

        // 4. Comprovante de transferência
        {
            var parametros = new ComprovanteParametros
            {
                Valor = 150.00m,
                NomePagador = "João da Silva",
                NomeDestinatario = "Empresa BotFatura",
                ChavePixDestinatario = null,
                TipoPagamento = "Transferência"
            };
            var bytes = _generator.GerarComprovanteTransferencia(parametros);
            File.WriteAllBytes(Path.Combine(outputPath, "transferencia_valido_150.png"), bytes);
            Console.WriteLine("Gerado: transferencia_valido_150.png");
        }

        // 5. Imagem que não é comprovante
        {
            var bytes = _generator.GerarImagemNaoComprovante();
            File.WriteAllBytes(Path.Combine(outputPath, "nao_comprovante_paisagem.png"), bytes);
            Console.WriteLine("Gerado: nao_comprovante_paisagem.png");
        }

        // 6. Comprovante parcialmente legível
        {
            var parametros = new ComprovanteParametros
            {
                Valor = 150.00m,
                NomePagador = "João da Silva",
                NomeDestinatario = "Empresa BotFatura",
            };
            var bytes = _generator.GerarComprovanteParcialmenteLegivel(parametros);
            File.WriteAllBytes(Path.Combine(outputPath, "pix_baixa_qualidade.png"), bytes);
            Console.WriteLine("Gerado: pix_baixa_qualidade.png");
        }

        // 7. Comprovantes com valores para teste de tolerância
        var tolerancias = new[]
        {
            (150.00m, "valor_exato"),
            (150.01m, "valor_limite_superior"),
            (149.99m, "valor_limite_inferior"),
            (150.02m, "valor_fora_tolerancia_superior"),
            (149.98m, "valor_fora_tolerancia_inferior"),
        };
        foreach (var (valor, descricao) in tolerancias)
        {
            var parametros = new ComprovanteParametros
            {
                Valor = valor,
                NomePagador = "João da Silva",
                NomeDestinatario = "Empresa BotFatura",
                ChavePixDestinatario = "pix@botfatura.com.br"
            };
            var bytes = _generator.GerarComprovantePix(parametros);
            var nomeArquivo = $"pix_tolerancia_{descricao}.png";
            File.WriteAllBytes(Path.Combine(outputPath, nomeArquivo), bytes);
            Console.WriteLine($"Gerado: {nomeArquivo}");
        }

        Console.WriteLine($"\nTotal de fixtures gerados no diretório: {outputPath}");
    }

    /// <summary>
    /// Retorna uma lista de todos os fixtures disponíveis com seus metadados
    /// </summary>
    public static IEnumerable<FixtureMetadata> ListarFixturesDisponiveis()
    {
        yield return new FixtureMetadata("pix_valido_100.png", "Comprovante PIX válido R$ 100", 100.00m, true, true);
        yield return new FixtureMetadata("pix_valido_150.png", "Comprovante PIX válido R$ 150", 150.00m, true, true);
        yield return new FixtureMetadata("pix_valido_1000.png", "Comprovante PIX válido R$ 1.000", 1000.00m, true, true);
        yield return new FixtureMetadata("pix_valido_10000.png", "Comprovante PIX válido R$ 10.000", 10000.00m, true, true);
        yield return new FixtureMetadata("pix_valido_50000.png", "Comprovante PIX válido R$ 50.000", 50000.00m, true, true);
        yield return new FixtureMetadata("pix_destinatario_errado.png", "Comprovante com destinatário incorreto", 150.00m, true, false);
        yield return new FixtureMetadata("nao_comprovante_paisagem.png", "Imagem que não é comprovante", 0m, false, false);
        yield return new FixtureMetadata("pix_tolerancia_valor_exato.png", "Valor exato da fatura", 150.00m, true, true);
        yield return new FixtureMetadata("pix_tolerancia_valor_limite_superior.png", "Valor R$ 0,01 acima", 150.01m, true, true);
        yield return new FixtureMetadata("pix_tolerancia_valor_fora_tolerancia_superior.png", "Valor R$ 0,02 acima (fora)", 150.02m, true, false);
    }
}

/// <summary>
/// Metadados de um fixture de comprovante
/// </summary>
public record FixtureMetadata(
    string NomeArquivo,
    string Descricao,
    decimal Valor,
    bool EhComprovante,
    bool DeveSerAceito
);
