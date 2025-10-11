#!/bin/bash

# Load Test Runner
# Run specific load test with parameters

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K6_DIR="$(dirname "$SCRIPT_DIR")"
TEST_DIR="$K6_DIR/tests"
REPORTS_DIR="$K6_DIR/reports"

# Create reports directory
mkdir -p "$REPORTS_DIR"

# Parse arguments
TEST_NAME="${1:-video-ingestion-load}"
BASE_URL="${2:-http://localhost:5000}"

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
EXPORT_HTML="$REPORTS_DIR/${TEST_NAME}-${TIMESTAMP}.html"
EXPORT_JSON="$REPORTS_DIR/${TEST_NAME}-${TIMESTAMP}.json"

echo "================================"
echo "Running k6 Load Test: $TEST_NAME"
echo "================================"
echo "Target: $BASE_URL"
echo "Timestamp: $TIMESTAMP"
echo ""

# Check if test file exists
if [ ! -f "$TEST_DIR/${TEST_NAME}.js" ]; then
  echo "Error: Test file not found: $TEST_DIR/${TEST_NAME}.js"
  echo ""
  echo "Available tests:"
  ls -1 "$TEST_DIR"/*.js | xargs -n 1 basename
  exit 1
fi

# Run the test
k6 run \
  --out json="$EXPORT_JSON" \
  -e BASE_URL="$BASE_URL" \
  -e EXPORT_HTML="$EXPORT_HTML" \
  -e EXPORT_JSON="$EXPORT_JSON" \
  -e ENVIRONMENT="${ENVIRONMENT:-local}" \
  "$TEST_DIR/${TEST_NAME}.js"

echo ""
echo "================================"
echo "Test complete!"
echo "HTML Report: $EXPORT_HTML"
echo "JSON Report: $EXPORT_JSON"
echo "================================"
