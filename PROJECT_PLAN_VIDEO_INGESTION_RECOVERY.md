# Video Ingestion Pipeline Recovery - Project Plan
**Project Name**: YoutubeRag .NET - Video Ingestion Pipeline Bug Fixes
**Project Manager**: Senior Project Manager
**Document Version**: 1.0
**Created**: October 6, 2025
**Status**: APPROVED FOR EXECUTION
**Priority**: CRITICAL

---

## Executive Summary

### Project Overview
The Video Ingestion Pipeline for YoutubeRag .NET is currently blocked with a 0% success rate. This recovery project aims to fix 6 critical bugs identified during E2E testing and restore the pipeline to full operational status within 3-5 calendar days.

### Business Impact
- **Current State**: 0% video processing success rate - system is completely blocked for production
- **Risk Level**: CRITICAL - no videos can be processed, core functionality unusable
- **Business Value**: Restoring core product functionality to enable MVP delivery

### Project Goals
1. Fix all critical bugs blocking video ingestion (FK constraint, error handling)
2. Implement proper metadata fallback execution
3. Unify monitoring and progress reporting system
4. Improve testing validation to prevent false positives
5. Achieve 80%+ E2E success rate (4/5 videos)

### Key Success Metrics
- **Functional**: 80%+ E2E success rate (excluding private/unavailable videos)
- **Quality**: All critical bugs resolved, proper error handling implemented
- **Performance**: Pipeline processes videos successfully within expected timeframes
- **Monitoring**: Real-time progress accurately reflects actual job status

### Timeline Summary
- **Total Duration**: 3-5 calendar days (recommended: 4 days with buffer)
- **Total Effort**: 14-18 hours of development + testing
- **Team Size**: 2 resources (Backend Developer + Test Engineer)
- **Start Date**: October 7, 2025 (recommended)
- **Target Completion**: October 11, 2025

---

## Project Context & Background

### Technology Stack (Per Project Standards)
- **Framework**: .NET 8.0, ASP.NET Core
- **Architecture**: Clean Architecture (Domain, Application, Infrastructure, API layers)
- **Database**: MySQL 8.0+ with EF Core
- **Background Jobs**: Hangfire with 3 workers (critical, default, low queues)
- **Authentication**: Mock authentication in Local/Development environments
- **External Services**: YoutubeExplode, yt-dlp (fallback), Whisper (transcription)

### What Works (Confirmed Operational)
1. Infrastructure base: API running on ports 62787/62788
2. MySQL database connectivity
3. Hangfire server with 3 active workers processing jobs
4. Mock authentication system
5. URL validation and YouTube ID extraction
6. All service implementations (audio, transcription, embedding)
7. Clean Architecture structure properly configured

### What's Broken (0% Success Rate)
1. Foreign Key constraint failure - test user doesn't exist in DB
2. Misleading API error responses (200 OK on failures)
3. Metadata fallback to yt-dlp never executes
4. Progress monitoring disconnected from real Hangfire jobs
5. E2E tests validate HTTP codes, not actual DB inserts
6. Ambiguous terminology in reports

---

## Detailed Problem Analysis

### Problem #1: Foreign Key Constraint Failure (CRITICAL)
**Status**: Fix implemented, requires testing
**Impact**: 100% of video ingestion attempts fail at database insert
**Root Cause**: Mock authentication creates userId "test-user-id" which doesn't exist in Users table

**Fix Applied**:
- Modified `VideoIngestionService.cs` to auto-create test users in Local/Development environments
- Validates user existence before creating video record
- Maintains FK constraint for production security

**Testing Required**:
- Verify test user is created automatically
- Confirm video inserts succeed
- Validate jobs are created and enqueued

---

### Problem #2: Poor Error Handling in API (CRITICAL)
**Status**: Not fixed, requires implementation
**Impact**: Misleading responses, difficult debugging, false positive monitoring
**Root Cause**: `VideosController.IngestVideo()` returns 200 OK even when DB save fails

**Required Fix**:
```csharp
// VideosController.cs - Endpoint POST /api/v1/videos/ingest
[HttpPost("ingest")]
public async Task<ActionResult<VideoIngestionResponse>> IngestVideo([FromBody] VideoIngestionRequestDto request)
{
    try
    {
        var response = await _videoIngestionService.IngestVideoFromUrlAsync(request, cancellationToken);
        return Ok(response);
    }
    catch (ValidationException ex)
    {
        return BadRequest(new { error = ex.Message, errors = ex.Errors });
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Database error during video ingestion: {Message}", ex.Message);
        return StatusCode(500, new { error = "Failed to save video to database", details = ex.InnerException?.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during video ingestion: {Message}", ex.Message);
        return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
    }
}
```

---

### Problem #3: Metadata Fallback Not Executing (HIGH)
**Status**: Not fixed, requires implementation
**Impact**: Videos with 403 Forbidden fail completely instead of using yt-dlp fallback
**Root Cause**: Exception re-thrown as `InvalidOperationException` prevents fallback catch block from executing

