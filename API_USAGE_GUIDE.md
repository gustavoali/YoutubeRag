# YouTube RAG API - Guía de Uso

**Versión**: 1.0
**Base URL**: `http://localhost:5000` (desarrollo)
**Swagger UI**: `http://localhost:5000/swagger`
**Hangfire Dashboard**: `http://localhost:5000/hangfire`

---

## 📋 Tabla de Contenidos

1. [Autenticación](#autenticación)
2. [Ingesta de Videos](#ingesta-de-videos)
3. [Consulta de Videos](#consulta-de-videos)
4. [Transcripciones](#transcripciones)
5. [Búsqueda Semántica](#búsqueda-semántica)
6. [Monitoreo de Jobs](#monitoreo-de-jobs)
7. [Códigos de Error](#códigos-de-error)
8. [Rate Limiting](#rate-limiting)

---

## 🔐 Autenticación

### 1. Registro de Usuario

```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "username": "usuario123",
  "email": "usuario@example.com",
  "password": "SecurePassword123!",
  "fullName": "Juan Pérez"
}
```

**Response 201 Created:**
```json
{
  "userId": "guid-string",
  "username": "usuario123",
  "email": "usuario@example.com",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh-token-guid",
  "expiresIn": 3600
}
```

### 2. Login

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "usuario123",
  "password": "SecurePassword123!"
}
```

**Response 200 OK:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh-token-guid",
  "expiresIn": 3600,
  "user": {
    "id": "guid-string",
    "username": "usuario123",
    "email": "usuario@example.com",
    "fullName": "Juan Pérez"
  }
}
```

### 3. Refresh Token

```http
POST /api/v1/auth/refresh
Content-Type: application/json

{
  "refreshToken": "refresh-token-guid"
}
```

**Response 200 OK:**
```json
{
  "accessToken": "new-jwt-token",
  "refreshToken": "new-refresh-token",
  "expiresIn": 3600
}
```

### 4. Usar Token en Requests

Incluir el token en el header `Authorization`:

```http
GET /api/v1/videos
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

---

## 📥 Ingesta de Videos

### 1. Ingerir Video desde URL de YouTube

Este es el endpoint principal para procesar un video.

```http
POST /api/v1/videos/ingest
Authorization: Bearer {token}
Content-Type: application/json

{
  "url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
  "title": "Título personalizado (opcional)",
  "description": "Descripción personalizada (opcional)",
  "priority": 1
}
```

**Parámetros:**
- `url` (requerido): URL del video de YouTube
- `title` (opcional): Título personalizado, si no se proporciona se usa el de YouTube
- `description` (opcional): Descripción personalizada
- `priority` (opcional): 0=Low, 1=Normal (default), 2=High, 3=Critical

**Formatos de URL soportados:**
- `https://www.youtube.com/watch?v=VIDEO_ID`
- `https://youtu.be/VIDEO_ID`
- `https://www.youtube.com/embed/VIDEO_ID`
- `https://www.youtube.com/v/VIDEO_ID`

**Response 200 OK:**
```json
{
  "videoId": "550e8400-e29b-41d4-a716-446655440000",
  "jobId": "660e8400-e29b-41d4-a716-446655440001",
  "youTubeId": "dQw4w9WgXcQ",
  "status": "Pending",
  "message": "Video processing from URL started - real processing",
  "submittedAt": "2025-10-03T12:00:00Z",
  "progressUrl": "/api/v1/videos/550e8400-e29b-41d4-a716-446655440000/progress"
}
```

**Response 400 Bad Request (URL inválida):**
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Must be a valid YouTube URL"
  }
}
```

**Response 409 Conflict (Video duplicado):**
```json
{
  "error": {
    "code": "DUPLICATE_VIDEO",
    "message": "This video has already been ingested by user"
  }
}
```

### 2. Verificar Progreso

```http
GET /api/v1/videos/{videoId}/progress
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
{
  "video_id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Processing",
  "progress": 45,
  "current_stage": "transcription",
  "stages": [
    {
      "name": "metadata_extraction",
      "status": "Completed",
      "progress": 100,
      "started_at": "2025-10-03T12:00:00Z",
      "completed_at": "2025-10-03T12:00:30Z",
      "error_message": null
    },
    {
      "name": "transcription",
      "status": "Running",
      "progress": 45,
      "started_at": "2025-10-03T12:00:30Z",
      "completed_at": null,
      "error_message": null
    },
    {
      "name": "embedding_generation",
      "status": "Pending",
      "progress": 0,
      "started_at": null,
      "completed_at": null,
      "error_message": null
    }
  ],
  "estimated_completion": "2025-10-03T12:15:00Z",
  "error_message": null,
  "mode": "real"
}
```

**Estados de Procesamiento:**
- `Pending`: Esperando para empezar
- `Running`: En ejecución
- `Completed`: Completado exitosamente
- `Failed`: Falló con error
- `Cancelled`: Cancelado por usuario

**Progreso por Etapa:**
- **metadata_extraction**: 0-100%
- **transcription**:
  - 0%: Inicio
  - 30%: Audio extraído
  - 70%: Transcripción completada
  - 90%: Guardando segmentos
  - 100%: Completo
- **embedding_generation**:
  - 0%: Inicio
  - 20%: Cargando segmentos
  - 40-80%: Generando embeddings (incremental)
  - 95%: Guardando
  - 100%: Completo

---

## 📹 Consulta de Videos

### 1. Listar Mis Videos

```http
GET /api/v1/videos?page=1&pageSize=20&search=tutorial&status=Completed&sortBy=created_at&sortOrder=desc
Authorization: Bearer {token}
```

**Parámetros de Query:**
- `page` (opcional, default: 1): Número de página
- `pageSize` (opcional, default: 20, max: 100): Items por página
- `search` (opcional): Buscar en título y descripción
- `status` (opcional): Filtrar por estado (Pending, Processing, Completed, Failed, Cancelled)
- `sortBy` (opcional, default: "created_at"): Campo para ordenar
- `sortOrder` (opcional, default: "desc"): Dirección (asc, desc)

**Response 200 OK:**
```json
{
  "videos": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "title": "Tutorial de Python",
      "description": "Aprende Python desde cero",
      "youTubeId": "dQw4w9WgXcQ",
      "thumbnailUrl": "https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
      "duration": "00:10:30",
      "processingStatus": "Completed",
      "transcriptionStatus": "Completed",
      "embeddingStatus": "Completed",
      "viewCount": 1000000,
      "likeCount": 50000,
      "channelTitle": "CodeMaster",
      "publishedAt": "2025-01-01T00:00:00Z",
      "createdAt": "2025-10-03T12:00:00Z",
      "transcribedAt": "2025-10-03T12:05:00Z",
      "embeddedAt": "2025-10-03T12:10:00Z"
    }
  ],
  "total": 50,
  "page": 1,
  "page_size": 20,
  "total_pages": 3,
  "has_more": true
}
```

### 2. Obtener Detalles de Video

```http
GET /api/v1/videos/{videoId}
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "user-guid",
  "title": "Tutorial de Python",
  "description": "Aprende Python desde cero con este tutorial completo",
  "youTubeId": "dQw4w9WgXcQ",
  "url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
  "thumbnailUrl": "https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
  "duration": "00:10:30",
  "language": "es",

  "processingStatus": "Completed",
  "transcriptionStatus": "Completed",
  "embeddingStatus": "Completed",
  "processingProgress": 100,
  "embeddingProgress": 100,

  "viewCount": 1000000,
  "likeCount": 50000,
  "publishedAt": "2025-01-01T00:00:00Z",
  "channelId": "UCxxxxxx",
  "channelTitle": "CodeMaster",
  "categoryId": "28",
  "tags": ["python", "tutorial", "programming"],

  "createdAt": "2025-10-03T12:00:00Z",
  "updatedAt": "2025-10-03T12:10:00Z",
  "transcribedAt": "2025-10-03T12:05:00Z",
  "embeddedAt": "2025-10-03T12:10:00Z",

  "transcriptSegmentCount": 150,
  "jobs": [
    {
      "id": "job-guid-1",
      "type": "Transcription",
      "status": "Completed",
      "progress": 100,
      "completedAt": "2025-10-03T12:05:00Z"
    },
    {
      "id": "job-guid-2",
      "type": "EmbeddingGeneration",
      "status": "Completed",
      "progress": 100,
      "completedAt": "2025-10-03T12:10:00Z"
    }
  ]
}
```

**Response 404 Not Found:**
```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "Video not found"
  }
}
```

**Response 403 Forbidden:**
```json
{
  "error": {
    "code": "FORBIDDEN",
    "message": "You do not have permission to view this video"
  }
}
```

### 3. Actualizar Metadata de Video

```http
PATCH /api/v1/videos/{videoId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Nuevo título",
  "description": "Nueva descripción"
}
```

**Response 200 OK:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "message": "Video updated successfully",
  "video": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "title": "Nuevo título",
    "description": "Nueva descripción",
    "updatedAt": "2025-10-03T13:00:00Z"
  }
}
```

