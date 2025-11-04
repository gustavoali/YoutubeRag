# Sprint 11 - Video Ingestion Pipeline MVP

**Sprint Duration:** 10 días (2025-10-20 to 2025-10-31)
**Sprint Goal:** Implementar pipeline completo de ingesta de videos de YouTube con descarga, extracción de audio y almacenamiento persistente
**Methodology Version:** 2.0 (Two-Track Agile)
**Team:** 1 Developer (Technical Lead) + Claude Code Agents

---

## 🎯 Sprint Goal

> "Entregar un pipeline funcional end-to-end que permita a usuarios enviar URLs de YouTube, descargar videos, extraer audio y prepararlo para transcripción, con manejo robusto de errores y progreso visible."

---

## 📊 Capacity Planning (v2.0 Formula)

### Calculation:

```
Capacity = Team Days × Hours/Day × Efficiency × Availability

Donde:
  Team Days: 1 developer × 10 días = 10 team-days
  Hours/Day: 6 hours (productive time)
  Efficiency: 0.80 (80% - account for context switching, meetings)
  Availability: 0.98 (98% - 2% for unexpected interruptions)

Capacity = 10 × 6 × 0.80 × 0.98 = 47.04 hours
```

### Buffer Strategy:

```
Total Capacity: 47 hours

Allocation:
  - Commitment (80%): 37.6 hours → ~21 story points
  - Buffer (20%): 9.4 hours
    - Bug fixes: 4 hours
    - Code reviews: 2 hours
    - Documentation: 2 hours
    - Unexpected issues: 1.4 hours
```

### Story Points to Hours Conversion:

```
Fibonacci: 1, 2, 3, 5, 8, 13

Mapping (conservative):
  1 pt = 1.5h
  2 pts = 2.5h
  3 pts = 4h
  5 pts = 7h
  8 pts = 12h
  13 pts = 20h

Sprint 11 Commitment: 21 story points = ~38 hours ✅
```

**Confidence Level:** HIGH (90%)
- Historias bien definidas
- Dependencies mínimas
- Tech stack conocido (.NET 8)
- Test coverage actual: 99.3%

---

## 📋 Backlog Items Selected

### Epic 1: Video Ingestion Pipeline (21 pts)

#### 1. US-101: Submit YouTube URL for Processing (5 pts)
**Priority:** P0 (Critical)
**Estimated Hours:** 7h
**Dependencies:** None
**Agentes:** dotnet-backend-developer, test-engineer, code-reviewer

**As a** content creator
**I want** to submit YouTube video URLs for processing
**So that** I can search and analyze video content

**Acceptance Criteria:**

**AC1: URL Validation**
- Given a user submits a URL
- When the URL is processed
- Then system validates it's a valid YouTube URL format
- And rejects non-YouTube URLs with clear error message
- And accepts both youtube.com and youtu.be formats

**AC2: Duplicate Detection**
- Given a YouTube URL is submitted
- When the video ID already exists in database
- Then system returns existing video record
- And does not create duplicate processing job
- And informs user video already processed

**AC3: Metadata Extraction**
- Given a valid YouTube URL
- When video is accepted for processing
- Then system extracts video title, duration, author, thumbnail
- And stores metadata in Video entity
- And returns video ID to user immediately

**AC4: Job Creation**
- Given video metadata is extracted
- When processing begins
- Then system creates Job entity with "Pending" status
- And queues background processing job
- And returns job ID for progress tracking

**Technical Requirements:**
- Controller: `POST /api/videos/from-url`
- Service: `IVideoService.SubmitVideoFromUrlAsync()`
- Library: YoutubeExplode NuGet package
- Retry logic: 3 attempts for network failures
- Transaction: Atomic Video + Job creation
- Validation: FluentValidation for URL format

**Definition of Done:**
- [ ] Code implemented in VideoService
- [ ] Controller endpoint created
- [ ] YoutubeExplode integration complete
- [ ] Unit tests: VideoService (>80% coverage)
- [ ] Integration tests: API endpoint
- [ ] Error handling for all scenarios
- [ ] Swagger documentation updated
- [ ] Database constraints tested
- [ ] Code review approved
- [ ] Deployed to local environment

