# Sprint 11 - Two-Track Agile Discovery Track

**Epic:** Epic 2 - Transcription Pipeline
**Sprint:** 11 (Discovery for Sprint 12 Delivery)
**Methodology:** Two-Track Agile v2.0
**Time Allocation:** 12% of sprint capacity = 5-6 hours

---

## 🎯 Two-Track Agile Overview

### Concept:

```
Sprint N (Sprint 11):
  Track 1 (Discovery): Prepare Epic 2 for Sprint 12 → 12% capacity (5-6h)
  Track 2 (Delivery): Execute Epic 1 (US-101, 102, 103) → 88% capacity (42h)

Result: Sprint 12 starts immediately with zero gap ✅
```

### Benefit:

```
Traditional Agile:
  Sprint 11: 10 días delivery + 2-3 días planning gap = 13 días
  Sprints/year: 365 / 13 ≈ 28 sprints

Two-Track Agile:
  Sprint 11: 10 días delivery + 0 días gap = 10 días
  Sprints/year: 365 / 10 ≈ 36 sprints

GANANCIA: +25% más sprints completados por año (+8 sprints)
```

---

## 📋 Epic 2: Transcription Pipeline - Discovery Tasks

### Goal:
> "Preparar completamente Epic 2 (Whisper Transcription) para comenzar Sprint 12 sin delays"

### User Stories in Epic 2:

1. **US-201:** Whisper Model Management (5 pts)
2. **US-202:** Execute Whisper Transcription (8 pts)
3. **US-203:** Store Transcript Segments (5 pts)

**Total Epic 2:** 18 story points (fits in Sprint 12 capacity)

---

## 🔍 Discovery Tasks (5-6 hours)

### Task 1: Whisper Setup Verification (2 hours)
**Owner:** Technical Lead
**Schedule:** Day 6-7 (during US-102 finalization)

**Objectives:**
- [ ] Verify Whisper installation paths
- [ ] Test Whisper CLI on local machine
- [ ] Document model sizes and performance benchmarks
- [ ] Validate model download process

**Actions:**

1. **Install Whisper** (if not already):
   ```bash
   pip install openai-whisper
   # Or: pip install git+https://github.com/openai/whisper.git
   ```

2. **Test Whisper CLI:**
   ```bash
   whisper --help
   whisper sample.wav --model tiny --output_format json
   ```

3. **Document Model Info:**
   ```markdown
   | Model | Size | Speed | Quality |
   |-------|------|-------|---------|
   | tiny  | 39MB | ~5x faster | Basic |
   | base  | 74MB | ~3x faster | Good |
   | small | 244MB | ~1x real-time | Better |
   ```

4. **Verify Model Download:**
   - Test automatic download on first use
   - Document download URLs
   - Verify checksum validation

**Deliverable:** `WHISPER_SETUP_GUIDE.md` with:
- Installation steps
- Model comparison table
- Performance benchmarks
- Configuration options

---

### Task 2: Test Data Preparation (2 hours)
**Owner:** Technical Lead
**Schedule:** Day 7-8 (during US-103 execution)

**Objectives:**
- [ ] Create sample audio files for testing
- [ ] Prepare various audio qualities
- [ ] Document test scenarios
- [ ] Generate expected outputs

**Actions:**

1. **Create Test Audio Files:**
   ```bash
   # Short sample (30 seconds)
   ffmpeg -f lavfi -i "sine=frequency=1000:duration=30" \
     -acodec pcm_s16le -ar 16000 -ac 1 test_short.wav

   # Medium sample (5 minutes) - Extract from video
   ffmpeg -i sample_video.mp4 -vn -acodec pcm_s16le \
     -ar 16000 -ac 1 -t 300 test_medium.wav
   ```

2. **Test Scenarios:**
   - Clear speech (podcast quality)
   - Background noise (YouTube quality)
   - Multiple speakers
   - Different languages (English, Spanish)
   - Various audio lengths (30s, 5m, 30m)

3. **Generate Expected Outputs:**
   ```bash
   whisper test_short.wav --model tiny --output_format json
   # Save output as test_short_expected.json
   ```

4. **Document Test Data:**
   - File locations
   - Expected transcription outputs
   - Performance baselines

**Deliverable:** `test_data/audio/` folder with:
- 5 sample WAV files
- Expected JSON outputs
- Test scenarios documentation

---

### Task 3: Whisper CLI Integration Research (1 hour)
**Owner:** Technical Lead
**Schedule:** Day 8 (during US-103 testing)

**Objectives:**
- [ ] Review Whisper CLI options
- [ ] Test JSON output format parsing
- [ ] Document command line parameters
- [ ] Identify error scenarios

**Actions:**