### 4. Eliminar Video

```http
DELETE /api/v1/videos/{videoId}
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
{
  "message": "Video deleted successfully"
}
```

**Nota:** La eliminación es en cascada - también elimina:
- Todos los segmentos de transcripción
- Todos los jobs asociados
- Archivos de audio temporales (si existen)

---

## 📝 Transcripciones

### 1. Obtener Transcripción Completa

```http
GET /api/v1/videos/{videoId}/transcript
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
{
  "video_id": "550e8400-e29b-41d4-a716-446655440000",
  "total_segments": 150,
  "language": "es",
  "duration": "00:10:30",
  "segments": [
    {
      "id": "segment-guid-1",
      "segment_index": 0,
      "start_time": 0.0,
      "end_time": 3.5,
      "text": "Bienvenidos a este tutorial de Python",
      "language": "es",
      "confidence": 0.95,
      "speaker": null,
      "has_embedding": true
    },
    {
      "id": "segment-guid-2",
      "segment_index": 1,
      "start_time": 3.5,
      "end_time": 8.2,
      "text": "En este video aprenderemos los conceptos básicos",
      "language": "es",
      "confidence": 0.92,
      "speaker": null,
      "has_embedding": true
    }
  ]
}
```

### 2. Obtener Segmentos por Rango de Tiempo

