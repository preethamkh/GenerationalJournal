# Starts the Azure Functions host locally for development.
#
# Run the Azurite emulator first (see scripts/start-azurite.ps1), then
# invoke this script from the repository root. Requires the Azure Functions
# Core Tools: npm install -g azure-functions-core-tools@4

if (-not (Get-Command func -ErrorAction SilentlyContinue)) {
    Write-Error "Azure Functions Core Tools are not installed. Install with: npm install -g azure-functions-core-tools@4"
    exit 1
}

$functionsRoot = Join-Path $PSScriptRoot "..\GenerationalJournal.Functions"

Write-Host "Starting Functions host from: $functionsRoot"
Push-Location $functionsRoot
try {
    func start
}
finally {
    Pop-Location
}
