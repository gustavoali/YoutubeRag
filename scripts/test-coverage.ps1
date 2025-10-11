#!/usr/bin/env pwsh
# PowerShell script to run tests with coverage and generate reports

param(
    [string]$Configuration = "Debug",
    [switch]$SkipBuild = $false,
    [switch]$OpenReport = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  YoutubeRag.NET Coverage Test Runner  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent $PSScriptRoot
$coverageDir = Join-Path $rootDir "TestResults"
$reportDir = Join-Path $coverageDir "CoverageReport"

# Clean previous results
Write-Host "Cleaning previous test results..." -ForegroundColor Yellow
if (Test-Path $coverageDir) {
    Remove-Item -Path $coverageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $coverageDir -Force | Out-Null

# Run tests with coverage
Write-Host ""
Write-Host "Running tests with coverage collection..." -ForegroundColor Yellow
Write-Host ""

$testProjects = @(
    "YoutubeRag.Tests.Unit",
    "YoutubeRag.Tests.Integration"
)

$coverageFiles = @()

foreach ($project in $testProjects) {
    $projectPath = Join-Path $rootDir $project
    if (Test-Path $projectPath) {
        Write-Host "Testing: $project" -ForegroundColor Green

        $buildArg = if ($SkipBuild) { "--no-build" } else { "" }

        dotnet test "$projectPath" `
            --configuration $Configuration `
            $buildArg `
            --settings "$rootDir\.runsettings" `
            --collect:"XPlat Code Coverage" `
            --results-directory $coverageDir `
            --logger "console;verbosity=normal"

        if ($LASTEXITCODE -ne 0) {
            Write-Host "Tests failed for $project" -ForegroundColor Red
            exit $LASTEXITCODE
        }

        Write-Host ""
    } else {
        Write-Host "Warning: Project not found: $project" -ForegroundColor Yellow
    }
}

# Find all coverage files
Write-Host "Collecting coverage files..." -ForegroundColor Yellow
$coverageFiles = Get-ChildItem -Path $coverageDir -Filter "coverage.cobertura.xml" -Recurse | Select-Object -ExpandProperty FullName

if ($coverageFiles.Count -eq 0) {
    Write-Host "No coverage files found!" -ForegroundColor Red
    exit 1
}

Write-Host "Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Green

# Generate coverage report
Write-Host ""
Write-Host "Generating coverage report..." -ForegroundColor Yellow

$reportTypes = "Html;JsonSummary;Badges;Cobertura;MarkdownSummaryGithub"
$coverageFilesArg = $coverageFiles -join ";"

reportgenerator `
    "-reports:$coverageFilesArg" `
    "-targetdir:$reportDir" `
    "-reporttypes:$reportTypes" `
    "-verbosity:Info" `
    "-title:YoutubeRag.NET Coverage Report" `
    "-assemblyfilters:+YoutubeRag.Domain;+YoutubeRag.Application;+YoutubeRag.Infrastructure;+YoutubeRag.Api"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to generate coverage report" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Display coverage summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Coverage Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$summaryFile = Join-Path $reportDir "Summary.json"
if (Test-Path $summaryFile) {
    $summary = Get-Content $summaryFile | ConvertFrom-Json

    Write-Host ""
    Write-Host "Overall Coverage:" -ForegroundColor Green
    Write-Host "  Line Coverage:   $($summary.coverage.linecoverage)%" -ForegroundColor $(if ($summary.coverage.linecoverage -ge 90) { "Green" } elseif ($summary.coverage.linecoverage -ge 80) { "Yellow" } else { "Red" })
    Write-Host "  Branch Coverage: $($summary.coverage.branchcoverage)%" -ForegroundColor $(if ($summary.coverage.branchcoverage -ge 85) { "Green" } elseif ($summary.coverage.branchcoverage -ge 75) { "Yellow" } else { "Red" })
    Write-Host "  Method Coverage: $($summary.coverage.methodcoverage)%" -ForegroundColor $(if ($summary.coverage.methodcoverage -ge 90) { "Green" } elseif ($summary.coverage.methodcoverage -ge 80) { "Yellow" } else { "Red" })
    Write-Host ""
}

Write-Host "Coverage report generated at:" -ForegroundColor Green
Write-Host "  $reportDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "HTML Report: $reportDir\index.html" -ForegroundColor Cyan
Write-Host ""

# Open report if requested
if ($OpenReport) {
    $indexPath = Join-Path $reportDir "index.html"
    if (Test-Path $indexPath) {
        Write-Host "Opening coverage report in browser..." -ForegroundColor Yellow
        Start-Process $indexPath
    }
}

Write-Host "Done!" -ForegroundColor Green