**DoR Verification:** ✅ PASSED (100%)
- Story completeness: ✅
- Acceptance criteria: ✅ (4 AC in Given-When-Then format)
- Dependencies: ✅ (None - entry point)
- Technical requirements: ✅ (Fully specified)
- Test requirements: ✅ (Unit + Integration)
- Team readiness: ✅ (dotnet-backend-developer available)
- Approval: ✅ (Product Owner + Technical Lead)

---

#### 2. US-102: Download Video Content (8 pts)
**Priority:** P0 (Critical)
**Estimated Hours:** 12h
**Dependencies:** US-101 (requires video ID)
**Agentes:** dotnet-backend-developer, test-engineer, code-reviewer

**As a** system
**I want** to download video/audio content from YouTube
**So that** I can process it locally for transcription

**Acceptance Criteria:**

**AC1: Stream Selection**
- Given a YouTube video URL
- When downloading content
- Then system selects highest quality audio stream available
- And falls back to audio from video stream if needed
- And handles videos without separate audio streams

**AC2: Download Progress**
- Given a download in progress
- When monitoring the operation
- Then progress updates every 10 seconds
- And includes bytes downloaded and total size
- And estimates completion time

**AC3: Storage Management**
- Given a successful download
- When saving the file
- Then content stored in configured temp directory
- And file named with video ID + timestamp
- And old temp files cleaned up after 24 hours

**AC4: Error Recovery**
- Given a download fails (network, timeout)
- When retry logic executes
- Then system retries up to 3 times with exponential backoff
- And logs detailed error information
- And updates job status to "Failed" after final failure

**Technical Requirements:**
- Service: `IVideoDownloadService.DownloadAsync()`
- Streaming: HttpClient with streaming
- Progress: IProgress<DownloadProgress>
- Storage: `{TEMP_PATH}/{videoId}_{timestamp}.mp4`
- Retry: Polly library with exponential backoff (10s, 30s, 90s)
- Timeout: 30 seconds per connection
- Cleanup: Hangfire recurring job daily

**Performance Requirements:**
- Support files up to 10GB
- Memory usage <500MB during download
- Resume capability for interrupted downloads
- Bandwidth throttling configurable

**Definition of Done:**
- [ ] VideoDownloadService implemented
- [ ] Streaming download functional
- [ ] Progress tracking working
- [ ] Retry logic with exponential backoff
- [ ] Temp file management system
- [ ] Unit tests (>80% coverage)
- [ ] Integration tests with mock YouTube
- [ ] Performance tested with 2+ hour videos
- [ ] Error scenarios thoroughly tested
- [ ] Disk space validation implemented
- [ ] Code review approved
- [ ] Documentation updated

**DoR Verification:** ✅ PASSED (100%)
- All criteria from US-101 apply
- Dependency: US-101 must complete first
- Technical complexity: HIGH (streaming, retry, progress)
- Risk mitigation: Polly library handles retry elegantly

---

#### 3. US-103: Extract Audio from Video (5 pts)
**Priority:** P0 (Critical)
**Estimated Hours:** 7h
**Dependencies:** US-102 (requires downloaded video)
**Agentes:** dotnet-backend-developer, test-engineer, code-reviewer

**As a** system
**I want** to extract audio from downloaded video files
**So that** Whisper can transcribe the content

**Acceptance Criteria:**

**AC1: Audio Extraction**
- Given a downloaded video file
- When extraction process runs
- Then audio extracted to WAV format
- And audio normalized to 16kHz mono
- And file saved with .wav extension

**AC2: Format Support**
- Given various video formats from YouTube
- When processing different formats
- Then system handles MP4, WebM, MKV formats
- And converts any audio codec to PCM
- And maintains audio quality