**Required Fix**:
```csharp
// MetadataExtractionService.cs (lines 78-84)
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
{
    _logger.LogWarning("YouTube API returned 403 Forbidden, attempting yt-dlp fallback for {VideoUrl}", youtubeUrl);

    // Don't re-throw! Execute fallback directly here
    return await ExtractMetadataWithYtDlpAsync(youtubeUrl, cancellationToken);
}
```

---

### Problem #4: Progress Monitoring Disconnected (HIGH)
**Status**: Not fixed, requires implementation
**Impact**: Users cannot monitor real job progress, SignalR notifications potentially useless
**Root Cause**: Progress endpoint uses `VideoProcessingService` (mock data) instead of real Hangfire job status

**Required Fix**:
```csharp
// VideosController.cs - GET /api/v1/videos/{videoId}/progress
[HttpGet("{videoId}/progress")]
public async Task<ActionResult> GetVideoProgress(string videoId)
{
    var video = await _videoRepository.GetByIdAsync(videoId);
    if (video == null) return NotFound();

    var job = await _jobRepository.GetByVideoIdAsync(videoId, JobType.Transcription);

    return Ok(new {
        video_id = videoId,
        job_id = job?.Id,
        status = video.ProcessingStatus,
        transcription_status = video.TranscriptionStatus,
        embedding_status = video.EmbeddingStatus,
        progress = video.ProcessingProgress,
        hangfire_job_id = job?.HangfireJobId,
        current_stage = DetermineCurrentStage(video),
        job_status = job?.Status
    });
}
```

---

### Problem #5: Misleading Test Reports (MEDIUM)
**Status**: Not fixed, requires implementation
**Impact**: False confidence in functionality, wasted debugging time
**Root Cause**: E2E tests only check HTTP 200/201 status codes, not actual DB state

**Required Fix**:
```powershell
# run_e2e_tests.ps1 - Add DB validation step
foreach ($video in $testVideos) {
    $response = Invoke-RestMethod -Method POST -Uri "$apiUrl/api/v1/videos/ingest" -Body $body

    # NEW: Verify video actually exists in database
    Start-Sleep -Seconds 2
    try {
        $dbCheck = Invoke-RestMethod -Method GET -Uri "$apiUrl/api/v1/videos/$($response.videoId)"
        $videosInDb++
        Write-Host "✓ Video confirmed in database: $($dbCheck.title)" -ForegroundColor Green
    } catch {
        Write-Host "✗ Video NOT found in database despite 200 OK response!" -ForegroundColor Red
    }
}
```

---

### Problem #6: Hangfire Workers Confusion (RESOLVED)
**Status**: Resolved - no action needed
**Finding**: Workers are functioning correctly; initial diagnosis was incorrect
**Lesson**: Always verify complete logs before diagnosing issues

---

## Project Schedule

### Gantt Chart Breakdown (4-Day Timeline)

```
Day 1 (Oct 7) - Phase 1: Critical Fix Validation
├─ 08:00-09:00 │ Project kickoff, environment setup
├─ 09:00-10:00 │ Compile project with FK fix
├─ 10:00-11:00 │ Manual test with 1 short video
├─ 11:00-12:00 │ DB validation, Hangfire monitoring
├─ 12:00-13:00 │ LUNCH BREAK
├─ 13:00-14:00 │ Document findings, prepare Phase 2
└─ 14:00-15:00 │ Phase 1 Go/No-Go decision checkpoint

Day 2 (Oct 8) - Phase 2A: Error Handling Fixes
├─ 09:00-11:00 │ Implement proper error handling in VideosController
├─ 11:00-12:00 │ Implement metadata fallback fix
├─ 12:00-13:00 │ LUNCH BREAK
├─ 13:00-15:00 │ Unit tests for error scenarios
├─ 15:00-16:00 │ Code review session
└─ 16:00-17:00 │ Integration test with 3 videos (happy, 403, private)

Day 3 (Oct 9) - Phase 2B: Monitoring & Testing
├─ 09:00-11:00 │ Fix progress endpoint to use real jobs
├─ 11:00-12:00 │ Update E2E tests with DB validation
├─ 12:00-13:00 │ LUNCH BREAK
├─ 13:00-15:00 │ Standardize terminology across codebase
├─ 15:00-16:00 │ Update test reports with DB metrics
└─ 16:00-17:00 │ Phase 2 Go/No-Go decision checkpoint

Day 4 (Oct 10) - Phase 3: Complete Testing
├─ 09:00-10:00 │ Re-run complete E2E suite (5 videos)
├─ 10:00-11:00 │ Validate all fallback scenarios
├─ 11:00-12:00 │ Generate final test report
├─ 12:00-13:00 │ LUNCH BREAK
├─ 13:00-14:00 │ Stakeholder demo
├─ 14:00-15:00 │ Sign-off meeting
└─ 15:00-16:00 │ Documentation update, project closure

Day 5 (Buffer) - Contingency for unexpected issues
```

---

## Phase Details & Milestones

### Phase 1: Critical Fix Validation (4 hours)
**Duration**: 4 hours (Day 1)
**Owner**: Backend Developer
**Dependencies**: None

