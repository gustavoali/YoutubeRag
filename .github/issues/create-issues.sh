#!/bin/bash
# Script to create GitHub issues for Test Suite Stabilization - Sprint 3
# Run this script from the repository root directory

# Ensure we're in the correct directory
cd "$(dirname "$0")/../.."

echo "Creating GitHub issues for Test Suite Stabilization - Sprint 3"
echo "================================================================"
echo ""

# Check if gh CLI is installed
if ! command -v gh &> /dev/null; then
    echo "ERROR: GitHub CLI (gh) is not installed."
    echo "Please install it from: https://cli.github.com/"
    echo ""
    echo "Alternative: Create issues manually from the markdown files in .github/issues/"
    exit 1
fi

# Check if logged in to GitHub
if ! gh auth status &> /dev/null; then
    echo "ERROR: Not logged in to GitHub CLI."
    echo "Please run: gh auth login"
    exit 1
fi

echo "GitHub CLI is ready. Creating issues..."
echo ""

# Create the meta-issue first
echo "[1/8] Creating meta-issue: Test Suite Stabilization - Sprint 3"
META_ISSUE=$(gh issue create \
    --title "Test Suite Stabilization - Sprint 3 (Meta Issue)" \
    --label "epic,tests,sprint-3" \
    --body-file .github/issues/issue-00-test-suite-stabilization-meta.md \
    2>&1)

if [ $? -eq 0 ]; then
    echo "✅ Meta-issue created: $META_ISSUE"
    META_NUMBER=$(echo "$META_ISSUE" | grep -oP '\d+$')
else
    echo "❌ Failed to create meta-issue"
    META_NUMBER="META"
fi
echo ""

# Create Issue 1: Job Processor Tests
echo "[2/8] Creating issue: Fix Job Processor Integration Tests (13 failures)"
ISSUE_1=$(gh issue create \
    --title "Fix Job Processor Integration Tests (13 failures)" \
    --label "bug,tests,sprint-3" \
    --body-file .github/issues/issue-01-job-processor-tests.md \
    2>&1)

if [ $? -eq 0 ]; then
    echo "✅ Issue created: $ISSUE_1"
    ISSUE_1_NUMBER=$(echo "$ISSUE_1" | grep -oP '\d+$')
else
    echo "❌ Failed to create issue"
    ISSUE_1_NUMBER="1"
fi
echo ""

# Create Issue 2: Multi-Stage Pipeline Tests
echo "[3/8] Creating issue: Fix Multi-Stage Pipeline Integration Tests (17 failures)"
ISSUE_2=$(gh issue create \
    --title "Fix Multi-Stage Pipeline Integration Tests (17 failures)" \
    --label "bug,tests,sprint-3" \
    --body-file .github/issues/issue-02-multistage-pipeline-tests.md \
    2>&1)

if [ $? -eq 0 ]; then
    echo "✅ Issue created: $ISSUE_2"
    ISSUE_2_NUMBER=$(echo "$ISSUE_2" | grep -oP '\d+$')
else
    echo "❌ Failed to create issue"
    ISSUE_2_NUMBER="2"
fi
echo ""

# Create Issue 3: Transcription Job Processor Tests
echo "[4/8] Creating issue: Fix Transcription Job Processor Integration Tests (3 failures)"
ISSUE_3=$(gh issue create \
    --title "Fix Transcription Job Processor Integration Tests (3 failures)" \
    --label "bug,tests,sprint-3" \
    --body-file .github/issues/issue-03-transcription-job-processor-tests.md \
    2>&1)

if [ $? -eq 0 ]; then
    echo "✅ Issue created: $ISSUE_3"
    ISSUE_3_NUMBER=$(echo "$ISSUE_3" | grep -oP '\d+$')
else
    echo "❌ Failed to create issue"
    ISSUE_3_NUMBER="3"
fi
echo ""

