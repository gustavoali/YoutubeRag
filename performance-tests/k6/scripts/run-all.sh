#!/bin/bash

# Run All Performance Tests
# Execute complete test suite sequentially

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BASE_URL="${1:-http://localhost:5000}"

echo "================================"
echo "Running Complete k6 Test Suite"
echo "================================"
echo "Target: $BASE_URL"
echo ""

# Array of tests to run
TESTS=(
  "smoke"
  "video-ingestion-load"
  "search-load"
  "stress"
  "spike"
)

FAILED_TESTS=()
PASSED_TESTS=()

for test in "${TESTS[@]}"; do
  echo ""
  echo "→ Running $test..."
  echo ""

  if bash "$SCRIPT_DIR/run-load.sh" "$test" "$BASE_URL"; then
    PASSED_TESTS+=("$test")
    echo "✅ $test PASSED"
  else
    FAILED_TESTS+=("$test")
    echo "❌ $test FAILED"
  fi

  # Wait between tests
  sleep 10
done

echo ""
echo "================================"
echo "Test Suite Complete"
echo "================================"
echo "Passed: ${#PASSED_TESTS[@]}"
echo "Failed: ${#FAILED_TESTS[@]}"
echo ""

if [ ${#FAILED_TESTS[@]} -gt 0 ]; then
  echo "Failed tests:"
  printf '  - %s\n' "${FAILED_TESTS[@]}"
  exit 1
fi

echo "All tests passed! ✅"
