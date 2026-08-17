$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'common.ps1')

$repositoryRoot = Get-RepositoryRoot

Push-Location $repositoryRoot
try {
    Assert-CommandAvailable -Name 'dotnet'
    Assert-CommandAvailable -Name 'docker'
    Assert-CommandAvailable -Name 'node'
    Assert-CommandAvailable -Name 'pnpm'

    Invoke-Checked `
        -Description 'pnpm install --frozen-lockfile' `
        -FilePath 'pnpm' `
        -ArgumentList @('install', '--frozen-lockfile')

    Invoke-Checked `
        -Description 'dotnet --version' `
        -FilePath 'dotnet' `
        -ArgumentList @('--version')
    Invoke-Checked `
        -Description 'docker info' `
        -FilePath 'docker' `
        -ArgumentList @('info', '--format', '{{.ServerVersion}}')
    Invoke-Checked `
        -Description 'docker compose up --detach --wait postgres' `
        -FilePath 'docker' `
        -ArgumentList @('compose', 'up', '--detach', '--wait', 'postgres')
    Invoke-Checked `
        -Description 'dotnet tool restore --configfile NuGet.Config' `
        -FilePath 'dotnet' `
        -ArgumentList @('tool', 'restore', '--configfile', 'NuGet.Config')
    Invoke-Checked `
        -Description 'dotnet restore Friggy.sln --configfile NuGet.Config' `
        -FilePath 'dotnet' `
        -ArgumentList @('restore', 'Friggy.sln', '--configfile', 'NuGet.Config')
    Invoke-Checked `
        -Description 'dotnet ef database update' `
        -FilePath 'dotnet' `
        -ArgumentList @(
            'ef',
            'database',
            'update',
            '--project',
            'src/Friggy.Infrastructure/Friggy.Infrastructure.csproj',
            '--startup-project',
            'src/Friggy.Infrastructure/Friggy.Infrastructure.csproj',
            '--context',
            'FriggyDbContext'
        )
    Invoke-Checked `
        -Description 'dotnet build tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj --no-restore' `
        -FilePath 'dotnet' `
        -ArgumentList @(
            'build',
            'tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj',
            '--no-restore'
        )

    $playwrightInstaller = Join-Path `
        $repositoryRoot `
        'tests/Friggy.EndToEndTests/bin/Debug/net10.0/playwright.ps1'
    if (-not (Test-Path -LiteralPath $playwrightInstaller -PathType Leaf)) {
        throw "No se generó el instalador de Playwright en '$playwrightInstaller'."
    }

    Invoke-Checked `
        -Description 'playwright.ps1 install chromium' `
        -FilePath $playwrightInstaller `
        -ArgumentList @('install', 'chromium')

    Write-Host 'Friggy está preparado para desarrollo local.' -ForegroundColor Green
}
finally {
    Pop-Location
}
