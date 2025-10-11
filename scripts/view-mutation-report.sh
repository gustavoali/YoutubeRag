#!/bin/bash
################################################################################
# Open the Stryker.NET mutation testing HTML report
#
# This script locates and opens the most recent Stryker.NET mutation report
# for the specified project in the default web browser.
#
# Usage:
#   ./view-mutation-report.sh [project]
#
# Arguments:
#   project - Target project: domain, application, or latest (default: latest)
#
# Examples:
#   ./view-mutation-report.sh
#   ./view-mutation-report.sh domain
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

PROJECT="${1:-latest}"

log_info "================================================================"
log_info "   View Stryker.NET Mutation Report"
log_info "================================================================"
echo ""

STRYKER_OUTPUT_DIR="$REPO_ROOT/StrykerOutput"

# Check if StrykerOutput directory exists
if [ ! -d "$STRYKER_OUTPUT_DIR" ]; then
    log_error "StrykerOutput directory not found!"
    log_info "Please run mutation tests first:"
    log_info "  ./scripts/run-mutation-tests.sh"
    exit 1
fi

# Find report based on project selection
REPORT_PATH=""

if [ "$PROJECT" == "latest" ]; then
    log_info "Looking for the most recent mutation report..."

    # Find the most recent mutation report
    REPORT_PATH=$(find "$STRYKER_OUTPUT_DIR" -name "mutation-report.html" -type f -printf '%T@ %p\n' |
                  sort -rn | head -1 | cut -d' ' -f2-)

    if [ -z "$REPORT_PATH" ]; then
        log_error "No mutation reports found!"
        log_info "Please run mutation tests first:"
        log_info "  ./scripts/run-mutation-tests.sh"
        exit 1
    fi

    PROJECT_NAME=$(basename $(dirname $(dirname "$REPORT_PATH")))
    REPORT_DATE=$(stat -c %y "$REPORT_PATH" 2>/dev/null || stat -f "%Sm" "$REPORT_PATH" 2>/dev/null)

    log_info "Found report for project: $PROJECT_NAME"
    log_info "Report date: $REPORT_DATE"
else
    log_info "Looking for $PROJECT mutation report..."

    # Convert to proper case
    case $PROJECT in
        domain)
            PROJECT_PROPER="Domain"
            ;;
        application)
            PROJECT_PROPER="Application"
            ;;
        *)
            log_error "Unknown project: $PROJECT"
            log_info "Valid options: domain, application, latest"
            exit 1
            ;;
    esac

    REPORT_PATH="$STRYKER_OUTPUT_DIR/$PROJECT_PROPER/reports/mutation-report.html"

    if [ ! -f "$REPORT_PATH" ]; then
        log_error "Mutation report not found for project: $PROJECT_PROPER"
        log_info "Expected location: $REPORT_PATH"
        echo ""
        log_info "Please run mutation tests for this project:"
        log_info "  ./scripts/run-mutation-tests.sh $PROJECT"
        exit 1
    fi
fi

echo ""
log_success "Opening mutation report..."
log_info "Location: $REPORT_PATH"
echo ""

# Open the report with the default browser
if command -v xdg-open > /dev/null; then
    xdg-open "$REPORT_PATH" &
elif command -v open > /dev/null; then
    open "$REPORT_PATH"
elif command -v start > /dev/null; then
    start "$REPORT_PATH"
else
    log_warning "Could not detect browser command."
    log_info "Please open the report manually:"
    log_info "  $REPORT_PATH"
fi

log_info "================================================================"
log_info "Report opened in your default web browser"
log_info "================================================================"
echo ""
log_info "Understanding the report:"
log_info "  - Green: Mutants killed by tests (good)"
log_info "  - Red: Mutants survived (need more tests)"
log_info "  - Yellow: Mutants timeout (tests too slow)"
log_info "  - Gray: Mutants not covered by tests"
echo ""
log_info "Mutation Score = (Killed / Total) * 100"
echo ""
log_info "Target thresholds:"
log_info "  - High: >= 80% (excellent test coverage)"
log_info "  - Low: >= 60% (acceptable test coverage)"
log_info "  - Break: >= 50% (minimum acceptable)"
echo ""

# Show available reports
log_info "Available reports:"
find "$STRYKER_OUTPUT_DIR" -name "mutation-report.html" -type f -printf '%T@ %p\n' 2>/dev/null |
    sort -rn |
    while read -r timestamp path; do
        project_name=$(basename $(dirname $(dirname "$path")))
        report_date=$(date -d "@${timestamp%.*}" '+%Y-%m-%d %H:%M:%S' 2>/dev/null ||
                      date -r "${timestamp%.*}" '+%Y-%m-%d %H:%M:%S' 2>/dev/null)
        log_info "  - $project_name: $report_date"
    done 2>/dev/null || true

echo ""
