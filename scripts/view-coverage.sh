#!/bin/bash
# Bash script to open the coverage report

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
REPORT_PATH="$ROOT_DIR/TestResults/CoverageReport/index.html"

if [ -f "$REPORT_PATH" ]; then
    echo "Opening coverage report..."

    if command -v xdg-open > /dev/null; then
        xdg-open "$REPORT_PATH"
    elif command -v open > /dev/null; then
        open "$REPORT_PATH"
    else
        echo "Could not open browser automatically."
        echo "Please open: $REPORT_PATH"
    fi
else
    echo "Coverage report not found at: $REPORT_PATH"
    echo "Run './scripts/test-coverage.sh' first to generate the report."
    exit 1
fi
