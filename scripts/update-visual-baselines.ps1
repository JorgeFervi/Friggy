param(
    [switch]$Accept
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'common.ps1')

if (-not $Accept) {
    throw 'Para aceptar capturas revisadas, ejecuta update-visual-baselines.ps1 -Accept.'
}

$repositoryRoot = Get-RepositoryRoot
$platformName = if ($env:OS -eq 'Windows_NT') {
    'windows-chromium'
}
elseif ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Unix) {
    'linux-chromium'
}
else {
    'macos-chromium'
}
$actualDirectory = Join-Path $repositoryRoot "TestResults/visual/$platformName/actual"
$baselineDirectory = Join-Path $repositoryRoot "tests/Friggy.EndToEndTests/VisualBaselines/$platformName"

if (-not (Test-Path -LiteralPath $actualDirectory -PathType Container)) {
    throw "No hay capturas actuales revisables en '$actualDirectory'."
}

$actualImages = @(Get-ChildItem -LiteralPath $actualDirectory -Filter '*.png' -File)
if ($actualImages.Count -eq 0) {
    throw "No hay capturas PNG actuales en '$actualDirectory'."
}

New-Item -ItemType Directory -Force -Path $baselineDirectory | Out-Null
foreach ($actualImage in $actualImages) {
    Copy-Item `
        -LiteralPath $actualImage.FullName `
        -Destination (Join-Path $baselineDirectory $actualImage.Name) `
        -Force
}

Write-Host "Se han aceptado $($actualImages.Count) baseline visuales para $platformName." -ForegroundColor Green
