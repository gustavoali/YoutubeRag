# Sprint 2 - Historias de Usuario
# YouTube RAG .NET Project

**Versión del Documento:** 1.0
**Sprint:** Sprint 2 (Semana 2)
**Fechas:** 7 de Octubre - 17 de Octubre, 2025
**Duración:** 10 días laborables
**Product Owner:** Senior Product Owner
**Technical Lead:** Desarrollador Principal
**Objetivo del Sprint:** Implementar el pipeline completo de procesamiento de videos

---

## Resumen Ejecutivo

Sprint 2 se centra en la **implementación del core del sistema**: el pipeline completo de procesamiento de videos desde la ingesta hasta la generación de embeddings. Este sprint entrega las funcionalidades MVP críticas que permiten procesar videos de YouTube, transcribirlos usando Whisper local, y almacenar los segmentos con embeddings para futuras búsquedas semánticas.

### Alcance del Sprint

- **6 Epics:** Video Ingestion, Download & Audio, Transcription, Embeddings, Background Jobs, Progress Tracking
- **11 Historias de Usuario:** De YRUS-0101 a YRUS-0402
- **Total Story Points:** 58 puntos
- **Commitment:** 100% de las historias (trabajo individual intensivo)

### Métricas de Éxito

| Métrica | Target | Medición |
|---------|--------|----------|
| Historias Completadas | 11/11 (100%) | Checklist de DoD |
| Test Coverage | 80%+ en paths críticos | Coverage reports |
| Build Success | 0 errores, 0 warnings | dotnet build |
| Processing Performance | <2x duración del video | Benchmarks |
| Manual Testing | 100% AC validados | Test reports |
| P0 Bugs | 0 | Issue tracker |

---

## Distribución de Story Points por Epic

```
Epic 1: Video Ingestion (18 pts)     ████████████████░░
Epic 2: Download & Audio (8 pts)      ███████░░░░░░░░░░░
Epic 3: Transcription (18 pts)        ████████████████░░
Epic 4: Embeddings (8 pts)            ███████░░░░░░░░░░░
Epic 5: Background Jobs (8 pts)       ███████░░░░░░░░░░░
Epic 6: Progress Tracking (8 pts)     ███████░░░░░░░░░░░
─────────────────────────────────────────────────────────
TOTAL: 58 Story Points
```

---

## Epic 1: Video Ingestion Pipeline

**Prioridad:** MUST HAVE
**RICE Score:** 187.5
**Business Value:** Funcionalidad core que habilita todo el sistema
**Story Points Totales:** 18

### Objetivo del Epic

Permitir a los usuarios enviar URLs de YouTube para procesamiento, validando la URL, extrayendo metadata inicial, detectando duplicados y creando los trabajos de background necesarios.

---

## YRUS-0101: Enviar URL de YouTube para Procesamiento

**Story Points:** 5
**Prioridad:** Critical (P0)
**Sprint:** 2
**Epic:** Video Ingestion
**Rama Git:** `YRUS-0101_enviar_url_youtube`

### Historia de Usuario

**Como** usuario del sistema
**Quiero** enviar una URL de YouTube para procesamiento
**Para que** el sistema descargue, transcriba y almacene el contenido del video

### Acceptance Criteria

**AC1: Validación de URL de YouTube**
- Given un usuario envía una URL
- When el sistema procesa la solicitud
- Then valida que sea una URL de YouTube válida (youtube.com o youtu.be)
- And rechaza URLs no válidas con mensaje de error claro: "URL inválida. Debe ser un video de YouTube."
- And acepta formatos: `https://www.youtube.com/watch?v=VIDEO_ID` y `https://youtu.be/VIDEO_ID`
- And extrae correctamente el VIDEO_ID en ambos formatos

**AC2: Detección de Duplicados**
- Given una URL de YouTube enviada
- When el VIDEO_ID ya existe en la base de datos
- Then retorna el registro de Video existente
- And NO crea un nuevo Job de procesamiento
- And retorna mensaje: "Este video ya ha sido procesado. ID: {videoId}"
- And incluye el estado actual del video (Processing, Completed, Failed)

**AC3: Extracción de Metadata Inicial**
- Given una URL de YouTube válida y nueva
- When el sistema acepta la URL
- Then extrae metadata usando YoutubeExplode: título, duración, autor, thumbnail URL
- And almacena metadata en entidad Video
- And establece VideoStatus = Pending
- And retorna VideoId inmediatamente al usuario (respuesta sincrónica)

**AC4: Creación de Job de Background**
- Given metadata extraída exitosamente
- When se inicia el procesamiento
- Then crea entidad Job con JobType = VideoProcessing
- And establece JobStatus = Queued
- And vincula Job.VideoId al Video creado (FK constraint)
- And encola job en Hangfire queue "video-processing"
- And retorna JobId al usuario para tracking

**AC5: Manejo de Errores**
- Given diferentes escenarios de error
- When ocurren fallas
- Then maneja rate limiting de YouTube con retry (3 intentos, exponential backoff)
- And maneja videos privados/eliminados con error: "Video no disponible"
- And maneja timeouts de red con retry (max 3 intentos)
- And registra errores detallados en logs estructurados
- And retorna HTTP 400 para errores de usuario, HTTP 500 para errores de sistema

### Technical Notes

**Stack Tecnológico:**
- YoutubeExplode 6.x para interacción con YouTube API
- FluentValidation para validación de DTOs
- AutoMapper para mapeo Video → VideoDto
- Polly para retry policies (3 intentos, exponential backoff)
- MySQL para almacenamiento (Transaction scope para Video + Job)

**Ubicación de Archivos:**
```
YoutubeRag.Application/
├── DTOs/Video/
│   ├── VideoUrlRequest.cs (ya existe)
│   └── VideoIngestionResponse.cs (ya existe)
├── Interfaces/Services/
│   └── IVideoIngestionService.cs (ya existe)
├── Services/
│   └── VideoIngestionService.cs (ya existe - ACTUALIZAR)
└── Validators/Video/
    └── VideoUrlRequestValidator.cs (CREAR)

YoutubeRag.Api/
└── Controllers/
    └── VideoIngestionController.cs (CREAR)
```

**Constraints de Base de Datos:**
- Video.YoutubeId: UNIQUE constraint
- Job.VideoId: FK constraint con ON DELETE CASCADE
- Job.UserId: FK constraint (auto-create test user si no existe)