```http
GET /api/v1/videos/{videoId}/transcript?startTime=60.0&endTime=120.0
Authorization: Bearer {token}
```

Obtiene solo los segmentos entre 1:00 y 2:00 minutos.

### 3. Buscar en Transcripción

```http
GET /api/v1/videos/{videoId}/transcript/search?q=python&limit=10
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
{
  "query": "python",
  "total_matches": 15,
  "segments": [
    {
      "id": "segment-guid",
      "segment_index": 5,
      "start_time": 15.2,
      "end_time": 20.5,
      "text": "Python es un lenguaje de programación versátil",
      "match_score": 0.95,
      "has_embedding": true
    }
  ]
}
```

---

## 🔍 Búsqueda Semántica

### 1. Búsqueda Semántica en Todos los Videos

```http
GET /api/v1/search/semantic?query=cómo instalar dependencias&limit=10&threshold=0.7
Authorization: Bearer {token}
```

**Parámetros:**
- `query` (requerido): Texto de búsqueda
- `limit` (opcional, default: 10): Máximo de resultados
- `threshold` (opcional, default: 0.7): Umbral de similitud (0.0-1.0)
- `videoId` (opcional): Buscar solo en un video específico

**Response 200 OK:**
```json
{
  "query": "cómo instalar dependencias",
  "total_results": 25,
  "results": [
    {
      "segment_id": "segment-guid-1",
      "video_id": "video-guid-1",
      "video_title": "Tutorial de Python",
      "segment_index": 12,
      "start_time": 45.2,
      "end_time": 52.8,
      "text": "Para instalar dependencias usa pip install",
      "similarity_score": 0.92,
      "video_url": "https://youtube.com/watch?v=..."
    },
    {
      "segment_id": "segment-guid-2",
      "video_id": "video-guid-2",
      "video_title": "Node.js para principiantes",
      "segment_index": 8,
      "start_time": 30.1,
      "end_time": 35.5,
      "text": "Con npm puedes gestionar las dependencias del proyecto",
      "similarity_score": 0.85,
      "video_url": "https://youtube.com/watch?v=..."
    }
  ]
}
```

### 2. Búsqueda Semántica en Video Específico

```http
GET /api/v1/search/semantic?query=bucles&videoId=550e8400-e29b-41d4-a716-446655440000
Authorization: Bearer {token}
```

Busca solo dentro del video especificado.

### 3. Búsqueda por Similitud de Segmento

```http
POST /api/v1/search/similar-segments
Authorization: Bearer {token}
Content-Type: application/json

{
  "segmentId": "segment-guid-reference",
  "limit": 5,
  "threshold": 0.75,
  "excludeVideoId": "video-guid-to-exclude"
}
```

Encuentra segmentos similares a un segmento de referencia.

---

## ⚙️ Monitoreo de Jobs

### 1. Listar Jobs del Usuario

```http
GET /api/v1/jobs?status=Running&type=Transcription&page=1&pageSize=20
Authorization: Bearer {token}
```

