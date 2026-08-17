$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'common.ps1')

function Get-VulnerabilityCount {
    [CmdletBinding()]
    [OutputType([int])]
    param(
        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [object]$Node
    )

    if ($null -eq $Node -or $Node -is [string] -or $Node -is [ValueType]) {
        return 0
    }

    if ($Node -is [System.Collections.IEnumerable]) {
        $count = 0
        foreach ($item in $Node) {
            $count += Get-VulnerabilityCount -Node $item
        }

        return $count
    }

    $count = 0
    foreach ($property in $Node.PSObject.Properties) {
        if ($property.Name -eq 'vulnerabilities') {
            $count += @($property.Value).Count
        }
        else {
            $count += Get-VulnerabilityCount -Node $property.Value
        }
    }

    return $count
}

$repositoryRoot = Get-RepositoryRoot

Push-Location $repositoryRoot
try {
    Assert-CommandAvailable -Name 'dotnet'
    Assert-CommandAvailable -Name 'docker'
    Assert-CommandAvailable -Name 'node'
    Assert-CommandAvailable -Name 'pnpm'

    Invoke-Checked `
        -Description 'dotnet restore Friggy.sln --configfile NuGet.Config' `
        -FilePath 'dotnet' `
        -ArgumentList @('restore', 'Friggy.sln', '--configfile', 'NuGet.Config')
    Invoke-Checked `
        -Description 'pnpm install --frozen-lockfile' `
        -FilePath 'pnpm' `
        -ArgumentList @('install', '--frozen-lockfile')
    Invoke-Checked `
        -Description 'dotnet build Friggy.sln --configuration Release --no-restore' `
        -FilePath 'dotnet' `
        -ArgumentList @('build', 'Friggy.sln', '--configuration', 'Release', '--no-restore')
    Invoke-Checked `
        -Description 'dotnet format Friggy.sln --verify-no-changes --no-restore' `
        -FilePath 'dotnet' `
        -ArgumentList @('format', 'Friggy.sln', '--verify-no-changes', '--no-restore')

    Write-Host '> ./scripts/test.ps1 -SkipBuild' -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot 'test.ps1') -SkipBuild
    if ($LASTEXITCODE -ne 0) {
        throw "El gate de suites terminó con código $LASTEXITCODE."
    }

    Invoke-Checked `
        -Description 'pnpm audit --audit-level high' `
        -FilePath 'pnpm' `
        -ArgumentList @('audit', '--audit-level', 'high')

    Write-Host '> dotnet list Friggy.sln package --vulnerable --include-transitive --format json --no-restore' -ForegroundColor Cyan
    $auditOutput = & dotnet list Friggy.sln package `
        --vulnerable `
        --include-transitive `
        --format json `
        --no-restore 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "La auditoría de paquetes terminó con código $LASTEXITCODE."
    }

    $audit = ($auditOutput -join [Environment]::NewLine) | ConvertFrom-Json
    $vulnerabilityCount = Get-VulnerabilityCount -Node $audit
    if ($vulnerabilityCount -gt 0) {
        throw "La auditoría detectó $vulnerabilityCount vulnerabilidad(es) de paquetes."
    }

    Write-Host 'Gate de calidad completado correctamente.' -ForegroundColor Green
}
finally {
    Pop-Location
}
