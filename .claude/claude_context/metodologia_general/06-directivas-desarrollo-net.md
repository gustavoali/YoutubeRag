# Directrices de Desarrollo - YouTube RAG Project (.NET)

## Fecha de creación: 2025-09-28
## Aplicando principios del proyecto Python exitoso

## 🚫 REGLA FUNDAMENTAL: NO DUPLICACIÓN + CONFIGURACIÓN UNIFICADA

### Principios Aplicados desde el Proyecto Python
Basándose en los principios exitosos aplicados al proyecto Python YouTube RAG, este proyecto .NET ha implementado las mismas directrices:

1. **UN SOLO PUNTO DE ENTRADA**: Un solo `Program.cs` configurado por variables de entorno
2. **CONFIGURACIÓN DECLARATIVA**: Todo comportamiento via archivos appsettings
3. **FEATURE TOGGLES**: Funcionalidades habilitadas/deshabilitadas por configuración
4. **ENTORNOS MÚLTIPLES**: Development, Testing, Production con configuraciones específicas

## 📋 ARQUITECTURA IMPLEMENTADA

### Configuración por Entornos
```
appsettings.json              - Configuración base
appsettings.Development.json  - Desarrollo (Mock, Auth deshabilitado)
appsettings.Testing.json      - Testing (Memory, Sin auth, Sin WebSockets)
appsettings.Production.json   - Producción (Real processing, Full security)
```

### Variables de Configuración Unificadas
```csharp
public class AppSettings
{
    public string Environment { get; set; } = "Development";
    public string ProcessingMode { get; set; } = "Mock";        // Mock|Real|Hybrid
    public string StorageMode { get; set; } = "Database";       // Memory|Database|Hybrid
    public bool EnableAuth { get; set; } = true;
    public bool EnableWebSockets { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableRealProcessing { get; set; } = false;
    public bool EnableDocs { get; set; } = true;
    public bool EnableCors { get; set; } = true;
}
```

### Middleware y Servicios Condicionales
```csharp
// JWT Authentication - Solo si EnableAuth = true
if (appSettings.EnableAuth) { /* Configurar JWT */ }

// CORS - Solo si EnableCors = true
if (appSettings.EnableCors) { /* Configurar CORS */ }

// Base de datos - Solo si StorageMode = Database
if (appSettings.UseDatabaseStorage) { /* Configurar Entity Framework */ }

// Swagger - Solo si EnableDocs = true
if (appSettings.EnableDocs) { /* Configurar Swagger/OpenAPI */ }
```

## 🎯 CONFIGURACIONES POR ENTORNO

### Development (appsettings.Development.json)
```json
{
  "AppSettings": {
    "Environment": "Development",
    "ProcessingMode": "Mock",
    "StorageMode": "Database",
    "EnableAuth": false,           // ← Sin autenticación para desarrollo
    "EnableWebSockets": true,
    "EnableMetrics": false,        // ← Sin métricas para desarrollo
    "EnableRealProcessing": false, // ← Solo procesamiento mock
    "EnableDocs": true,            // ← Swagger habilitado
    "EnableCors": true
  }
}
```

### Testing (appsettings.Testing.json)
```json
{
  "AppSettings": {
    "Environment": "Testing",
    "ProcessingMode": "Mock",
    "StorageMode": "Memory",       // ← Storage en memoria para tests
    "EnableAuth": false,           // ← Sin auth para tests unitarios
    "EnableWebSockets": false,     // ← Sin WebSockets para tests
    "EnableMetrics": false,
    "EnableRealProcessing": false,
    "EnableDocs": true,
    "EnableCors": false            // ← Sin CORS para tests
  }
}
```

### Production (appsettings.Production.json)
```json
{
  "AppSettings": {
    "Environment": "Production",
    "ProcessingMode": "Real",      // ← Procesamiento real de videos
    "StorageMode": "Database",
    "EnableAuth": true,            // ← Autenticación completa
    "EnableWebSockets": true,
    "EnableMetrics": true,         // ← Métricas habilitadas
    "EnableRealProcessing": true,  // ← Procesamiento completo
    "EnableDocs": false,           // ← Sin docs en producción
    "EnableCors": true
  }
}
```

## 🛠️ BENEFICIOS LOGRADOS

