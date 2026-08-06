$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-RepositoryRoot {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    return Split-Path -Parent $PSScriptRoot
}

function Assert-CommandAvailable {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name
    )

    if ($null -eq (Get-Command -Name $Name -ErrorAction SilentlyContinue)) {
        throw "No se encontró el comando '$Name' en PATH."
    }
}

function Invoke-Checked {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Description,

        [Parameter(Mandatory)]
        [string]$FilePath,

        [string[]]$ArgumentList = @()
    )

    Write-Host "> $Description" -ForegroundColor Cyan
    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "El comando '$Description' terminó con código $LASTEXITCODE."
    }
}

function Test-HttpEndpoint {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory)]
        [uri]$Uri
    )

    try {
        $response = Invoke-WebRequest `
            -Uri $Uri `
            -Method Get `
            -TimeoutSec 2 `
            -UseBasicParsing

        return $response.StatusCode -ge 200 -and $response.StatusCode -lt 400
    }
    catch {
        return $false
    }
}

function Wait-HttpEndpoint {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [uri]$Uri,

        [ValidateRange(1, 300)]
        [int]$TimeoutSeconds = 60
    )

    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        if (Test-HttpEndpoint -Uri $Uri) {
            Write-Host "Disponible: $Uri" -ForegroundColor Green
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "El endpoint '$Uri' no respondió antes de $TimeoutSeconds segundos."
}
