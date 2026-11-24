# Compila a solução, abre uma janela para cada microserviço e, por último, o cliente CMD.
# Uso: .\iniciar.ps1            (ou dois cliques em iniciar.bat)
#      .\iniciar.ps1 -SemCliente (só sobe os serviços, útil para testar a API com curl)
param([switch]$SemCliente)

$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot

Write-Host "Compilando a solução..." -ForegroundColor DarkYellow
dotnet build (Join-Path $raiz 'ArteSuave.Microservicos.sln') -nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Falha na compilação." }

function Get-Executavel([string]$projeto) {
    Join-Path $raiz "src\$projeto\bin\Debug\net9.0\$projeto.exe"
}

# A ordem importa pouco (cada serviço sobe sozinho), mas o Gateway por último deixa o log mais limpo
$servicos = 'ArteSuave.Catalogo.Api', 'ArteSuave.Vendas.Api', 'ArteSuave.Relatorios.Api', 'ArteSuave.Gateway'
foreach ($servico in $servicos) {
    $exe = Get-Executavel $servico
    Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
    Write-Host "  iniciado: $servico" -ForegroundColor Gray
}

# Espera o Gateway responder antes de abrir o cliente
$gateway = if ($env:ARTESUAVE_GATEWAY_URL) { $env:ARTESUAVE_GATEWAY_URL.TrimEnd('/') } else { 'http://localhost:5100' }
for ($i = 0; $i -lt 30; $i++) {
    try { Invoke-RestMethod "$gateway/saude" -TimeoutSec 1 | Out-Null; break } catch { Start-Sleep -Milliseconds 500 }
}

Write-Host "Serviços no ar. Gateway em $gateway" -ForegroundColor Green

if (-not $SemCliente) {
    & (Get-Executavel 'ArteSuave.Cliente.Cmd')
}