**AC3: FFmpeg Integration**
- Given FFmpeg is required
- When extraction runs
- Then system validates FFmpeg installation
- And provides clear error if missing
- And uses configured FFmpeg path

**AC4: Performance Requirements**
- Given a video file
- When extracting audio
- Then extraction completes in <10% of video duration
- And uses reasonable CPU/memory
- And handles files up to 10GB

**Technical Requirements:**
- Service: `IAudioExtractionService.ExtractAudioAsync()`
- Library: FFMpegCore NuGet package
- Command: `ffmpeg -i input -vn -acodec pcm_s16le -ar 16000 -ac 1 output.wav`
- Progress: Parse FFmpeg stdout
- Validation: Check FFmpeg.exe exists at startup
- Cleanup: Delete video file after successful extraction
- Storage: `{TEMP_PATH}/{videoId}_audio.wav`

**FFmpeg Command Details:**
```bash
ffmpeg -i {input_video} \
  -vn \                      # No video
  -acodec pcm_s16le \       # PCM 16-bit little-endian
  -ar 16000 \               # Sample rate 16kHz
  -ac 1 \                   # Mono (1 channel)
  {output_audio}.wav
```

**Definition of Done:**
- [ ] AudioExtractionService implemented
- [ ] FFMpegCore integration complete
- [ ] FFmpeg validation at startup
- [ ] Audio extraction working
- [ ] Progress tracking functional
- [ ] Format compatibility tested (MP4, WebM, MKV)
- [ ] Performance benchmarks met (<10% duration)
- [ ] Unit tests (>80% coverage)
- [ ] Integration tests with sample videos
- [ ] Error handling for missing FFmpeg
- [ ] Cleanup logic implemented
- [ ] Code review approved
- [ ] Documentation updated

**DoR Verification:** ✅ PASSED (100%)
- Dependency: US-102 must complete first
- External tool: FFmpeg required (document in setup)
- Performance: Benchmarks defined clearly
- Risk: FFmpeg installation documented in setup guide

---

#### 4. Buffer Tasks (3 pts)
**Priority:** P1 (High)
**Estimated Hours:** 4h

**Tasks Included:**
- Close Issue #13 with PR
- Comprehensive testing of US-101, US-102, US-103
- API documentation updates (Swagger)
- Code reviews and feedback incorporation
- Bug fixes identified during sprint
- Sprint retrospective documentation

---

## 📅 Sprint Timeline

### Week 1 (Days 1-5):

**Day 1 (Oct 20):**
- [ ] Sprint kickoff meeting (30 min)
- [ ] US-101 implementation start
- [ ] YoutubeExplode integration
- [ ] Milestone: URL validation working

**Day 2 (Oct 21):**
- [ ] US-101 continuation
- [ ] Metadata extraction complete
- [ ] Job creation logic
- [ ] Unit tests for US-101
- [ ] Milestone: US-101 90% complete

**Day 3 (Oct 22):**
- [ ] US-101 finalization
- [ ] Integration tests
- [ ] Code review
- [ ] US-102 implementation start
- [ ] Milestone: US-101 DONE ✅

**Day 4 (Oct 23):**
- [ ] US-102 download service
- [ ] Streaming implementation
- [ ] Progress tracking
- [ ] Milestone: Download functional

**Day 5 (Oct 24):**
- [ ] US-102 retry logic
- [ ] Error handling
- [ ] Storage management
- [ ] Milestone: US-102 80% complete

### Week 2 (Days 6-10):

**Day 6 (Oct 25):**
- [ ] US-102 finalization
- [ ] Unit + integration tests
- [ ] Code review
- [ ] Milestone: US-102 DONE ✅

**Day 7 (Oct 27):**
- [ ] US-103 implementation start
- [ ] FFMpegCore integration
- [ ] Audio extraction working
- [ ] Milestone: Basic extraction functional

**Day 8 (Oct 28):**
- [ ] US-103 progress tracking
- [ ] Format support testing
- [ ] Performance optimization
- [ ] Milestone: US-103 80% complete

