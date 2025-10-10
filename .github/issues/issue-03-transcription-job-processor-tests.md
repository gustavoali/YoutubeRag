# Fix Transcription Job Processor Integration Tests (3 failures)

**Labels:** bug, tests, sprint-3

## Overview
3 integration tests related to transcription job processor error handling are currently failing. These tests specifically cover failure scenarios and retry logic.

## Failing Tests

1. `ProcessTranscriptionJobAsync_PermanentFailure_DoesNotRetryIndefinitely`
2. `ProcessTranscriptionJobAsync_TransientFailure_UpdatesJobWithErrorMessage`
3. `ProcessTranscriptionJobAsync_Failure_TransitionsToPendingToRunningToFailed`

## Common Issues Identified

**Primary Pattern:** Error message expectations not matching actual values
- Test assertions expect specific error messages that don't match actual error messages
- Error handling paths may have changed
- Retry logic validation may be incorrect

## Impact

**Priority:** Medium
**Estimated Effort:** 1-2 hours
**Test Coverage Impact:** 3/425 tests (0.7% of total test suite)

## Acceptance Criteria

- [ ] All 3 transcription job processor tests pass in local environment
- [ ] All 3 transcription job processor tests pass in CI pipeline
- [ ] Error messages match expected values
- [ ] Retry logic correctly implemented and tested
- [ ] Job state transitions verified (Pending → Running → Failed)
- [ ] No regression in currently passing tests

## Technical Notes

**Investigation Areas:**
- Compare expected vs. actual error messages in test assertions
- Verify retry policy configuration for transcription jobs
- Check job state transition logic
- Ensure proper exception handling and logging
- Validate that permanent failures don't retry indefinitely

**Related Components:**
- `TranscriptionJobProcessor`
- Job retry policy configuration
- Error message standardization
- Job state transition logic

## Definition of Done

- [ ] All tests passing locally and in CI
- [ ] Code reviewed and approved
- [ ] Error messages standardized and documented
- [ ] Retry logic validated
- [ ] Test assertions updated to match actual behavior
- [ ] No breaking changes to error handling

---

**Context:** Part of Test Suite Stabilization effort for Sprint 3
**Related PR:** #2 (Sprint 2 Integration - CI/CD Fixes)