1. **Explore CLI Options:**
   ```bash
   whisper --help

   Key options:
     --model [tiny/base/small]
     --output_format [json/txt/srt/vtt]
     --language [en/es/auto]
     --task [transcribe/translate]
     --verbose [True/False]
   ```

2. **Test JSON Output:**
   ```json
   {
     "text": "Full transcription text",
     "segments": [
       {
         "id": 0,
         "start": 0.0,
         "end": 3.5,
         "text": "Segment text",
         "tokens": [...],
         "temperature": 0.0,
         "avg_logprob": -0.3,
         "compression_ratio": 1.5,
         "no_speech_prob": 0.01
       }
     ],
     "language": "en"
   }
   ```

3. **Error Scenarios:**
   - File not found
   - Invalid audio format
   - Out of memory
   - Model not downloaded
   - Unsupported language

4. **Performance Measurement:**
   ```bash
   time whisper test_medium.wav --model tiny
   # Document: Real time vs. processing time
   ```

**Deliverable:** `WHISPER_CLI_INTEGRATION.md` with:
- CLI command templates
- JSON output schema
- Error handling guide
- Performance expectations

---

### Task 4: Epic 2 Definition of Ready Completion (1 hour)
**Owner:** Technical Lead + Product Owner
**Schedule:** Day 9 (during buffer time)

**Objectives:**
- [ ] Complete DoR for US-201, US-202, US-203
- [ ] Update dependencies
- [ ] Get Product Owner approval
- [ ] Finalize Sprint 12 commitment

**Actions:**

1. **Review US-201 (Whisper Model Management):**
   - ✅ Story completeness
   - ✅ Acceptance criteria (Given-When-Then)
   - ✅ Dependencies (none - Whisper is standalone)
   - ✅ Technical requirements (auto-download, model selection)
   - ✅ Test requirements (model detection, download, selection)
   - ✅ Team readiness (dotnet-backend-developer agent)

2. **Review US-202 (Execute Transcription):**
   - ✅ Story completeness
   - ✅ Acceptance criteria (execution, performance, output, errors)
   - ✅ Dependencies (US-201 for model management)
   - ✅ Technical requirements (CLI invocation, JSON parsing)
   - ✅ Test requirements (unit, integration, performance)
   - ✅ Team readiness (whisper installation verified)

3. **Review US-203 (Store Segments):**
   - ✅ Story completeness
   - ✅ Acceptance criteria (storage, batch ops, embeddings, integrity)
   - ✅ Dependencies (US-202 for transcription output)
   - ✅ Technical requirements (bulk insert, EF Core, embeddings)
   - ✅ Test requirements (unit, integration, performance)
   - ✅ Team readiness (database patterns known)

4. **Update PRODUCT_BACKLOG.md:**
   - Mark Epic 2 as "READY for Sprint 12"
   - Update dependencies
   - Document any risks identified

5. **Get Approval:**
   - Technical Lead: Self-approved after verification
   - Product Owner: Document approval decision

**Deliverable:** Epic 2 READY for Sprint 12 ✅

---

## 📊 Discovery Track Schedule

### Timeline:

| Day | Track 1 (Discovery) | Track 2 (Delivery) | Hours Discovery |
|-----|---------------------|-------------------|-----------------|
| 1-5 | - | Epic 1 (US-101, 102 start) | 0h (focus on delivery) |
| 6 | Task 1: Whisper Setup (start) | US-102 finalization | 1.5h |
| 7 | Task 1: Whisper Setup (finish) + Task 2: Test Data (start) | US-103 start | 2h |
| 8 | Task 2: Test Data (finish) + Task 3: CLI Research | US-103 continuation | 1.5h |
| 9 | Task 4: Epic 2 DoR | US-103 finalization + Buffer | 1h |
| 10 | - | Sprint Review + Retro | 0h |
| **TOTAL** | **Epic 2 READY** | **Epic 1 DONE** | **6h (12.8%)** |

**Result:** Sprint 12 can start Day 11 with ZERO planning gap ✅

---

## ✅ Discovery Track Success Criteria

Epic 2 Discovery is successful if:

1. ✅ **Whisper Verified:** Whisper CLI works on local machine
2. ✅ **Test Data Ready:** 5+ sample audio files with expected outputs
3. ✅ **Integration Documented:** CLI commands and JSON parsing clear
4. ✅ **DoR Complete:** All 3 US in Epic 2 pass Definition of Ready
5. ✅ **No Blockers:** Zero unresolved blockers for Sprint 12
6. ✅ **Time Budget:** Discovery uses ≤6 hours (12% of sprint)
7. ✅ **Team Ready:** Technical Lead confident to start Sprint 12 immediately

---

## 🎯 Epic 2 Risk Assessment

### Risks Identified During Discovery:

