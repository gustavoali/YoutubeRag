# Smoke Test Runner (PowerShell)
# Quick smoke test to validate all critical endpoints

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$K6Dir = Split-Path -Parent $ScriptDir
$TestDir = Join-Path $K6Dir "tests"
$ReportsDir = Join-Path $K6Dir "reports"

# Create reports directory if it doesn't exist
New-Item -ItemType Directory -Force -Path $ReportsDir | Out-Null

# Default values
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5000" }
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$ExportHtml = Join-Path $ReportsDir "smoke-test-$Timestamp.html"
$ExportJson = Join-Path $ReportsDir "smoke-test-$Timestamp.json"

Write-Host "================================"
Write-Host "Running k6 Smoke Test"
Write-Host "================================"
Write-Host "Target: $BaseUrl"
Write-Host "Reports: $ReportsDir"
Write-Host ""

# Run the test
$TestFile = Join-Path $TestDir "smoke.js"

k6 run `
  --out "json=$ExportJson" `
  -e "BASE_URL=$BaseUrl" `
  -e "EXPORT_HTML=$ExportHtml" `
  -e "EXPORT_JSON=$ExportJson" `
  $TestFile

Write-Host ""
Write-Host "================================"
Write-Host "Test complete!"
Write-Host "HTML Report: $ExportHtml"
Write-Host "JSON Report: $ExportJson"
Write-Host "================================"
