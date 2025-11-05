# 09 - Flujo Completo: De Idea a Deploy

**Versión:** 1.0
**Fecha:** 2025-10-20
**Estado:** ACTIVO

---

## 🎯 Ejemplo Práctico: Video Ingestion Pipeline (Sprint 11)

Este ejemplo muestra el flujo completo desde una idea de negocio hasta producción, aplicando la metodología v2.0.

---

## Paso 1: IDEA (Business Stakeholder)

**TÚ (Stakeholder) identificas necesidad:**
> "Necesitamos un sistema para ingestar videos de YouTube automáticamente"

**Business Value:**
- Permitir a usuarios procesar videos de YouTube
- Base para transcripción automática
- Core MVP functionality

**Decisión:** GO ✅

---

## Paso 2: BACKLOG (Product Owner)

**TÚ (PO) creas épica:**

```markdown
### Epic 1: Video Ingestion Pipeline

**As a** content creator
**I want** to submit YouTube videos for processing
**So that** I can search and analyze video content

**Business Value:** Core MVP feature
**RICE Score:** 187.5
**Priority:** MUST HAVE
```

**Opcionalmente, usas agente `product-owner`:**
```
Prompt: "Crea historias de usuario para video ingestion pipeline"
Output: US-101, US-102, US-103 con AC completos
```

**TÚ priorizas:**
- US-101: Submit YouTube URL (5 pts)
- US-102: Download Video (8 pts)
- US-103: Extract Audio (5 pts)

---

## Paso 3: DEFINITION OF READY (Product Owner + Technical Lead)

**TÚ verificas DoR para cada US:**

```markdown
US-101 DoR Checklist:
✅ Story completeness (ID, título, formato)
✅ 4 AC en Given-When-Then format
✅ Dependencies: None (entry point)
✅ Technical requirements: YoutubeExplode, retry logic
✅ Test requirements: Unit >80% + Integration
✅ Team readiness: dotnet-backend-developer agent
✅ Approval: PO + TL signed off

Result: ✅ READY TO START
```

---

## Paso 4: SPRINT PLANNING (Project Manager)

**TÚ (PM) calculas capacity:**

```
Formula v2.0:
Capacity = 10 días × 6h/día × 0.80 × 0.98 = 47h

Allocation:
- Commitment (80%): 37.6h → 21 story points
- Buffer (20%): 9.4h

Sprint 11 Commitment: US-101 + US-102 + US-103 = 21 pts ✅
```

**Opcionalmente, usas agente `project-manager`:**
```
Prompt: "Crea plan para Sprint 11 con US-101-103"
Output: Timeline, capacity calculation, risk analysis
```

**TÚ apruebas el plan** ✅

---

## Paso 5: GIT WORKFLOW (Technical Lead)

**TÚ creas rama:**
```bash
git checkout master
git pull origin master
git checkout -b feature/epic-1-video-ingestion
```

**TÚ documentas sprint:**
- Creas `SPRINT_11_PLAN.md`
- Creas `SPRINT_11_CAPACITY_CALCULATION.md`
- Creas `SPRINT_11_TWO_TRACK_DISCOVERY.md`

---

## Paso 6: IMPLEMENTACIÓN US-101 (Technical Lead + Agents)

### 6.1 Diseño de Arquitectura

**Agente `software-architect`:**
```
Input: "Diseña integración de YoutubeExplode para US-101"
Output:
- IVideoService.SubmitVideoFromUrlAsync()
- Validation layer con FluentValidation
- Retry logic con Polly
- Transaction scope para Video + Job
```

**TÚ apruebas diseño** ✅

### 6.2 Implementación

**Agente `dotnet-backend-developer`:**
```
Input: "Implementa US-101 según SPRINT_11_PLAN.md"

Tasks ejecutadas:
1. Install YoutubeExplode NuGet package
2. Implement VideoService.SubmitVideoFromUrlAsync()
3. Add URL validation (youtube.com and youtu.be)
4. Add duplicate detection by YoutubeId
5. Extract metadata (title, duration, author, thumbnail)
6. Create Job entity with "Pending" status
7. Implement retry logic (3 attempts, exponential backoff)
8. Add transaction scope

Output: Código implementado en Clean Architecture
```

### 6.3 Testing Manual

**TÚ ejecutas testing manual:**
```
Test Cases:
✅ Submit valid YouTube URL → Video created
✅ Submit duplicate URL → Returns existing video
✅ Submit invalid URL → Error message
✅ Submit youtu.be short URL → Works
✅ YouTube API timeout → Retries 3 times
✅ Transaction rollback → No partial data

Result: All tests pass ✅
```