**Tasks**:
1. Compile project with FK constraint fix
2. Run database migrations if needed
3. Start API and Hangfire server
4. Manual test: Ingest 1 short YouTube video (2-3 min)
5. Validate in database: `SELECT * FROM Videos WHERE YouTubeId = ?`
6. Validate in database: `SELECT * FROM Jobs WHERE VideoId = ?`
7. Monitor Hangfire dashboard for job execution
8. Verify job completes successfully or identify blockers

**Success Criteria**:
- ✓ Video record created in database
- ✓ Job record created with HangfireJobId
- ✓ Hangfire job executes successfully
- ✓ Video ProcessingStatus transitions to Completed
- ✓ No FK constraint errors in logs

**Deliverables**:
- Phase 1 validation report
- Database query results (screenshots)
- Hangfire dashboard screenshots
- Go/No-Go decision for Phase 2

**Risks**:
- Fix may not work as expected (Probability: Low, Impact: High)
- **Mitigation**: Have rollback plan, alternative fix prepared

**Go/No-Go Decision Point**:
- **GO**: If 1 video processes end-to-end successfully
- **NO-GO**: If FK constraint still fails → escalate, investigate alternative solutions

---

### Phase 2A: Critical Bug Fixes (6 hours)
**Duration**: 6 hours (Day 2)
**Owner**: Backend Developer
**Dependencies**: Phase 1 must pass

**Tasks**:
1. **Error Handling in VideosController** (2 hours)
   - Add try-catch blocks in `IngestVideo()` action
   - Return proper HTTP status codes (400, 409, 500)
   - Include descriptive error messages in response
   - Add structured logging for all error paths
   - Test manually with invalid URLs, duplicate videos

2. **Metadata Fallback Execution** (2 hours)
   - Modify catch block in `MetadataExtractionService.cs`
   - Remove re-throw of `InvalidOperationException`
   - Execute yt-dlp fallback directly in catch block
   - Add logging for fallback execution
   - Test with video known to return 403

3. **Unit Tests for Error Scenarios** (1 hour)
   - Test invalid URL validation
   - Test duplicate video detection
   - Test database save failures
   - Test metadata extraction failures

4. **Code Review** (1 hour)
   - Review all changes with Test Engineer
   - Verify error handling patterns are consistent
   - Check logging is comprehensive
   - Validate tests cover edge cases

**Success Criteria**:
- ✓ Errors return appropriate HTTP status codes (not 200 OK)
- ✓ Error messages are clear and actionable
- ✓ Metadata fallback executes when primary method fails with 403
- ✓ 2/3 integration test videos process successfully (exclude private)
- ✓ All unit tests pass

**Deliverables**:
- Updated `VideosController.cs`
- Updated `MetadataExtractionService.cs`
- New unit test suite for error handling
- Code review sign-off document

**Risks**:
- yt-dlp fallback may also fail (Probability: Medium, Impact: Medium)
- **Mitigation**: Implement graceful degradation, allow video without full metadata

---

### Phase 2B: Monitoring & Testing Improvements (4 hours)
**Duration**: 4 hours (Day 3)
**Owner**: Backend Developer (2h) + Test Engineer (2h)

**Tasks**:
1. **Unify Progress Endpoint** (2 hours - Backend Dev)
   - Refactor `/videos/{videoId}/progress` endpoint
   - Query real Job and Video entities from database
   - Remove dependency on `VideoProcessingService` mock
   - Return Hangfire job status
   - Test endpoint returns accurate real-time data

2. **Improve E2E Test Validation** (2 hours - Test Engineer)
   - Add database query validation to `run_e2e_tests.ps1`
   - Query Videos table after ingestion
   - Query Jobs table for job creation
   - Add metrics: videos_in_db, jobs_in_db
   - Update report format with clear terminology

3. **Standardize Terminology** (included in above tasks)
   - Use consistent terms: "ingested", "created", "processed"
   - Update API responses, logs, reports
   - Document terminology in API_USAGE_GUIDE.md

**Success Criteria**:
- ✓ Progress endpoint returns data from database, not mock service
- ✓ E2E tests validate actual database state, not just HTTP codes
- ✓ Reports clearly distinguish HTTP success from DB persistence
- ✓ Terminology is consistent across all documentation

**Deliverables**:
- Updated `VideosController.cs` (progress endpoint)
- Updated `run_e2e_tests.ps1` script
- Updated test report templates
- Terminology standardization document

**Risks**:
- Database queries may slow down tests (Probability: Low, Impact: Low)
- **Mitigation**: Add small delays (1-2 sec) for async operations to complete

---

### Phase 3: Complete E2E Testing (2 hours)
**Duration**: 2 hours (Day 4 morning)
**Owner**: Test Engineer
**Dependencies**: Phase 2A and 2B must pass

**Tasks**:
1. **Re-run Complete E2E Suite** (1 hour)
   - Execute `run_e2e_tests.ps1` with all 5 test videos
   - Monitor Hangfire dashboard during execution
   - Capture all logs (API, Hangfire, database)
   - Validate database state after all tests

