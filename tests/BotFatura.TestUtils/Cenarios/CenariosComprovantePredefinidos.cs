using BotFatura.Application.Common.Interfaces;
using BotFatura.TestUtils.Builders;

namespace BotFatura.TestUtils.Cenarios;

/// <summary>
/// Cenários de teste predefinidos para análise de comprovantes.
/// Estes cenários cobrem os principais casos de uso e edge cases.
/// </summary>
public static class CenariosComprovantePredefinidos
{
    /// <summary>
    /// Comprovante PIX válido com todos os dados preenchidos
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovantePixValido(decimal valor = 150.00m)
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(valor)
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante com valor diferente da fatura (teste de tolerância)
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteValorIncorreto(decimal valorErrado)
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(valorErrado)
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante com destinatário incorreto
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteDestinatarioErrado(decimal valor = 150.00m)
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(valor)
            .ComDadosDestinatario(
                nome: "Outra Empresa LTDA",
                chavePix: "outra@empresa.com"
            )
            .Build();
    }

    /// <summary>
    /// Imagem que não é um comprovante
    /// </summary>
    public static ComprovanteAnalisadoDto ImagemNaoComprovante()
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoNaoComprovante()
            .Build();
    }

    /// <summary>
    /// Comprovante com valor alto (R$ 10.000)
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteValorAlto()
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(10000.00m)
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante com valor muito alto (R$ 50.000)
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteValorMuitoAlto()
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(50000.00m)
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante de transferência bancária
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteTransferencia(decimal valor = 150.00m)
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovanteTransferencia(valor)
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: null,
                banco: "Banco do Brasil",
                agencia: "1234",
                conta: "12345-6"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante de boleto
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteBoleto(decimal valor = 150.00m)
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovanteBoleto(valor)
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                documento: "12.345.678/0001-90"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante com baixa confiança (imagem de má qualidade)
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteBaixaConfianca(decimal valor = 150.00m)
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(valor)
            .ComConfianca(45)
            .ComObservacoes("Imagem de baixa qualidade, dados podem estar incorretos")
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante sem dados do pagador
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteSemDadosPagador(decimal valor = 150.00m)
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(valor)
            .SemDadosPagador()
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante com valor no limite da tolerância (R$ 0,01 de diferença)
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteNoLimiteToleranciaSuperior(decimal valorBase)
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(valorBase + 0.01m)
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante com valor fora da tolerância (R$ 0,02 de diferença)
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteForaToleranciaSuperior(decimal valorBase)
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(valorBase + 0.02m)
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante com nome do destinatário parcialmente correto
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteNomeParcial(decimal valor = 150.00m)
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(valor)
            .ComDadosDestinatario(
                nome: "BotFatura LTDA ME", // Contém "BotFatura" mas não é exato
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante com valor zero (inválido)
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteValorZero()
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComoComprovantePix(0m)
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante com valor negativo (inválido)
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteValorNegativo()
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComValor(-100.00m)
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante com valor null
    /// </summary>
    public static ComprovanteAnalisadoDto ComprovanteSemValor()
    {
        return new ComprovanteAnalisadoDtoBuilder()
            .ComIsComprovante(true)
            .ComValor(null)
            .ComDadosDestinatario(
                nome: "Empresa BotFatura",
                chavePix: "pix@botfatura.com.br"
            )
            .Build();
    }

    /// <summary>
    /// Comprovante com diferentes nomes de pagador para testar variações
    /// </summary>
    public static IEnumerable<ComprovanteAnalisadoDto> ComprovantesComNomesDiversos(decimal valor = 150.00m)
    {
        var nomes = new[]
        {
            "João da Silva",
            "Maria de Souza Santos",
            "José Carlos Pereira Junior",
            "Ana Paula Oliveira da Costa",
            "EMPRESA TESTE LTDA ME",
            "Distribuidora XYZ S/A",
            "MEI - Fulano de Tal",
            "Condomínio Edifício Exemplo"
        };

        foreach (var nome in nomes)
        {
            yield return new ComprovanteAnalisadoDtoBuilder()
                .ComoComprovantePix(valor)
                .ComDadosPagador(nome: nome)
                .ComDadosDestinatario(
                    nome: "Empresa BotFatura",
                    chavePix: "pix@botfatura.com.br"
                )
                .Build();
        }
    }

    /// <summary>
    /// Comprovantes com diferentes tipos de pagamento
    /// </summary>
    public static IEnumerable<(string Tipo, ComprovanteAnalisadoDto Comprovante)> ComprovantesComTiposDiversos(decimal valor = 150.00m)
    {
        yield return ("PIX", ComprovantePixValido(valor));
        yield return ("Transferência", ComprovanteTransferencia(valor));
        yield return ("Boleto", ComprovanteBoleto(valor));
    }

    /// <summary>
    /// Comprovantes com diferentes valores para teste de tolerância
    /// </summary>
    public static IEnumerable<(decimal Valor, bool DeveSerAceito, string Descricao)> CenariosToleranciaValor(decimal valorEsperado)
    {
        yield return (valorEsperado, true, "Valor exato");
        yield return (valorEsperado + 0.01m, true, "Valor R$0,01 acima (no limite)");
        yield return (valorEsperado - 0.01m, true, "Valor R$0,01 abaixo (no limite)");
        yield return (valorEsperado + 0.02m, false, "Valor R$0,02 acima (fora da tolerância)");
        yield return (valorEsperado - 0.02m, false, "Valor R$0,02 abaixo (fora da tolerância)");
        yield return (valorEsperado + 1.00m, false, "Valor R$1,00 acima");
        yield return (valorEsperado * 2, false, "Valor em dobro");
        yield return (valorEsperado / 2, false, "Metade do valor");
    }
}