**Polly Retry Policy:**
```csharp
// Retry para network errors
Policy
  .Handle<HttpRequestException>()
  .WaitAndRetryAsync(3, retryAttempt =>
    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Retry para YouTube rate limiting
Policy
  .Handle<RateLimitException>()
  .WaitAndRetryAsync(3, retryAttempt =>
    TimeSpan.FromSeconds(60 * retryAttempt));
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] VideoUrlRequestValidator con FluentValidation
  - [ ] VideoIngestionService.IngestVideoAsync() implementado
  - [ ] VideoIngestionController con endpoint POST /api/videos/ingest
  - [ ] Integración con YoutubeExplode completa
  - [ ] Retry logic con Polly configurado
  - [ ] Transaction scope para Video + Job
  - [ ] Code review completado

- [ ] **Testing**
  - [ ] Unit tests para VideoUrlRequestValidator (10+ test cases)
  - [ ] Unit tests para YouTubeUrlParser (validación de formatos)
  - [ ] Integration tests para VideoIngestionService (mock YouTube API)
  - [ ] Integration tests para API endpoint (TestServer)
  - [ ] Manual testing con 10+ URLs diferentes:
    - URLs válidas (youtube.com y youtu.be)
    - URLs inválidas
    - Videos privados/eliminados
    - Duplicados
    - Rate limiting (simulated)
  - [ ] Test coverage >80% en VideoIngestionService

- [ ] **Build y Deployment**
  - [ ] Build exitoso: `dotnet build --no-incremental` (0 errores, 0 warnings)
  - [ ] Swagger documentation actualizada
  - [ ] Postman collection actualizada con ejemplo
  - [ ] appsettings.json configurado para Local/Staging

- [ ] **Documentación**
  - [ ] XML comments en interfaces y clases públicas
  - [ ] README actualizado con ejemplo de ingesta
  - [ ] Error codes documentados

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Cada AC testeado manualmente
  - [ ] Test report documentado con screenshots
  - [ ] Edge cases validados
  - [ ] Product Owner sign-off

### Dependencias

- **Bloqueantes:** Ninguna (historia inicial del sprint)
- **Bloquea a:** YRUS-0102 (requiere VideoId)

### Estimación de Esfuerzo

- **Desarrollo:** 4 horas
- **Testing:** 2 horas
- **Code Review:** 1 hora
- **Manual Testing:** 1 hora
- **Total:** 8 horas (~1 día de trabajo)

---

## YRUS-0102: Extraer Metadata Completa de YouTube

**Story Points:** 5
**Prioridad:** Critical (P0)
**Sprint:** 2
**Epic:** Video Ingestion
**Rama Git:** `YRUS-0102_extraer_metadata_completa`

### Historia de Usuario

**Como** sistema de procesamiento
**Quiero** extraer metadata completa del video de YouTube
**Para que** tenga toda la información necesaria antes de descargar y procesar

### Acceptance Criteria

**AC1: Extracción de Metadata Extendida**
- Given un VIDEO_ID válido
- When se extrae metadata del video
- Then obtiene: título, descripción, canal/autor, duración (segundos), thumbnail URL (maxresdefault)
- And obtiene: fecha de publicación, número de vistas, categoría
- And obtiene: tags/keywords (si disponibles)
- And obtiene: formato de audio preferido (bitrate más alto)
- And almacena toda metadata en campos correspondientes de Video entity

**AC2: Actualización de Video Entity**
- Given metadata extraída exitosamente
- When se almacena en base de datos
- Then actualiza Video.Title, Video.Description, Video.Author
- And actualiza Video.DurationSeconds, Video.ThumbnailUrl
- And actualiza Video.PublishedAt, Video.ViewCount
- And actualiza Video.UpdatedAt = DateTime.UtcNow
- And actualiza VideoStatus = MetadataExtracted

**AC3: Validación de Metadata**
- Given metadata extraída
- When se valida antes de almacenar
- Then verifica que duración sea > 0 y < 14400 segundos (4 horas max)
- And verifica que título no esté vacío
- And verifica que thumbnail URL sea válida
- And rechaza videos demasiado largos con error claro
- And registra warning si faltan campos opcionales (tags, description)

**AC4: Manejo de Videos No Disponibles**
- Given diferentes estados de disponibilidad del video
- When se intenta extraer metadata
- Then detecta videos privados y retorna error específico
- And detecta videos eliminados y retorna error específico
- And detecta videos con restricciones geográficas
- And detecta videos con restricciones de edad
- And actualiza JobStatus = Failed con mensaje descriptivo

**AC5: Performance y Retry**
- Given llamadas a YouTube API
- When hay fallas transitorias
- Then implementa timeout de 30 segundos
- And implementa retry (3 intentos) con exponential backoff
- And cachea metadata por 5 minutos (evitar llamadas redundantes)
- And completa extracción en <5 segundos para 95% de videos

### Technical Notes

**Stack Tecnológico:**
- YoutubeExplode 6.x para metadata extraction
- Polly para retry policies y timeouts
- IMemoryCache para caching temporal de metadata
- MySQL para almacenamiento persistente

**Ubicación de Archivos:**
```
YoutubeRag.Application/
├── Interfaces/
│   └── IMetadataExtractionService.cs (ya existe)
├── Services/
│   └── MetadataExtractionService.cs (CREAR)
└── DTOs/Video/
    └── VideoMetadataDto.cs (ya existe)

YoutubeRag.Infrastructure/
└── Services/
    └── YouTubeMetadataService.cs (CREAR - wrapper de YoutubeExplode)
```

**Configuración (appsettings.json):**
```json
{
  "YouTube": {
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "MaxVideoDurationSeconds": 14400,
    "MetadataCacheDurationMinutes": 5
  }
}
```

**Video Entity Updates:**
```csharp
public class Video : BaseEntity
{
    // Existing fields
    public string YoutubeId { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string Author { get; set; }
    public int DurationSeconds { get; set; }
    public string? ThumbnailUrl { get; set; }

    // New fields for extended metadata
    public DateTime? PublishedAt { get; set; }
    public long? ViewCount { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; } // JSON array
    public VideoStatus Status { get; set; } // Add MetadataExtracted
}
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] IMetadataExtractionService interface definida
  - [ ] MetadataExtractionService implementado
  - [ ] YouTubeMetadataService wrapper de YoutubeExplode
  - [ ] Retry policy con Polly configurado
  - [ ] Caching con IMemoryCache implementado
  - [ ] Validación de metadata implementada
  - [ ] Migration para nuevos campos de Video
  - [ ] Code review completado

- [ ] **Testing**
  - [ ] Unit tests para MetadataExtractionService (mock YouTube)
  - [ ] Unit tests para validación de metadata
  - [ ] Integration tests con videos reales (usando TestContainers si necesario)
  - [ ] Manual testing con:
    - Videos públicos estándar
    - Videos muy largos (>4 horas)
    - Videos privados
    - Videos eliminados
    - Videos con restricciones
  - [ ] Test coverage >85% en MetadataExtractionService

- [ ] **Build y Deployment**
  - [ ] Build exitoso sin warnings
  - [ ] Migration ejecutada en Local/Staging
  - [ ] Configuración documentada en appsettings

- [ ] **Documentación**
  - [ ] XML comments completos
  - [ ] README actualizado con campos de metadata
  - [ ] Error codes documentados

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Test report con resultados de AC1-AC5
  - [ ] Evidencia de caching funcionando
  - [ ] Performance benchmarks documentados

### Dependencias

- **Bloqueantes:** YRUS-0101 (requiere VideoId creado)
- **Bloquea a:** YRUS-0103 (metadata necesaria para download)

### Estimación de Esfuerzo

- **Desarrollo:** 4 horas
- **Testing:** 2 horas
- **Migration:** 1 hora
- **Code Review + Manual Testing:** 1 hora
- **Total:** 8 horas (~1 día de trabajo)

---

## YRUS-0103: Descargar Video y Extraer Audio

**Story Points:** 8
**Prioridad:** Critical (P0)
**Sprint:** 2
**Epic:** Download & Audio Extraction
**Rama Git:** `YRUS-0103_descargar_extraer_audio`

### Historia de Usuario

**Como** sistema de procesamiento
**Quiero** descargar el video de YouTube y extraer su audio
**Para que** pueda transcribirlo usando Whisper

### Acceptance Criteria

**AC1: Descarga de Video con Streaming**
- Given un VIDEO_ID con metadata extraída
- When se inicia la descarga
- Then selecciona el stream de audio de mayor calidad disponible
- And descarga usando streaming (no cargar todo en memoria)
- And almacena archivo temporal en `{TempPath}/{videoId}/{timestamp}.mp4`
- And actualiza VideoStatus = Downloading
- And reporta progreso cada 10 segundos vía Job progress

**AC2: Extracción de Audio con FFmpeg**
- Given un video descargado exitosamente
- When se extrae el audio
- Then usa FFmpeg para convertir a WAV: `-i input.mp4 -vn -acodec pcm_s16le -ar 16000 -ac 1 output.wav`
- And normaliza a 16kHz mono (requerimiento de Whisper)
- And almacena audio en `{TempPath}/{videoId}/{timestamp}.wav`
- And actualiza VideoStatus = AudioExtracted
- And elimina archivo de video (.mp4) después de extracción exitosa

**AC3: Gestión de Archivos Temporales**
- Given archivos temporales creados
- When se gestionan
- Then crea directorio único por video: `{TempPath}/{videoId}/`
- And incluye timestamp en nombres: `{videoId}_{timestamp}.wav`
- And verifica espacio en disco antes de descargar (requiere ~2x tamaño video)
- And limpia archivos >24 horas automáticamente (Hangfire recurring job)
- And limpia archivos en caso de error (cleanup en catch block)

**AC4: Tracking de Progreso**
- Given operación de descarga en curso
- When reporta progreso
- Then actualiza Job.ProgressPercentage cada 10 segundos
- And calcula % basado en bytes descargados / total size
- And estima tiempo restante basado en velocidad promedio
- And emite evento a SignalR hub (para UI futura)
- And persiste progreso en Job entity

**AC5: Manejo de Errores y Retry**
- Given diferentes escenarios de error
- When ocurren fallas
- Then maneja network timeout con retry (3 intentos)
- And maneja disk space insufficient con error claro
- And maneja FFmpeg no encontrado con error descriptivo
- And maneja video format incompatible con fallback
- And actualiza JobStatus = Failed con error message
- And preserva archivos parciales para debugging (primeras 24h)

### Technical Notes

**Stack Tecnológico:**
- YoutubeExplode para download de video
- FFMpegCore NuGet package para FFmpeg wrapper
- Polly para retry policies
- System.IO para file management
- Hangfire para cleanup job

**Ubicación de Archivos:**
```
YoutubeRag.Application/
├── Interfaces/
│   ├── IVideoDownloadService.cs (CREAR)
│   └── IAudioExtractionService.cs (ya existe)
└── Services/
    └── VideoDownloadOrchestrator.cs (CREAR - coordina download + audio)

YoutubeRag.Infrastructure/
└── Services/
    ├── YouTubeDownloadService.cs (CREAR)
    ├── FFmpegAudioExtractionService.cs (CREAR)
    └── TempFileManagementService.cs (CREAR)
```

**Configuración (appsettings.json):**
```json
{
  "Processing": {
    "TempFilePath": "C:\\Temp\\YoutubeRag",
    "MaxVideoSizeMB": 2048,
    "FFmpegPath": "ffmpeg",
    "CleanupAfterHours": 24,
    "MinDiskSpaceGB": 5
  }
}
```