**Parámetros:**
- `status` (opcional): Pending, Running, Completed, Failed, Cancelled, Retrying
- `type` (opcional): VideoProcessing, Transcription, EmbeddingGeneration, Metadata
- `videoId` (opcional): Filtrar por video
- `page`, `pageSize`: Paginación

**Response 200 OK:**
```json
{
  "jobs": [
    {
      "id": "job-guid",
      "videoId": "video-guid",
      "videoTitle": "Tutorial de Python",
      "type": "Transcription",
      "status": "Running",
      "progress": 45,
      "priority": 1,
      "statusMessage": "Transcribing audio...",
      "startedAt": "2025-10-03T12:00:00Z",
      "createdAt": "2025-10-03T11:59:00Z",
      "retryCount": 0,
      "maxRetries": 3,
      "hangfireJobId": "hangfire-job-id"
    }
  ],
  "total": 10,
  "page": 1,
  "page_size": 20,
  "total_pages": 1
}
```

### 2. Obtener Detalles de Job

```http
GET /api/v1/jobs/{jobId}
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
{
  "id": "job-guid",
  "videoId": "video-guid",
  "userId": "user-guid",
  "type": "Transcription",
  "status": "Completed",
  "statusMessage": "Successfully transcribed 150 segments",
  "progress": 100,
  "priority": 1,
  "result": "{ \"segmentCount\": 150, \"duration\": \"00:10:30\" }",
  "errorMessage": null,
  "parameters": null,
  "metadata": "{\"VideoTitle\":\"Tutorial\",\"YouTubeId\":\"abc123\"}",
  "hangfireJobId": "hangfire-job-id",
  "workerId": "YoutubeRag-MACHINE-NAME",
  "retryCount": 0,
  "maxRetries": 3,
  "createdAt": "2025-10-03T11:59:00Z",
  "updatedAt": "2025-10-03T12:05:00Z",
  "startedAt": "2025-10-03T12:00:00Z",
  "completedAt": "2025-10-03T12:05:00Z",
  "failedAt": null
}
```

### 3. Cancelar Job

```http
POST /api/v1/jobs/{jobId}/cancel
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
{
  "message": "Job cancelled successfully",
  "jobId": "job-guid"
}
```

**Nota:** Solo se pueden cancelar jobs en estado Pending o Running.

### 4. Reintentar Job Fallido

```http
POST /api/v1/jobs/{jobId}/retry
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
{
  "message": "Job retry scheduled",
  "jobId": "job-guid",
  "newHangfireJobId": "new-hangfire-id"
}
```

---

## ❌ Códigos de Error

### Errores Comunes

| Código HTTP | Error Code | Descripción |
|-------------|------------|-------------|
| 400 | VALIDATION_ERROR | Datos de entrada inválidos |
| 400 | INVALID_URL | URL de YouTube inválida |
| 400 | NO_FILE | Archivo no proporcionado (upload) |
| 401 | UNAUTHORIZED | No autenticado o token inválido |
| 403 | FORBIDDEN | Sin permisos para el recurso |
| 404 | NOT_FOUND | Recurso no encontrado |
| 409 | DUPLICATE_VIDEO | Video ya existe para este usuario |
| 422 | PROCESSING_ERROR | Error al procesar video |
| 429 | RATE_LIMIT_EXCEEDED | Límite de requests excedido |
| 500 | INTERNAL_ERROR | Error interno del servidor |
| 503 | SERVICE_UNAVAILABLE | Servicio temporalmente no disponible |

### Formato de Error

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Descripción del error",
    "details": {
      "field": "fieldName",
      "reason": "Razón específica"
    }
  }
}
```

### Ejemplos

**400 Validation Error:**
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": {
      "url": "Must be a valid YouTube URL",
      "priority": "Must be between 0 and 3"
    }
  }
}
```

**401 Unauthorized:**
```json
{
  "error": {
    "code": "UNAUTHORIZED",
    "message": "Invalid or expired token"
  }
}
```

**429 Rate Limit:**
```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Too many requests",
    "details": {
      "limit": 100,
      "window": "1 minute",
      "retryAfter": 45
    }
  }
}
```

---

## 🚦 Rate Limiting

### Límites por Defecto

- **100 requests por minuto** por usuario autenticado
- **20 requests por minuto** por IP sin autenticar

### Headers de Rate Limit

Cada response incluye headers informativos:

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 85
X-RateLimit-Reset: 1696348800
```

### Exceder el Límite

Response cuando se excede:

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 45

{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded. Please try again in 45 seconds."
  }
}
```