| Risk | Probability | Impact | Mitigation | Status |
|------|------------|--------|------------|--------|
| **Whisper installation fails** | Low | High | Document in setup guide, provide alternatives | Monitor |
| **Model download slow** | Medium | Low | Pre-download models, document sizes | Accept |
| **Performance worse than expected** | Medium | Medium | Test with real videos, adjust model selection | Monitor |
| **JSON parsing complex** | Low | Medium | Use strongly-typed models, test thoroughly | Accept |
| **Memory issues with long videos** | Medium | High | Implement chunking strategy, test limits | Mitigate |

**Action:** Document all risks in Sprint 12 planning

---

## 📝 Discovery Outputs

### Artifacts to Create:

1. **WHISPER_SETUP_GUIDE.md**
   - Installation steps
   - Model comparison
   - Configuration options

2. **test_data/audio/ folder**
   - 5 sample WAV files
   - Expected JSON outputs
   - Test scenarios doc

3. **WHISPER_CLI_INTEGRATION.md**
   - CLI templates
   - JSON schema
   - Error handling
   - Performance benchmarks

4. **Epic 2 DoR Updates**
   - Complete Definition of Ready
   - Dependencies updated
   - Approval documented

5. **SPRINT_12_PLAN.md (draft)**
   - Epic 2 user stories
   - Capacity calculation
   - Timeline
   - Dependencies validated

---

## 🔄 Integration with Delivery Track

### Coordination Points:

**Day 6:**
- Delivery: US-102 code review
- Discovery: Start Whisper setup (no conflict)

**Day 7:**
- Delivery: US-103 implementation
- Discovery: Test data creation uses FFmpeg (already familiar from US-103)

**Day 8:**
- Delivery: US-103 testing
- Discovery: CLI research uses test audio from US-103

**Day 9:**
- Delivery: Buffer tasks (testing, docs)
- Discovery: Epic 2 DoR completion (Product Owner time)

**Result:** No conflicts, synergies identified ✅

---

## 📊 Two-Track Metrics

### Track What Matters:

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Discovery Time** | ≤6h (12%) | Track actual hours |
| **Delivery Impact** | 0% slowdown | Monitor velocity |
| **Sprint 12 Readiness** | 100% DoR | Checklist completion |
| **Blocker Count** | 0 blockers | Track discoveries |
| **Team Confidence** | HIGH | Subjective rating |

**Review:** During Sprint 11 retrospective

---

## 🎓 Lessons for Sprint 12

### Evaluate Two-Track Effectiveness:

After Sprint 11, assess:

1. **Did Discovery track complete in 6 hours?**
   - If yes: Maintain 12% allocation
   - If no: Adjust to 15% for Sprint 12

2. **Did it slow down Delivery track?**
   - If yes: Separate Discovery to different days
   - If no: Continue parallel execution

3. **Did Sprint 12 start without gaps?**
   - If yes: Two-Track success ✅
   - If no: Identify what was missing

4. **Was Epic 2 DoR complete?**
   - If yes: DoR checklist works
   - If no: Add missing items to checklist

5. **Would we do this again?**
   - If yes: Make Two-Track standard
   - If no: Document why and alternatives

---

## ✅ Discovery Track Checklist

**Before Starting Sprint 11:**
- [x] Discovery tasks identified (4 tasks)
- [x] Time allocated (6 hours, 12%)
- [x] Schedule defined (Days 6-9)
- [x] Artifacts specified (4 deliverables)
- [x] Success criteria clear (7 criteria)

**During Sprint 11:**
- [ ] Day 6: Whisper setup verification (1.5h)
- [ ] Day 7: Test data preparation (2h)
- [ ] Day 8: CLI integration research (1.5h)
- [ ] Day 9: Epic 2 DoR completion (1h)
- [ ] Track actual hours spent
- [ ] Monitor impact on delivery track

**After Sprint 11:**
- [ ] All 4 artifacts created
- [ ] Epic 2 DoR verified (100%)
- [ ] Sprint 12 plan drafted
- [ ] Evaluate Two-Track effectiveness
- [ ] Document lessons learned

---

## 🚀 Sprint 12 Preview

### If Discovery Successful:

**Sprint 12 (starts Day 11):**
- Epic 2: Transcription Pipeline
- US-201, US-202, US-203 (18 story points)
- Zero planning gap ✅
- High confidence start (DoR complete)

**Two-Track continues:**
- Discovery: Epic 3 (Background Jobs)
- Delivery: Epic 2 (Transcription)

**Result:** Continuous flow, +25% productivity ✅

---

**Status:** PLANNED
**Owner:** Technical Lead
**Sprint:** 11
**Time Budget:** 6 hours (12% capacity)
**Success Measure:** Sprint 12 starts Day 11 with ZERO delays