**FFmpeg Command:**
```bash
ffmpeg -i "{tempVideoPath}" -vn -acodec pcm_s16le -ar 16000 -ac 1 "{tempAudioPath}"
```

**Hangfire Cleanup Job:**
```csharp
RecurringJob.AddOrUpdate<TempFileCleanupJob>(
    "cleanup-temp-files",
    x => x.CleanupOldFiles(),
    Cron.Hourly);
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] IVideoDownloadService implementado
  - [ ] YouTubeDownloadService con streaming download
  - [ ] FFmpegAudioExtractionService con normalización
  - [ ] TempFileManagementService con cleanup
  - [ ] VideoDownloadOrchestrator coordinando todo
  - [ ] Progress tracking integrado
  - [ ] Retry logic con Polly
  - [ ] Hangfire cleanup job configurado
  - [ ] Code review completado

- [ ] **Testing**
  - [ ] Unit tests para FFmpegAudioExtractionService
  - [ ] Unit tests para TempFileManagementService
  - [ ] Integration tests con video real (corto, <1 min)
  - [ ] Integration tests para error scenarios
  - [ ] Manual testing:
    - Video corto (<5 min)
    - Video mediano (10-30 min)
    - Video largo (>1 hora)
    - Network interruption (simulated)
    - Disk space insufficient (simulated)
    - FFmpeg not found
  - [ ] Test coverage >80%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] FFmpeg instalado y accesible en PATH
  - [ ] Directorio temp configurado y accesible
  - [ ] Cleanup job registrado en Hangfire

- [ ] **Performance**
  - [ ] Descarga completa en tiempo razonable
  - [ ] Extracción de audio <10% duración del video
  - [ ] Memory usage razonable (streaming, no full load)
  - [ ] Benchmarks documentados

- [ ] **Documentación**
  - [ ] XML comments completos
  - [ ] README con instrucciones FFmpeg setup
  - [ ] Troubleshooting guide para errores comunes

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Test report completo por AC
  - [ ] Screenshots de archivos generados
  - [ ] Verificación de cleanup funcionando

### Dependencias

- **Bloqueantes:** YRUS-0102 (requiere metadata completa)
- **Bloquea a:** YRUS-0201 (audio WAV necesario para transcripción)
- **Dependencias Externas:** FFmpeg instalado en sistema

### Estimación de Esfuerzo

- **Desarrollo:** 6 horas
- **Testing:** 3 horas
- **Setup FFmpeg + Config:** 1 hora
- **Code Review + Manual Testing:** 2 horas
- **Total:** 12 horas (~1.5 días de trabajo)

---

## Epic 2: Transcription Pipeline

**Prioridad:** MUST HAVE
**RICE Score:** 165.0
**Business Value:** Core feature que genera contenido searchable
**Story Points Totales:** 18

### Objetivo del Epic

Implementar la transcripción de audio usando Whisper local, gestionando modelos, ejecutando transcripción y almacenando segmentos con timestamps para búsqueda futura.

---

## YRUS-0201: Gestionar Modelos de Whisper

**Story Points:** 5
**Prioridad:** Critical (P0)
**Sprint:** 2
**Epic:** Transcription Pipeline
**Rama Git:** `YRUS-0201_gestionar_modelos_whisper`

### Historia de Usuario

**Como** sistema de transcripción
**Quiero** gestionar automáticamente los modelos de Whisper
**Para que** la transcripción funcione sin setup manual

### Acceptance Criteria

**AC1: Detección de Modelos Instalados**
- Given el sistema inicia o solicita transcripción
- When verifica modelos disponibles
- Then busca modelos en directorio configurado: `{ModelsPath}/whisper/`
- And lista modelos encontrados: tiny, base, small, medium, large
- And valida integridad de cada modelo (checksum si disponible)
- And registra en logs modelos disponibles
- And cachea lista de modelos (evitar escaneo repetido)

**AC2: Descarga Automática de Modelos**
- Given un modelo requerido no está disponible
- When se solicita por primera vez
- Then descarga modelo desde OpenAI CDN o Hugging Face
- And muestra progreso de descarga en logs
- And valida checksum después de descarga
- And almacena en `{ModelsPath}/whisper/{modelName}/`
- And registra descarga exitosa
- And falla job si descarga falla (no continuar sin modelo)

**AC3: Selección Automática de Modelo**
- Given un video con duración conocida
- When selecciona modelo para transcripción
- Then usa "tiny" para videos <10 minutos (39 MB, más rápido)
- And usa "base" para videos 10-30 minutos (74 MB, balance)
- And usa "small" para videos >30 minutos (244 MB, mejor calidad)
- And permite override via configuración: `ForceModel = "base"`
- And registra modelo seleccionado en Job metadata

**AC4: Gestión de Almacenamiento**
- Given modelos descargados
- When gestiona espacio
- Then verifica espacio disponible antes de descargar (requiere 2x tamaño modelo)
- And reutiliza modelos existentes (no re-descargar)
- And limpia modelos no usados por >30 días (configurable)
- And mantiene al menos "tiny" siempre disponible
- And registra warning si espacio <10 GB

**AC5: Manejo de Errores**
- Given diferentes escenarios de error
- When ocurren fallas
- Then maneja download failure con retry (3 intentos)
- And maneja checksum mismatch eliminando archivo corrupto
- And maneja disk space insufficient con error claro
- And maneja modelo corrupto intentando re-descarga
- And actualiza JobStatus = Failed si no puede obtener modelo

### Technical Notes

**Stack Tecnológico:**
- Whisper CLI (instalado en sistema) o Whisper.NET (NuGet)
- HttpClient para descarga de modelos
- System.IO para file management
- SHA256 para checksum validation
- IMemoryCache para cache de modelo list

**Ubicación de Archivos:**
```
YoutubeRag.Application/
├── Interfaces/
│   └── IWhisperModelService.cs (CREAR)
└── Services/
    └── WhisperModelManager.cs (CREAR)

YoutubeRag.Infrastructure/
└── Services/
    └── WhisperModelDownloadService.cs (CREAR)
```

**Configuración (appsettings.json):**
```json
{
  "Whisper": {
    "ModelsPath": "C:\\Models\\Whisper",
    "DefaultModel": "auto",
    "ForceModel": null,
    "ModelDownloadUrl": "https://openaipublic.azureedge.net/main/whisper/models/",
    "CleanupUnusedModelsDays": 30,
    "MinDiskSpaceGB": 10
  }
}
```

**Tamaños de Modelos:**
- tiny: ~39 MB, ~10x realtime
- base: ~74 MB, ~7x realtime
- small: ~244 MB, ~4x realtime
- medium: ~769 MB, ~2x realtime (no usar en MVP)
- large: ~1550 MB, ~1x realtime (no usar en MVP)

**Algoritmo de Selección:**
```csharp
public string SelectModel(int durationSeconds)
{
    if (!string.IsNullOrEmpty(_config.ForceModel))
        return _config.ForceModel;

    return durationSeconds switch
    {
        < 600 => "tiny",      // <10 min
        < 1800 => "base",     // <30 min
        _ => "small"          // >30 min
    };
}
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] IWhisperModelService interface definida
  - [ ] WhisperModelManager implementado
  - [ ] WhisperModelDownloadService con retry
  - [ ] Checksum validation implementado
  - [ ] Model selection algorithm implementado
  - [ ] Cleanup recurring job configurado
  - [ ] Configuration binding implementado
  - [ ] Code review completado

- [ ] **Testing**
  - [ ] Unit tests para model selection algorithm (10+ cases)
  - [ ] Unit tests para checksum validation
  - [ ] Integration tests para model download (mock HTTP)
  - [ ] Manual testing:
    - Download de modelo tiny
    - Reutilización de modelo existente
    - Selección automática por duración
    - Override con ForceModel
    - Disk space insufficient
  - [ ] Test coverage >85%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] Directorio models creado y accesible
  - [ ] Al menos modelo "tiny" descargado
  - [ ] Whisper CLI instalado (si se usa CLI)

- [ ] **Performance**
  - [ ] Model detection <1 segundo
  - [ ] Model download con progress logging
  - [ ] No impacto en startup time (lazy load)

- [ ] **Documentación**
  - [ ] XML comments completos
  - [ ] README con setup de modelos
  - [ ] Troubleshooting guide

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Test report con AC1-AC5
  - [ ] Evidencia de modelo descargado
  - [ ] Logs de selección de modelo

### Dependencias

- **Bloqueantes:** YRUS-0103 (requiere audio WAV)
- **Bloquea a:** YRUS-0202 (modelo necesario para transcripción)
- **Dependencias Externas:** Whisper instalado en sistema

### Estimación de Esfuerzo

- **Desarrollo:** 4 horas
- **Testing:** 2 horas
- **Setup Whisper:** 1 hora
- **Code Review + Manual Testing:** 1 hora
- **Total:** 8 horas (~1 día de trabajo)

---

## YRUS-0202: Ejecutar Transcripción con Whisper

