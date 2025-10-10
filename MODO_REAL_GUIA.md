# 🚀 Guía para Usar el Sistema YouTube RAG en Modo Real

## 📋 Resumen

El sistema YouTube RAG .NET ahora está completamente implementado para funcionar en **modo Real** con procesamiento real de videos, transcripción con IA y búsqueda semántica.

---

## 🎯 Modos Disponibles

### 1. **Modo Mock (Development)**
- **Propósito**: Desarrollo rápido y testing
- **Características**: Datos simulados, respuestas inmediatas
- **Configuración**: `ProcessingMode: "Mock"`, `EnableRealProcessing: false`

### 2. **Modo Real (Production/Testing)**
- **Propósito**: Procesamiento real de videos de YouTube
- **Características**: Descarga real, transcripción con OpenAI Whisper, embeddings reales
- **Configuración**: `ProcessingMode: "Real"`, `EnableRealProcessing: true`

---

## ⚙️ Configuración para Modo Real

### **Paso 1: Configurar OpenAI API Key**

#### En `appsettings.Real.json` o `appsettings.Production.json`:
```json
{
  "OpenAI": {
    "ApiKey": "sk-your-actual-openai-api-key-here"
  }
}
```

#### O usando variables de entorno:
```bash
export OPENAI__APIKEY="sk-your-actual-openai-api-key-here"
```

### **Paso 2: Configurar Base de Datos**

#### MySQL (Principal):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=youtube_rag_real;Uid=youtube_rag_user;Pwd=secure_password;"
  }
}
```

#### PostgreSQL (Para vectores - opcional):
```json
{
  "ConnectionStrings": {
    "VectorDatabase": "Host=localhost;Database=youtube_rag_vectors;Username=postgres;Password=secure_password;"
  }
}
```

### **Paso 3: Instalar Dependencias del Sistema**

#### FFmpeg (para procesamiento de audio):
```bash
# Windows (con Chocolatey)
choco install ffmpeg

# macOS (con Homebrew)
brew install ffmpeg

# Ubuntu/Debian
sudo apt update && sudo apt install ffmpeg
```

---

## 🚀 Comandos para Ejecutar

### **Modo Development (Mock):**
```bash
dotnet run --environment Development
```

### **Modo Real (con procesamiento real):**
```bash
dotnet run --configuration Real
# O alternativamente:
ASPNETCORE_ENVIRONMENT=Real dotnet run
```

### **Modo Production:**
```bash
dotnet run --environment Production
```

---

## 🎥 Funcionalidades del Modo Real

### **1. Descarga Real de Videos de YouTube**
```http
POST /api/v1/videos/from-url
Content-Type: application/json

{
  "url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
  "title": "Mi video de YouTube",
  "description": "Video para procesar con IA"
}
```

**Respuesta Real:**
```json
{
  "id": "abc123-def456",
  "title": "Mi video de YouTube",
  "youtube_id": "dQw4w9WgXcQ",
  "thumbnail_url": "https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
  "status": "Processing",
  "processing_progress": 5,
  "message": "Video processing from URL started - real processing",
  "created_at": "2025-01-15T10:30:00Z"
}
```

### **2. Seguimiento de Progreso Real**
```http
GET /api/v1/videos/{videoId}/progress
```

**Respuesta Real:**
```json
{
  "video_id": "abc123-def456",
  "status": "Processing",
  "progress": 75,
  "current_stage": "transcription",
  "stages": [
    {
      "name": "download",
      "status": "completed",
      "progress": 100,
      "started_at": "2025-01-15T10:30:00Z",
      "completed_at": "2025-01-15T10:32:00Z"
    },
    {
      "name": "audio_extraction",
      "status": "completed",
      "progress": 100,
      "started_at": "2025-01-15T10:32:00Z",
      "completed_at": "2025-01-15T10:33:00Z"
    },
    {
      "name": "transcription",
      "status": "running",
      "progress": 75,
      "started_at": "2025-01-15T10:33:00Z"
    },
    {
      "name": "embedding",
      "status": "pending",
      "progress": 0
    }
  ],
  "estimated_completion": "2025-01-15T10:38:00Z",
  "mode": "real"
}
```

### **3. Búsqueda Semántica Real**
```http
POST /api/v1/search/semantic
Content-Type: application/json