2. **Validate Fallback Scenarios** (30 minutes)
   - Test video with 403 Forbidden response
   - Test private/unavailable video
   - Verify graceful failures with proper error messages

3. **Generate Final Test Report** (30 minutes)
   - Document success rate (expected: 80%+)
   - List any remaining issues
   - Include screenshots and logs
   - Provide recommendations for future improvements

**Success Criteria**:
- ✓ 80%+ success rate (4/5 videos, excluding private/unavailable)
- ✓ All successful videos have entries in Videos and Jobs tables
- ✓ Fallback scenarios execute correctly
- ✓ Error messages are clear and actionable

**Deliverables**:
- Final E2E Test Report
- Test execution logs
- Database state snapshots
- Hangfire dashboard screenshots

**Go/No-Go Decision Point**:
- **GO**: If success rate >= 80% → proceed to stakeholder demo
- **NO-GO**: If success rate < 80% → extend testing, investigate failures

---

### Phase 4: Stakeholder Sign-Off (2 hours)
**Duration**: 2 hours (Day 4 afternoon)
**Owner**: Project Manager + Development Team
**Dependencies**: Phase 3 must achieve 80%+ success rate

**Tasks**:
1. **Stakeholder Demo** (1 hour)
   - Live demonstration of video ingestion
   - Show Hangfire dashboard
   - Walk through database state
   - Display monitoring and progress tracking
   - Answer stakeholder questions

2. **Sign-Off Meeting** (30 minutes)
   - Review final test results
   - Discuss any known limitations
   - Obtain formal sign-off for deployment
   - Agree on next steps (staging deployment)

3. **Documentation Update** (30 minutes)
   - Update API_USAGE_GUIDE.md
   - Document troubleshooting procedures
   - Update README.md with latest status
   - Archive diagnostic reports

**Success Criteria**:
- ✓ Stakeholders approve pipeline for staging deployment
- ✓ All documentation is up-to-date
- ✓ Known limitations are clearly documented
- ✓ Next steps are agreed upon

**Deliverables**:
- Demo recording or presentation
- Sign-off approval (email/document)
- Updated documentation
- Lessons learned document

---

## Resource Allocation

### Team Structure

| Role | Responsibility | Allocation | Total Hours |
|------|---------------|------------|-------------|
| **Backend .NET Developer** | Critical bug fixes, code implementation | 80% (Days 1-3) | 12-14 hours |
| **Test Engineer** | Test script updates, E2E validation | 50% (Days 2-4) | 4-6 hours |
| **Project Manager** | Coordination, stakeholder management | 20% (Days 1-4) | 2-3 hours |

### Resource Allocation Matrix (RACI)

| Task | Backend Dev | Test Engineer | PM | Stakeholders |
|------|------------|---------------|----|--------------|
| Phase 1: FK Fix Validation | R, A | C | I | I |
| Phase 2A: Error Handling | R, A | C | I | - |
| Phase 2A: Metadata Fallback | R, A | C | I | - |
| Phase 2B: Progress Endpoint | R, A | C | I | - |
| Phase 2B: Test Improvements | C | R, A | I | - |
| Phase 3: E2E Testing | C | R, A | I | I |
| Phase 4: Stakeholder Demo | C | C | R, A | C |
| Phase 4: Documentation | R | C | A | I |

**Legend**: R = Responsible, A = Accountable, C = Consulted, I = Informed

### Skills Required

**Backend .NET Developer**:
- Expert in C#, .NET 8, ASP.NET Core
- EF Core and database migrations
- Hangfire background job framework
- Error handling and logging patterns
- Unit testing with xUnit/NUnit

**Test Engineer**:
- PowerShell scripting
- API testing (REST, HTTP)
- Database query validation (MySQL)
- Test report generation
- E2E testing methodologies

---

## Budget & Cost Analysis

### Effort Estimation

| Phase | Hours | Confidence Level |
|-------|-------|-----------------|
| Phase 1: Fix Validation | 4 | High (90%) |
| Phase 2A: Critical Fixes | 6 | Medium (80%) |
| Phase 2B: Monitoring | 4 | High (85%) |
| Phase 3: Testing | 2 | High (90%) |
| Phase 4: Sign-Off | 2 | High (95%) |
| **Total Estimated** | **18** | **Medium-High (85%)** |

### Timeline in Calendar Days

**Recommended Timeline**: 4 days with 1 buffer day

| Scenario | Duration | Probability |
|----------|----------|-------------|
| **Best Case** | 3 days | 20% |
| **Most Likely** | 4 days | 60% |
| **Worst Case** | 5 days | 20% |

### Cost Drivers

1. **Primary Cost**: Developer time (12-14 hours)
2. **Secondary Cost**: Testing effort (4-6 hours)
3. **Overhead**: PM coordination (2-3 hours)

**Total Project Hours**: 18-23 hours
**Cost per Hour**: (Varies by organization)
**Estimated Total Cost**: 18-23 developer hours equivalent

### Budget Allocation by Category

| Category | % of Budget | Hours |
|----------|------------|-------|
| Development | 65% | 12-14 |
| Testing | 25% | 4-6 |
| Management | 10% | 2-3 |

