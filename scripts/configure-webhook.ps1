# Script para configurar webhook da Evolution API

# Carregar variáveis de ambiente do .env
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptPath "_LoadEnv.ps1")
Load-EnvFile

# Obter valores do .env ou usar padrões
$evolutionBaseUrl = Get-EnvValue "EvolutionApi__BaseUrl" "http://localhost:8080/"
$evolutionInstanceName = Get-EnvValue "EvolutionApi__InstanceName" "BotFatura"
$evolutionApiKey = Get-EnvValue "EvolutionApi__ApiKey" ""
$apiBaseUrl = Get-EnvValue "API_BASE_URL" "http://localhost:5000"

if ([string]::IsNullOrEmpty($evolutionApiKey)) {
    Write-Host "❌ ERRO: EvolutionApi__ApiKey não encontrada no .env!" -ForegroundColor Red
    Write-Host "Configure a chave EvolutionApi__ApiKey no arquivo .env" -ForegroundColor Yellow
    exit 1
}

# Remover barra final se existir
$evolutionBaseUrl = $evolutionBaseUrl.TrimEnd('/')

$uri = "$evolutionBaseUrl/webhook/set/$evolutionInstanceName"
$webhookUrl = "$apiBaseUrl/webhook/whatsapp"

# Ajustar URL do webhook para Docker se necessário
if ($webhookUrl -like "http://localhost*") {
    $webhookUrl = $webhookUrl -replace "http://localhost", "http://host.docker.internal"
}

$body = @{
    webhook = @{
        enabled = $true
        url = $webhookUrl
        webhook_by_events = $true
        webhook_base64 = $false
        events = @("MESSAGES_UPSERT")
    }
} | ConvertTo-Json -Depth 3

Write-Host "Configurando webhook..."
Write-Host "URL: $uri"
Write-Host "Body: $body"

try {
    $response = Invoke-RestMethod -Uri $uri -Method POST -Headers @{
        "apikey" = $evolutionApiKey
        "Content-Type" = "application/json"
    } -Body $body
    
    Write-Host "✅ Webhook configurado com sucesso!" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10
}
catch {
    Write-Host "❌ Erro ao configurar webhook:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    if ($_.ErrorDetails) {
        Write-Host $_.ErrorDetails.Message
    }
}
