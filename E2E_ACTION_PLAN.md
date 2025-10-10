# Plan de Acción - Resolución de Problemas E2E
## YoutubeRag Pipeline de Ingesta de Videos

**Fecha**: 2025-10-06
**Prioridad**: CRÍTICA
**Owner**: Equipo de Desarrollo

---

## 🎯 Objetivo

Resolver el problema crítico de Hangfire workers que impide el procesamiento de videos y lograr una tasa de éxito E2E del 100% en la suite de pruebas.

---

## 🔴 Problema Crítico: Hangfire Workers No Procesan Jobs

### Checklist de Diagnóstico

#### 1. Verificar Logs de la API

```bash
# Ubicación esperada de logs
C:\agents\youtube_rag_net\logs\youtuberag-local-*.log

# Buscar errores de Hangfire
grep -i "hangfire\|worker\|job" logs/youtuberag-local-*.log

# Buscar excepciones
grep -i "exception\|error\|fail" logs/youtuberag-local-*.log | grep -i hangfire
```

**Qué buscar**:
- ❌ Errores de conexión a MySQL
- ❌ Excepciones al iniciar workers
- ❌ Timeouts en queries de Hangfire
- ❌ Problemas de serialización/deserialización
- ❌ Assembly loading errors

#### 2. Verificar Configuración de Hangfire

**Archivo**: `YoutubeRag.Api/Program.cs`

```csharp
// Verificar que esta configuración existe y está activa
builder.Services.AddHangfire(config =>
{
    config.UseMySqlStorage(
        connectionString,
        new MySqlStorageOptions { /* options */ }
    );
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 3; // Debe estar presente
    options.ServerName = "YoutubeRag-Local";
});
```

**Checklist**:
- [ ] `AddHangfire` está llamado
- [ ] `AddHangfireServer` está llamado
- [ ] `WorkerCount` está configurado (esperado: 3)
- [ ] `UseHangfireDashboard` está activo
- [ ] Connection string es correcta

#### 3. Verificar Base de Datos Hangfire

```sql
-- Conectar a MySQL
mysql -h localhost -u youtube_rag_user -p youtube_rag_local

-- Verificar tablas de Hangfire
SHOW TABLES LIKE 'Hangfire%';

-- Verificar jobs encolados
SELECT * FROM HangfireJob WHERE StateName = 'Enqueued' ORDER BY CreatedAt DESC LIMIT 10;

-- Verificar servers activos
SELECT * FROM HangfireServer ORDER BY LastHeartbeat DESC;

-- Verificar jobs fallidos
SELECT * FROM HangfireJob WHERE StateName = 'Failed' ORDER BY CreatedAt DESC LIMIT 10;
```

**Qué verificar**:
- [ ] Tablas Hangfire existen
- [ ] Jobs están en tabla HangfireJob
- [ ] Hay servers en tabla HangfireServer con LastHeartbeat reciente
- [ ] No hay locks bloqueando procesamiento

#### 4. Verificar Dashboard de Hangfire

**URL**: https://localhost:62787/hangfire

**Checklist**:
- [ ] Dashboard es accesible
- [ ] Sección "Servers" muestra al menos 1 server activo
- [ ] Server tiene Workers: 3
- [ ] Queues muestra "default" con jobs encolados
- [ ] No hay errores en Failed Jobs

**Capturas a revisar**:
- Pestaña "Jobs" → "Enqueued"
- Pestaña "Jobs" → "Processing"
- Pestaña "Jobs" → "Failed"
- Pestaña "Servers"

#### 5. Verificar Configuración de Base de Datos

**Archivo**: `YoutubeRag.Api/appsettings.Local.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=youtube_rag_local;Uid=youtube_rag_user;Pwd=youtube_rag_password;"
  }
}
```

**Acción**: Verificar que la DB existe y es accesible

```bash
# Test de conexión
mysql -h localhost -P 3306 -u youtube_rag_user -p -e "USE youtube_rag_local; SELECT COUNT(*) FROM Videos;"
```

---

## 🟠 Problemas Adicionales

### Problema 2: Endpoint de Progreso Desconectado

**Ubicación**: `YoutubeRag.Api/Controllers/VideosController.cs:145-179`

**Situación Actual**:
```csharp
[HttpGet("{videoId}/progress")]
public async Task<ActionResult> GetVideoProgress(string videoId)
{
    var progress = await _videoProcessingService.GetProcessingProgressAsync(videoId);
    // ... retorna información de VideoProcessingService
}
```

**Problema**: `VideoProcessingService` retorna mock data, no el estado real de `TranscriptionJobProcessor`

**Soluciones Propuestas**:

#### Opción A: Modificar endpoint para usar Job real