**Day 9 (Oct 29):**
- [ ] US-103 finalization
- [ ] Comprehensive testing
- [ ] Code review
- [ ] Milestone: US-103 DONE ✅

**Day 10 (Oct 30):**
- [ ] Close Issue #13 with PR
- [ ] Sprint review meeting
- [ ] Sprint retrospective
- [ ] Documentation updates
- [ ] Milestone: Sprint 11 COMPLETE ✅

---

## 🔄 Two-Track Agile Implementation

### Track 1: Discovery (12% of sprint = 5-6h)

**Epic 2 Preparation (for Sprint 12):**

**Tasks:**
1. **Whisper Model Setup Verification** (2h)
   - Verify Whisper installation paths
   - Test model download process
   - Document model sizes and performance

2. **Test Data Preparation** (2h)
   - Create sample audio files for testing
   - Prepare various audio qualities
   - Document test scenarios

3. **Whisper CLI Integration Research** (1h)
   - Review Whisper CLI options
   - Test JSON output format
   - Document command line parameters

4. **Epic 2 DoR Completion** (1h)
   - Complete Definition of Ready for US-201, US-202, US-203
   - Update dependencies
   - Get approval from Product Owner

**Owner:** Technical Lead
**Schedule:** During Days 6-9 (parallel with US-102/103 execution)

### Track 2: Delivery (88% of sprint)

Execute US-101, US-102, US-103 as planned above.

**Result:** Sprint 12 can start immediately without planning delays.

---

## 📊 Success Metrics

### Sprint-Level Metrics:

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| **Sprint Goal Achievement** | 100% | All 3 US completed |
| **Velocity** | 21 pts | Story points delivered |
| **Test Coverage** | >80% | New code coverage report |
| **DoD Compliance** | 100% | All checklist items met |
| **Zero Critical Bugs** | 0 P0 bugs | Bug tracking during sprint |
| **Code Review Turnaround** | <4 hours | Track review timestamps |

### Leading Indicators (v2.0):

| Indicator | Threshold | Action if Exceeded |
|-----------|-----------|-------------------|
| **WIP Limit** | Max 2 stories in-progress | Block new work until completion |
| **Cycle Time** | <3 days per 5-pt story | Break into smaller stories |
| **Blocked Time** | <10% of sprint | Escalate blockers immediately |
| **Rework Rate** | <10% of code | Improve DoR thoroughness |
| **Test Failure Rate** | <5% | Investigate test quality |

**Tracking Frequency:** Daily during standup

---

## 🚨 Risk Register

| Risk | Probability | Impact | Mitigation | Owner |
|------|------------|--------|------------|-------|
| **YoutubeExplode API changes** | Medium | High | Use stable NuGet version, implement adapter pattern | Tech Lead |
| **YouTube rate limiting** | Medium | Medium | Implement retry with exponential backoff, document limits | Tech Lead |
| **FFmpeg not installed** | Low | High | Document in setup guide, validate at startup | Tech Lead |
| **Large file handling (10GB)** | Medium | Medium | Implement streaming, add disk space checks | Tech Lead |
| **Network failures during download** | High | Medium | Polly retry logic, resume capability | Tech Lead |
| **Timeline pressure** | Low | Low | 20% buffer, can defer buffer tasks if needed | Tech Lead |

**Risk Review:** Daily during standup
**Escalation:** Immediately if High Impact risk materializes

---

## 🎯 Definition of Done (Sprint-Level)

### For Each User Story:
- [ ] Code implemented following Clean Architecture
- [ ] Unit tests written (minimum 80% coverage for new code)
- [ ] Integration tests for critical paths
- [ ] Code reviewed and approved by code-reviewer agent
- [ ] API documentation updated (Swagger)
- [ ] No compiler warnings
- [ ] Performance validated against AC requirements
- [ ] Security considerations addressed
- [ ] Error handling comprehensive (try-catch, logging)
- [ ] Logging implemented (structured with Serilog)
- [ ] Deployed to local environment
- [ ] Acceptance criteria validated (100%)
- [ ] No P0 bugs remaining