---

## Risk Management

### Risk Register

| # | Risk | Probability | Impact | Risk Score | Mitigation Strategy | Contingency Plan |
|---|------|------------|--------|-----------|---------------------|------------------|
| R1 | FK fix doesn't work as expected | Low (20%) | High (9) | 1.8 | Thoroughly test fix in isolated environment before Phase 1 | Have alternative fix prepared: seed test users in database initialization |
| R2 | yt-dlp fallback also fails for some videos | Medium (40%) | Medium (6) | 2.4 | Test with multiple video types; implement graceful degradation | Allow videos to exist without full metadata; document as known limitation |
| R3 | E2E tests still show failures after fixes | Medium (35%) | High (8) | 2.8 | Comprehensive testing in Phase 2; identify root causes early | Extend timeline by 1 day; perform deep-dive debugging session |
| R4 | Hangfire jobs fail with new error patterns | Low (15%) | High (9) | 1.35 | Monitor Hangfire logs continuously; test with various video types | Implement additional retry logic; add circuit breaker pattern |
| R5 | Database performance issues with queries | Low (10%) | Low (3) | 0.3 | Use indexed queries; test with representative data volumes | Optimize queries; add database indexes if needed |
| R6 | Team member unavailable during critical phase | Low (20%) | Medium (6) | 1.2 | Cross-train team members; document all work thoroughly | PM can step in for testing; extend timeline by 0.5 days |
| R7 | Scope creep - stakeholders request additional features | Medium (30%) | Medium (5) | 1.5 | Clear scope definition upfront; Phase gates with go/no-go decisions | Defer non-critical features to future sprint; maintain focus on bug fixes only |
| R8 | Integration issues with external services (YouTube, yt-dlp) | Low (25%) | Medium (6) | 1.5 | Test with multiple video sources; implement robust error handling | Document service limitations; provide user guidance for unsupported videos |

**Risk Score Calculation**: Probability (0-1) × Impact (1-10)
**Priority**: Address risks with score > 2.0 first

### Top 3 Risks & Mitigations

#### Risk #1: E2E Tests Still Show Failures (Score: 2.8)
**Mitigation**:
- Implement comprehensive error handling in Phase 2A before testing
- Test each fix incrementally, not all at once
- Use Hangfire dashboard to identify exact failure points
- Maintain detailed logs for debugging

**Contingency**:
- If success rate < 80% after Phase 2: conduct root cause analysis session
- Extend timeline by 1 day for deep investigation
- Escalate to senior architect if architectural issues found

#### Risk #2: yt-dlp Fallback Failures (Score: 2.4)
**Mitigation**:
- Test yt-dlp installation and functionality before Phase 2
- Have test videos known to trigger 403 responses
- Implement comprehensive logging in fallback path
- Consider multiple fallback strategies

**Contingency**:
- Allow videos to be ingested without complete metadata
- Mark such videos with special status for manual review
- Document as known limitation in user guide

#### Risk #3: FK Fix Doesn't Work (Score: 1.8)
**Mitigation**:
- Code review the fix before Phase 1
- Test in isolated environment first
- Have database rollback plan ready
- Prepare alternative solution (seed users in DB init)

**Contingency**:
- Implement alternative: Add test users to database initialization script
- Or: Modify mock authentication to use existing admin user
- Time impact: +2 hours

### Escalation Procedures

**Level 1 - Team Level** (Backend Dev, Test Engineer, PM):
- Daily standups to identify blockers
- Immediate communication via Slack/Teams
- Decision authority: Task-level changes, minor scope adjustments

**Level 2 - Technical Lead** (for architectural issues):
- Escalate if: New bugs discovered requiring architectural changes
- Response time: Within 4 hours
- Decision authority: Technical approach, alternative solutions

**Level 3 - Product Owner/Stakeholders** (for scope/timeline changes):
- Escalate if: Timeline extension needed, scope reduction required, budget impact
- Response time: Within 1 business day
- Decision authority: Go/no-go decisions, scope trade-offs, timeline approval

**Level 4 - Executive Sponsor** (for critical project issues):
- Escalate if: Project cannot meet objectives, fundamental blockers, resource issues
- Response time: Within 24 hours
- Decision authority: Project continuation, major resource allocation

---

## Quality Assurance

### Testing Strategy

**Phase 1 Testing**:
- Manual test with 1 video
- Database state verification
- Hangfire job monitoring
- Log file analysis

**Phase 2A Testing**:
- Unit tests for all error paths
- Integration tests with 3 video types:
  1. Valid public video (happy path)
  2. Video returning 403 (fallback test)
  3. Private/unavailable video (graceful failure test)

**Phase 2B Testing**:
- API endpoint testing (progress endpoint)
- E2E test script validation
- Database query performance testing

**Phase 3 Testing**:
- Complete E2E suite (5 videos)
- Fallback scenario validation
- Load testing (optional, if time permits)

### Quality Gates

**Gate 1 - After Phase 1**:
- [ ] 1 video processes successfully end-to-end
- [ ] No FK constraint errors
- [ ] Job created and executed in Hangfire
- [ ] Database state is correct
- **Decision**: GO/NO-GO for Phase 2

