#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run Stryker.NET mutation testing on YoutubeRag.NET project
.DESCRIPTION
    This script runs mutation testing using Stryker.NET on specified projects.
    Mutation testing validates the quality of unit tests by introducing code mutations
    and checking if tests catch them.
.PARAMETER Project
    Target project: Domain, Application, or All (default: Domain)
.PARAMETER Threshold
    Mutation score threshold: 50, 60, or 80 (default: 60)
.PARAMETER Open
    Automatically open HTML report after completion
.PARAMETER DiffOnly
    Only mutate changed files (faster, for incremental testing)
.PARAMETER Concurrency
    Number of concurrent test runners (default: 4)
.EXAMPLE
    .\run-mutation-tests.ps1 -Project Domain
    Run mutation testing on Domain layer only
.EXAMPLE
    .\run-mutation-tests.ps1 -Project Application -Threshold 80 -Open
    Run on Application layer with 80% threshold and open report
.EXAMPLE
    .\run-mutation-tests.ps1 -Project All -DiffOnly
    Run on both Domain and Application, only changed files
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Domain", "Application", "All")]
    [string]$Project = "Domain",

    [Parameter(Mandatory=$false)]
    [ValidateSet(50, 60, 80)]
    [int]$Threshold = 60,

    [Parameter(Mandatory=$false)]
    [switch]$Open,

    [Parameter(Mandatory=$false)]
    [switch]$DiffOnly,

    [Parameter(Mandatory=$false)]
    [int]$Concurrency = 4
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
Write-Info "   Stryker.NET Mutation Testing - YoutubeRag.NET"
Write-Info "================================================================"
Write-Info ""

# Check if Stryker.NET is installed
Write-Info "Checking Stryker.NET installation..."
$strykerInstalled = dotnet tool list | Select-String "dotnet-stryker"

if (-not $strykerInstalled) {
    Write-Warning "Stryker.NET not found. Installing from dotnet-tools.json..."
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Failed to restore .NET tools"
        exit 1
    }
    Write-Success "Stryker.NET installed successfully"
} else {
    Write-Success "Stryker.NET is already installed"
}

Write-Info ""

# Set thresholds
$highThreshold = 80
$lowThreshold = 60
$breakThreshold = $Threshold

Write-Info "Configuration:"
Write-Info "  - Target Project: $Project"
Write-Info "  - Break Threshold: ${breakThreshold}%"
Write-Info "  - Low Threshold: ${lowThreshold}%"
Write-Info "  - High Threshold: ${highThreshold}%"
Write-Info "  - Concurrency: $Concurrency"
Write-Info "  - Diff Only: $DiffOnly"
Write-Info ""

# Function to run Stryker on a specific project
function Run-Stryker {
    param(
        [string]$ProjectName,
        [string]$ProjectPath
    )

    Write-Info "================================================================"
    Write-Info "  Running mutation testing on: $ProjectName"
    Write-Info "================================================================"
    Write-Info ""

    $outputDir = "StrykerOutput/$ProjectName"

    # Build base command
    $strykerArgs = @(
        "--project", "$ProjectPath",
        "--test-project", "YoutubeRag.Tests.Unit/YoutubeRag.Tests.Unit.csproj",
        "--reporter", "html",
        "--reporter", "json",
        "--reporter", "cleartext",
        "--reporter", "progress",
        "--output", $outputDir,
        "--concurrency", $Concurrency,
        "--mutation-level", "standard",
        "--verbosity", "info"
    )

    # Add diff flag if requested
    if ($DiffOnly) {
        $strykerArgs += "--diff"
        Write-Info "Running in DIFF mode - only mutating changed files"
    }

    Write-Info "Starting Stryker.NET..."
    Write-Info "This may take several minutes depending on project size..."
    Write-Info ""

    $startTime = Get-Date

    # Run Stryker
    & dotnet stryker $strykerArgs

    $exitCode = $LASTEXITCODE
    $endTime = Get-Date
    $duration = $endTime - $startTime

    Write-Info ""
    Write-Info "Mutation testing completed in $($duration.ToString('mm\:ss'))"
    Write-Info ""

    # Check results
    if ($exitCode -eq 0) {
        Write-Success "Mutation score meets threshold (>= ${breakThreshold}%)"
        Write-Success "Report generated at: $outputDir/reports"
    } elseif ($exitCode -eq 1) {
        Write-Warning "Mutation score below threshold (< ${breakThreshold}%)"
        Write-Warning "Report generated at: $outputDir/reports"
        Write-Info ""
        Write-Info "Review the HTML report to identify weak tests"
    } else {
        Write-Failure "Mutation testing failed with exit code: $exitCode"
        return $false
    }

    # Open report if requested
    if ($Open) {
        $reportPath = Join-Path $RepoRoot "$outputDir/reports/mutation-report.html"
        if (Test-Path $reportPath) {
            Write-Info "Opening mutation report..."
            Start-Process $reportPath
        }
    }

    return $true
}

# Run mutation testing based on project selection
$success = $true

switch ($Project) {
    "Domain" {
        $success = Run-Stryker -ProjectName "Domain" -ProjectPath "YoutubeRag.Domain/YoutubeRag.Domain.csproj"
    }
    "Application" {
        $success = Run-Stryker -ProjectName "Application" -ProjectPath "YoutubeRag.Application/YoutubeRag.Application.csproj"
    }
    "All" {
        Write-Info "Running mutation tests on all projects..."
        Write-Info ""

        $domainSuccess = Run-Stryker -ProjectName "Domain" -ProjectPath "YoutubeRag.Domain/YoutubeRag.Domain.csproj"
        Write-Info ""

        $appSuccess = Run-Stryker -ProjectName "Application" -ProjectPath "YoutubeRag.Application/YoutubeRag.Application.csproj"

        $success = $domainSuccess -and $appSuccess
    }
}

Write-Info ""
Write-Info "================================================================"
if ($success) {
    Write-Success "Mutation Testing Complete!"
} else {
    Write-Warning "Mutation Testing Completed with Warnings"
}
Write-Info "================================================================"
Write-Info ""
Write-Info "Next steps:"
Write-Info "  1. Review HTML report: StrykerOutput/<project>/reports/mutation-report.html"
Write-Info "  2. Identify surviving mutants"
Write-Info "  3. Add tests to kill surviving mutants"
Write-Info "  4. Re-run mutation testing to verify improvements"
Write-Info ""
Write-Info "Quick commands:"
Write-Info "  - View report: .\scripts\view-mutation-report.ps1 -Project $Project"
Write-Info "  - Run diff only: .\scripts\run-mutation-tests.ps1 -Project $Project -DiffOnly"
Write-Info ""

exit $(if ($success) { 0 } else { 1 })
