# Script para testar o Gemini com uma imagem de URL

# Carregar variáveis de ambiente do .env
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptPath "_LoadEnv.ps1")
Load-EnvFile

# Obter URL da API do .env ou usar padrão
$apiBaseUrl = Get-EnvValue "API_BASE_URL" "http://localhost:5000"

Write-Host "=== TESTE DO GEMINI API ===" -ForegroundColor Yellow
Write-Host "API Base URL: $apiBaseUrl" -ForegroundColor Gray
Write-Host ""

# URL de um comprovante de teste (imagem pública de exemplo)
$imageUrl = "https://via.placeholder.com/600/92c952"

Write-Host "Opções de teste:" -ForegroundColor Cyan
Write-Host "1. Testar com URL de exemplo (placeholder)"
Write-Host "2. Testar com URL personalizada"
Write-Host "3. Testar com imagem local (converte para base64)"
Write-Host ""

$opcao = Read-Host "Escolha uma opção (1-3)"

if ($opcao -eq "2") {
    $imageUrl = Read-Host "Digite a URL da imagem"
}
elseif ($opcao -eq "3") {
    $filePath = Read-Host "Digite o caminho completo da imagem"
    
    if (Test-Path $filePath) {
        Write-Host "Convertendo imagem para base64..." -ForegroundColor Yellow
        $imageBytes = [System.IO.File]::ReadAllBytes($filePath)
        $base64 = [Convert]::ToBase64String($imageBytes)
        
        # Detectar MIME type
        $extension = [System.IO.Path]::GetExtension($filePath).ToLower()
        $mimeType = switch ($extension) {
            ".jpg"  { "image/jpeg" }
            ".jpeg" { "image/jpeg" }
            ".png"  { "image/png" }
            ".gif"  { "image/gif" }
            ".webp" { "image/webp" }
            ".pdf"  { "application/pdf" }
            default { "image/jpeg" }
        }
        
        Write-Host "Tamanho: $([Math]::Round($imageBytes.Length / 1024, 2)) KB" -ForegroundColor Cyan
        Write-Host "MIME Type: $mimeType" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Enviando para o Gemini..." -ForegroundColor Yellow
        
        $body = @{
            base64Image = $base64
            mimeType = $mimeType
        } | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri "$apiBaseUrl/api/test/gemini/base64" -Method Post -Body $body -ContentType "application/json"
            
            Write-Host ""
            Write-Host "✅ SUCESSO!" -ForegroundColor Green
            Write-Host ""
            Write-Host "Resultado da análise:" -ForegroundColor Cyan
            $response | ConvertTo-Json -Depth 5
        }
        catch {
            Write-Host ""
            Write-Host "❌ ERRO!" -ForegroundColor Red
            Write-Host $_.Exception.Message
            if ($_.ErrorDetails.Message) {
                Write-Host ""
                Write-Host "Detalhes:" -ForegroundColor Yellow
                $_.ErrorDetails.Message | ConvertFrom-Json | ConvertTo-Json -Depth 5
            }
        }
        
        exit
    }
    else {
        Write-Host "Arquivo não encontrado!" -ForegroundColor Red
        exit
    }
}

Write-Host "URL da imagem: $imageUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "Enviando para o Gemini..." -ForegroundColor Yellow

$body = @{
    imageUrl = $imageUrl
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$apiBaseUrl/api/test/gemini/url" -Method Post -Body $body -ContentType "application/json"
    
    Write-Host ""
    Write-Host "✅ SUCESSO!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Resultado da análise:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 5
}
catch {
    Write-Host ""
    Write-Host "❌ ERRO!" -ForegroundColor Red
    Write-Host $_.Exception.Message
    if ($_.ErrorDetails.Message) {
        Write-Host ""
        Write-Host "Detalhes:" -ForegroundColor Yellow
        $_.ErrorDetails.Message | ConvertFrom-Json | ConvertTo-Json -Depth 5
    }
}
