# Run All Performance Tests (PowerShell)
# Execute complete test suite sequentially

param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "================================"
Write-Host "Running Complete k6 Test Suite"
Write-Host "================================"
Write-Host "Target: $BaseUrl"
Write-Host ""

# Array of tests to run
$Tests = @(
    "smoke",
    "video-ingestion-load",
    "search-load",
    "stress",
    "spike"
)

$FailedTests = @()
$PassedTests = @()

foreach ($Test in $Tests) {
    Write-Host ""
    Write-Host "→ Running $Test..." -ForegroundColor Cyan
    Write-Host ""

    try {
        & "$ScriptDir\run-load.ps1" -TestName $Test -BaseUrl $BaseUrl
        $PassedTests += $Test
        Write-Host "✅ $Test PASSED" -ForegroundColor Green
    }
    catch {
        $FailedTests += $Test
        Write-Host "❌ $Test FAILED" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Red
    }

    # Wait between tests
    Start-Sleep -Seconds 10
}

Write-Host ""
Write-Host "================================"
Write-Host "Test Suite Complete"
Write-Host "================================"
Write-Host "Passed: $($PassedTests.Count)"
Write-Host "Failed: $($FailedTests.Count)"
Write-Host ""

if ($FailedTests.Count -gt 0) {
    Write-Host "Failed tests:" -ForegroundColor Red
    foreach ($Test in $FailedTests) {
        Write-Host "  - $Test" -ForegroundColor Red
    }
    exit 1
}

Write-Host "All tests passed! ✅" -ForegroundColor Green
