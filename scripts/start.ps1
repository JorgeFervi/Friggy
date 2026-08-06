$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'common.ps1')

function Start-FriggyProject {
    [CmdletBinding()]
    [OutputType([System.Diagnostics.Process])]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Project,

        [Parameter(Mandatory)]
        [uri]$Url,

        [Parameter(Mandatory)]
        [uri]$HealthUri,

        [Parameter(Mandatory)]
        [string]$WorkingDirectory,

        [Parameter(Mandatory)]
        [string]$LogDirectory
    )

    if (Test-HttpEndpoint -Uri $HealthUri) {
        Write-Host "$Name ya está disponible en $Url" -ForegroundColor Yellow
        return $null
    }

    $standardOutput = Join-Path $LogDirectory "$($Name.ToLowerInvariant()).out.log"
    $standardError = Join-Path $LogDirectory "$($Name.ToLowerInvariant()).err.log"
    $process = Start-Process `
        -FilePath 'dotnet' `
        -ArgumentList @(
            'run',
            '--project',
            $Project,
            '--no-launch-profile',
            '--no-restore',
            '--environment',
            'ASPNETCORE_ENVIRONMENT=Development',
            '--urls',
            $Url.AbsoluteUri
        ) `
        -WorkingDirectory $WorkingDirectory `
        -WindowStyle Hidden `
        -RedirectStandardOutput $standardOutput `
        -RedirectStandardError $standardError `
        -PassThru

    try {
        Wait-HttpEndpoint -Uri $HealthUri
        return $process
    }
    catch {
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id
        }

        throw "$Name no pudo iniciarse. Revisa '$standardOutput' y '$standardError'. $($_.Exception.Message)"
    }
}

$repositoryRoot = Get-RepositoryRoot
$logDirectory = Join-Path $repositoryRoot '.friggy/logs'
$apiUrl = [uri]'http://localhost:5292'
$apiHealthUri = [uri]'http://localhost:5292/health'
$webUrl = [uri]'http://localhost:5179'
$startedProcesses = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()

Push-Location $repositoryRoot
try {
    Assert-CommandAvailable -Name 'dotnet'
    Assert-CommandAvailable -Name 'docker'
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

    Invoke-Checked `
        -Description 'docker compose up --detach --wait postgres' `
        -FilePath 'docker' `
        -ArgumentList @('compose', 'up', '--detach', '--wait', 'postgres')

    $apiProcess = Start-FriggyProject `
        -Name 'Api' `
        -Project 'src/Friggy.Api/Friggy.Api.csproj' `
        -Url $apiUrl `
        -HealthUri $apiHealthUri `
        -WorkingDirectory $repositoryRoot `
        -LogDirectory $logDirectory
    if ($null -ne $apiProcess) {
        $startedProcesses.Add($apiProcess)
    }

    $webProcess = Start-FriggyProject `
        -Name 'Web' `
        -Project 'src/Friggy.Web/Friggy.Web.csproj' `
        -Url $webUrl `
        -HealthUri $webUrl `
        -WorkingDirectory $repositoryRoot `
        -LogDirectory $logDirectory
    if ($null -ne $webProcess) {
        $startedProcesses.Add($webProcess)
    }

    Write-Host ''
    Write-Host "API: $apiUrl" -ForegroundColor Green
    Write-Host "Web: $webUrl" -ForegroundColor Green
    Write-Host "Logs: $logDirectory"
    Write-Host "Para E2E: `$env:FRIGGY_WEB_BASE_URL = '$webUrl'"
}
catch {
    foreach ($startedProcess in $startedProcesses) {
        if (-not $startedProcess.HasExited) {
            Stop-Process -Id $startedProcess.Id
        }
    }

    throw
}
finally {
    Pop-Location
}