{
  "query": "machine learning and artificial intelligence",
  "maxResults": 10,
  "minRelevanceScore": 0.7
}
```

**Respuesta Real:**
```json
{
  "query": "machine learning and artificial intelligence",
  "results": [
    {
      "video_id": "abc123-def456",
      "video_title": "Introduction to AI",
      "segment_id": "seg_001",
      "segment_text": "Machine learning is a subset of artificial intelligence that enables computers to learn...",
      "start_time": 45.2,
      "end_time": 52.8,
      "relevance_score": 0.942,
      "youtube_url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
      "thumbnail_url": "https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg"
    }
  ],
  "total_results": 1,
  "search_type": "semantic",
  "processing_time_ms": 234.5,
  "mode": "real",
  "min_relevance_score": 0.7
}
```

---

## 🔧 Servicios Implementados

### **Real Services:**
1. **YouTubeService**: Descarga real usando `YoutubeExplode`
2. **TranscriptionService**: Transcripción real con OpenAI Whisper
3. **EmbeddingService**: Embeddings reales con OpenAI text-embedding-3-small
4. **VideoProcessingService**: Orquestación completa del procesamiento
5. **JobService**: Gestión de trabajos asincrónicos

### **Mock Services** (para desarrollo):
- Versiones simuladas de todos los servicios
- Respuestas rápidas con datos de ejemplo
- Útiles para desarrollo frontend y testing

---

## 📊 Diferencias: Mock vs Real

| Característica | Mock Mode | Real Mode |
|----------------|-----------|-----------|
| **Descarga de Video** | Archivo simulado | Descarga real de YouTube |
| **Transcripción** | Texto predefinido | OpenAI Whisper API |
| **Embeddings** | Vectores aleatorios | OpenAI Embeddings API |
| **Tiempo de Proceso** | 2-5 segundos | 5-15 minutos |
| **Costo** | Gratuito | Consume API credits |
| **Dependencias** | Ninguna | OpenAI API, FFmpeg |
| **Base de Datos** | Mínima | Completa con vectores |

---

## 💡 Casos de Uso Reales

### **1. Análisis de Contenido Educativo**
```bash
# Procesar un video educativo
curl -X POST http://localhost:5000/api/v1/videos/from-url \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://www.youtube.com/watch?v=EDUCATIONAL_VIDEO_ID",
    "title": "Curso de Machine Learning",
    "description": "Video educativo para análisis de contenido"
  }'
```

### **2. Búsqueda en Biblioteca de Videos**
```bash
# Buscar conceptos específicos
curl -X POST http://localhost:5000/api/v1/search/semantic \
  -H "Content-Type: application/json" \
  -d '{
    "query": "redes neuronales convolucionales",
    "maxResults": 5,
    "minRelevanceScore": 0.8
  }'
```

### **3. Transcripción para Accessibility**
- Procesar videos para generar subtítulos automáticos
- Extraer transcripciones para documentación
- Crear índices searchables de contenido de video

---

## ⚠️ Consideraciones Importantes

### **Costos de OpenAI:**
- **Whisper**: ~$0.006 por minuto de audio
- **Embeddings**: ~$0.0001 per 1K tokens
- **Video de 10 minutos**: ~$0.06-0.10 total

### **Tiempo de Procesamiento:**
- **Video corto (2-5 min)**: 3-8 minutos
- **Video medio (10-15 min)**: 8-15 minutos
- **Video largo (30+ min)**: 20-45 minutos

### **Almacenamiento:**
- Archivos temporales de audio/video
- Transcripciones en base de datos
- Vectores de embeddings (1536 dimensiones por segmento)

---

## 🔄 Migración Mock → Real

### **Para activar Modo Real:**

1. **Actualizar configuración:**
```json
{
  "AppSettings": {
    "ProcessingMode": "Real",
    "EnableRealProcessing": true
  }
}
```

2. **Agregar OpenAI API Key**
3. **Instalar FFmpeg**
4. **Configurar base de datos de vectores (opcional)**
5. **Ejecutar con configuración Real**

### **Comando:**
```bash
dotnet run --configuration Real
```

### **Verificar modo:**
```bash
curl http://localhost:5000/
# Verificar que "mode": "real" aparece en la respuesta
```

---

## ✅ Resultado Final

El sistema YouTube RAG .NET ahora soporta:

- ✅ **Procesamiento real** de videos de YouTube
- ✅ **Transcripción con IA** usando OpenAI Whisper
- ✅ **Búsqueda semántica** con embeddings reales
- ✅ **Modo Mock** para desarrollo rápido
- ✅ **Configuración flexible** por entorno
- ✅ **Arquitectura escalable** sin duplicación de código

**¡El sistema está listo para procesar videos reales con datos reales y IA real!** 🎉