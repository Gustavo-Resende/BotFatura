# Script completo para configurar e testar o sistema

# Carregar variáveis de ambiente do .env
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptPath "_LoadEnv.ps1")
Load-EnvFile

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CONFIGURAÇÃO E TESTE DO BOT FATURA" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Obter valores do .env ou usar padrões
$apiBaseUrl = Get-EnvValue "API_BASE_URL" "http://localhost:5000"
$adminEmail = Get-EnvValue "DefaultAdmin__Email" "admin@botfatura.com.br"
$adminPassword = Get-EnvValue "DefaultAdmin__Password" ""

if ([string]::IsNullOrEmpty($adminPassword)) {
    Write-Host "⚠️  AVISO: Senha do admin não encontrada no .env. Usando valor padrão." -ForegroundColor Yellow
    $adminPassword = "SUA_SENHA_AQUI"
}

# 1. Login
Write-Host "1️⃣  Fazendo login..." -ForegroundColor Yellow
$loginBody = @{
    email = $adminEmail
    password = $adminPassword
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$apiBaseUrl/api/auth/login" -Method POST -Headers @{
        "Content-Type" = "application/json"
    } -Body $loginBody
    
    $token = $loginResponse.token
    Write-Host "   ✅ Login realizado com sucesso!" -ForegroundColor Green
    Write-Host "   Token: $($token.Substring(0, 20))...`n" -ForegroundColor Gray
}
catch {
    Write-Host "   ❌ Erro no login: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 2. Cadastrar Cliente
Write-Host "2️⃣  Cadastrando cliente..." -ForegroundColor Yellow
$clienteBody = @{
    nomeCompleto = "Rodrigo Teste"
    whatsapp = "154687642832914"
} | ConvertTo-Json

try {
    $clienteResponse = Invoke-RestMethod -Uri "$apiBaseUrl/api/clientes" -Method POST -Headers @{
        "Content-Type" = "application/json"
        "Authorization" = "Bearer $token"
    } -Body $clienteBody
    
    $clienteId = $clienteResponse.id
    Write-Host "   ✅ Cliente cadastrado com sucesso!" -ForegroundColor Green
    Write-Host "   ID: $clienteId" -ForegroundColor Gray
    Write-Host "   Nome: $($clienteResponse.nomeCompleto)" -ForegroundColor Gray
    Write-Host "   WhatsApp: $($clienteResponse.whatsApp)`n" -ForegroundColor Gray
}
catch {
    Write-Host "   ❌ Erro ao cadastrar cliente: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "   Detalhes: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

# 3. Obter/Criar Configuração
Write-Host "3️⃣  Configurando PIX..." -ForegroundColor Yellow
try {
    $configResponse = Invoke-RestMethod -Uri "$apiBaseUrl/api/configuracoes" -Method GET -Headers @{
        "Authorization" = "Bearer $token"
    }
    Write-Host "   ℹ️  Configuração já existe" -ForegroundColor Blue
}
catch {
    # Criar nova configuração
    $configBody = @{
        chavePix = "seuemail@exemplo.com"
        nomeTitularPix = "SEU NOME AQUI"
        diasAntecedenciaLembrete = 3
        diasAposVencimentoCobranca = 7
        grupoSociosWhatsAppId = "120363023769164146@g.us"
    } | ConvertTo-Json
    
    try {
        $configResponse = Invoke-RestMethod -Uri "$apiBaseUrl/api/configuracoes" -Method POST -Headers @{
            "Content-Type" = "application/json"
            "Authorization" = "Bearer $token"
        } -Body $configBody
        Write-Host "   ✅ Configuração criada!" -ForegroundColor Green
    }
    catch {
        Write-Host "   ⚠️  Erro na configuração (não crítico): $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host "   Chave PIX: $($configResponse.chavePix)" -ForegroundColor Gray
Write-Host "   Titular: $($configResponse.nomeTitularPix)`n" -ForegroundColor Gray

# 4. Criar Fatura
Write-Host "4️⃣  Criando fatura de teste..." -ForegroundColor Yellow
$faturaBody = @{
    clienteId = $clienteId
    valor = 150.00
    dataVencimento = "2026-03-15"
} | ConvertTo-Json

try {
    $faturaResponse = Invoke-RestMethod -Uri "$apiBaseUrl/api/faturas" -Method POST -Headers @{
        "Content-Type" = "application/json"
        "Authorization" = "Bearer $token"
    } -Body $faturaBody
    
    $faturaId = $faturaResponse.id
    Write-Host "   ✅ Fatura criada com sucesso!" -ForegroundColor Green
    Write-Host "   ID: $faturaId" -ForegroundColor Gray
    Write-Host "   Valor: R`$ $($faturaResponse.valor)" -ForegroundColor Gray
    Write-Host "   Vencimento: $($faturaResponse.dataVencimento)" -ForegroundColor Gray
    Write-Host "   Status: $($faturaResponse.status)`n" -ForegroundColor Gray
}
catch {
    Write-Host "   ❌ Erro ao criar fatura: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "   Detalhes: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

# Resumo
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  ✅ CONFIGURAÇÃO CONCLUÍDA!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📱 AGORA VOCÊ PODE TESTAR:" -ForegroundColor Yellow
Write-Host "   1. Envie uma IMAGEM ou PDF de um comprovante" -ForegroundColor White
Write-Host "      de PIX de R$ 150,00 via WhatsApp" -ForegroundColor White
Write-Host ""
Write-Host "   2. O número deve ser: 154687642832914" -ForegroundColor White
Write-Host "      (o Rodrigo que apareceu no log)" -ForegroundColor White
Write-Host ""
Write-Host "   3. Acompanhe os logs no terminal da API" -ForegroundColor White
Write-Host ""
Write-Host "📊 DADOS DO TESTE:" -ForegroundColor Cyan
Write-Host "   Cliente ID: $clienteId" -ForegroundColor Gray
Write-Host "   Fatura ID: $faturaId" -ForegroundColor Gray
Write-Host "   Valor: R`$ 150,00" -ForegroundColor Gray
Write-Host "   Vencimento: 15/03/2026" -ForegroundColor Gray
Write-Host ""
