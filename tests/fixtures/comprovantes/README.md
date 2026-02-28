# Fixtures de Comprovantes para Testes

Este diretório contém imagens de comprovantes sintéticos gerados para testes automatizados.

## Gerando os Fixtures

Para gerar/atualizar os fixtures, execute:

```powershell
cd tests/BotFatura.TestUtils
dotnet run -- generate-fixtures
```

Ou use o endpoint da API (ambiente de desenvolvimento):

```
POST /api/test/comprovante/gerar
```

## Lista de Fixtures

### Comprovantes PIX Válidos
| Arquivo | Valor | Descrição |
|---------|-------|-----------|
| `pix_valido_50.png` | R$ 50,00 | Comprovante PIX padrão |
| `pix_valido_100.png` | R$ 100,00 | Comprovante PIX padrão |
| `pix_valido_150.png` | R$ 150,00 | Comprovante PIX padrão |
| `pix_valido_500.png` | R$ 500,00 | Comprovante PIX padrão |
| `pix_valido_1000.png` | R$ 1.000,00 | Comprovante PIX padrão |
| `pix_valido_10000.png` | R$ 10.000,00 | Comprovante PIX valor alto |
| `pix_valido_50000.png` | R$ 50.000,00 | Comprovante PIX valor muito alto |

### Testes de Tolerância (base R$ 150,00)
| Arquivo | Valor | Esperado |
|---------|-------|----------|
| `pix_tolerancia_valor_exato.png` | R$ 150,00 | Aceitar |
| `pix_tolerancia_valor_limite_superior.png` | R$ 150,01 | Aceitar |
| `pix_tolerancia_valor_limite_inferior.png` | R$ 149,99 | Aceitar |
| `pix_tolerancia_valor_fora_tolerancia_superior.png` | R$ 150,02 | Rejeitar |
| `pix_tolerancia_valor_fora_tolerancia_inferior.png` | R$ 149,98 | Rejeitar |

### Cenários de Falha
| Arquivo | Descrição |
|---------|-----------|
| `pix_destinatario_errado.png` | Comprovante para outro destinatário |
| `nao_comprovante_paisagem.png` | Imagem que não é comprovante |
| `pix_baixa_qualidade.png` | Comprovante parcialmente legível |

### Variações de Nome do Pagador
| Arquivo | Nome |
|---------|------|
| `pix_nome_maria_santos.png` | Maria Santos |
| `pix_nome_jose_carlos_pereira.png` | José Carlos Pereira |
| `pix_nome_empresa_ltda_me.png` | EMPRESA LTDA ME |

## Uso nos Testes

### Testes Unitários (com Mock)
```csharp
var cenario = CenariosComprovantePredefinidos.ComprovantePixValido(150.00m);
_geminiClientMock.Setup(x => x.AnalisarComprovanteAsync(...))
    .ReturnsAsync(Result.Success(cenario));
```

### Testes de Integração (com FakeGeminiApiClient)
```csharp
var imagemBytes = File.ReadAllBytes("fixtures/comprovantes/pix_valido_150.png");
_fakeGemini.ComRespostaParaImagem(imagemBytes, cenarioEsperado);
```

### Testes Visuais (com Gemini real)
```csharp
[Trait("Category", "Visual")]
public async Task TestarComGeminiReal()
{
    var imagemBytes = File.ReadAllBytes("fixtures/comprovantes/pix_valido_150.png");
    var resultado = await _geminiClient.AnalisarComprovanteAsync(imagemBytes, "image/png");
    // ...
}
```

## Importante

- Os fixtures são **imagens sintéticas** geradas por código
- Não contêm dados pessoais reais
- São determinísticos (mesmos parâmetros = mesma imagem)
- Ideais para testes de regressão e CI/CD
