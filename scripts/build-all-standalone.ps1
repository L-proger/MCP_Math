param(
    [string]$Configuration = "Release",
    [string[]]$Runtimes = @("win-x64", "linux-x64", "linux-arm64", "osx-arm64")
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildScript = Join-Path $scriptDir "build-standalone.ps1"

if (-not (Test-Path -LiteralPath $buildScript)) {
    throw "Build script was not found: $buildScript"
}

foreach ($runtime in $Runtimes) {
    Write-Host ""
    Write-Host "Building standalone package for $runtime..."
    & $buildScript -Runtime $runtime -Configuration $Configuration
}

Write-Host ""
Write-Host "Standalone builds completed."
