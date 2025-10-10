# Epic 3: Download & Audio Extraction - Validation Report

**Epic:** Epic 3 - Download & Audio Extraction
**Versión Target:** v2.3.0-download-audio
**Fecha Validación:** 8 de Octubre, 2025
**Status:** ⏳ PENDIENTE VALIDACIÓN

---

## 📋 Executive Summary

Epic 3 consiste en una única user story (YRUS-0103) con 8 story points estimados para implementar descarga de video y extracción de audio.

**Hallazgo preliminar:** Según SPRINT_2_EPIC_WORKFLOW.md, el `AudioExtractionService` **YA ESTÁ IMPLEMENTADO** con las siguientes capacidades:
- Descarga con YoutubeExplode
- Fallback a yt-dlp
- Conversión a WAV
- Gestión de archivos temporales

**Acción requerida:** Validar que la implementación existente cumple TODOS los AC de YRUS-0103.

---

## 🎯 User Story: YRUS-0103

### YRUS-0103: Descargar Video y Extraer Audio
**Story Points:** 8
**Prioridad:** Critical (P0)
**Epic:** Download & Audio Extraction

#### Historia de Usuario
**Como** sistema de procesamiento
**Quiero** descargar el video de YouTube y extraer su audio
**Para que** pueda transcribirlo usando Whisper

---

## ✅ Acceptance Criteria Analysis

### AC1: Descarga de Video con Streaming ⏳
**Criterios:**
- ✓ Selecciona stream de audio de mayor calidad
- ✓ Descarga usando streaming (no cargar todo en memoria)
- ✓ Almacena archivo temporal en `{TempPath}/{videoId}/{timestamp}.mp4`
- ? Actualiza VideoStatus = Downloading
- ? Reporta progreso cada 10 segundos vía Job progress

**Validación requerida:**
1. Verificar que `AudioExtractionService.ExtractAudioFromYouTubeAsync()` existe
2. Verificar implementación de streaming download
3. Verificar estructura de directorios temporales
4. **CRÍTICO:** Verificar integración con Job progress tracking
5. **CRÍTICO:** Verificar actualización de VideoStatus

**Archivo a revisar:** `YoutubeRag.Infrastructure/Services/AudioExtractionService.cs`

---

### AC2: Extracción de Audio con FFmpeg ⏳
**Criterios:**
- ✓ Usa FFmpeg para convertir a WAV
- ✓ Parámetros: `-i input.mp4 -vn -acodec pcm_s16le -ar 16000 -ac 1 output.wav`
- ✓ Normaliza a 16kHz mono (requerimiento de Whisper)
- ✓ Almacena en `{TempPath}/{videoId}/{timestamp}.wav`
- ? Actualiza VideoStatus = AudioExtracted
- ? Elimina archivo de video (.mp4) después de extracción

**Validación requerida:**
1. Verificar comando FFmpeg exacto
2. Verificar parámetros de normalización (16kHz, mono)
3. Verificar cleanup de archivo .mp4 después de extracción
4. **CRÍTICO:** Verificar actualización de VideoStatus

**Comando esperado:**
```bash
ffmpeg -i input.mp4 -vn -acodec pcm_s16le -ar 16000 -ac 1 output.wav
```

---

### AC3: Gestión de Archivos Temporales ⏳
**Criterios:**
- ✓ Crea directorio único por video: `{TempPath}/{videoId}/`
- ✓ Incluye timestamp en nombres: `{videoId}_{timestamp}.wav`
- ? Verifica espacio en disco antes de descargar
- ? Limpia archivos >24 horas automáticamente (Hangfire recurring job)
- ✓ Limpia archivos en caso de error (cleanup en catch block)

**Validación requerida:**
1. Verificar estructura de directorios
2. Verificar naming convention con timestamp
3. **CRÍTICO:** Verificar disk space check antes de descarga
4. **CRÍTICO:** Verificar Hangfire recurring job para cleanup (¿existe?)
5. Verificar error handling con cleanup

**Gap potencial:** Recurring job de cleanup puede no estar implementado.

---

### AC4: Tracking de Progreso ⏳
**Criterios:**
- ? Actualiza Job.ProgressPercentage cada 10 segundos
- ? Calcula % basado en bytes descargados / total size
- ? Estima tiempo restante basado en velocidad promedio
- ? Emite evento a SignalR hub (para UI futura)
- ? Persiste progreso en Job entity