# Create Issue 4: Dead Letter Queue Tests
echo "[5/8] Creating issue: Fix Dead Letter Queue Integration Tests (2 failures)"
ISSUE_4=$(gh issue create \
    --title "Fix Dead Letter Queue Integration Tests (2 failures)" \
    --label "bug,tests,sprint-3" \
    --body-file .github/issues/issue-04-dead-letter-queue-tests.md \
    2>&1)

if [ $? -eq 0 ]; then
    echo "✅ Issue created: $ISSUE_4"
    ISSUE_4_NUMBER=$(echo "$ISSUE_4" | grep -oP '\d+$')
else
    echo "❌ Failed to create issue"
    ISSUE_4_NUMBER="4"
fi
echo ""

# Create Issue 5: Metadata Extraction Tests
echo "[6/8] Creating issue: Fix Metadata Extraction Service Integration Tests (5 failures)"
ISSUE_5=$(gh issue create \
    --title "Fix Metadata Extraction Service Integration Tests (5 failures)" \
    --label "bug,tests,sprint-3" \
    --body-file .github/issues/issue-05-metadata-extraction-tests.md \
    2>&1)

if [ $? -eq 0 ]; then
    echo "✅ Issue created: $ISSUE_5"
    ISSUE_5_NUMBER=$(echo "$ISSUE_5" | grep -oP '\d+$')
else
    echo "❌ Failed to create issue"
    ISSUE_5_NUMBER="5"
fi
echo ""

# Create Issue 6: E2E Tests
echo "[7/8] Creating issue: Fix E2E Integration Tests (2 failures)"
ISSUE_6=$(gh issue create \
    --title "Fix E2E Integration Tests (2 failures)" \
    --label "bug,tests,sprint-3,e2e" \
    --body-file .github/issues/issue-06-e2e-tests.md \
    2>&1)

if [ $? -eq 0 ]; then
    echo "✅ Issue created: $ISSUE_6"
    ISSUE_6_NUMBER=$(echo "$ISSUE_6" | grep -oP '\d+$')
else
    echo "❌ Failed to create issue"
    ISSUE_6_NUMBER="6"
fi
echo ""

# Create Issue 7: Miscellaneous Tests
echo "[8/8] Creating issue: Fix Miscellaneous Integration Tests (3 failures)"
ISSUE_7=$(gh issue create \
    --title "Fix Miscellaneous Integration Tests (3 failures)" \
    --label "bug,tests,sprint-3" \
    --body-file .github/issues/issue-07-miscellaneous-tests.md \
    2>&1)

if [ $? -eq 0 ]; then
    echo "✅ Issue created: $ISSUE_7"
    ISSUE_7_NUMBER=$(echo "$ISSUE_7" | grep -oP '\d+$')
else
    echo "❌ Failed to create issue"
    ISSUE_7_NUMBER="7"
fi
echo ""

echo "================================================================"
echo "Issue Creation Complete!"
echo "================================================================"
echo ""
echo "Created Issues:"
echo "  Meta Issue: #$META_NUMBER - Test Suite Stabilization - Sprint 3"
echo "  Issue #$ISSUE_1_NUMBER - Job Processor Tests (13 failures) - High Priority"
echo "  Issue #$ISSUE_2_NUMBER - Multi-Stage Pipeline Tests (17 failures) - High Priority"
echo "  Issue #$ISSUE_6_NUMBER - E2E Tests (2 failures) - High Priority"
echo "  Issue #$ISSUE_3_NUMBER - Transcription Job Processor Tests (3 failures) - Medium Priority"
echo "  Issue #$ISSUE_4_NUMBER - Dead Letter Queue Tests (2 failures) - Medium Priority"
echo "  Issue #$ISSUE_5_NUMBER - Metadata Extraction Tests (5 failures) - Medium Priority"
echo "  Issue #$ISSUE_7_NUMBER - Miscellaneous Tests (3 failures) - Low-Medium Priority"
echo ""
echo "Next Steps:"
echo "  1. Review all created issues"
echo "  2. Update meta-issue with links to individual issues"
echo "  3. Conduct Sprint 3 planning session"
echo "  4. Assign issues to team members"
echo ""
