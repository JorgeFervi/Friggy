$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'common.ps1')

function Stop-ProcessListeningOnPort {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateRange(1, 65535)]
        [int]$Port,

        [Parameter(Mandatory)]
        [string]$Name
    )

    $processIds = @(
        Get-NetTCPConnection `
            -LocalPort $Port `
            -State Listen `
            -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty OwningProcess -Unique
    )

    if ($processIds.Count -eq 0) {
        Write-Host "$Name ya estaba detenido (puerto $Port)." -ForegroundColor Yellow
        return
    }

    foreach ($processId in $processIds) {
        Stop-Process -Id $processId -ErrorAction Stop
        Write-Host "$Name detenido (puerto $Port, PID $processId)." -ForegroundColor Green
    }
}

$repositoryRoot = Get-RepositoryRoot

Push-Location $repositoryRoot
try {
    Assert-CommandAvailable -Name 'docker'

    Stop-ProcessListeningOnPort -Name 'API' -Port 5292
    Stop-ProcessListeningOnPort -Name 'Web' -Port 5179

    Invoke-Checked `
        -Description 'docker compose stop postgres' `
        -FilePath 'docker' `
        -ArgumentList @('compose', 'stop', 'postgres')

    Write-Host 'API, Web y PostgreSQL detenidos.' -ForegroundColor Green
}
finally {
    Pop-Location
}