### Antes (Riesgo de Duplicación):
- ❌ Configuración hardcodeada en Program.cs
- ❌ Valores mock dispersos en controllers
- ❌ CORS con puertos hardcodeados
- ❌ Sin flexibilidad por entornos

### Después (Arquitectura Unificada):
- ✅ Configuración centralizada por entorno
- ✅ Feature toggles configurables
- ✅ Mock vs Real procesamiento por configuración
- ✅ Misma base de código para todos los entornos
- ✅ Startup condicional según configuración

## 📋 REGLAS DE DESARROLLO

### 1. Antes de Crear Funcionalidad Duplicada
- [ ] ¿Puede resolverse con un feature toggle en AppSettings?
- [ ] ¿Es una diferencia de entorno o funcionalidad real?
- [ ] ¿Se puede abstraer con interfaces y DI?

### 2. Al Agregar Nueva Configuración
- [ ] Agregar a `AppSettings.cs`
- [ ] Documentar en este archivo
- [ ] Agregar a todos los archivos appsettings.*.json
- [ ] Implementar lógica condicional en Program.cs

### 3. Testing de Configuraciones
- [ ] Probar modo Development
- [ ] Probar modo Testing (sin auth, memory storage)
- [ ] Probar modo Production (todas las funcionalidades)

## 🔧 COMANDOS DE DESARROLLO

### Ejecutar en modo Development
```bash
dotnet run --environment Development
```

### Ejecutar en modo Testing
```bash
dotnet run --environment Testing
```

### Ejecutar en modo Production
```bash
dotnet run --environment Production
```

### Verificar configuración actual
```bash
curl http://localhost:5000/
# Devuelve: environment, processing_mode, storage_mode, features habilitadas
```

## 📊 VALIDACIÓN DE PRINCIPIOS

### ✅ PRINCIPIOS APLICADOS EXITOSAMENTE

1. **NO DUPLICACIÓN**:
   - ❌ No hay múltiples Program.cs
   - ❌ No hay múltiples configuraciones manuales
   - ❌ No hay implementaciones duplicadas por entorno

2. **CONFIGURACIÓN UNIFICADA**:
   - ✅ Mismo código para todos los entornos
   - ✅ Comportamiento diferenciado por configuración
   - ✅ Feature toggles implementados

3. **ARQUITECTURA LIMPIA**:
   - ✅ Separación clara: Domain, Infrastructure, Application, API
   - ✅ Un controller por funcionalidad
   - ✅ Sin duplicación de endpoints

## 🎯 PRÓXIMOS PASOS RECOMENDADOS

### Inmediatos:
1. **Implementar ServiceFactory** para Mock vs Real processing
2. **Crear IStorageService** para Memory vs Database storage
3. **Implementar tests** para cada configuración de entorno

### A Mediano Plazo:
1. **Métricas y Monitoring** configurables
2. **WebSocket** opcional por configuración
3. **Logging levels** por entorno
4. **Validation pipeline** para configuraciones

### Patrones Recomendados:
```csharp
// Factory Pattern para servicios intercambiables
public interface IVideoProcessorFactory
{
    IVideoProcessor CreateProcessor();
}

public class VideoProcessorFactory : IVideoProcessorFactory
{
    public IVideoProcessor CreateProcessor()
    {
        return _appSettings.UseRealProcessing
            ? new RealVideoProcessor()
            : new MockVideoProcessor();
    }
}
```

## 🚨 VALIDACIONES OBLIGATORIAS

### Pre-commit Checks:
- [ ] No hay configuraciones hardcodeadas en Program.cs
- [ ] Todas las funcionalidades tienen feature toggles
- [ ] Los tres entornos tienen configuración completa
- [ ] Los endpoints mock están claramente marcados

### Code Review Obligatorio:
- [ ] Verificar que no se duplica funcionalidad
- [ ] Validar uso de AppSettings en lugar de valores hardcodeados
- [ ] Confirmar que el cambio funciona en los 3 entornos

---

**✅ PRINCIPIOS DEL PROYECTO PYTHON APLICADOS EXITOSAMENTE AL PROYECTO .NET**

**Este proyecto ahora sigue las mismas mejores prácticas de configuración unificada y arquitectura sin duplicación.**