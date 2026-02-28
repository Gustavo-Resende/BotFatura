# Scripts de Utilidade

Esta pasta contém scripts PowerShell para auxiliar no desenvolvimento e testes do BotFatura.

## Scripts de Configuração

| Script | Descrição |
|--------|-----------|
| `configure-webhook.ps1` | Configura o webhook da Evolution API |
| `setup-teste.ps1` | Configura ambiente completo para testes de comprovante (login, cliente, configuração PIX e fatura) |

## Scripts de Teste

| Script | Descrição |
|--------|-----------|
| `test-gemini.ps1` | Testa a integração com a API do Gemini (URL ou base64) |
| `test-webhook.ps1` | Simula um webhook da Evolution API para testes |

## Uso

Execute os scripts a partir da raiz do projeto:

```powershell
# Exemplo - Configurar ambiente de teste completo
.\scripts\setup-teste.ps1

# Exemplo - Testar integração com Gemini
.\scripts\test-gemini.ps1

# Exemplo - Configurar webhook
.\scripts\configure-webhook.ps1
```

> **Nota:** Certifique-se de que a API está rodando antes de executar os scripts de teste.
> 
> **Atenção:** Alguns scripts podem conter valores hardcoded. Em produção, use variáveis de ambiente do arquivo `.env`.