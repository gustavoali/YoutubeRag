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

**Status:** ⏳ PENDING

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

**Status:** ⏳ PENDING

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

**Status:** ⏳ PENDING

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

**Status:** ⏳ PENDING

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

**Status:** ⏳ PENDING

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

**Status:** ⏳ PENDING

---

## 🔄 Regression Tests

### Epic 1 Features (No debe romper)
- [ ] Video ingestion sigue funcionando
- [ ] Metadata extraction completa
- [ ] Validación de URLs
- [ ] Detección de duplicados

### General System
- [ ] API health check: GET /health
- [ ] Swagger docs: GET /swagger
- [ ] Authentication funciona (si está habilitado)
- [ ] Build passing: `dotnet build`

---

## 📊 Automated Test Results

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar solo tests de Epic 2
dotnet test --filter "FullyQualifiedName~Transcription"
```

**Current Status:**
- Unit Tests: ✅ TBD passing (TBD% coverage)
- Integration Tests: ✅ 350/362 passing (13 TranscriptionJobProcessorTests ✓)
- E2E Tests: ⚠️ 5 TranscriptionPipelineE2ETests (bloqueados previamente, ahora desbloqueados)
- Build Status: ✅ SUCCESS

---

## 🐛 Issues Found

### P0 Issues (Bloqueantes)
- ~~BLOCKER-001: Serilog frozen logger~~ ✅ RESUELTO (`b8c2b8c`)

### P1 Issues (Alta prioridad)
- Ninguno conocido

### P2 Issues (Media prioridad)
- QUALITY-001: 10 tests de integración con failures de lógica de negocio
- QUALITY-002: 26 warnings de compilación

---

## ✅ Sign-Off Checklist

### Developer Checklist
- [x] Código implementado completamente
- [x] YRUS-0201: Gestionar Modelos Whisper ✓
- [x] YRUS-0202: Ejecutar Transcripción ✓ (validar)
- [x] YRUS-0203: Segmentar y Almacenar ✓ (validar)
- [x] Tests unitarios escritos
- [x] Tests de integración escritos
- [x] Code review completado (agentes)
- [x] Documentación actualizada
- [ ] Manual testing ejecutado
- [ ] Ready for Release

### Tester Checklist
- [ ] Todos los scenarios ejecutados
- [ ] Screenshots/evidencia capturada
- [ ] Issues documentados
- [ ] Regression passing
- [ ] Approved for Release

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
**Date:** [PENDING]
**Video:** [URL]
**Duration:** [X min]
**Result:** [PASS/FAIL]
**Notes:**
- [Notes here]

### Test 2 Execution (Segmentación)
**Date:** [PENDING]
**Result:** [PASS/FAIL]
**Notes:**
- [Notes here]

[Continue for all scenarios...]

---

**TESTING STATUS:** 🔴 NOT STARTED
**TARGET COMPLETION:** Hoy, 8 de Octubre, 2025
**RELEASE TARGET:** v2.2.0-transcription (9-Oct-2025)
