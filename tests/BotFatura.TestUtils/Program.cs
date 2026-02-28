using BotFatura.TestUtils.Geradores;

// Utilitário CLI para gerar fixtures de comprovantes
// Uso: dotnet run --project tests/BotFatura.TestUtils -- generate-fixtures [output-path]

if (args.Length > 0 && args[0] == "generate-fixtures")
{
    var outputPath = args.Length > 1 
        ? args[1] 
        : Path.Combine(Directory.GetCurrentDirectory(), "..", "fixtures", "comprovantes");
    
    outputPath = Path.GetFullPath(outputPath);
    
    Console.WriteLine("=== BotFatura - Gerador de Fixtures ===");
    Console.WriteLine($"Gerando fixtures em: {outputPath}");
    Console.WriteLine();
    
    FixtureGenerator.GerarTodosFixtures(outputPath);
    
    Console.WriteLine();
    Console.WriteLine("=== Concluído! ===");
}
else
{
    Console.WriteLine("BotFatura TestUtils");
    Console.WriteLine();
    Console.WriteLine("Comandos disponíveis:");
    Console.WriteLine("  generate-fixtures [output-path]  - Gera fixtures de comprovantes sintéticos");
    Console.WriteLine();
    Console.WriteLine("Exemplo:");
    Console.WriteLine("  dotnet run -- generate-fixtures");
    Console.WriteLine("  dotnet run -- generate-fixtures C:\\output\\fixtures");
}
