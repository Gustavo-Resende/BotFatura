# Script de teste do webhook
# Simula uma mensagem do Evolution API com imagem

# Carregar variáveis de ambiente do .env
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptPath "_LoadEnv.ps1")
Load-EnvFile

# Obter valores do .env ou usar padrões
$apiBaseUrl = Get-EnvValue "API_BASE_URL" "http://localhost:5000"
$evolutionInstanceName = Get-EnvValue "EvolutionApi__InstanceName" "BotFatura"

$webhookBody = @{
    event = "messages.upsert"
    instance = $evolutionInstanceName
    data = @{
        key = @{
            remoteJid = "5573996488137@s.whatsapp.net"  # Cliente de teste
            fromMe = $false
            id = "TEST123"
        }
        pushName = "Rodrigo"
        message = @{
            imageMessage = @{
                url = "https://via.placeholder.com/600/92c952"
                mimetype = "image/jpeg"
            }
        }
        messageType = "imageMessage"
    }
} | ConvertTo-Json -Depth 10

$webhookUrl = "$apiBaseUrl/webhook/whatsapp"
Write-Host "Enviando webhook de teste para: $webhookUrl" -ForegroundColor Cyan
Write-Host "Body:" $webhookBody

try {
    $response = Invoke-RestMethod -Uri $webhookUrl -Method Post -Body $webhookBody -ContentType "application/json"
    Write-Host "Resposta:" ($response | ConvertTo-Json -Depth 3)
} catch {
    Write-Host "Erro ao enviar webhook: $_"
    Write-Host "Detalhes: $($_.Exception.Message)"
}
