#!/bin/bash
# Bash script to run tests with coverage and generate reports

set -e

CONFIGURATION="${1:-Debug}"
SKIP_BUILD="${2:-false}"
OPEN_REPORT="${3:-false}"

echo "========================================"
echo "  YoutubeRag.NET Coverage Test Runner  "
echo "========================================"
echo ""

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
COVERAGE_DIR="$ROOT_DIR/TestResults"
REPORT_DIR="$COVERAGE_DIR/CoverageReport"

# Clean previous results
echo "Cleaning previous test results..."
rm -rf "$COVERAGE_DIR"
mkdir -p "$COVERAGE_DIR"

# Run tests with coverage
echo ""
echo "Running tests with coverage collection..."
echo ""

TEST_PROJECTS=(
    "YoutubeRag.Tests.Unit"
    "YoutubeRag.Tests.Integration"
)

for project in "${TEST_PROJECTS[@]}"; do
    PROJECT_PATH="$ROOT_DIR/$project"
    if [ -d "$PROJECT_PATH" ]; then
        echo "Testing: $project"

        BUILD_ARG=""
        if [ "$SKIP_BUILD" = "true" ]; then
            BUILD_ARG="--no-build"
        fi

        dotnet test "$PROJECT_PATH" \
            --configuration "$CONFIGURATION" \
            $BUILD_ARG \
            --settings "$ROOT_DIR/.runsettings" \
            --collect:"XPlat Code Coverage" \
            --results-directory "$COVERAGE_DIR" \
            --logger "console;verbosity=normal"

        echo ""
    else
        echo "Warning: Project not found: $project"
    fi
done

# Find all coverage files
echo "Collecting coverage files..."
COVERAGE_FILES=$(find "$COVERAGE_DIR" -name "coverage.cobertura.xml" -type f | tr '\n' ';' | sed 's/;$//')

if [ -z "$COVERAGE_FILES" ]; then
    echo "No coverage files found!"
    exit 1
fi

echo "Found coverage files"

# Generate coverage report
echo ""
echo "Generating coverage report..."

reportgenerator \
    "-reports:$COVERAGE_FILES" \
    "-targetdir:$REPORT_DIR" \
    "-reporttypes:Html;JsonSummary;Badges;Cobertura;MarkdownSummaryGithub" \
    "-verbosity:Info" \
    "-title:YoutubeRag.NET Coverage Report" \
    "-assemblyfilters:+YoutubeRag.Domain;+YoutubeRag.Application;+YoutubeRag.Infrastructure;+YoutubeRag.Api"

# Display coverage summary
echo ""
echo "========================================"
echo "  Coverage Summary"
echo "========================================"
echo ""

SUMMARY_FILE="$REPORT_DIR/Summary.json"
if [ -f "$SUMMARY_FILE" ]; then
    LINE_COVERAGE=$(cat "$SUMMARY_FILE" | grep -o '"linecoverage":[0-9.]*' | cut -d':' -f2)
    BRANCH_COVERAGE=$(cat "$SUMMARY_FILE" | grep -o '"branchcoverage":[0-9.]*' | cut -d':' -f2)
    METHOD_COVERAGE=$(cat "$SUMMARY_FILE" | grep -o '"methodcoverage":[0-9.]*' | cut -d':' -f2)

    echo "Overall Coverage:"
    echo "  Line Coverage:   $LINE_COVERAGE%"
    echo "  Branch Coverage: $BRANCH_COVERAGE%"
    echo "  Method Coverage: $METHOD_COVERAGE%"
    echo ""
fi

echo "Coverage report generated at:"
echo "  $REPORT_DIR"
echo ""
echo "HTML Report: $REPORT_DIR/index.html"
echo ""

# Open report if requested
if [ "$OPEN_REPORT" = "true" ]; then
    INDEX_PATH="$REPORT_DIR/index.html"
    if [ -f "$INDEX_PATH" ]; then
        echo "Opening coverage report in browser..."
        if command -v xdg-open > /dev/null; then
            xdg-open "$INDEX_PATH"
        elif command -v open > /dev/null; then
            open "$INDEX_PATH"
        else
            echo "Could not open browser automatically. Please open: $INDEX_PATH"
        fi
    fi
fi

echo "Done!"