### 6.4 Testing Automatizado

**Agente `test-engineer`:**
```
Input: "Crea unit tests para US-101, coverage >80%"

Output:
- VideoServiceTests.cs
  - SubmitVideoFromUrlAsync_WithValidUrl_CreatesVideoSuccessfully
  - SubmitVideoFromUrlAsync_WithDuplicateUrl_ReturnsExistingVideo
  - SubmitVideoFromUrlAsync_WithInvalidUrl_ThrowsValidationException
  - SubmitVideoFromUrlAsync_WithYoutuBeUrl_CreatesVideoSuccessfully
  - SubmitVideoFromUrlAsync_WithNetworkError_RetriesThreeTimes
  - [10 more tests]

Coverage: 95% ✅
```

### 6.5 Code Review

**Agente `code-reviewer`:**
```
Input: "Revisa código de US-101"

Output:
Critical (2):
- Fix SQL injection risk in query
- Add input sanitization for video title

High (3):
- Add timeout to YoutubeExplode calls
- Improve error messages
- Add logging for retry attempts

Medium (5):
- Extract magic numbers to constants
- Add XML documentation
- [3 more suggestions]

Low (7):
- Improve variable naming
- [6 more suggestions]
```

**TÚ aplicas feedback Critical y High** ✅

### 6.6 Definition of Done

**TÚ verificas DoD:**
```
✅ Code implemented following Clean Architecture
✅ Unit tests >80% coverage (actual: 95%)
✅ Integration tests passing
✅ Code reviewed and approved
✅ Swagger documentation updated
✅ No compiler warnings
✅ Performance validated (<200ms p95)
✅ Security reviewed (no vulnerabilities)
✅ Error handling comprehensive
✅ Logging implemented
✅ Deployed to local environment
✅ All AC validated (100%)
✅ No P0 bugs

Result: DoD 100% COMPLETE ✅
```

---

## Paso 7: REPETIR PARA US-102 y US-103

**Days 4-6:** US-102 (Download Video) - Same process
**Days 7-9:** US-103 (Extract Audio) - Same process

---

## Paso 8: TWO-TRACK AGILE (Parallel)

**Durante Sprint 11, TÚ también:**

### Track 1: Discovery (12% capacity = 5-6h)

**Days 6-9:**
- Instalar y verificar Whisper CLI
- Crear test data (sample audio files)
- Documentar CLI integration
- Completar DoR para Epic 2 (Sprint 12)

**Result:** Epic 2 READY para Sprint 12 ✅

### Track 2: Delivery (88% capacity)

**Days 1-10:** Ejecutar US-101, US-102, US-103

---

## Paso 9: SPRINT REVIEW (Day 10)

**TÚ ejecutas:**

### 9.1 Regresión Automática
```bash
dotnet test --configuration Release
Result: 474/474 tests passing ✅ (49 new unit tests)
```

### 9.2 Testing Manual Completo
```
Epic 1 End-to-End Test:
1. Submit YouTube URL → ✅ Video created
2. Download starts → ✅ Progress visible
3. Audio extracted → ✅ WAV file created
4. Job completed → ✅ Status updated

Result: Epic 1 functional end-to-end ✅
```

### 9.3 Validation (Product Owner)
```
TÚ (PO) verificas AC:

US-101:
✅ AC1: URL validation works
✅ AC2: Duplicate detection works
✅ AC3: Metadata extracted correctly
✅ AC4: Job created successfully

US-102:
✅ AC1: Stream selection correct
✅ AC2: Progress updates work
✅ AC3: Storage management works
✅ AC4: Error recovery works

US-103:
✅ AC1: Audio extraction works
✅ AC2: Format support verified
✅ AC3: FFmpeg integration works
✅ AC4: Performance meets requirements

Result: All AC validated ✅
```

### 9.4 Sign-off (Business Stakeholder)
```
TÚ (Stakeholder) evalúas:
✅ Business value delivered (video ingestion works)
✅ Quality acceptable (99%+ test coverage)
✅ Timeline met (10 días committed, 10 días actual)
✅ Ready for next sprint

Decision: APPROVED ✅ GO to Sprint 12
```

---

## Paso 10: TECHNICAL DEBT REGISTER

**TÚ actualizas TD Register:**
```markdown
## Sprint 11 Review

New Debt Identified:
- TD-001: FFmpeg path hardcoded (Low priority, ROI 2.5x)
- TD-002: YoutubeExplode error messages in English only (Low priority, ROI 1.2x)

Decision: Defer both to "Fix when capacity" (ROI <5x)

Result: Interest rate: 0.5h/sprint (well below 5h target) ✅
```