**Gate 2 - After Phase 2A**:
- [ ] All unit tests pass
- [ ] 2/3 integration test videos succeed (exclude private)
- [ ] Error handling returns proper status codes
- [ ] Metadata fallback executes on 403 errors
- [ ] Code review completed and approved
- **Decision**: GO/NO-GO for Phase 2B

**Gate 3 - After Phase 2B**:
- [ ] Progress endpoint returns real job data
- [ ] E2E tests validate database state
- [ ] Test reports are clear and accurate
- [ ] All terminology is standardized
- **Decision**: GO/NO-GO for Phase 3

**Gate 4 - After Phase 3**:
- [ ] 80%+ E2E success rate achieved
- [ ] All database validations pass
- [ ] Final test report generated
- [ ] No critical bugs remaining
- **Decision**: GO/NO-GO for stakeholder demo

### Definition of Done

**For Each Bug Fix**:
- [ ] Code implemented following Clean Architecture principles
- [ ] Unit tests written and passing
- [ ] Code reviewed and approved
- [ ] Manual testing completed
- [ ] No new compiler warnings introduced
- [ ] Logging implemented for all error paths
- [ ] Documentation updated

**For Project Completion**:
- [ ] All 6 identified bugs addressed
- [ ] 80%+ E2E success rate achieved
- [ ] All quality gates passed
- [ ] Stakeholder demo completed
- [ ] Sign-off obtained
- [ ] Documentation updated
- [ ] Lessons learned documented

---

## Communication Plan

### Stakeholder Communication

| Stakeholder Group | Frequency | Method | Content |
|------------------|-----------|--------|---------|
| **Development Team** | Daily | Standup meeting (15 min) | Progress, blockers, plan for day |
| **Technical Lead** | Daily | Email summary | Status, risks, decisions needed |
| **Product Owner** | After each phase | Phase completion report | Results, metrics, go/no-go decision |
| **Business Stakeholders** | Daily | Status email | High-level progress, timeline status |
| **Executive Sponsor** | Upon completion | Final report + demo | Results, business impact, next steps |

### Status Reporting Template

**Daily Status Update**:
```
Subject: Video Pipeline Recovery - Day X Status

Current Phase: [Phase Name]
Overall Status: [On Track / At Risk / Blocked]

Completed Today:
- [Task 1]
- [Task 2]

Planned for Tomorrow:
- [Task 1]
- [Task 2]

Blockers/Risks:
- [None / Issue description]

Metrics:
- E2E Success Rate: X%
- Critical Bugs Resolved: X/6
- Timeline Status: [On schedule / X hours behind]
```

### Phase Completion Report Template

```
Subject: [Phase Name] - Completion Report

Phase: [Phase X]
Status: [Completed / Requires Extension]
Duration: [Actual vs Planned]

Objectives Achieved:
- [Objective 1] ✓/✗
- [Objective 2] ✓/✗

Key Metrics:
- Success Rate: X%
- Bugs Fixed: X
- Tests Passing: X/Y

Issues Encountered:
- [Issue 1 + Resolution]

Next Phase: [Phase X+1]
GO/NO-GO Decision: [GO / NO-GO + Reason]

Recommendation: [Proceed / Pause / Escalate]
```

---

## Dependencies & Assumptions

### External Dependencies

1. **Infrastructure**:
   - MySQL database accessible and operational
   - Hangfire server running with 3 workers
   - Redis available (if used for caching)
   - Network access to YouTube

2. **Tools & Libraries**:
   - YoutubeExplode library functioning
   - yt-dlp installed and accessible
   - Whisper installed (for Phase 3 testing)
   - PowerShell available for test scripts

3. **Team Availability**:
   - Backend Developer available 80% during Days 1-3
   - Test Engineer available 50% during Days 2-4
   - Stakeholders available for demo on Day 4

### Assumptions

1. **Technical Assumptions**:
   - FK constraint fix is correctly implemented (validated in Phase 1)
   - Database schema supports all required changes
   - No additional database migrations needed
   - Hangfire configuration is correct

2. **Business Assumptions**:
   - 80% success rate is acceptable for MVP
   - Private/unavailable videos are acceptable failures
   - Timeline of 3-5 days is acceptable to stakeholders
   - No new feature requests during fix period

3. **Process Assumptions**:
   - Code review can be completed within same day
   - Test environment matches production configuration
   - Access to all required systems and credentials
   - No other critical bugs will be discovered during fixes

### Constraints

1. **Time Constraints**:
   - Target completion: 3-5 calendar days
   - Limited developer hours per day (~4 hours actual work)
   - Phase 4 demo must happen on Day 4 afternoon

2. **Resource Constraints**:
   - Single backend developer for implementation
   - Test engineer part-time only
   - No access to production environment for testing

3. **Technical Constraints**:
   - Must maintain Clean Architecture principles
   - Cannot modify database schema (prefer code-level fixes)
   - Must use existing technology stack (.NET 8, Hangfire, MySQL)
   - Must maintain backward compatibility with existing data

