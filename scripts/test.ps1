param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'common.ps1')

$repositoryRoot = Get-RepositoryRoot

Push-Location $repositoryRoot
try {
    Assert-CommandAvailable -Name 'dotnet'
    Assert-CommandAvailable -Name 'docker'

    Invoke-Checked `
        -Description 'docker info' `
        -FilePath 'docker' `
        -ArgumentList @('info', '--format', '{{.ServerVersion}}')
    if (-not $SkipBuild) {
        Invoke-Checked `
            -Description 'dotnet build Friggy.sln --configuration Release' `
            -FilePath 'dotnet' `
            -ArgumentList @('build', 'Friggy.sln', '--configuration', 'Release')
    }
    Invoke-Checked `
        -Description 'dotnet test --project tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj --configuration Release --no-build --no-restore' `
        -FilePath 'dotnet' `
        -ArgumentList @(
            'test',
            '--project',
            'tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj',
            '--configuration',
            'Release',
            '--no-build',
            '--no-restore'
        )
    Invoke-Checked `
        -Description 'dotnet test --project tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj --configuration Release --no-build --no-restore' `
        -FilePath 'dotnet' `
        -ArgumentList @(
            'test',
            '--project',
            'tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj',
            '--configuration',
            'Release',
            '--no-build',
            '--no-restore'
        )
    Invoke-Checked `
        -Description 'dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj --configuration Release --no-build --no-restore' `
        -FilePath 'dotnet' `
        -ArgumentList @(
            'test',
            '--project',
            'tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj',
            '--configuration',
            'Release',
            '--no-build',
            '--no-restore'
        )
    Invoke-Checked `
        -Description 'dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj --configuration Release --no-build --no-restore' `
        -FilePath 'dotnet' `
        -ArgumentList @(
            'test',
            '--project',
            'tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj',
            '--configuration',
            'Release',
            '--no-build',
            '--no-restore'
        )

    if ([string]::IsNullOrWhiteSpace($env:FRIGGY_WEB_BASE_URL)) {
        $env:FRIGGY_WEB_BASE_URL = 'http://localhost:5179'
    }

    Invoke-Checked `
        -Description 'dotnet test --project tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj --configuration Release --no-build --no-restore' `
        -FilePath 'dotnet' `
        -ArgumentList @(
            'test',
            '--project',
            'tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj',
            '--configuration',
            'Release',
            '--no-build',
            '--no-restore'
        )

    Write-Host 'Todas las suites han finalizado correctamente.' -ForegroundColor Green
}
finally {
    Pop-Location
}