**Validación requerida:**
1. **CRÍTICO:** Verificar integración con Job progress tracking
2. **CRÍTICO:** Verificar cálculo de % basado en bytes
3. **CRÍTICO:** Verificar emisión de eventos SignalR
4. **CRÍTICO:** Verificar persistencia en DB

**Gap muy probable:** Progress tracking granular durante descarga puede no estar implementado.

---

### AC5: Manejo de Errores y Retry ⏳
**Criterios:**
- ? Network timeout con retry (3 intentos)
- ? Disk space insufficient con error claro
- ? YouTube rate limit: espera y retry
- ? FFmpeg errors: log detallado
- ? Video privado/eliminado: error descriptivo
- ? Geo-restricted: intenta con proxy o error claro

**Validación requerida:**
1. **CRÍTICO:** Verificar retry logic (3 intentos para network)
2. **CRÍTICO:** Verificar disk space validation
3. Verificar manejo de rate limit
4. Verificar error messages descriptivos
5. Verificar logging de errores FFmpeg

**Gap probable:** Retry logic puede no estar implementado (esto puede ser parte de YRUS-0302).

---

## 🔍 Validación Técnica Requerida

### Paso 1: Leer AudioExtractionService completo
```bash
# Archivo a revisar
YoutubeRag.Infrastructure/Services/AudioExtractionService.cs
```

**Checklist de lectura:**
- [ ] ¿Método ExtractAudioFromYouTubeAsync existe?
- [ ] ¿Usa YoutubeExplode para descarga?
- [ ] ¿Tiene fallback a yt-dlp?
- [ ] ¿Usa FFmpeg para conversión?
- [ ] ¿Parámetros FFmpeg correctos (16kHz, mono, PCM)?
- [ ] ¿Gestión de archivos temporales?
- [ ] ¿Cleanup en caso de error?
- [ ] ¿Progress tracking integrado?
- [ ] ¿Actualiza VideoStatus?
- [ ] ¿Manejo de errores robusto?

---

### Paso 2: Buscar Progress Tracking Integration
```bash
# Buscar en TranscriptionJobProcessor o VideoProcessingBackgroundJob
grep -r "ProgressPercentage" YoutubeRag.Infrastructure/
grep -r "AudioExtractionService" YoutubeRag.Application/Services/
```

**Preguntas:**
- ¿Dónde se llama AudioExtractionService?
- ¿Se actualiza Job.ProgressPercentage durante descarga?
- ¿Se emiten eventos SignalR?

---

### Paso 3: Verificar Recurring Job de Cleanup
```bash
# Buscar en RecurringJobsSetup
grep -r "cleanup" YoutubeRag.Infrastructure/Jobs/RecurringJobsSetup.cs
```

**Preguntas:**
- ¿Existe job recurrente para limpiar archivos >24h?
- ¿Cuál es la frecuencia?

---

### Paso 4: Verificar Tests Existentes
```bash
# Buscar tests de AudioExtraction
find . -name "*AudioExtraction*Test*.cs"
```

**Preguntas:**
- ¿Existen tests unitarios?
- ¿Existen tests de integración?
- ¿Cobertura de casos de error?

---

## 🎯 Gaps Esperados (Predicción)

Basándome en la descripción de EPIC_WORKFLOW que dice "YA IMPLEMENTADO", pero analizando los AC detallados:

### GAP 1: Progress Tracking durante Descarga (ALTO)
**Probabilidad:** 80%
**Impacto:** Alto
**AC afectado:** AC4
**Descripción:** AudioExtractionService puede no reportar progreso granular durante descarga.

**Solución:**
- Integrar callback de progreso en YoutubeExplode
- Actualizar Job.ProgressPercentage cada 10s
- Emitir eventos SignalR

**Esfuerzo estimado:** 2-3 horas

---

### GAP 2: Disk Space Validation (MEDIO)
**Probabilidad:** 60%
**Impacto:** Medio
**AC afectado:** AC3
**Descripción:** Puede no verificar espacio en disco antes de descargar.

**Solución:**
- Usar `DriveInfo.AvailableSpace`
- Comparar con `video.FileSize * 2` (buffer)
- Lanzar excepción descriptiva si insuficiente

**Esfuerzo estimado:** 1 hora

---

### GAP 3: Recurring Job de Cleanup (MEDIO)
**Probabilidad:** 70%
**Impacto:** Medio
**AC afectado:** AC3
**Descripción:** Puede no haber job recurrente para limpiar archivos >24h.

