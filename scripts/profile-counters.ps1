param(
    [Parameter(Mandatory = $true)]
    [int]$ProcessId,

    [int]$DurationSeconds = 60,

    [string]$OutputDirectory = "profiling-results"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputPath = Join-Path $OutputDirectory "counters-$timestamp.csv"

dotnet-counters collect `
    --process-id $ProcessId `
    --duration "00:00:$($DurationSeconds.ToString('00'))" `
    --format csv `
    --output $outputPath `
    --counters System.Runtime,Microsoft.AspNetCore.Hosting

Write-Host "Counters saved to $outputPath"