**Story Points:** 8
**Prioridad:** Critical (P0)
**Sprint:** 2
**Epic:** Transcription Pipeline
**Rama Git:** `YRUS-0202_ejecutar_transcripcion`

### Historia de Usuario

**Como** sistema de procesamiento
**Quiero** transcribir audio usando Whisper local
**Para que** convierta el audio a texto con timestamps

### Acceptance Criteria

**AC1: Ejecución de Whisper**
- Given un archivo de audio WAV normalizado (16kHz mono)
- When ejecuta Whisper transcription
- Then invoca Whisper con comando: `whisper "{audioPath}" --model {modelName} --output_format json --language es --task transcribe`
- And procesa archivo en modo background (Hangfire job)
- And actualiza VideoStatus = Transcribing
- And captura stdout/stderr para logging
- And espera hasta completar (puede tomar varios minutos)

**AC2: Parsing de Output JSON**
- Given Whisper completa exitosamente
- When parsea el output JSON
- Then extrae array de segments
- And por cada segment extrae: text, start (segundos), end (segundos)
- And extrae confidence score si disponible
- And extrae language detected
- And valida que segments estén ordenados por timestamp
- And valida que no haya gaps >5 segundos entre segments

**AC3: Creación de TranscriptSegments**
- Given segments parseados del JSON
- When almacena en base de datos
- Then crea entidad TranscriptSegment por cada segment
- And establece: VideoId (FK), Text, StartTime, EndTime, SegmentIndex (orden)
- And establece TranscriptionStatus = Completed
- And usa bulk insert para performance (puede ser 100+ segments)
- And actualiza Video.TranscriptionStatus = Completed
- And actualiza VideoStatus = Transcribed

**AC4: Performance y Timeouts**
- Given videos de diferentes duraciones
- When ejecuta transcripción
- Then completa en <2x realtime para modelo "tiny"
- And completa en <5x realtime para modelo "base"
- And completa en <10x realtime para modelo "small"
- And implementa timeout dinámico: `durationSeconds * 15` (safety margin)
- And actualiza Job progress cada 30 segundos (estimado)
- And reporta tiempo total de transcripción en logs

**AC5: Manejo de Errores**
- Given diferentes escenarios de error
- When ocurren fallas
- Then maneja Whisper process crash con retry (1 intento con modelo más pequeño)
- And maneja out-of-memory reduciendo a modelo "tiny"
- And maneja audio file corrupted con error claro
- And maneja timeout con error descriptivo
- And actualiza JobStatus = Failed con stack trace
- And preserva archivo de audio para debugging

### Technical Notes

**Stack Tecnológico:**
- Whisper CLI (invocado via Process)
- System.Diagnostics.Process para execution
- Newtonsoft.Json para parsing JSON output
- EF Core Bulk Extensions para bulk insert
- Hangfire para background processing

**Ubicación de Archivos:**
```
YoutubeRag.Application/
├── Interfaces/
│   └── ITranscriptionService.cs (ya existe)
└── Services/
    ├── WhisperTranscriptionService.cs (CREAR)
    └── TranscriptSegmentProcessor.cs (CREAR)

YoutubeRag.Infrastructure/
└── Services/
    └── WhisperCliExecutor.cs (CREAR - wrapper de Process)
```

**Whisper CLI Command:**
```bash
whisper "C:\Temp\YoutubeRag\{videoId}\{timestamp}.wav" \
  --model tiny \
  --output_format json \
  --language es \
  --task transcribe \
  --output_dir "C:\Temp\YoutubeRag\{videoId}"
```

**Whisper JSON Output Format:**
```json
{
  "text": "Texto completo...",
  "segments": [
    {
      "id": 0,
      "start": 0.0,
      "end": 5.5,
      "text": "Hola mundo",
      "tokens": [...],
      "temperature": 0.0,
      "avg_logprob": -0.5,
      "compression_ratio": 1.2,
      "no_speech_prob": 0.01
    }
  ],
  "language": "es"
}
```

**Bulk Insert Implementation:**
```csharp
await _context.BulkInsertAsync(transcriptSegments, options =>
{
    options.BatchSize = 1000;
    options.EnableStreaming = true;
});
```

**Timeout Calculation:**
```csharp
var timeoutSeconds = video.DurationSeconds * 15; // 15x safety margin
var timeout = TimeSpan.FromSeconds(timeoutSeconds);
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] ITranscriptionService interface actualizada
  - [ ] WhisperTranscriptionService implementado
  - [ ] WhisperCliExecutor wrapper implementado
  - [ ] TranscriptSegmentProcessor con bulk insert
  - [ ] JSON parsing robusto implementado
  - [ ] Timeout dinámico implementado
  - [ ] Error handling con retry implementado
  - [ ] Code review completado

- [ ] **Testing**
  - [ ] Unit tests para JSON parsing (mock Whisper output)
  - [ ] Unit tests para timeout calculation
  - [ ] Integration tests con audio real (corto, <1 min)
  - [ ] Manual testing:
    - Video corto en español (<5 min)
    - Video mediano en inglés (10-20 min)
    - Audio corrupto
    - Timeout simulation
    - Out-of-memory simulation (video muy largo)
  - [ ] Test coverage >80%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] Whisper CLI instalado y en PATH
  - [ ] Modelos descargados (tiny, base)
  - [ ] Bulk insert package instalado

- [ ] **Performance**
  - [ ] Benchmarks ejecutados y documentados
  - [ ] Tiempo de transcripción <2x realtime (tiny)
  - [ ] Memory usage razonable
  - [ ] Bulk insert completa en <5 segundos (1000 segments)

- [ ] **Documentación**
  - [ ] XML comments completos
  - [ ] README con ejemplos de transcripción
  - [ ] Performance benchmarks documentados
  - [ ] Error codes documentados

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Test report completo
  - [ ] Screenshots de segments en DB
  - [ ] Evidencia de performance

### Dependencias

- **Bloqueantes:** YRUS-0201 (requiere modelo de Whisper)
- **Bloquea a:** YRUS-0203 (segments necesarios para embeddings)
- **Dependencias Externas:** Whisper CLI instalado

### Estimación de Esfuerzo

- **Desarrollo:** 6 horas
- **Testing:** 3 horas
- **Performance Tuning:** 2 horas
- **Code Review + Manual Testing:** 2 horas
- **Total:** 13 horas (~1.5 días de trabajo)

---

## YRUS-0203: Segmentar y Almacenar Transcripción

**Story Points:** 5
**Prioridad:** Critical (P0)
**Sprint:** 2
**Epic:** Transcription Pipeline
**Rama Git:** `YRUS-0203_segmentar_almacenar`

### Historia de Usuario

**Como** sistema de almacenamiento
**Quiero** segmentar y almacenar eficientemente la transcripción
**Para que** sea searchable y recuperable rápidamente

### Acceptance Criteria

**AC1: Segmentación Inteligente**
- Given segments de Whisper (pueden ser muy largos)
- When procesa para almacenamiento
- Then divide segments largos (>500 chars) en chunks más pequeños
- And mantiene coherencia semántica (split en pausas naturales)
- And preserva timestamps proporcionales al split
- And asigna SegmentIndex secuencial (0, 1, 2...)
- And mantiene referencia al VideoId

**AC2: Almacenamiento Bulk**
- Given lista de TranscriptSegments procesados
- When inserta en base de datos
- Then usa bulk insert para performance (100+ segments en <5 seg)
- And usa transaction para mantener atomicidad
- And verifica foreign key constraint (VideoId existe)
- And establece CreatedAt = DateTime.UtcNow
- And retorna número de segments insertados

**AC3: Generación de Embeddings Placeholder**
- Given segments almacenados
- When genera embeddings (MVP: mock)
- Then crea vector de 384 dimensiones con valores aleatorios normalizados
- And almacena como JSON: `[0.123, -0.456, ...]` en campo Embedding
- And establece EmbeddingStatus = Generated
- And prepara para futura integración con ONNX (post-MVP)
- And documenta que son embeddings MOCK en logs

**AC4: Índices de Base de Datos**
- Given necesidad de búsqueda rápida
- When consulta segments
- Then usa índice en (VideoId, SegmentIndex) para ordering
- And usa índice en VideoId para filtering
- And usa índice en CreatedAt para cleanup
- And queries de segments completan en <100ms para video típico

**AC5: Validación de Integridad**
- Given segments almacenados
- When valida integridad
- Then verifica que SegmentIndex sea secuencial sin gaps
- And verifica que timestamps sean crecientes (StartTime[i] <= StartTime[i+1])
- And verifica que no haya overlaps: EndTime[i] <= StartTime[i+1]
- And verifica que todos los segments tengan VideoId válido
- And registra warning si detecta inconsistencias

### Technical Notes

**Stack Tecnológico:**
- EF Core para data access
- EF Core Bulk Extensions para bulk insert
- AutoMapper para DTO mapping
- MySQL para storage
- Newtonsoft.Json para embedding serialization

**Ubicación de Archivos:**
```
YoutubeRag.Application/
├── Interfaces/
│   └── ISegmentationService.cs (ya existe)
└── Services/
    ├── TranscriptSegmentationService.cs (CREAR)
    └── MockEmbeddingService.cs (CREAR - temporal para MVP)