```csharp
[HttpGet("{videoId}/progress")]
public async Task<ActionResult> GetVideoProgress(string videoId)
{
    // Get the real transcription job for this video
    var job = await _jobRepository.GetByVideoIdAsync(videoId, JobType.Transcription);

    if (job == null)
        return NotFound(new { error = "Job not found" });

    // Return real job progress
    return Ok(new {
        video_id = videoId,
        job_id = job.Id,
        status = job.Status.ToString(),
        progress = job.Progress,
        started_at = job.StartedAt,
        completed_at = job.CompletedAt,
        error_message = job.ErrorMessage,
        hangfire_job_id = job.HangfireJobId
    });
}
```

#### Opción B: Bridge entre VideoProcessingService y Jobs reales

```csharp
public class VideoProgressAdapter : IVideoProgressService
{
    private readonly IJobRepository _jobRepository;
    private readonly IVideoRepository _videoRepository;

    public async Task<VideoProgressDto> GetProgressAsync(string videoId)
    {
        var video = await _videoRepository.GetByIdAsync(videoId);
        var jobs = await _jobRepository.GetByVideoIdAsync(videoId);

        // Combine information from video and jobs
        return new VideoProgressDto
        {
            VideoId = videoId,
            Status = video.Status,
            Jobs = jobs.Select(j => new JobProgressDto
            {
                JobId = j.Id,
                Type = j.Type,
                Status = j.Status,
                Progress = j.Progress
            }).ToList()
        };
    }
}
```

**Decisión Requerida**: Elegir opción A o B

---

### Problema 3: Autorización en GET /api/v1/videos/{id}

**Ubicación**: `YoutubeRag.Api/Controllers/VideosController.cs:184-222`

**Código Actual**:
```csharp
// Check if the current user owns the video
if (!string.IsNullOrEmpty(videoDto.UserId) && videoDto.UserId != userId)
{
    return StatusCode(403, new { error = "FORBIDDEN" });
}
```

**Problema**: Mock auth crea `test-user-id`, pero ingestion usa userId del request

**Solución Temporal (para tests)**:

```csharp
// In MockAuthenticationHandler.cs
protected override Task<AuthenticateResult> HandleAuthenticateAsync()
{
    // Use consistent userId for all tests
    claims.Add(new Claim(ClaimTypes.NameIdentifier, "test-user"));

    // OR read from environment variable for flexibility
    var testUserId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? "test-user";
    claims.Add(new Claim(ClaimTypes.NameIdentifier, testUserId));
}
```

**Solución Permanente**:

```csharp
// In appsettings.Local.json
{
  "AppSettings": {
    "EnableAuth": false,
    "BypassOwnershipCheck": true  // New setting
  }
}

// In VideosController.cs
if (_appSettings.BypassOwnershipCheck || videoDto.UserId == userId)
{
    // Allow access
}
```

---

## 📋 Plan de Ejecución

### Fase 1: Diagnóstico (Estimado: 2-4 horas)

| # | Tarea | Owner | Estimación | Completado |
|---|-------|-------|------------|------------|
| 1.1 | Revisar logs de API | Dev | 30 min | [ ] |
| 1.2 | Verificar configuración Hangfire en Program.cs | Dev | 15 min | [ ] |
| 1.3 | Consultar DB MySQL para estado de Hangfire | Dev | 30 min | [ ] |
| 1.4 | Revisar Hangfire Dashboard | Dev | 15 min | [ ] |
| 1.5 | Verificar conectividad a DB | Dev | 15 min | [ ] |
| 1.6 | Documentar hallazgos | Dev | 30 min | [ ] |

**Entregables**: Documento con causa raíz identificada

---

### Fase 2: Corrección de Hangfire Workers (Estimado: 2-6 horas)

**Escenario A: Workers no iniciados**

```csharp
// En Program.cs, verificar orden de llamadas
app.UseHangfireDashboard();  // Debe estar antes de UseEndpoints

// Asegurar que workers se inician
var app = builder.Build();
// ... otros middlewares
app.MapHangfireDashboard();  // Si usas MapHangfireDashboard
```

**Escenario B: Problema de serialización**

```csharp
// Verificar que métodos de background jobs son serializables
// INCORRECTO:
BackgroundJob.Enqueue(() => ProcessVideo(videoObject));  // ❌ No serializable

// CORRECTO:
BackgroundJob.Enqueue<TranscriptionJobProcessor>(
    x => x.ProcessTranscriptionJobAsync(videoId, CancellationToken.None)
);  // ✅ Serializable
```

**Escenario C: Connection pool agotado**

```csharp
// En Program.cs, configurar connection pool
builder.Services.AddHangfire(config =>
{
    config.UseMySqlStorage(connectionString, new MySqlStorageOptions
    {
        PrepareSchemaIfNecessary = true,
        TransactionTimeout = TimeSpan.FromMinutes(5),
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        QueuePollInterval = TimeSpan.FromSeconds(15),

        // AGREGAR:
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        TablesPrefix = "Hangfire"
    });
});
```

**Escenario D: Restart necesario**