---

## 🔧 Configuración del Cliente

### Headers Recomendados

```http
Authorization: Bearer {jwt-token}
Content-Type: application/json
Accept: application/json
User-Agent: MyApp/1.0
```

### Manejo de Tokens

1. **Guardar token al login**
2. **Incluir en cada request**
3. **Refrescar antes de expirar**
4. **Renovar si recibe 401**

### Ejemplo de Cliente (JavaScript)

```javascript
class YoutubeRagClient {
  constructor(baseUrl, token) {
    this.baseUrl = baseUrl;
    this.token = token;
  }

  async ingestVideo(youtubeUrl, options = {}) {
    const response = await fetch(`${this.baseUrl}/api/v1/videos/ingest`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        url: youtubeUrl,
        title: options.title,
        priority: options.priority || 1
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error.message);
    }

    return await response.json();
  }

  async checkProgress(videoId) {
    const response = await fetch(
      `${this.baseUrl}/api/v1/videos/${videoId}/progress`,
      {
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      }
    );

    return await response.json();
  }

  async waitForCompletion(videoId, pollInterval = 5000) {
    return new Promise((resolve, reject) => {
      const poll = async () => {
        const progress = await this.checkProgress(videoId);

        if (progress.status === 'Completed') {
          resolve(progress);
        } else if (progress.status === 'Failed') {
          reject(new Error(progress.error_message));
        } else {
          setTimeout(poll, pollInterval);
        }
      };

      poll();
    });
  }
}

// Uso
const client = new YoutubeRagClient('http://localhost:5000', 'your-jwt-token');

try {
  const result = await client.ingestVideo('https://youtube.com/watch?v=abc123');
  console.log('Video ingested:', result.videoId);

  // Esperar a que complete
  await client.waitForCompletion(result.videoId);
  console.log('Processing completed!');

} catch (error) {
  console.error('Error:', error.message);
}
```

---

## 📊 Mejores Prácticas

### 1. Polling Inteligente

```javascript
// No hacer polling muy frecuente
// Usar backoff exponencial

let pollInterval = 2000; // Empezar con 2 segundos
const maxInterval = 30000; // Máximo 30 segundos

async function pollWithBackoff(videoId) {
  while (true) {
    const progress = await checkProgress(videoId);

    if (progress.status === 'Completed' || progress.status === 'Failed') {
      return progress;
    }

    await sleep(pollInterval);
    pollInterval = Math.min(pollInterval * 1.5, maxInterval);
  }
}
```

### 2. Manejo de Errores

```javascript
async function ingestWithRetry(url, maxRetries = 3) {
  for (let i = 0; i < maxRetries; i++) {
    try {
      return await client.ingestVideo(url);
    } catch (error) {
      if (error.code === 'RATE_LIMIT_EXCEEDED') {
        await sleep(error.retryAfter * 1000);
        continue;
      }
      if (i === maxRetries - 1) throw error;
      await sleep(1000 * (i + 1)); // Backoff exponencial
    }
  }
}
```

### 3. Batch Processing

```javascript
// Procesar múltiples videos con rate limiting
async function ingestBatch(urls, concurrency = 3) {
  const results = [];
  const queue = [...urls];

  const workers = Array(concurrency).fill(null).map(async () => {
    while (queue.length > 0) {
      const url = queue.shift();
      try {
        const result = await client.ingestVideo(url);
        results.push({ success: true, url, result });
      } catch (error) {
        results.push({ success: false, url, error });
      }
    }
  });

  await Promise.all(workers);
  return results;
}
```

---

## 🐛 Debugging

### Logs del Servidor

Los logs incluyen `RequestId` para tracking:

```
[12:00:00 INF] Request starting HTTP/1.1 POST /api/v1/videos/ingest
[12:00:00 INF] RequestId: 0HN1234567890
[12:00:05 INF] Video ingested successfully: video-guid
[12:00:05 INF] Request finished in 5000ms
```

### Swagger UI

Probar endpoints interactivamente:
- URL: `http://localhost:5000/swagger`
- Incluye todos los endpoints documentados
- Permite probar con autenticación

### Hangfire Dashboard

Monitorear jobs en tiempo real:
- URL: `http://localhost:5000/hangfire`
- Ver jobs en cola, en proceso, completados, fallidos
- Ver logs de cada job
- Reintentar jobs manualmente

---

**Documentación generada**: 3 de octubre de 2025
**Versión API**: 1.0
**Última actualización**: Sprint 2 Review