YoutubeRag.Infrastructure/
└── Repositories/
    └── TranscriptSegmentRepository.cs (ACTUALIZAR con bulk methods)
```

**TranscriptSegment Entity:**
```csharp
public class TranscriptSegment : BaseEntity
{
    public Guid VideoId { get; set; } // FK
    public int SegmentIndex { get; set; }
    public string Text { get; set; }
    public double StartTime { get; set; } // segundos
    public double EndTime { get; set; } // segundos
    public string? Embedding { get; set; } // JSON array [0.1, 0.2, ...]
    public EmbeddingStatus EmbeddingStatus { get; set; }
    public TranscriptionStatus Status { get; set; }

    public Video Video { get; set; } // Navigation property
}
```

**Database Indexes:**
```sql
CREATE INDEX IX_TranscriptSegments_VideoId_SegmentIndex
  ON TranscriptSegments (VideoId, SegmentIndex);

CREATE INDEX IX_TranscriptSegments_VideoId
  ON TranscriptSegments (VideoId);

CREATE INDEX IX_TranscriptSegments_CreatedAt
  ON TranscriptSegments (CreatedAt);
```

**Mock Embedding Generation:**
```csharp
public float[] GenerateMockEmbedding(string text)
{
    var random = new Random(text.GetHashCode()); // Deterministic
    var embedding = new float[384];

    for (int i = 0; i < 384; i++)
    {
        embedding[i] = (float)(random.NextDouble() * 2 - 1); // [-1, 1]
    }

    // Normalize
    var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
    return embedding.Select(x => x / (float)magnitude).ToArray();
}
```

**Segmentation Algorithm:**
```csharp
public List<TranscriptSegment> SegmentText(string longText, double startTime, double endTime)
{
    const int maxChars = 500;

    if (longText.Length <= maxChars)
        return [new TranscriptSegment { Text = longText, StartTime = startTime, EndTime = endTime }];

    // Split on sentence boundaries (. ! ? \n)
    var sentences = Regex.Split(longText, @"(?<=[.!?\n])\s+");
    var chunks = new List<TranscriptSegment>();
    var currentChunk = "";
    var duration = endTime - startTime;

    // ... (implementar lógica de chunking inteligente)
}
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] ISegmentationService implementado
  - [ ] TranscriptSegmentationService con chunking
  - [ ] MockEmbeddingService implementado
  - [ ] TranscriptSegmentRepository con bulk insert
  - [ ] Database indexes creados (migration)
  - [ ] Validation logic implementado
  - [ ] Code review completado

- [ ] **Testing**
  - [ ] Unit tests para segmentation algorithm (10+ cases)
  - [ ] Unit tests para mock embedding generation
  - [ ] Integration tests para bulk insert
  - [ ] Integration tests para validation logic
  - [ ] Manual testing:
    - Segments largos (>500 chars)
    - Segments cortos (<100 chars)
    - Bulk insert de 500+ segments
    - Validación de integridad
  - [ ] Test coverage >85%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] Migration ejecutada (indexes)
  - [ ] Bulk insert package configurado

- [ ] **Performance**
  - [ ] Bulk insert de 1000 segments <5 segundos
  - [ ] Query de segments por VideoId <100ms
  - [ ] Embedding generation <1ms per segment

- [ ] **Documentación**
  - [ ] XML comments completos
  - [ ] README con info de embeddings mock
  - [ ] Migration guide para futura ONNX integration

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Test report completo
  - [ ] Screenshots de DB con segments
  - [ ] Performance benchmarks

### Dependencias

- **Bloqueantes:** YRUS-0202 (requiere transcripción completa)
- **Bloquea a:** YRUS-0301 (segments listos para pipeline completo)

### Estimación de Esfuerzo

- **Desarrollo:** 4 horas
- **Testing:** 2 horas
- **Migration + Indexes:** 1 hora
- **Code Review + Manual Testing:** 1 hora
- **Total:** 8 horas (~1 día de trabajo)

---

## Epic 3: Background Job Orchestration

**Prioridad:** MUST HAVE
**RICE Score:** 112.5
**Business Value:** Coordinación del pipeline completo
**Story Points Totales:** 8

### Objetivo del Epic

Orquestar el pipeline completo de procesamiento usando Hangfire, coordinando todas las etapas desde ingesta hasta embeddings con manejo de errores y retry logic.

---

## YRUS-0301: Orquestar Pipeline Multi-Etapa

**Story Points:** 5
**Prioridad:** Critical (P0)
**Sprint:** 2
**Epic:** Background Job Orchestration
**Rama Git:** `YRUS-0301_orquestar_pipeline`

### Historia de Usuario

**Como** sistema de procesamiento
**Quiero** orquestar todas las etapas del pipeline automáticamente
**Para que** el procesamiento sea completamente automático de principio a fin

### Acceptance Criteria

**AC1: Pipeline Job Chain**
- Given un Video ingresado (YRUS-0101)
- When se encola el pipeline job
- Then ejecuta etapas en secuencia con Hangfire continuations:
  1. ExtractMetadataJob (YRUS-0102)
  2. DownloadVideoJob (YRUS-0103)
  3. ExtractAudioJob (YRUS-0103)
  4. TranscribeJob (YRUS-0202)
  5. GenerateEmbeddingsJob (YRUS-0203)
- And cada etapa actualiza JobStatus correspondiente
- And cada etapa reporta progreso vía Job entity
- And falla todo el pipeline si una etapa falla (no continuar)

**AC2: State Management**
- Given pipeline en ejecución
- When gestiona estado
- Then persiste estado actual en Job.CurrentStage
- And persiste metadata de cada etapa en Job.Metadata (JSON)
- And permite reanudar pipeline desde última etapa exitosa
- And mantiene historial de etapas completadas
- And actualiza Job.UpdatedAt en cada transición

**AC3: Progress Tracking**
- Given pipeline con múltiples etapas
- When reporta progreso general
- Then calcula % total basado en etapas completadas:
  - Metadata: 0-20%
  - Download: 20-40%
  - Audio Extraction: 40-50%
  - Transcription: 50-90%
  - Embeddings: 90-100%
- And actualiza Job.ProgressPercentage
- And estima tiempo restante basado en etapas previas
- And emite eventos a SignalR para UI

**AC4: Cleanup Automático**
- Given pipeline completado (exitoso o fallido)
- When ejecuta cleanup
- Then elimina archivos temporales (video, audio)
- And mantiene transcripción en DB
- And registra espacio liberado
- And marca archivos como eliminados en Job metadata
- And falla gracefully si cleanup falla (no bloquear)

**AC5: Error Propagation**
- Given error en cualquier etapa
- When maneja error
- Then captura exception completa con stack trace
- And almacena en Job.ErrorMessage
- And actualiza JobStatus = Failed
- And actualiza Video.Status correspondiente
- And NO continúa a siguiente etapa
- And preserva archivos temporales por 24h para debugging
- And envía notificación de error (log + evento)

### Technical Notes

**Stack Tecnológico:**
- Hangfire para job orchestration
- Hangfire continuations para job chaining
- EF Core para state persistence
- SignalR para real-time updates
- Polly para retry policies

**Ubicación de Archivos:**
```
YoutubeRag.Application/
├── Interfaces/Services/
│   └── IVideoProcessingPipeline.cs (CREAR)
└── Services/
    └── VideoProcessingPipelineOrchestrator.cs (CREAR)

YoutubeRag.Infrastructure/
└── Jobs/
    ├── ExtractMetadataJob.cs (CREAR)
    ├── DownloadVideoJob.cs (CREAR)
    ├── ExtractAudioJob.cs (CREAR)
    ├── TranscribeJob.cs (CREAR)
    └── GenerateEmbeddingsJob.cs (CREAR)
```

**Hangfire Job Chain:**
```csharp
public string EnqueuePipeline(Guid videoId)
{
    var jobId = BackgroundJob.Enqueue<ExtractMetadataJob>(
        x => x.ExecuteAsync(videoId));

    BackgroundJob.ContinueJobWith<DownloadVideoJob>(jobId,
        x => x.ExecuteAsync(videoId));

    // ... continue with other jobs

    return jobId;
}
```

**Job Metadata Structure:**
```json
{
  "stages": [
    {
      "name": "ExtractMetadata",
      "status": "Completed",
      "startedAt": "2025-10-07T10:00:00Z",
      "completedAt": "2025-10-07T10:00:05Z",
      "durationSeconds": 5
    },
    {
      "name": "DownloadVideo",
      "status": "InProgress",
      "startedAt": "2025-10-07T10:00:05Z",
      "progressPercentage": 45
    }
  ],
  "totalProgressPercentage": 32,
  "estimatedCompletionAt": "2025-10-07T10:15:00Z"
}
```

