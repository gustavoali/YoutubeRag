# Fix Job Processor Integration Tests (13 failures)

**Labels:** bug, tests, sprint-3

## Overview
13 integration tests related to job processors are currently failing in the CI pipeline. These are pre-existing failures that became visible after fixing the CI/CD infrastructure in Sprint 2.

## Failing Tests

### Audio Extraction Job Processor (3 tests)
1. `AudioExtractionJobProcessor_StoresAudioInfo`
2. `AudioExtractionJobProcessor_SuccessfulExtraction_EnqueuesTranscription`
3. `AudioExtractionJobProcessor_MissingVideoFilePath_Fails`

### Download Job Processor (3 tests)
4. `DownloadJobProcessor_ReportsProgressDuringDownload`
5. `DownloadJobProcessor_FailedDownload_UpdatesJobStatus`
6. `DownloadJobProcessor_SuccessfulDownload_EnqueuesAudioExtraction`

### Segmentation Job Processor (3 tests)
7. `SegmentationJobProcessor_SuccessfulSegmentation_CompletesJob`
8. `SegmentationJobProcessor_ReplacesExistingSegments`
9. `SegmentationJobProcessor_MissingTranscriptionResult_Fails`

### Transcription Stage Job Processor (2 tests)
10. `TranscriptionStageJobProcessor_SuccessfulTranscription_EnqueuesSegmentation`
11. `TranscriptionStageJobProcessor_WhisperNotAvailable_Fails`

### General Job Processor (2 tests)
12. `JobProcessor_NonExistentJob_ThrowsException`
13. `JobProcessor_NonExistentVideo_ThrowsException`

## Common Issues Identified

**Primary Pattern:** Mock service interactions and Hangfire job enqueuing
- Tests expect specific mock behaviors that aren't matching actual implementations
- Hangfire job enqueuing may not be properly mocked or verified
- Service interaction expectations may be incorrect

## Impact

**Priority:** High
**Estimated Effort:** 3-4 hours
**Test Coverage Impact:** 13/425 tests (3.1% of total test suite)

## Acceptance Criteria

- [ ] All 13 job processor tests pass in local environment
- [ ] All 13 job processor tests pass in CI pipeline
- [ ] Mock service interactions properly configured
- [ ] Hangfire job enqueuing properly tested
- [ ] Job state transitions correctly verified
- [ ] No regression in currently passing tests

## Technical Notes

**Investigation Areas:**
- Review Hangfire `IBackgroundJobClient` mocking setup
- Verify service interaction expectations match actual implementations
- Check job state transition logic in processors
- Ensure proper async/await patterns in tests
- Validate error handling paths

**Related Components:**
- `AudioExtractionJobProcessor`
- `DownloadJobProcessor`
- `SegmentationJobProcessor`
- `TranscriptionStageJobProcessor`
- Hangfire background job infrastructure

## Definition of Done

- [ ] All tests passing locally and in CI
- [ ] Code reviewed and approved
- [ ] Root cause analysis documented
- [ ] Test mocking patterns documented for future reference
- [ ] No breaking changes to existing functionality

---

**Context:** Part of Test Suite Stabilization effort for Sprint 3
**Related PR:** #2 (Sprint 2 Integration - CI/CD Fixes)
