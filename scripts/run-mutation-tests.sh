#!/bin/bash
################################################################################
# Run Stryker.NET mutation testing on YoutubeRag.NET project
#
# This script runs mutation testing using Stryker.NET on specified projects.
# Mutation testing validates the quality of unit tests by introducing code
# mutations and checking if tests catch them.
#
# Usage:
#   ./run-mutation-tests.sh [project] [threshold] [options]
#
# Arguments:
#   project     - Target project: domain, application, or all (default: domain)
#   threshold   - Mutation score threshold: 50, 60, or 80 (default: 60)
#
# Options:
#   --open      - Automatically open HTML report after completion
#   --diff      - Only mutate changed files (faster, for incremental testing)
#   --concurrency N - Number of concurrent test runners (default: 4)
#
# Examples:
#   ./run-mutation-tests.sh domain
#   ./run-mutation-tests.sh application 80 --open
#   ./run-mutation-tests.sh all --diff
################################################################################

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${CYAN}$1${NC}"
}

log_success() {
    echo -e "${GREEN}$1${NC}"
}

log_warning() {
    echo -e "${YELLOW}$1${NC}"
}

log_error() {
    echo -e "${RED}$1${NC}"
}

# Get script directory and repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$REPO_ROOT"

# Default values
PROJECT="domain"
THRESHOLD=60
OPEN_REPORT=false
DIFF_ONLY=false
CONCURRENCY=4

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        domain|application|all)
            PROJECT="$1"
            shift
            ;;
        50|60|80)
            THRESHOLD=$1
            shift
            ;;
        --open)
            OPEN_REPORT=true
            shift
            ;;
        --diff)
            DIFF_ONLY=true
            shift
            ;;
        --concurrency)
            CONCURRENCY="$2"
            shift 2
            ;;
        -h|--help)
            cat << EOF
Usage: $0 [project] [threshold] [options]

Arguments:
  project       Target project: domain, application, or all (default: domain)
  threshold     Mutation score threshold: 50, 60, or 80 (default: 60)

Options:
  --open        Automatically open HTML report after completion
  --diff        Only mutate changed files (faster)
  --concurrency N  Number of concurrent test runners (default: 4)
  -h, --help    Show this help message

Examples:
  $0 domain
  $0 application 80 --open
  $0 all --diff
EOF
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

log_info "================================================================"
log_info "   Stryker.NET Mutation Testing - YoutubeRag.NET"
log_info "================================================================"
echo ""

# Check if Stryker.NET is installed
log_info "Checking Stryker.NET installation..."
if ! dotnet tool list | grep -q "dotnet-stryker"; then
    log_warning "Stryker.NET not found. Installing from dotnet-tools.json..."
    dotnet tool restore
    if [ $? -ne 0 ]; then
        log_error "Failed to restore .NET tools"
        exit 1
    fi
    log_success "Stryker.NET installed successfully"
else
    log_success "Stryker.NET is already installed"
fi

echo ""

# Thresholds
HIGH_THRESHOLD=80
LOW_THRESHOLD=60
BREAK_THRESHOLD=$THRESHOLD

log_info "Configuration:"
log_info "  - Target Project: $PROJECT"
log_info "  - Break Threshold: ${BREAK_THRESHOLD}%"
log_info "  - Low Threshold: ${LOW_THRESHOLD}%"
log_info "  - High Threshold: ${HIGH_THRESHOLD}%"
log_info "  - Concurrency: $CONCURRENCY"
log_info "  - Diff Only: $DIFF_ONLY"
echo ""

# Function to run Stryker on a specific project
run_stryker() {
    local project_name=$1
    local project_path=$2

    log_info "================================================================"
    log_info "  Running mutation testing on: $project_name"
    log_info "================================================================"
    echo ""

    local output_dir="StrykerOutput/$project_name"

    # Build base command
    local stryker_args=(
        "--project" "$project_path"
        "--test-project" "YoutubeRag.Tests.Unit/YoutubeRag.Tests.Unit.csproj"
        "--reporter" "html"
        "--reporter" "json"
        "--reporter" "cleartext"
        "--reporter" "progress"
        "--output" "$output_dir"
        "--concurrency" "$CONCURRENCY"
        "--mutation-level" "standard"
        "--verbosity" "info"
    )

    # Add diff flag if requested
    if [ "$DIFF_ONLY" = true ]; then
        stryker_args+=("--diff")
        log_info "Running in DIFF mode - only mutating changed files"
    fi

    log_info "Starting Stryker.NET..."
    log_info "This may take several minutes depending on project size..."
    echo ""

    local start_time=$(date +%s)

    # Run Stryker
    dotnet stryker "${stryker_args[@]}"
    local exit_code=$?

    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    local minutes=$((duration / 60))
    local seconds=$((duration % 60))

    echo ""
    log_info "Mutation testing completed in ${minutes}:$(printf "%02d" $seconds)"
    echo ""

    # Check results
    if [ $exit_code -eq 0 ]; then
        log_success "Mutation score meets threshold (>= ${BREAK_THRESHOLD}%)"
        log_success "Report generated at: $output_dir/reports"
    elif [ $exit_code -eq 1 ]; then
        log_warning "Mutation score below threshold (< ${BREAK_THRESHOLD}%)"
        log_warning "Report generated at: $output_dir/reports"
        echo ""
        log_info "Review the HTML report to identify weak tests"
    else
        log_error "Mutation testing failed with exit code: $exit_code"
        return 1
    fi

    # Open report if requested
    if [ "$OPEN_REPORT" = true ]; then
        local report_path="$REPO_ROOT/$output_dir/reports/mutation-report.html"
        if [ -f "$report_path" ]; then
            log_info "Opening mutation report..."
            if command -v xdg-open > /dev/null; then
                xdg-open "$report_path" &
            elif command -v open > /dev/null; then
                open "$report_path"
            else
                log_warning "Could not detect browser command. Please open manually:"
                log_info "  $report_path"
            fi
        fi
    fi

    return 0
}

# Run mutation testing based on project selection
SUCCESS=true

case $PROJECT in
    domain)
        run_stryker "Domain" "YoutubeRag.Domain/YoutubeRag.Domain.csproj" || SUCCESS=false
        ;;
    application)
        run_stryker "Application" "YoutubeRag.Application/YoutubeRag.Application.csproj" || SUCCESS=false
        ;;
    all)
        log_info "Running mutation tests on all projects..."
        echo ""

        run_stryker "Domain" "YoutubeRag.Domain/YoutubeRag.Domain.csproj" || SUCCESS=false
        echo ""

        run_stryker "Application" "YoutubeRag.Application/YoutubeRag.Application.csproj" || SUCCESS=false
        ;;
esac

echo ""
log_info "================================================================"
if [ "$SUCCESS" = true ]; then
    log_success "Mutation Testing Complete!"
else
    log_warning "Mutation Testing Completed with Warnings"
fi
log_info "================================================================"
echo ""
log_info "Next steps:"
log_info "  1. Review HTML report: StrykerOutput/<project>/reports/mutation-report.html"
log_info "  2. Identify surviving mutants"
log_info "  3. Add tests to kill surviving mutants"
log_info "  4. Re-run mutation testing to verify improvements"
echo ""
log_info "Quick commands:"
log_info "  - View report: ./scripts/view-mutation-report.sh $PROJECT"
log_info "  - Run diff only: ./scripts/run-mutation-tests.sh $PROJECT --diff"
echo ""

exit $([ "$SUCCESS" = true ] && echo 0 || echo 1)
