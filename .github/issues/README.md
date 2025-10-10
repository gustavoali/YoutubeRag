# Sprint 3 Test Suite Stabilization - GitHub Issues

This directory contains ready-to-use GitHub issue templates for tracking the test suite stabilization work in Sprint 3.

## Quick Start

### Automated Creation (Recommended)

```bash
# Navigate to repository root
cd C:\agents\youtube_rag_net

# Run the automated script
./.github/issues/create-issues.sh
```

This will create all 8 issues (1 meta + 7 categories) automatically.

### Manual Creation

If `gh` CLI is not available, you can create issues manually:

1. Go to: https://github.com/gustavoali/YoutubeRag/issues/new
2. Open each `.md` file in this directory
3. Copy title and content
4. Add appropriate labels
5. Create the issue

## Issue Files

| File | Title | Tests | Priority | Effort |
|------|-------|-------|----------|--------|
| `issue-00-test-suite-stabilization-meta.md` | Test Suite Stabilization - Sprint 3 | 45 | Epic | 15-20h |
| `issue-01-job-processor-tests.md` | Fix Job Processor Tests | 13 | High | 3-4h |
| `issue-02-multistage-pipeline-tests.md` | Fix Multi-Stage Pipeline Tests | 17 | High | 4-5h |
| `issue-06-e2e-tests.md` | Fix E2E Tests | 2 | High | 2-3h |
| `issue-03-transcription-job-processor-tests.md` | Fix Transcription Job Processor Tests | 3 | Medium | 1-2h |
| `issue-04-dead-letter-queue-tests.md` | Fix Dead Letter Queue Tests | 2 | Medium | 1-2h |
| `issue-05-metadata-extraction-tests.md` | Fix Metadata Extraction Tests | 5 | Medium | 2-3h |
| `issue-07-miscellaneous-tests.md` | Fix Miscellaneous Tests | 3 | Low-Med | 1.5-3h |

## Labels to Use

- `epic` - For the meta-issue only
- `bug` - All test failure issues
- `tests` - All test-related issues
- `sprint-3` - All Sprint 3 work
- `e2e` - For E2E test issue

## Prerequisites

### Install GitHub CLI

**Windows (using winget):**
```powershell
winget install GitHub.cli
```

**Or download from:** https://cli.github.com/

### Authenticate
```bash
gh auth login
```

## Troubleshooting

### Script Fails: "gh: command not found"
- Install GitHub CLI (see above)
- Restart your terminal
- Verify: `gh --version`

### Script Fails: "Not logged in to GitHub CLI"
- Run: `gh auth login`
- Follow the prompts

### Permission Denied
- Run: `chmod +x .github/issues/create-issues.sh`
- Try again

## After Creating Issues

1. **Update Meta-Issue**
   - Edit the meta-issue to add links to the created issues
   - Replace placeholder issue numbers

2. **Sprint Planning**
   - Review all issues with the team
   - Assign based on expertise
   - Set Sprint 3 goals

3. **Begin Work**
   - Start with High priority issues
   - Create feature branches
   - Submit PRs as tests are fixed

## Reference Documents

- `SPRINT3_TEST_ISSUES_SUMMARY.md` - Complete overview and planning guide
- `FINAL_PR_STATUS_REPORT.md` - Sprint 2 results and test analysis
- `GITHUB_CI_LESSONS_LEARNED.md` - CI/CD troubleshooting guide

---

**Need Help?** See `SPRINT3_TEST_ISSUES_SUMMARY.md` for detailed planning and recommendations.