### For Sprint 11:
- [ ] Sprint goal achieved (pipeline functional end-to-end)
- [ ] All 3 committed stories complete (US-101, US-102, US-103)
- [ ] Test coverage target met (>80% for new code)
- [ ] Performance benchmarks passed
- [ ] Sprint retrospective conducted and documented
- [ ] Technical debt documented in TECHNICAL_DEBT_REGISTER.md
- [ ] Epic 2 ready for Sprint 12 (Two-Track Discovery)
- [ ] Issue #13 closed with PR merged

---

## 📈 Daily Standup Questions

**Format:** 15 minutes, 9:00 AM daily

**Questions:**
1. What did I complete yesterday?
2. What will I work on today?
3. Any blockers or impediments?
4. Leading indicators status?
   - WIP count?
   - Any stories blocked >1 day?
   - Test failures?

**Output:** Update TODO list, escalate blockers immediately

---

## 🎓 Sprint Retrospective Template

**Schedule:** Day 10 (Oct 30), 1 hour

**Agenda:**
1. **What went well?** (15 min)
2. **What didn't go well?** (15 min)
3. **What can we improve?** (15 min)
4. **Action items for Sprint 12** (15 min)

**Focus Areas:**
- Two-Track Agile effectiveness
- DoR completeness impact
- Capacity planning accuracy
- Leading indicators usefulness
- Agent usage patterns

**Output:** Document in `SPRINT_11_RETROSPECTIVE.md`

---

## 📝 Communication Plan

### Daily Updates:
- **Standup:** 9:00 AM (15 min)
- **Progress:** Update TODO list after each completed task
- **Blockers:** Immediate escalation (no waiting for standup)

### Weekly Updates:
- **Status Report:** End of Week 1 (Day 5)
- **Risk Review:** Day 5 and Day 9
- **Stakeholder Demo:** Day 10 (Sprint Review)

### Documentation:
- **Code Comments:** Inline for complex logic
- **API Docs:** Swagger auto-generated
- **Architecture Decisions:** Document in `docs/architecture/`
- **Sprint Artifacts:** This file + retrospective

---

## ✅ Readiness Checklist

**Before Starting Sprint 11:**

### Methodology v2.0:
- [x] SPRINT_11_PLAN.md created
- [ ] SPRINT_11_CAPACITY_CALCULATION.md created
- [ ] TECHNICAL_DEBT_REGISTER.md updated
- [ ] Definition of Ready verified for all US
- [ ] Two-Track Agile plan documented

### Technical Setup:
- [x] Development environment ready
- [x] Database migrations current
- [x] Test suite passing (99.3%)
- [ ] YoutubeExplode NuGet package available
- [ ] FFMpegCore NuGet package available
- [ ] FFmpeg installation documented

### Team Readiness:
- [x] Sprint goal communicated
- [x] User stories understood
- [x] Acceptance criteria clear
- [x] Technical approach agreed
- [x] Agent roles identified

### Previous Work:
- [ ] Issue #13 PR created and merged
- [ ] Branch `test/issue-13-coverage-50-percent` cleaned up
- [ ] Master branch updated

---

## 🎯 Sprint Success Criteria

Sprint 11 is successful if:

1. ✅ **Functional:** Users can submit YouTube URLs and videos download successfully
2. ✅ **Quality:** >80% test coverage on new code, zero P0 bugs
3. ✅ **Performance:** Download completes in reasonable time, extraction <10% of duration
4. ✅ **Completeness:** All 3 US meet 100% of acceptance criteria
5. ✅ **Process:** Two-Track Agile piloted, Epic 2 ready for Sprint 12
6. ✅ **Velocity:** 21 story points delivered, establishing baseline

**If we achieve 5/6:** Success
**If we achieve 6/6:** Excellent

---

**Status:** READY TO START
**Next Action:** Complete methodology v2.0 artifacts, then begin execution
**Owner:** Technical Lead
**Created:** 2025-10-16
**Sprint Start:** 2025-10-20