**Progress Calculation:**
```csharp
public int CalculateTotalProgress(JobStage currentStage, int stageProgress)
{
    var stageWeights = new Dictionary<JobStage, (int Start, int End)>
    {
        [JobStage.ExtractMetadata] = (0, 20),
        [JobStage.DownloadVideo] = (20, 40),
        [JobStage.ExtractAudio] = (40, 50),
        [JobStage.Transcribe] = (50, 90),
        [JobStage.GenerateEmbeddings] = (90, 100)
    };

    var (start, end) = stageWeights[currentStage];
    return start + (stageProgress * (end - start) / 100);
}
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] IVideoProcessingPipeline interface definida
  - [ ] VideoProcessingPipelineOrchestrator implementado
  - [ ] Todos los job classes implementados (5 jobs)
  - [ ] State management con metadata JSON
  - [ ] Progress calculation implementado
  - [ ] Cleanup logic implementado
  - [ ] Error propagation implementado
  - [ ] Code review completado

- [ ] **Testing**
  - [ ] Unit tests para progress calculation
  - [ ] Unit tests para state management
  - [ ] Integration tests para job chain (mock jobs)
  - [ ] Integration tests para error scenarios
  - [ ] Manual testing:
    - Pipeline completo end-to-end (video corto)
    - Error en etapa intermedia
    - Cleanup después de completion
    - Cleanup después de error
    - Progress tracking en cada etapa
  - [ ] Test coverage >80%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] Hangfire dashboard accesible
  - [ ] Jobs registrados correctamente
  - [ ] Queue configuration correcta

- [ ] **Performance**
  - [ ] Pipeline completo <2x duración video
  - [ ] State updates <100ms
  - [ ] Cleanup completo <5 segundos

- [ ] **Documentación**
  - [ ] XML comments completos
  - [ ] Pipeline flow diagram
  - [ ] Error handling guide
  - [ ] Monitoring guide

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Test report end-to-end
  - [ ] Screenshots de Hangfire dashboard
  - [ ] Logs de pipeline completo

### Dependencias

- **Bloqueantes:** YRUS-0101, YRUS-0102, YRUS-0103, YRUS-0202, YRUS-0203
- **Bloquea a:** YRUS-0302 (retry logic sobre pipeline)

### Estimación de Esfuerzo

- **Desarrollo:** 5 horas
- **Testing:** 3 horas
- **Integration Testing:** 2 horas
- **Code Review + Manual Testing:** 2 horas
- **Total:** 12 horas (~1.5 días de trabajo)

---

## YRUS-0302: Implementar Retry Logic

**Story Points:** 3
**Prioridad:** High (P1)
**Sprint:** 2
**Epic:** Background Job Orchestration
**Rama Git:** `YRUS-0302_implementar_retry`

### Historia de Usuario

**Como** sistema resiliente
**Quiero** retry inteligente para fallas transitorias
**Para que** errores temporales no pierdan trabajo

### Acceptance Criteria

**AC1: Retry Policies por Tipo de Error**
- Given diferentes tipos de errores
- When configura retry policies
- Then aplica policy específico por tipo:
  - Network errors: 3 retries, exponential backoff (10s, 30s, 90s)
  - YouTube rate limit: 5 retries, linear backoff (60s)
  - Whisper OOM: 1 retry con modelo más pequeño
  - Database transient: 2 retries, immediate
  - Disk space: 0 retries (error permanente)
- And registra cada intento de retry en logs
- And incrementa Job.RetryCount

**AC2: Exponential Backoff**
- Given retry con exponential backoff
- When calcula delay
- Then calcula: `delay = baseDelay * 2^(attemptNumber - 1)`
- And añade jitter aleatorio ±20% (evitar thundering herd)
- And limita max delay a 5 minutos
- And programa retry usando Hangfire.Schedule

**AC3: Dead Letter Queue**
- Given job que excede max retries
- When falla permanentemente
- Then mueve a dead letter queue (tabla separada o flag)
- And marca JobStatus = DeadLetter
- And preserva toda la información del job
- And envía notificación a admin (log crítico)
- And permite manual retry desde dashboard

**AC4: Retry Manual**
- Given job en estado Failed o DeadLetter
- When admin solicita retry manual
- Then permite re-encolar job desde Hangfire dashboard
- And resetea RetryCount a 0
- And mantiene audit trail (quién y cuándo)
- And permite especificar si empezar desde inicio o última etapa exitosa

### Technical Notes

**Stack Tecnológico:**
- Polly para retry policies
- Hangfire AutomaticRetry attribute
- Custom retry filters para Hangfire
- EF Core para dead letter persistence

**Ubicación de Archivos:**
```
YoutubeRag.Infrastructure/
├── Resilience/
│   ├── RetryPolicies.cs (CREAR)
│   └── RetryConfiguration.cs (CREAR)
└── Jobs/
    └── Filters/
        └── CustomRetryFilter.cs (CREAR)
```

**Retry Configuration:**
```csharp
public class RetryConfiguration
{
    public RetryPolicy NetworkError => Policy
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * 10)
            + TimeSpan.FromMilliseconds(Random.Shared.Next(-2000, 2000)));

    public RetryPolicy YouTubeRateLimit => Policy
        .Handle<RateLimitException>()
        .WaitAndRetryAsync(5, retryAttempt =>
            TimeSpan.FromSeconds(60));

    public RetryPolicy DatabaseTransient => Policy
        .Handle<DbUpdateException>()
        .RetryAsync(2);
}
```

**Custom Hangfire Filter:**
```csharp
public class CustomRetryFilter : JobFilterAttribute, IElectStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        var failedState = context.CandidateState as FailedState;
        if (failedState == null) return;

        var exception = failedState.Exception;
        var retryAttempt = context.GetJobParameter<int>("RetryCount") + 1;

        // Determine retry based on exception type
        if (ShouldRetry(exception, retryAttempt))
        {
            context.SetJobParameter("RetryCount", retryAttempt);
            context.CandidateState = new ScheduledState(CalculateDelay(exception, retryAttempt));
        }
        else
        {
            // Move to dead letter
            MoveToDeadLetter(context);
        }
    }
}
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] RetryPolicies implementado con Polly
  - [ ] CustomRetryFilter para Hangfire
  - [ ] Dead letter queue logic
  - [ ] Manual retry endpoint
  - [ ] Configuration bindings
  - [ ] Code review completado

- [ ] **Testing**
  - [ ] Unit tests para retry calculation
  - [ ] Unit tests para backoff algorithm
  - [ ] Integration tests simulando failures
  - [ ] Manual testing:
    - Network error retry
    - Rate limit retry
    - OOM retry con modelo pequeño
    - Max retries exceeded → Dead letter
    - Manual retry desde dashboard
  - [ ] Test coverage >85%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] Polly package instalado
  - [ ] Retry filter registrado en Hangfire

- [ ] **Documentación**
  - [ ] XML comments completos
  - [ ] Retry policies documentadas
  - [ ] Admin guide para manual retry

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Test report con retry scenarios
  - [ ] Logs de retry attempts
  - [ ] Dead letter queue funcionando

### Dependencias

- **Bloqueantes:** YRUS-0301 (requiere pipeline orquestado)
- **Bloquea a:** Ninguna

### Estimación de Esfuerzo

- **Desarrollo:** 3 horas
- **Testing:** 2 horas
- **Code Review + Manual Testing:** 1 hora
- **Total:** 6 horas (~0.75 días de trabajo)

---

## Epic 4: Progress & Error Tracking

**Prioridad:** SHOULD HAVE
**RICE Score:** 60.0
**Business Value:** UX crítico para usuarios
**Story Points Totales:** 8

### Objetivo del Epic

Proporcionar tracking en tiempo real del progreso de procesamiento y notificaciones de errores usando SignalR para updates instantáneos.

---

## YRUS-0401: Real-time Progress via SignalR

**Story Points:** 5
**Prioridad:** High (P1)
**Sprint:** 2
**Epic:** Progress Tracking
**Rama Git:** `YRUS-0401_realtime_progress`

### Historia de Usuario

**Como** usuario
**Quiero** ver el progreso de procesamiento en tiempo real
**Para que** sepa cuándo estará listo mi video

### Acceptance Criteria

**AC1: SignalR Hub Implementation**
- Given SignalR configurado (ya existe de Sprint 1)
- When implementa JobProgressHub
- Then define métodos cliente: `OnProgressUpdate`, `OnStageChange`, `OnCompleted`, `OnFailed`
- And mantiene conexión persistente con cliente
- And soporta múltiples clientes conectados simultáneamente
- And autentica conexión vía JWT (si usuario autenticado)

**AC2: Progress Updates desde Jobs**
- Given job ejecutándose (cualquier etapa)
- When reporta progreso
- Then invoca SignalR hub con: `jobId, stage, percentage, message`
- And calcula percentage total del pipeline (0-100%)
- And estima tiempo restante basado en velocidad actual
- And emite update cada 30 segundos mínimo (evitar spam)
- And persiste último update en Job entity