---

## Success Criteria & KPIs

### Project Success Criteria

| Criterion | Target | Measurement Method | Priority |
|-----------|--------|-------------------|----------|
| **Functional Success** | 80%+ E2E success rate | E2E test results | MUST HAVE |
| **Bug Resolution** | 6/6 critical bugs fixed | Code review + testing | MUST HAVE |
| **Error Handling** | All errors return correct HTTP codes | Integration tests | MUST HAVE |
| **Monitoring Accuracy** | Progress endpoint reflects real job status | Manual validation | MUST HAVE |
| **Test Reliability** | Tests validate DB state, not just HTTP codes | Test script review | SHOULD HAVE |
| **Timeline Adherence** | Complete within 5 days | Actual vs planned dates | SHOULD HAVE |
| **Stakeholder Satisfaction** | Approved for staging deployment | Sign-off obtained | MUST HAVE |

### Key Performance Indicators (KPIs)

**Functional KPIs**:
- **E2E Success Rate**: 80%+ (4/5 videos, target: 100% for valid videos)
- **Critical Bugs Resolved**: 6/6 (100%)
- **Error Response Accuracy**: 100% (errors return 4xx/5xx, not 200)

**Quality KPIs**:
- **Test Coverage**: All critical paths tested
- **Code Review Completion**: 100% of changed code reviewed
- **Regression Bugs**: 0 new bugs introduced

**Process KPIs**:
- **Timeline Variance**: ±1 day from 4-day plan
- **Effort Variance**: ±20% from 18-hour estimate
- **Quality Gate Pass Rate**: 100% (all gates passed)

**Business KPIs**:
- **Stakeholder Approval**: Obtained
- **Production Readiness**: Approved for staging deployment
- **User Impact**: 0 (not in production yet)

---

## Next Steps & Immediate Actions

### Immediate Actions (Day 0 - Today)

**Project Manager**:
1. [ ] Distribute this project plan to all stakeholders
2. [ ] Schedule daily standup meetings (9:00 AM, Days 1-4)
3. [ ] Schedule Phase 4 demo (Day 4, 1:00 PM)
4. [ ] Set up project tracking board (optional: Jira/Azure DevOps)
5. [ ] Confirm team availability for next 4 days

**Backend Developer**:
1. [ ] Review diagnostic report and FK fix implementation
2. [ ] Set up local development environment
3. [ ] Pull latest code from YRUS-0001_first_5_days branch
4. [ ] Verify database is accessible and clean
5. [ ] Test Hangfire dashboard accessibility

**Test Engineer**:
1. [ ] Review E2E test scripts and current results
2. [ ] Prepare test video URLs (5 videos: 3 public, 1 restricted, 1 private)
3. [ ] Set up monitoring tools (database client, Hangfire dashboard)
4. [ ] Review test report templates

### Day 1 Kickoff Agenda (1 hour)

**Time**: 8:00 AM - 9:00 AM, October 7, 2025

**Agenda**:
1. Project overview and objectives (10 min)
2. Review diagnostic report findings (10 min)
3. Walk through 4-phase plan (15 min)
4. Clarify roles and responsibilities (10 min)
5. Identify any immediate blockers (10 min)
6. Confirm Phase 1 approach (5 min)

**Deliverables**:
- Confirmed understanding of all team members
- Identified blockers documented
- Phase 1 ready to begin at 9:00 AM

### Post-Project Actions

**Upon Successful Completion**:
1. Deploy fixes to staging environment
2. Conduct staging environment testing
3. Schedule production deployment (Sprint 3)
4. Update product backlog with lessons learned
5. Document troubleshooting procedures for operations team

**If Project Extends Beyond 5 Days**:
1. Conduct retrospective to identify causes
2. Re-assess timeline and scope
3. Escalate to stakeholders with revised plan
4. Consider additional resources if needed

---

## Lessons Learned (To Be Documented)

### Areas to Capture Post-Project

1. **What Went Well**:
   - Effective aspects of the fix approach
   - Successful testing strategies
   - Good communication practices

2. **What Could Be Improved**:
   - Testing practices to prevent such bugs
   - Better validation in CI/CD pipeline
   - Enhanced error handling patterns

3. **Surprises & Discoveries**:
   - Unexpected bugs found during fixes
   - Better approaches identified
   - Tools or techniques that worked well

4. **Recommendations for Future**:
   - Preventive measures for similar issues
   - Process improvements
   - Additional automated testing needed

---

## Appendices

### Appendix A: Test Video URLs

| # | Type | URL | Expected Result | Purpose |
|---|------|-----|-----------------|---------|
| 1 | Short public video | (To be determined) | Success | Happy path validation |
| 2 | Long public video | (To be determined) | Success | Performance validation |
| 3 | Video with 403 | (To be determined) | Success (via fallback) | Fallback mechanism test |
| 4 | Private video | (To be determined) | Graceful failure | Error handling validation |
| 5 | Unavailable video | (To be determined) | Graceful failure | Error handling validation |

### Appendix B: Database Validation Queries

