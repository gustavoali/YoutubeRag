#!/usr/bin/env pwsh
# PowerShell script to open the coverage report

$rootDir = Split-Path -Parent $PSScriptRoot
$reportPath = Join-Path $rootDir "TestResults\CoverageReport\index.html"

if (Test-Path $reportPath) {
    Write-Host "Opening coverage report..." -ForegroundColor Green
    Start-Process $reportPath
} else {
    Write-Host "Coverage report not found at: $reportPath" -ForegroundColor Red
    Write-Host "Run './scripts/test-coverage.ps1' first to generate the report." -ForegroundColor Yellow
    exit 1
}