**Solución:**
- Crear `TempFileCleanupJob` en Infrastructure/Jobs
- Registrar en `RecurringJobsSetup`
- Ejecutar diariamente a las 3 AM

**Esfuerzo estimado:** 2 horas

---

### GAP 4: Retry Logic para Network Errors (ALTO)
**Probabilidad:** 90%
**Impacto:** Alto
**AC afectado:** AC5
**Descripción:** Retry logic probablemente no implementado (puede ser parte de YRUS-0302).

**Decisión:**
- **Opción A:** Implementar retry básico ahora (3 intentos, exponential backoff)
- **Opción B:** Dejar para YRUS-0302 (Retry Logic epic)
- **Recomendación:** Opción B - No duplicar esfuerzo

**Esfuerzo si se hace ahora:** 3-4 horas
**Esfuerzo en YRUS-0302:** Ya planificado

---

### GAP 5: Error Handling Comprehensivo (BAJO)
**Probabilidad:** 40%
**Impacto:** Medio
**AC afectado:** AC5
**Descripción:** Puede faltar manejo específico de algunos casos (geo-restricted, rate limit).

**Solución:**
- Agregar try-catch específicos
- Mejorar mensajes de error
- Logging detallado

**Esfuerzo estimado:** 2 horas

---

## 📊 Resumen de Validación

### Implementación Estimada vs AC

| AC | Descripción | Estimado Implementado | Gaps Probables |
|----|-------------|----------------------|----------------|
| AC1 | Descarga con Streaming | 70% | Progress tracking |
| AC2 | Extracción FFmpeg | 90% | VideoStatus updates |
| AC3 | Gestión Temporal | 60% | Disk check, Cleanup job |
| AC4 | Progress Tracking | 30% | CRÍTICO - Probablemente falta |
| AC5 | Error Handling | 50% | Retry logic, casos edge |

**Implementación general estimada:** 60% completo
**Esfuerzo para completar:** 8-12 horas (sin retry logic)

---

## 🎯 Plan de Acción

### Opción A: Validación Completa + Completar Gaps
**Timeline:** 1.5-2 días
1. Leer AudioExtractionService (1 hora)
2. Identificar gaps exactos (1 hora)
3. Implementar GAP 1 (Progress) - 3 horas
4. Implementar GAP 2 (Disk check) - 1 hora
5. Implementar GAP 3 (Cleanup job) - 2 horas
6. Implementar GAP 5 (Error handling) - 2 horas
7. Testing + documentación - 3 horas
**Total:** 13 horas (~1.6 días)

### Opción B: Validación Rápida + MVP
**Timeline:** 0.5 días
1. Leer AudioExtractionService (1 hora)
2. Identificar gaps críticos (30 min)
3. Implementar solo bloqueantes P0 (2-3 horas)
4. Testing básico (1 hora)
**Total:** 5 horas (~0.6 días)

### Opción C: Validación + Delegar
**Timeline:** 0.25 días + agente paralelo
1. Leer y validar AC (2 horas)
2. Delegar gaps a backend-developer agent
3. Continuar con Epic 4 en paralelo
**Total:** 2 horas + background work

---

## 📝 Recomendación

**Opción Recomendada:** Opción C (Validación + Delegar)

**Justificación:**
1. **Velocidad:** Podemos validar en 2 horas y continuar
2. **Paralelismo:** Agente trabaja gaps mientras avanzamos Epic 4
3. **Eficiencia:** Aprovecha metodología de agentes en paralelo
4. **Calidad:** No sacrificamos completitud

**Next Steps:**
1. **AHORA (2 horas):** Leer y validar AudioExtractionService completamente
2. **SI HAY GAPS:** Delegar a backend-developer agent
3. **EN PARALELO:** Preparar validación Epic 4 (Background Jobs)
4. **DESPUÉS:** Testing manual Epic 3 cuando agente complete

---

## 🎯 Próxima Acción Inmediata

**ACCIÓN:** Leer `AudioExtractionService.cs` completo y crear reporte de gaps.

**Comando:**
```bash
Read YoutubeRag.Infrastructure/Services/AudioExtractionService.cs
```

**Output esperado:**
- Lista de AC cumplidos ✅
- Lista de gaps identificados ⚠️
- Prioridad de cada gap (P0/P1/P2)
- Esfuerzo estimado real

---

**STATUS:** ⏳ LISTO PARA VALIDACIÓN
**TARGET:** Completar validación hoy (8-Oct-2025)
**RELEASE TARGET:** v2.3.0-download-audio (10-Oct-2025)
