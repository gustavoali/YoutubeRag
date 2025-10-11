# Load Test Runner (PowerShell)
# Run specific load test with parameters

param(
    [string]$TestName = "video-ingestion-load",
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$K6Dir = Split-Path -Parent $ScriptDir
$TestDir = Join-Path $K6Dir "tests"
$ReportsDir = Join-Path $K6Dir "reports"

# Create reports directory
New-Item -ItemType Directory -Force -Path $ReportsDir | Out-Null

$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$ExportHtml = Join-Path $ReportsDir "$TestName-$Timestamp.html"
$ExportJson = Join-Path $ReportsDir "$TestName-$Timestamp.json"

Write-Host "================================"
Write-Host "Running k6 Load Test: $TestName"
Write-Host "================================"
Write-Host "Target: $BaseUrl"
Write-Host "Timestamp: $Timestamp"
Write-Host ""

# Check if test file exists
$TestFile = Join-Path $TestDir "$TestName.js"
if (-not (Test-Path $TestFile)) {
    Write-Host "Error: Test file not found: $TestFile" -ForegroundColor Red
    Write-Host ""
    Write-Host "Available tests:"
    Get-ChildItem -Path $TestDir -Filter "*.js" | ForEach-Object { Write-Host "  - $($_.BaseName)" }
    exit 1
}

# Run the test
$Environment = if ($env:ENVIRONMENT) { $env:ENVIRONMENT } else { "local" }

k6 run `
  --out "json=$ExportJson" `
  -e "BASE_URL=$BaseUrl" `
  -e "EXPORT_HTML=$ExportHtml" `
  -e "EXPORT_JSON=$ExportJson" `
  -e "ENVIRONMENT=$Environment" `
  $TestFile

Write-Host ""
Write-Host "================================"
Write-Host "Test complete!"
Write-Host "HTML Report: $ExportHtml"
Write-Host "JSON Report: $ExportJson"
Write-Host "================================"