---

## Paso 11: SPRINT RETROSPECTIVE

**TÚ documenta lessons learned:**
```markdown
## Sprint 11 Retrospective

### What Went Well:
✅ Two-Track Agile worked perfectly (Epic 2 ready for Sprint 12)
✅ Definition of Ready prevented all surprises (zero scope creep)
✅ Capacity planning formula accurate (21 pts planned = 21 pts delivered)
✅ Test Data Builders made testing 66% faster

### What Didn't Go Well:
⚠️ FFmpeg installation required manual steps (not in setup guide)
⚠️ YoutubeExplode rate limiting hit once (need better docs)

### Action Items for Sprint 12:
1. Update setup guide with FFmpeg installation steps
2. Document YouTube API rate limits
3. Continue Two-Track Agile (proved beneficial)

Velocity: 21 pts (baseline established for Sprint 12)
```

---

## Paso 12: MERGE TO MASTER

**TÚ ejecutas merge:**
```bash
git checkout master
git merge --no-ff feature/epic-1-video-ingestion
git push origin master
```

**TÚ creas PR:**
```
Title: "Sprint 11: Epic 1 - Video Ingestion Pipeline (US-101, 102, 103)"
Description: Ver SPRINT_11_PLAN.md para detalles
Result: PR #25 created
```

---

## Paso 13: CONTINUOUS DEPLOYMENT

**CI/CD Pipeline ejecuta:**
```
✅ Build successful
✅ 474 tests passing
✅ Security scan passed
✅ Code coverage 99.3%
✅ Deploy to staging successful
✅ E2E smoke tests passed

Ready for production ✅
```

---

## Paso 14: PRODUCTION DEPLOY

**TÚ (Stakeholder) apruebas deploy:**
```
Decision: DEPLOY to production ✅

Rollout strategy:
- Feature flag: video_ingestion_enabled=true
- Gradual rollout: 10% → 50% → 100%
- Monitor: Error rate, performance, user feedback

Result: Production deploy successful 🚀
```

---

## 📊 Métricas del Sprint 11

### Velocity:
- Committed: 21 story points
- Delivered: 21 story points
- Accuracy: 100% ✅

### Quality:
- Test coverage: 99.3% (maintained)
- New tests: 49 unit tests
- Bugs found: 0 P0, 2 P3 (deferred to TD Register)

### Timeline:
- Planned: 10 días
- Actual: 10 días
- Accuracy: 100% ✅

### Capacity:
- Planned: 47h (formula v2.0)
- Actual: 45h
- Variance: -4% (within acceptable range)

### Two-Track Agile:
- Discovery time: 6h (12.8% of sprint)
- Epic 2 readiness: 100% ✅
- Gap eliminated: Sprint 12 starts immediately

---

## ✅ Success Criteria Met

1. ✅ **Sprint Goal:** Video ingestion pipeline functional end-to-end
2. ✅ **Quality:** 99.3% test coverage, zero P0 bugs
3. ✅ **Timeline:** 10 días committed = 10 días actual
4. ✅ **Methodology:** Two-Track Agile, DoR, Capacity Planning all applied
5. ✅ **Business Value:** Users can now submit YouTube videos
6. ✅ **Next Sprint:** Epic 2 ready to start immediately (zero gap)

---

## 🎓 Lessons Learned

### Methodology v2.0 Effectiveness:

1. **Two-Track Agile:** ⭐⭐⭐⭐⭐ (5/5)
   - Epic 2 ready without planning gap
   - +25% productivity validated

2. **Definition of Ready:** ⭐⭐⭐⭐⭐ (5/5)
   - Zero surprises during development
   - All dependencies resolved upfront

3. **Capacity Planning Formula:** ⭐⭐⭐⭐⭐ (5/5)
   - 100% accuracy (21 pts planned = 21 delivered)
   - Establishes baseline for Sprint 12

4. **Technical Debt Register:** ⭐⭐⭐⭐⭐ (5/5)
   - ROI-based decisions prevent feature creep
   - Interest rate 0.5h/sprint (well below target)

5. **Leading Indicators:** ⭐⭐⭐⭐☆ (4/5)
   - Helpful for early problem detection
   - Need more sprint data for trends

---

## 🚀 Ready for Sprint 12

**Epic 2:** Transcription Pipeline (Whisper)
**Status:** 100% READY (thanks to Two-Track Agile)
**Start Date:** Day 11 (zero gap)

**Sprint 11 COMPLETE** ✅

---

**Estado:** EJEMPLO COMPLETO
**Uso:** Referencia para futuros sprints
**Actualizado:** 2025-10-20