```bash
# Detener API
pkill -f "YoutubeRag.Api"

# Limpiar jobs atascados en DB
mysql -u youtube_rag_user -p youtube_rag_local -e "
UPDATE HangfireJob SET StateName = 'Enqueued' WHERE StateName = 'Processing';
"

# Reiniciar API
dotnet run --project YoutubeRag.Api
```

| # | Tarea | Owner | Estimación | Completado |
|---|-------|-------|------------|------------|
| 2.1 | Implementar fix según causa raíz | Dev | 1-3 hrs | [ ] |
| 2.2 | Reiniciar API y servicios | Dev | 15 min | [ ] |
| 2.3 | Verificar workers activos en dashboard | Dev | 15 min | [ ] |
| 2.4 | Ejecutar test con 1 video corto | QA | 5 min | [ ] |
| 2.5 | Validar procesamiento completo | QA | 15 min | [ ] |

**Entregables**: Workers procesando jobs exitosamente

---

### Fase 3: Re-ejecución de Suite E2E (Estimado: 1 hora)

```powershell
# Limpiar estado anterior
mysql -u youtube_rag_user -p youtube_rag_local -e "
DELETE FROM TranscriptSegments;
DELETE FROM Jobs;
DELETE FROM Videos;
"

Remove-Item -Path ".\data\audio\*" -Force -ErrorAction SilentlyContinue

# Ejecutar suite E2E
.\run_e2e_tests.ps1
```

| # | Tarea | Owner | Estimación | Completado |
|---|-------|-------|------------|------------|
| 3.1 | Limpiar DB y archivos | QA | 5 min | [ ] |
| 3.2 | Ejecutar run_e2e_tests.ps1 | QA | 30 min | [ ] |
| 3.3 | Analizar resultados | QA | 15 min | [ ] |
| 3.4 | Generar reporte actualizado | QA | 10 min | [ ] |

**Criterio de Éxito**: 4/5 videos (80%) procesados exitosamente

---

### Fase 4: Correcciones Adicionales (Estimado: 2-4 horas)

| # | Tarea | Owner | Estimación | Completado |
|---|-------|-------|------------|------------|
| 4.1 | Unificar endpoint de progreso | Dev | 1-2 hrs | [ ] |
| 4.2 | Corregir autenticación en tests | Dev | 30 min | [ ] |
| 4.3 | Alinear configuración de DB | Dev | 15 min | [ ] |
| 4.4 | Mejorar logging | Dev | 1 hr | [ ] |

---

### Fase 5: Validación de Fallbacks (Estimado: 2 horas)

| # | Tarea | Owner | Estimación | Completado |
|---|-------|-------|------------|------------|
| 5.1 | Buscar video que genera 403 | QA | 30 min | [ ] |
| 5.2 | Probar fallback yt-dlp metadata | QA | 30 min | [ ] |
| 5.3 | Probar fallback yt-dlp audio | QA | 30 min | [ ] |
| 5.4 | Documentar resultados | QA | 30 min | [ ] |

---

## 📊 Criterios de Éxito

### Mínimos Aceptables

- [ ] Al menos 1 worker activo en Hangfire
- [ ] Jobs pasan de "Enqueued" a "Processing" en <10 segundos
- [ ] Al menos 1 video se procesa completamente end-to-end
- [ ] Audio extraído y almacenado en ./data/audio
- [ ] Transcripción generada por Whisper
- [ ] Segmentos almacenados en DB

### Ideales

- [ ] 3 workers activos (según configuración)
- [ ] 80%+ de videos procesados exitosamente
- [ ] Tiempo promedio de procesamiento <120 segundos
- [ ] Endpoint de progreso retorna información real
- [ ] Fallbacks probados y funcionando
- [ ] Suite E2E automatizada en CI/CD

---

## 🚨 Escalación

### Si el problema persiste después de 8 horas

1. **Revisar diseño del pipeline**
   - ¿Es Hangfire la mejor solución para este caso?
   - ¿Deberíamos considerar alternativas (RabbitMQ, Azure Service Bus)?

2. **Consultar con expertos**
   - Buscar ayuda en comunidad de Hangfire
   - Revisar issues similares en GitHub

3. **Implementar workaround temporal**
   - Procesamiento sincrónico para videos cortos
   - Queue manual con polling

---

## 📝 Registro de Ejecución

### Sesión 1: 2025-10-06

**Participantes**:
- Desarrollador: [Nombre]
- QA: [Nombre]

**Actividades**:
- [X] Fase 1.1: Revisar logs - Resultado: [Hallazgos]
- [ ] Fase 1.2: Verificar configuración
- [ ] Fase 1.3: Consultar DB
- [ ] ...

**Hallazgos**:
- [Describir hallazgos aquí]

**Decisiones**:
- [Decisiones tomadas]

**Próximos Pasos**:
- [Acciones para siguiente sesión]

---

## 📞 Contactos

**Development Lead**: [Nombre]
**QA Lead**: [Nombre]
**DevOps**: [Nombre]

---

**Documento creado por**: Claude Code (Senior Test Engineer)
**Última actualización**: 2025-10-06
**Versión**: 1.0
