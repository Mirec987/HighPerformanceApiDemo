param(
    [Parameter(Mandatory = $true)]
    [int]$ProcessId,

    [int]$DurationSeconds = 60,

    [string]$OutputDirectory = "profiling-results"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputPath = Join-Path $OutputDirectory "trace-$timestamp.nettrace"

dotnet-trace collect `
    --process-id $ProcessId `
    --duration "00:00:$($DurationSeconds.ToString('00'))" `
    --profile dotnet-common `
    --output $outputPath

Write-Host "Trace saved to $outputPath"
