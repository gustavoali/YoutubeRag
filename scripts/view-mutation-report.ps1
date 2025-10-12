#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Open the Stryker.NET mutation testing HTML report
.DESCRIPTION
    This script locates and opens the most recent Stryker.NET mutation report
    for the specified project in the default web browser.
.PARAMETER Project
    Target project: Domain, Application, or latest (default: latest)
.EXAMPLE
    .\view-mutation-report.ps1
    Open the most recent mutation report
.EXAMPLE
    .\view-mutation-report.ps1 -Project Domain
    Open the Domain project mutation report
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Domain", "Application", "Latest")]
    [string]$Project = "Latest"
)

# Color functions
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Write-Success { Write-ColorOutput Green $args }
function Write-Info { Write-ColorOutput Cyan $args }
function Write-Warning { Write-ColorOutput Yellow $args }
function Write-Failure { Write-ColorOutput Red $args }

# Get script directory and repository root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
Set-Location $RepoRoot

Write-Info "================================================================"
Write-Info "   View Stryker.NET Mutation Report"
Write-Info "================================================================"
Write-Info ""

$strykerOutputDir = Join-Path $RepoRoot "StrykerOutput"

# Check if StrykerOutput directory exists
if (-not (Test-Path $strykerOutputDir)) {
    Write-Failure "StrykerOutput directory not found!"
    Write-Info "Please run mutation tests first:"
    Write-Info "  .\scripts\run-mutation-tests.ps1"
    exit 1
}

# Find report based on project selection
$reportPath = $null

if ($Project -eq "Latest") {
    Write-Info "Looking for the most recent mutation report..."

    # Find all mutation reports
    $reports = Get-ChildItem -Path $strykerOutputDir -Recurse -Filter "mutation-report.html" -File |
               Sort-Object LastWriteTime -Descending

    if ($reports.Count -eq 0) {
        Write-Failure "No mutation reports found!"
        Write-Info "Please run mutation tests first:"
        Write-Info "  .\scripts\run-mutation-tests.ps1"
        exit 1
    }

    $reportPath = $reports[0].FullName
    $projectName = $reports[0].Directory.Parent.Parent.Name

    Write-Info "Found report for project: $projectName"
    Write-Info "Report date: $($reports[0].LastWriteTime)"
} else {
    Write-Info "Looking for $Project mutation report..."

    $projectReportPath = Join-Path $strykerOutputDir "$Project\reports\mutation-report.html"

    if (-not (Test-Path $projectReportPath)) {
        Write-Failure "Mutation report not found for project: $Project"
        Write-Info "Expected location: $projectReportPath"
        Write-Info ""
        Write-Info "Please run mutation tests for this project:"
        Write-Info "  .\scripts\run-mutation-tests.ps1 -Project $Project"
        exit 1
    }

    $reportPath = $projectReportPath
}

Write-Info ""
Write-Success "Opening mutation report..."
Write-Info "Location: $reportPath"
Write-Info ""

# Open the report
Start-Process $reportPath

Write-Info "================================================================"
Write-Info "Report opened in your default web browser"
Write-Info "================================================================"
Write-Info ""
Write-Info "Understanding the report:"
Write-Info "  - Green: Mutants killed by tests (good)"
Write-Info "  - Red: Mutants survived (need more tests)"
Write-Info "  - Yellow: Mutants timeout (tests too slow)"
Write-Info "  - Gray: Mutants not covered by tests"
Write-Info ""
Write-Info "Mutation Score = (Killed / Total) * 100"
Write-Info ""
Write-Info "Target thresholds:"
Write-Info "  - High: >= 80% (excellent test coverage)"
Write-Info "  - Low: >= 60% (acceptable test coverage)"
Write-Info "  - Break: >= 50% (minimum acceptable)"
Write-Info ""

# Show available reports
Write-Info "Available reports:"
Get-ChildItem -Path $strykerOutputDir -Recurse -Filter "mutation-report.html" -File |
    Sort-Object LastWriteTime -Descending |
    ForEach-Object {
        $projectName = $_.Directory.Parent.Parent.Name
        $date = $_.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
        Write-Info "  - $projectName`: $date"
    }

Write-Info ""