**AC3: Connection Management**
- Given múltiples clientes conectados
- When gestiona conexiones
- Then asigna clientes a grupos por JobId: `job:{jobId}`
- And permite cliente subscribirse a múltiples jobs
- And remueve cliente de grupo al desconectar
- And mantiene lista de conexiones activas
- And implementa reconnection logic en cliente (auto-reconnect)

**AC4: Progress API Fallback**
- Given cliente sin soporte SignalR (o conexión caída)
- When solicita progreso
- Then proporciona API REST: `GET /api/jobs/{jobId}/progress`
- And retorna mismo DTO que SignalR
- And implementa caching (30 segundos)
- And permite polling cada 5 segundos (rate limit)

**AC5: Performance**
- Given múltiples jobs activos (10+)
- When emite progress updates
- Then no impacta performance de jobs (async fire-and-forget)
- And SignalR hub soporta 100+ conexiones concurrentes
- And latencia de update <500ms desde job a cliente
- And no acumula backlog de mensajes

### Technical Notes

**Stack Tecnológico:**
- SignalR (ya configurado en Sprint 1)
- Hangfire background jobs
- IMemoryCache para caching
- EF Core para progress persistence

**Ubicación de Archivos:**
```
YoutubeRag.Api/
└── Hubs/
    └── JobProgressHub.cs (ACTUALIZAR - ya existe basic)

YoutubeRag.Application/
├── Interfaces/Services/
│   └── IProgressNotificationService.cs (ya existe)
└── Services/
    └── ProgressNotificationService.cs (CREAR)

YoutubeRag.Api/
└── Controllers/
    └── JobProgressController.cs (CREAR - REST fallback)
```

**SignalR Hub:**
```csharp
public class JobProgressHub : Hub
{
    public async Task SubscribeToJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job:{jobId}");
    }

    public async Task UnsubscribeFromJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job:{jobId}");
    }
}
```

**Progress Notification Service:**
```csharp
public class ProgressNotificationService : IProgressNotificationService
{
    private readonly IHubContext<JobProgressHub> _hubContext;

    public async Task NotifyProgressAsync(Guid jobId, JobProgressDto progress)
    {
        await _hubContext.Clients
            .Group($"job:{jobId}")
            .SendAsync("OnProgressUpdate", progress);
    }
}
```

**Progress DTO:**
```csharp
public class JobProgressDto
{
    public Guid JobId { get; set; }
    public string CurrentStage { get; set; }
    public int ProgressPercentage { get; set; }
    public string Message { get; set; }
    public DateTime? EstimatedCompletionAt { get; set; }
    public TimeSpan? ElapsedTime { get; set; }
}
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] JobProgressHub actualizado
  - [ ] ProgressNotificationService implementado
  - [ ] Connection management implementado
  - [ ] REST API fallback implementado
  - [ ] Rate limiting implementado
  - [ ] Code review completado

- [ ] **Testing**
  - [ ] Unit tests para ProgressNotificationService
  - [ ] Integration tests para SignalR hub
  - [ ] Integration tests para REST API
  - [ ] Manual testing:
    - Conexión SignalR exitosa
    - Subscribe/unsubscribe a job
    - Progress updates en tiempo real
    - Reconnection después de disconnect
    - REST API fallback
    - Múltiples clientes simultáneos
  - [ ] Test coverage >75%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] SignalR endpoint accesible
  - [ ] CORS configurado correctamente

- [ ] **Performance**
  - [ ] Latencia <500ms
  - [ ] Soporte 100+ conexiones
  - [ ] No impacto en jobs

- [ ] **Documentación**
  - [ ] XML comments completos
  - [ ] Client integration guide (JavaScript example)
  - [ ] REST API documented in Swagger

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Test report con screenshots
  - [ ] Cliente de prueba conectado
  - [ ] Logs de progress updates

### Dependencias

- **Bloqueantes:** YRUS-0301 (requiere pipeline reportando progreso)
- **Bloquea a:** Ninguna

### Estimación de Esfuerzo

- **Desarrollo:** 4 horas
- **Testing:** 2 horas
- **Client testing:** 1 hora
- **Code Review + Manual Testing:** 1 hora
- **Total:** 8 horas (~1 día de trabajo)

---

## YRUS-0402: Notificaciones de Completitud y Error

**Story Points:** 3
**Prioridad:** High (P1)
**Sprint:** 2
**Epic:** Progress Tracking
**Rama Git:** `YRUS-0402_notificaciones_eventos`

### Historia de Usuario

**Como** usuario
**Quiero** recibir notificación cuando mi video se complete o falle
**Para que** sepa cuándo actuar

### Acceptance Criteria

**AC1: Notificación de Completitud**
- Given job completa exitosamente
- When finaliza última etapa
- Then emite evento SignalR: `OnCompleted` con JobId, VideoId, duración total
- And persiste notificación en tabla UserNotifications (si usuario autenticado)
- And marca Job.CompletedAt = DateTime.UtcNow
- And marca JobStatus = Completed
- And incluye stats: tiempo total, segments creados, duración video

**AC2: Notificación de Error**
- Given job falla en cualquier etapa
- When se marca como failed
- Then emite evento SignalR: `OnFailed` con JobId, error message amigable
- And persiste notificación de error
- And NO expone stack trace (solo mensaje user-friendly)
- And incluye etapa que falló
- And incluye sugerencia de acción (ej: "Verifica URL", "Intenta más tarde")

**AC3: Categorización de Errores**
- Given diferentes tipos de errores
- When categoriza para usuario
- Then identifica categorías:
  - Network: "Error de conexión. Intenta de nuevo."
  - YouTube: "Video no disponible o privado."
  - Format: "Formato de video no soportado."
  - System: "Error del sistema. Contacta soporte."
  - Resource: "Sin espacio en disco."
- And mapea exception técnica a categoría
- And retorna mensaje amigable por categoría

**AC4: Historial de Notificaciones**
- Given notificaciones generadas
- When usuario consulta historial
- Then API endpoint: `GET /api/notifications?userId={userId}`
- And retorna notificaciones ordenadas por fecha (desc)
- And incluye paginación (20 por página)
- And marca notificaciones como leídas al consultarlas
- And permite filtrar por tipo (success, error)

**AC5: Cleanup de Notificaciones**
- Given notificaciones antiguas
- When ejecuta cleanup (Hangfire recurring)
- Then elimina notificaciones >30 días
- And mantiene al menos últimas 100 por usuario
- And registra número de notificaciones eliminadas
- And ejecuta weekly (domingo 2am)

### Technical Notes

**Stack Tecnológico:**
- SignalR para real-time events
- EF Core para persistence
- AutoMapper para DTO mapping
- Hangfire para cleanup job

**Ubicación de Archivos:**
```
YoutubeRag.Domain/
└── Entities/
    └── UserNotification.cs (CREAR)

YoutubeRag.Application/
├── DTOs/Progress/
│   └── UserNotificationDto.cs (ya existe)
├── Interfaces/Services/
│   └── INotificationService.cs (CREAR)
└── Services/
    └── NotificationService.cs (CREAR)

YoutubeRag.Api/
└── Controllers/
    └── NotificationsController.cs (CREAR)

YoutubeRag.Infrastructure/
└── Jobs/
    └── NotificationCleanupJob.cs (CREAR)
```

**UserNotification Entity:**
```csharp
public class UserNotification : BaseEntity
{
    public Guid? UserId { get; set; } // Nullable (si no autenticado)
    public Guid JobId { get; set; }
    public string Type { get; set; } // "Success", "Error"
    public string Title { get; set; }
    public string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Job Job { get; set; }
}
```

**Error Message Mapping:**
```csharp
public string GetUserFriendlyMessage(Exception ex)
{
    return ex switch
    {
        HttpRequestException => "Error de conexión. Por favor, intenta de nuevo.",
        YouTubeUnavailableException => "El video no está disponible o es privado.",
        UnsupportedFormatException => "Formato de video no soportado.",
        InsufficientDiskSpaceException => "Sin espacio en disco. Contacta al administrador.",
        _ => "Error del sistema. Por favor, contacta soporte."
    };
}
```

**SignalR Events:**
```csharp
public async Task NotifyCompletion(Guid jobId, JobCompletionDto completion)
{
    await _hubContext.Clients
        .Group($"job:{jobId}")
        .SendAsync("OnCompleted", completion);
}

public async Task NotifyError(Guid jobId, JobErrorDto error)
{
    await _hubContext.Clients
        .Group($"job:{jobId}")
        .SendAsync("OnFailed", error);
}
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] UserNotification entity creada
  - [ ] INotificationService implementado
  - [ ] NotificationService implementado
  - [ ] Error categorization implementado
  - [ ] NotificationsController implementado
  - [ ] Cleanup job implementado
  - [ ] Migration creada
  - [ ] Code review completado

- [ ] **Testing**
  - [ ] Unit tests para error categorization
  - [ ] Unit tests para notification service
  - [ ] Integration tests para API endpoints
  - [ ] Manual testing:
    - Job success notification
    - Job error notification
    - Error message mapping
    - Notification history API
    - Cleanup job
  - [ ] Test coverage >80%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] Migration ejecutada
  - [ ] Cleanup job registrado

