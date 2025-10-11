#!/bin/bash

# Smoke Test Runner
# Quick smoke test to validate all critical endpoints

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K6_DIR="$(dirname "$SCRIPT_DIR")"
TEST_DIR="$K6_DIR/tests"
REPORTS_DIR="$K6_DIR/reports"

# Create reports directory if it doesn't exist
mkdir -p "$REPORTS_DIR"

# Default values
BASE_URL="${BASE_URL:-http://localhost:5000}"
EXPORT_HTML="${EXPORT_HTML:-$REPORTS_DIR/smoke-test-$(date +%Y%m%d-%H%M%S).html}"
EXPORT_JSON="${EXPORT_JSON:-$REPORTS_DIR/smoke-test-$(date +%Y%m%d-%H%M%S).json}"

echo "================================"
echo "Running k6 Smoke Test"
echo "================================"
echo "Target: $BASE_URL"
echo "Reports: $REPORTS_DIR"
echo ""

# Run the test
k6 run \
  --out json="$EXPORT_JSON" \
  -e BASE_URL="$BASE_URL" \
  -e EXPORT_HTML="$EXPORT_HTML" \
  -e EXPORT_JSON="$EXPORT_JSON" \
  "$TEST_DIR/smoke.js"

echo ""
echo "================================"
echo "Test complete!"
echo "HTML Report: $EXPORT_HTML"
echo "JSON Report: $EXPORT_JSON"
echo "================================"
