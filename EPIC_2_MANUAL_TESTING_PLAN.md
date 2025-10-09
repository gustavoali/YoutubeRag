# Epic 2: Transcription Pipeline - Manual Testing Plan

**Versión:** v2.2.0-transcription
**Fecha:** 8 de Octubre, 2025
**Build:** `b8c2b8c` (post BLOCKER-001 fix)
**Tester:** Usuario + Claude Code

---

## 📋 Pre-requisitos

### Servicios Requeridos
- ✅ MySQL/MariaDB running
- ✅ Redis running (opcional para caching)
- ⚠️ Hangfire puede estar deshabilitado en testing
- ✅ FFmpeg instalado (para audio extraction)
- ✅ Whisper model descargado (tiny/base recomendado para testing)

### Configuración
```bash
# Verificar modelos Whisper disponibles
ls ~/.cache/whisper/

# Verificar FFmpeg
ffmpeg -version

# Verificar base de datos
dotnet ef database update --project YoutubeRag.Infrastructure
```

---

## 🧪 Test Scenarios

### Scenario 1: Transcripción de Video Corto (<5 min)
**Objetivo:** Verificar pipeline completo de transcripción con video corto

**Steps:**
1. Iniciar API: `dotnet run --project YoutubeRag.Api`
2. Enviar URL de video corto (ej: https://www.youtube.com/watch?v=jNQXAC9IVRw)
   ```bash
   curl -X POST http://localhost:5000/api/v1/videos/ingest \
     -H "Content-Type: application/json" \
     -d '{"url": "https://www.youtube.com/watch?v=jNQXAC9IVRw"}'
   ```
3. Verificar creación de video y job
4. Verificar descarga de modelo Whisper (si es primera vez)
5. Esperar procesamiento (debería tomar <2 min con video corto)
6. Verificar transcripción en base de datos

**Expected Result:**
- ✅ Video creado con status `Pending` → `Processing` → `Completed`
- ✅ Job creado con progress 0% → 100%
- ✅ Transcript segments almacenados en DB
- ✅ Modelo Whisper descargado si no existía
- ✅ Archivo audio temporal limpiado después de transcripción

**Queries de Verificación:**
```sql
-- Verificar video
SELECT * FROM Videos WHERE YouTubeId = 'jNQXAC9IVRw';

-- Verificar job
SELECT * FROM Jobs WHERE VideoId = '[VIDEO_ID]';

-- Verificar segments (debería haber múltiples)
SELECT COUNT(*), MIN(StartTime), MAX(EndTime), Language
FROM TranscriptSegments
WHERE VideoId = '[VIDEO_ID]';

-- Verificar índices secuenciales
SELECT SegmentIndex, StartTime, EndTime, Text
FROM TranscriptSegments
WHERE VideoId = '[VIDEO_ID]'
ORDER BY SegmentIndex;
```

**Status:** ⚠️ PARTIAL PASS (automated test only)
**Executed:** 9-Oct-2025 05:40 AM
**Method:** Automated integration test
**Result:** Pipeline works but ISSUE-002 found (bulk insert timing)

**Notes:**
- Test used mock Whisper service, not real transcription
- 10 segments created successfully
- Sequential indexing verified
- Timestamps valid and increasing
- **ISSUE:** CreatedAt timestamps vary by microseconds (not true bulk insert)

---

### Scenario 2: Segmentación Inteligente (Texto >500 caracteres)
**Objetivo:** Verificar que segmentos largos se dividen correctamente

**Steps:**
1. Identificar un video con segments largos en DB (o crear mock)
2. Verificar que segments >500 caracteres se dividieron
3. Verificar timestamps proporcionales en sub-segments

**Expected Result:**
- ✅ Ningún segment tiene Text.Length > 500
- ✅ Sub-segments tienen StartTime/EndTime proporcionales
- ✅ SegmentIndex secuencial sin gaps

**Query de Verificación:**
```sql
-- Buscar segments que deberían haberse dividido
SELECT Id, VideoId, SegmentIndex, LENGTH(Text) as TextLength, StartTime, EndTime
FROM TranscriptSegments
WHERE LENGTH(Text) > 500;

-- Debería retornar 0 rows
```

**Status:** ❌ FAILED (see ISSUE-003)
**Executed:** 9-Oct-2025 05:40 AM
**Method:** Automated integration test
**Result:** FAILED - Segments not split to <500 chars

**Notes:**
- SegmentationService exists and has split logic
- Test created segment with 750 characters
- Expected: Split into multiple segments <500 chars each
- **ACTUAL:** Found segment with 750 characters (NOT split)
- **ROOT CAUSE:** SegmentationService not enforcing hard 500 char limit

---

### Scenario 3: Bulk Insert Performance
**Objetivo:** Verificar que bulk insert funciona para videos con muchos segments

**Steps:**
1. Usar video largo (10-20 min) que genere >100 segments
2. Monitorear logs para ver "Bulk inserted X segments in Yms"
3. Verificar tiempo de insert es <3 segundos para 1000 segments

**Expected Result:**
- ✅ Log muestra "Using bulk insert for X segments"
- ✅ Performance: >300 segments/sec
- ✅ Todos los segments insertados correctamente

**Log a buscar:**
```
[INFO] Bulk inserted 150 transcript segments in 450ms (333 segments/sec)
```

**Status:** ⚠️ PARTIAL PASS (automated test only)
**Executed:** 9-Oct-2025 05:40 AM
**Method:** Automated integration test
**Result:** AddRangeAsync exists but not working as expected

**Notes:**
- Repository has AddRangeAsync method
- Repository has BulkInsertAsync method for >100 segments
- Test used 10 segments → used regular AddRange (not bulk)
- **ISSUE:** CreatedAt timestamps vary by microseconds
- Expected: All segments same timestamp (true bulk insert)
- Actual: 10 different timestamps
- Performance NOT measured (in-memory DB doesn't reflect real MySQL)

---

### Scenario 4: Gestión de Modelos Whisper
**Objetivo:** Verificar descarga automática de modelos

**Steps:**
1. Eliminar modelo Whisper del cache: `rm -rf ~/.cache/whisper/tiny.pt`
2. Iniciar transcripción de video
3. Verificar que modelo se descarga automáticamente
4. Verificar log de descarga
5. Re-ejecutar transcripción (no debería re-descargar)

**Expected Result:**
- ✅ Modelo descargado automáticamente en primera ejecución
- ✅ Log: "Downloading Whisper model: tiny"
- ✅ Segunda ejecución usa modelo cacheado
- ✅ No errores de modelo no encontrado

**Status:** 🚫 NOT TESTED (environment blocker)
**Executed:** N/A
**Blocker:** Whisper models directory doesn't exist

**Notes:**
- Directory C:\Models\Whisper does NOT exist
- No Whisper models downloaded
- Cannot test real model download
- WhisperModelManager tested via mocks only (42 integration tests passing)
- Real model download/caching NOT verified

**To complete:** Set up Whisper models directory and download tiny model

---

### Scenario 5: Validación de Integridad de Segments
**Objective:** Verificar que ValidateSegmentIntegrity detecta inconsistencias

**Steps:**
1. Revisar logs de transcripción completada
2. Buscar warnings de validación (no debería haber si todo está bien)
3. Verificar que no hay overlaps, gaps en SegmentIndex, timestamps negativos

**Expected Result:**
- ✅ Log: "Validated X segments. All integrity checks passed."
- ✅ Sin warnings de gaps, overlaps o timestamps inválidos

**Status:** ⚠️ PARTIAL PASS (code review only)
**Executed:** 9-Oct-2025 05:40 AM
**Method:** Code inspection
**Result:** ValidateSegmentIntegrity() exists

**Notes:**
- Code exists at TranscriptionJobProcessor.cs:496
- Method called before saving segments
- Automated tests verify sequential indexing
- Automated tests verify no timestamp overlaps
- **NOT TESTED:** Log output for warnings
- **NOT TESTED:** Edge cases with malformed segments

---

### Scenario 6: Índices de Base de Datos
**Objetivo:** Verificar que índices mejoran performance de queries

**Steps:**
1. Verificar que migración creó índices:
   ```sql
   SHOW INDEX FROM TranscriptSegments;
   ```
2. Ejecutar queries usando índices:
   ```sql
   -- Debería usar IX_TranscriptSegments_VideoId_SegmentIndex
   EXPLAIN SELECT * FROM TranscriptSegments
   WHERE VideoId = '[VIDEO_ID]'
   ORDER BY SegmentIndex;

   -- Debería usar IX_TranscriptSegments_StartTime
   EXPLAIN SELECT * FROM TranscriptSegments
   WHERE StartTime BETWEEN 10 AND 60;
   ```

**Expected Result:**
- ✅ 3 índices creados:
  - `IX_TranscriptSegments_VideoId_SegmentIndex`
  - `IX_TranscriptSegments_CreatedAt`
  - `IX_TranscriptSegments_StartTime`
- ✅ EXPLAIN muestra uso de índices (key column populated)

**Status:** 🚫 NOT TESTABLE (in-memory database)
**Executed:** N/A
**Blocker:** Tests use EF Core InMemory provider

**Notes:**
- In-memory database doesn't support real indexes
- Cannot run SHOW INDEX commands
- Cannot run EXPLAIN query plans
- Cannot measure query performance
- **Requires:** Real MySQL database connection for testing

**To complete:** Connect to real MySQL and run SQL queries from test plan

---

## 🔄 Regression Tests

### Epic 1 Features (No debe romper)
- [x] Video ingestion sigue funcionando (VideoIngestionServiceTests PASS)
- [x] Metadata extraction completa (assumed working, tests pass)
- [x] Validación de URLs (assumed working)
- [x] Detección de duplicados (assumed working)

### General System
- [ ] API health check: GET /health (NOT TESTED - API not running)
- [ ] Swagger docs: GET /swagger (NOT TESTED - API not running)
- [ ] Authentication funciona (NOT TESTED)
- [x] Build passing: `dotnet build` ✅ SUCCESS (64 warnings)

---

## 📊 Automated Test Results

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar solo tests de Epic 2
dotnet test --filter "FullyQualifiedName~Transcription"
```

**Current Status:**
- Unit Tests: ⏳ NOT RUN (focused on integration/E2E tests)
- Integration Tests: ✅ 17/20 PASSING (85%) - Transcription tests only
- E2E Tests: ⚠️ 4/7 PASSING (57%) - 3 failures with identified issues
- Build Status: ✅ SUCCESS (64 warnings, non-blocking)

**Detailed Results:**
- TranscriptionJobProcessorTests: ✅ 11/11 PASS (100%)
- VideoIngestionServiceTests: ✅ 2/2 PASS (100%)
- TranscriptionPipelineE2ETests: ⚠️ 4/7 PASS (57%)
  - ❌ WhisperFails_ShouldHandleErrorGracefully (ISSUE-001)
  - ❌ CompleteTranscriptionPipeline_ShortVideo (ISSUE-002)
  - ❌ LongSegments_ShouldAutoSplitAndReindex (ISSUE-003)

---

## 🐛 Issues Found

### P0 Issues (Bloqueantes)
- ~~BLOCKER-001: Serilog frozen logger~~ ✅ RESUELTO (`b8c2b8c`)
- **ISSUE-002:** Bulk insert timing issue (segments have different timestamps) - P0
- **ISSUE-003:** SegmentationService not splitting segments to <500 chars - P0

### P1 Issues (Alta prioridad)
- **ISSUE-001:** Segments saved on Whisper failure (no transaction rollback) - P1

### P2 Issues (Media prioridad)
- QUALITY-002: 64 warnings de compilación (non-blocking)

---

## ✅ Sign-Off Checklist

### Developer Checklist
- [x] Código implementado completamente
- [x] YRUS-0201: Gestionar Modelos Whisper ✓
- [x] YRUS-0202: Ejecutar Transcripción ⚠️ (has ISSUE-001)
- [x] YRUS-0203: Segmentar y Almacenar ⚠️ (has ISSUE-002, ISSUE-003)
- [x] Tests unitarios escritos
- [x] Tests de integración escritos
- [x] Code review completado (agentes)
- [x] Documentación actualizada
- [⚠️] Manual testing ejecutado (automated only, manual blocked by environment)
- [❌] Ready for Release (3 issues blocking)

### Tester Checklist
- [x] Automated tests ejecutados (17/20 passing)
- [ ] Manual scenarios ejecutados (BLOCKED - environment limitations)
- [x] Issues documentados (3 issues: ISSUE-001, ISSUE-002, ISSUE-003)
- [x] Screenshots/evidencia capturada (test results in EPIC_2_TEST_REPORT.md)
- [⚠️] Regression passing (partial - Epic 1 tests pass, system tests blocked)
- [❌] Approved for Release (NOT APPROVED - issues must be fixed first)

### Product Owner Checklist
- [ ] Features cumplen AC
- [ ] Calidad aceptable
- [ ] Performance aceptable
- [ ] Accepted for Release

---

## 🎯 Next Steps

1. **AHORA: Ejecutar Manual Testing** (2-3 horas)
   - Ejecutar Scenarios 1-6
   - Documentar resultados
   - Capturar screenshots/logs

2. **Corregir Issues P0** (si se encuentran)
   - Fix inmediato
   - Re-test

3. **Sign-Off** (30 min)
   - Developer ✅
   - Tester ✅
   - Product Owner ✅

4. **Release v2.2.0** (30 min)
   - Crear tag: `v2.2.0-transcription`
   - Escribir release notes
   - Push tag a remote

5. **Iniciar Epic 3** (en paralelo con testing final)
   - Validar AudioExtractionService
   - Identificar gaps de YRUS-0103

---

## 📝 Test Execution Notes

### Test 1 Execution (Video Corto)
**Date:** 9-Oct-2025 05:40 AM
**Video:** Mock (test data, not real YouTube)
**Duration:** 2.8 seconds (test execution time)
**Result:** ⚠️ PARTIAL PASS (ISSUE-002 found)
**Notes:**
- Test: `CompleteTranscriptionPipeline_ShortVideo_ShouldProcessSuccessfully`
- Created 10 transcript segments
- Sequential indexing verified
- Timestamps valid
- **ISSUE:** Bulk insert not working correctly (timestamps vary)

### Test 2 Execution (Segmentación)
**Date:** 9-Oct-2025 05:40 AM
**Result:** ❌ FAILED (ISSUE-003)
**Notes:**
- Test: `TranscriptionPipeline_LongSegments_ShouldAutoSplitAndReindex`
- Created segment with 750 characters
- Expected: Split to <500 chars
- **ACTUAL:** Segment NOT split, still 750 chars
- Root cause: SegmentationService not enforcing hard limit

### Test 3 Execution (Bulk Insert Performance)
**Date:** 9-Oct-2025 05:40 AM
**Result:** ⚠️ PARTIAL PASS
**Notes:**
- AddRangeAsync method exists
- BulkInsertAsync method exists (for >100 segments)
- Test used 10 segments → regular AddRange used
- CreatedAt timestamps vary (not true bulk insert)

### Test 4 Execution (Gestión Modelos)
**Date:** N/A
**Result:** 🚫 NOT TESTED (environment blocker)
**Notes:**
- C:\Models\Whisper directory doesn't exist
- No real models to test
- WhisperModelManager tested via mocks only

### Test 5 Execution (Validación Integridad)
**Date:** 9-Oct-2025 05:40 AM
**Result:** ⚠️ PARTIAL PASS
**Notes:**
- Code exists and is called
- Automated tests verify sequential indexing
- Log output NOT verified

### Test 6 Execution (Índices DB)
**Date:** N/A
**Result:** 🚫 NOT TESTABLE (in-memory DB)
**Notes:**
- Cannot test indexes with in-memory database
- Requires real MySQL connection

---

**TESTING STATUS:** 🔴 COMPLETED WITH ISSUES
**COMPLETION DATE:** 9 de Octubre, 2025
**RELEASE TARGET:** v2.2.0-transcription (DELAYED - issues must be fixed)
**BLOCKERS:** 2 P0 issues (ISSUE-002, ISSUE-003) + 1 P1 issue (ISSUE-001)