- [ ] **Documentación**
  - [ ] XML comments completos
  - [ ] API documentation en Swagger
  - [ ] Error codes documentados

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Test report completo
  - [ ] Screenshots de notificaciones
  - [ ] Logs de eventos SignalR

### Dependencias

- **Bloqueantes:** YRUS-0401 (requiere SignalR hub)
- **Bloquea a:** Ninguna

### Estimación de Esfuerzo

- **Desarrollo:** 3 horas
- **Testing:** 1.5 horas
- **Migration:** 0.5 horas
- **Code Review + Manual Testing:** 1 hora
- **Total:** 6 horas (~0.75 días de trabajo)

---

## Resumen de Historias del Sprint 2

### Tabla Resumen

| ID | Historia | Epic | Points | Prioridad | Duración Estimada |
|----|----------|------|--------|-----------|-------------------|
| YRUS-0101 | Enviar URL YouTube | Video Ingestion | 5 | P0 | 1 día |
| YRUS-0102 | Extraer Metadata Completa | Video Ingestion | 5 | P0 | 1 día |
| YRUS-0103 | Descargar y Extraer Audio | Download & Audio | 8 | P0 | 1.5 días |
| YRUS-0201 | Gestionar Modelos Whisper | Transcription | 5 | P0 | 1 día |
| YRUS-0202 | Ejecutar Transcripción | Transcription | 8 | P0 | 1.5 días |
| YRUS-0203 | Segmentar y Almacenar | Transcription | 5 | P0 | 1 día |
| YRUS-0301 | Orquestar Pipeline | Background Jobs | 5 | P0 | 1.5 días |
| YRUS-0302 | Implementar Retry Logic | Background Jobs | 3 | P1 | 0.75 días |
| YRUS-0401 | Real-time Progress | Progress Tracking | 5 | P1 | 1 día |
| YRUS-0402 | Notificaciones | Progress Tracking | 3 | P1 | 0.75 días |

**Total:** 52 Story Points, ~11 días de trabajo estimado (incluye buffer)

---

## Planning de Implementación Recomendado

### Semana 1 (Días 1-5)

**Día 1-2:**
- YRUS-0101: Enviar URL YouTube (5 pts)

**Día 2-3:**
- YRUS-0102: Extraer Metadata Completa (5 pts)

**Día 3-4:**
- YRUS-0103: Descargar y Extraer Audio (8 pts)

**Día 4-5:**
- YRUS-0201: Gestionar Modelos Whisper (5 pts)

### Semana 2 (Días 6-10)

**Día 6-7:**
- YRUS-0202: Ejecutar Transcripción (8 pts)

**Día 7-8:**
- YRUS-0203: Segmentar y Almacenar (5 pts)

**Día 8-9:**
- YRUS-0301: Orquestar Pipeline (5 pts)
- YRUS-0302: Implementar Retry Logic (3 pts)

**Día 9-10:**
- YRUS-0401: Real-time Progress (5 pts)
- YRUS-0402: Notificaciones (3 pts)
- Testing Manual Completo del Sprint
- Regresión Automática
- Sprint Report

---

## Dependencias del Sprint

### Gráfico de Dependencias

```
YRUS-0101 (Ingesta URL)
    ↓
YRUS-0102 (Metadata)
    ↓
YRUS-0103 (Download/Audio) ────┐
    ↓                           │
YRUS-0201 (Modelos Whisper)    │
    ↓                           │
YRUS-0202 (Transcripción) ─────┤
    ↓                           │
YRUS-0203 (Segmentación) ──────┘
    ↓
YRUS-0301 (Pipeline Orquestación)
    ↓
YRUS-0302 (Retry Logic) ────┐
    ↓                        │
YRUS-0401 (Progress) ───────┤
    ↓                        │
YRUS-0402 (Notifications) ──┘
```

---

## Criterios de Aceptación del Sprint

### Sprint Goal

**"Implementar el pipeline completo de procesamiento de videos desde URL submission hasta embeddings generados, con tracking en tiempo real y manejo robusto de errores."**

### Sprint Success Criteria

- [ ] **Funcional:**
  - [ ] Procesar exitosamente 5+ videos de prueba end-to-end
  - [ ] Transcripción con >90% accuracy (validación manual)
  - [ ] Embeddings (mock) generados para todos los segments

- [ ] **Performance:**
  - [ ] Procesamiento completo <2x duración del video
  - [ ] Progress updates con latencia <500ms
  - [ ] Queries de DB <100ms

- [ ] **Calidad:**
  - [ ] Test coverage >80% en paths críticos
  - [ ] Build: 0 errores, 0 warnings
  - [ ] Zero bugs P0, <5 bugs P1

- [ ] **Documentación:**
  - [ ] API documentation completa (Swagger)
  - [ ] README actualizado
  - [ ] Troubleshooting guide creado

### Definition of Done (Sprint Level)

- [ ] Todas las historias completas (11/11)
- [ ] Todas las AC validadas manualmente
- [ ] Regresión automática >85% passing
- [ ] Testing manual end-to-end completado
- [ ] Performance benchmarks ejecutados y documentados
- [ ] Security review completado
- [ ] Sprint report documentado
- [ ] Product Owner sign-off obtenido

---

## Riesgos Identificados

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| Whisper performance issues | Alta | Alto | Usar modelo "tiny" inicialmente, optimizar después |
| YouTube API rate limits | Media | Alto | Implementar backoff agresivo, caching |
| Memory issues (videos largos) | Media | Medio | Streaming processing, límites de duración |
| FFmpeg installation issues | Baja | Alto | Dockerfile con FFmpeg preinstalado |
| Disk space insufficient | Baja | Medio | Cleanup agresivo, monitoring |

---

## Notas Técnicas Generales

### Tecnologías Clave del Sprint

- **YoutubeExplode:** Metadata extraction & download
- **FFmpeg:** Audio extraction & normalization
- **Whisper:** Speech-to-text transcription
- **Hangfire:** Background job orchestration
- **SignalR:** Real-time progress updates
- **Polly:** Retry policies & resilience
- **EF Core:** Data access & persistence
- **MySQL:** Database storage

### Configuración de Desarrollo

**appsettings.Local.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=youtube_rag_local;User=root;Password=***;"
  },
  "Processing": {
    "TempFilePath": "C:\\Temp\\YoutubeRag",
    "MaxVideoSizeMB": 2048,
    "FFmpegPath": "ffmpeg",
    "CleanupAfterHours": 24
  },
  "Whisper": {
    "ModelsPath": "C:\\Models\\Whisper",
    "DefaultModel": "auto",
    "ForceModel": null
  },
  "YouTube": {
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "MaxVideoDurationSeconds": 14400
  }
}
```

### Dependencias Externas a Instalar

1. **FFmpeg:** `winget install ffmpeg` o descargar de ffmpeg.org
2. **Whisper:** `pip install openai-whisper` o usar Whisper.NET
3. **MySQL:** Ya instalado (Docker WSL)
4. **Redis:** Ya instalado (Docker WSL)

---

## Checklist de Inicio de Sprint

Antes de comenzar el desarrollo:

- [ ] Todas las historias revisadas y entendidas
- [ ] Dependencias externas instaladas (FFmpeg, Whisper)
- [ ] Directorio temp creado: `C:\Temp\YoutubeRag`
- [ ] Directorio models creado: `C:\Models\Whisper`
- [ ] Database migrations ejecutadas
- [ ] Hangfire dashboard accesible: http://localhost:62787/hangfire
- [ ] SignalR endpoint accesible
- [ ] Git branch creada: `YRUS-0101_enviar_url_youtube`
- [ ] appsettings.Local.json configurado
- [ ] Build exitoso inicial

---

## Checklist de Fin de Sprint

Al completar el Sprint 2:

- [ ] Todas las historias mergeadas a develop
- [ ] Testing manual completo ejecutado
- [ ] Regresión automática ejecutada (>85% passing)
- [ ] Performance benchmarks documentados
- [ ] Sprint Report creado
- [ ] Known issues documentados
- [ ] Technical debt identificado
- [ ] Product Owner sign-off obtenido
- [ ] Retrospectiva completada
- [ ] Sprint 3 planning iniciado

---

## Contacto y Soporte

**Product Owner:** Senior Product Owner
**Technical Lead:** Desarrollador Principal
**Documentación:** C:\agents\youtube_rag_net\

**Documentos Relacionados:**
- SPRINT_2_PLAN.md
- PRODUCT_BACKLOG.md
- WORKFLOW_METHODOLOGY.md
- TESTING_METHODOLOGY_RULES.md
- ROLES_METHODOLOGY.md

---

**Versión:** 1.0
**Estado:** READY FOR EXECUTION
**Próxima Revisión:** Fin de Sprint 2 (17 de Octubre, 2025)
**Aprobado por:** Product Owner + Technical Lead

---

**FIN DEL DOCUMENTO - SPRINT 2 USER STORIES**
