# Função auxiliar para carregar variáveis de ambiente do arquivo .env
# Esta função deve ser dot-sourced nos scripts que precisam carregar o .env

function Load-EnvFile {
    param(
        [string]$EnvPath = ".env"
    )
    
    # Tentar encontrar o arquivo .env na raiz do projeto
    $scriptDir = Split-Path -Parent $MyInvocation.PSScriptRoot
    $projectRoot = Split-Path -Parent $scriptDir
    $envFile = Join-Path $projectRoot $EnvPath
    
    # Se não encontrou, tentar no diretório atual
    if (-not (Test-Path $envFile)) {
        $envFile = Join-Path (Get-Location) $EnvPath
    }
    
    # Se ainda não encontrou, tentar no diretório do script
    if (-not (Test-Path $envFile)) {
        $envFile = Join-Path $scriptDir $EnvPath
    }
    
    if (-not (Test-Path $envFile)) {
        Write-Warning "Arquivo .env não encontrado em: $envFile"
        Write-Warning "Usando valores padrão ou variáveis de ambiente do sistema"
        return
    }
    
    Write-Host "Carregando variáveis de ambiente de: $envFile" -ForegroundColor Gray
    
    # Ler o arquivo .env e processar cada linha
    Get-Content $envFile | ForEach-Object {
        $line = $_.Trim()
        
        # Ignorar linhas vazias e comentários
        if ($line -and -not $line.StartsWith("#")) {
            # Separar chave e valor
            $parts = $line -split "=", 2
            if ($parts.Length -eq 2) {
                $key = $parts[0].Trim()
                $value = $parts[1].Trim()
                
                # Converter formato ASP.NET Core (__) para variável de ambiente
                # ConnectionStrings__DefaultConnection -> ConnectionStrings:DefaultConnection
                $envKey = $key -replace "__", ":"
                
                # Definir variável de ambiente
                [Environment]::SetEnvironmentVariable($key, $value, "Process")
                
                # Também criar variável PowerShell para facilitar acesso
                $psKey = $key -replace "__", "_" -replace "-", "_"
                Set-Variable -Name $psKey -Value $value -Scope Script -ErrorAction SilentlyContinue
            }
        }
    }
    
    Write-Host "Variáveis de ambiente carregadas com sucesso!" -ForegroundColor Green
}

# Função helper para obter valor do .env ou variável de ambiente
function Get-EnvValue {
    param(
        [string]$Key,
        [string]$DefaultValue = ""
    )
    
    # Tentar obter da variável de ambiente do processo
    $value = [Environment]::GetEnvironmentVariable($Key, "Process")
    
    if ([string]::IsNullOrEmpty($value)) {
        # Tentar obter da variável de ambiente do sistema
        $value = [Environment]::GetEnvironmentVariable($Key, "Machine")
    }
    
    if ([string]::IsNullOrEmpty($value)) {
        # Tentar obter da variável de ambiente do usuário
        $value = [Environment]::GetEnvironmentVariable($Key, "User")
    }
    
    if ([string]::IsNullOrEmpty($value)) {
        return $DefaultValue
    }
    
    return $value
}