```sql
-- Check if video was created
SELECT * FROM Videos WHERE YouTubeId = '[YOUTUBE_ID]';

-- Check if job was created
SELECT * FROM Jobs WHERE VideoId = '[VIDEO_ID]';

-- Check if transcript segments exist
SELECT COUNT(*) FROM TranscriptSegments WHERE VideoId = '[VIDEO_ID]';

-- Check video processing status
SELECT
    Id, Title, ProcessingStatus, TranscriptionStatus,
    EmbeddingStatus, CreatedAt, TranscribedAt
FROM Videos
WHERE YouTubeId = '[YOUTUBE_ID]';

-- Check job execution status
SELECT
    Id, Type, Status, Progress, HangfireJobId,
    CreatedAt, StartedAt, CompletedAt, ErrorMessage
FROM Jobs
WHERE VideoId = '[VIDEO_ID]'
ORDER BY CreatedAt DESC;
```

### Appendix C: Hangfire Dashboard Monitoring Checklist

- [ ] Navigate to http://localhost:62788/hangfire
- [ ] Check "Succeeded" count increases after video ingestion
- [ ] Check "Failed" count remains 0 (or investigate failures)
- [ ] Verify job appears in "Processing" while running
- [ ] Click on job to see detailed execution log
- [ ] Verify job transitions to "Succeeded" state
- [ ] Check job execution time is reasonable
- [ ] Verify no jobs stuck in "Enqueued" state

### Appendix D: Code Files to Modify

**Phase 2A**:
- `YoutubeRag.Api/Controllers/VideosController.cs` (error handling)
- `YoutubeRag.Infrastructure/Services/MetadataExtractionService.cs` (fallback fix)
- `YoutubeRag.Tests.Integration/Controllers/VideosControllerTests.cs` (new tests)

**Phase 2B**:
- `YoutubeRag.Api/Controllers/VideosController.cs` (progress endpoint)
- `run_e2e_tests.ps1` (DB validation)
- Test report templates

**All Phases**:
- Update logs and comments for clarity
- Ensure consistent terminology across all files

---

## Summary: Recommended Timeline & Critical Path

### Recommended Timeline: 4 Calendar Days

**Day 1 (Oct 7)**: Phase 1 - Validate FK fix (CRITICAL PATH)
**Day 2 (Oct 8)**: Phase 2A - Implement error handling & fallback fixes (CRITICAL PATH)
**Day 3 (Oct 9)**: Phase 2B - Fix monitoring & improve tests (CRITICAL PATH)
**Day 4 (Oct 10)**: Phase 3 & 4 - E2E testing & stakeholder demo (CRITICAL PATH)
**Day 5 (Oct 11)**: Buffer day for unexpected issues (CONTINGENCY)

### Critical Path Items

All items are on critical path as they are sequential dependencies:

1. **FK Fix Validation** → Must pass before any other work proceeds
2. **Error Handling Implementation** → Required for reliable operation
3. **Progress Endpoint Fix** → Needed for accurate monitoring
4. **E2E Testing** → Final validation before demo
5. **Stakeholder Demo** → Required for sign-off

**Any delay in items 1-3 will delay project completion.**

### Resource Requirements Summary

- **Backend .NET Developer**: 12-14 hours over 3 days (Days 1-3)
- **Test Engineer**: 4-6 hours over 3 days (Days 2-4)
- **Project Manager**: 2-3 hours over 4 days (coordination)
- **Stakeholders**: 1 hour on Day 4 (demo & sign-off)

**Total Project Effort**: 18-23 hours
**Calendar Duration**: 4 days (+ 1 buffer day)

### Top 3 Risks & Mitigations Summary

1. **E2E Tests Still Show Failures** (Score: 2.8)
   - **Mitigation**: Incremental testing, comprehensive logging, daily monitoring
   - **Contingency**: +1 day for deep investigation if needed

2. **yt-dlp Fallback Failures** (Score: 2.4)
   - **Mitigation**: Test installation beforehand, implement graceful degradation
   - **Contingency**: Document as limitation, allow partial metadata

3. **FK Fix Doesn't Work** (Score: 1.8)
   - **Mitigation**: Thorough code review, isolated testing before Phase 1
   - **Contingency**: Alternative solution (seed test users), +2 hours

### Key Milestones

| Milestone | Target Date | Success Criteria |
|-----------|-------------|------------------|
| **M1: Phase 1 Complete** | Oct 7, 3:00 PM | 1 video processes successfully |
| **M2: Phase 2A Complete** | Oct 8, 5:00 PM | Error handling works, fallback executes |
| **M3: Phase 2B Complete** | Oct 9, 5:00 PM | Monitoring unified, tests improved |
| **M4: E2E Testing Complete** | Oct 10, 12:00 PM | 80%+ success rate achieved |
| **M5: Stakeholder Sign-Off** | Oct 10, 3:00 PM | Approval for staging deployment |

---

**Document Status**: READY FOR APPROVAL
**Next Action**: Distribute to stakeholders and schedule Day 1 kickoff
**Prepared By**: Senior Project Manager
**Date**: October 6, 2025
**Version**: 1.0
