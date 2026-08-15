# Starts the Azurite storage emulator for local Azure Functions development.
#
# Prerequisites: npm install -g azurite   (or run via Docker)
# Once running, the Functions local.settings.json value
# "AzureWebJobsStorage": "UseDevelopmentStorage=true" points at:
#   Blob  -> http://127.0.0.1:10000
#   Queue -> http://127.0.0.1:10001
#   Table -> http://127.0.0.1:10002

param(
    [string]$Location = (Join-Path $PSScriptRoot "..\.azurite"),
    [int]$BlobPort = 10000,
    [int]$QueuePort = 10001,
    [int]$TablePort = 10002
)

if (-not (Get-Command azurite -ErrorAction SilentlyContinue)) {
    Write-Error "Azurite is not installed. Install it with: npm install -g azurite"
    exit 1
}

if (-not (Test-Path -LiteralPath $Location)) {
    New-Item -ItemType Directory -Path $Location | Out-Null
}

$resolvedLocation = (Resolve-Path -LiteralPath $Location).Path

Write-Host "Starting Azurite:"
Write-Host "  Location: $resolvedLocation"
Write-Host "  Blob:     http://127.0.0.1:$BlobPort"
Write-Host "  Queue:    http://127.0.0.1:$QueuePort"
Write-Host "  Table:    http://127.0.0.1:$TablePort"
Write-Host "Press Ctrl+C to stop."

azurite --location $resolvedLocation `
    --blobPort $BlobPort `
    --queuePort $QueuePort `
    --tablePort $TablePort
