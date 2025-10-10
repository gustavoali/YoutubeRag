# Product Backlog - YouTube RAG .NET

**Última actualización:** 8 de Octubre, 2025

---

## ✅ Bloqueadores Resueltos

### ~~BLOCKER-001: Serilog Logger Frozen en Integration Tests~~ ✅ RESUELTO
**Prioridad:** ~~P0 - CRÍTICO~~ → RESUELTO
**Tipo:** Technical Debt / Bug
**Esfuerzo real:** 2 horas
**Sprint:** Sprint 2 - Epic 2
**Resuelto:** 8 de Octubre, 2025
**Commit:** `b8c2b8c` - BLOCKER-001: Fix Serilog frozen logger issue

**Descripción del problema:**
Todos los integration tests fallaban con `InvalidOperationException: The logger is already frozen` cuando se intentaba crear múltiples instancias de `WebApplicationFactory`.

**Solución implementada (Híbrida):**
Se implementó una combinación de Opción A + Opción C:

1. **Program.cs:** Skip Serilog en Testing environment
   ```csharp
   if (builder.Environment.EnvironmentName != "Testing")
   {
       builder.Host.UseSerilog(/* ... */);
   }
   ```

2. **CustomWebApplicationFactory.cs:** Logging simple para tests
   ```csharp
   builder.ConfigureLogging(logging => {
       logging.ClearProviders();
       logging.AddConsole();
       logging.SetMinimumLevel(LogLevel.Warning);
   });
   ```

**Resultados:**
- ✅ **350/362 tests passing** (96.7% success rate)
- ✅ 13/13 TranscriptionJobProcessorTests passing
- ✅ Blocker completamente eliminado
- ✅ Production logging sin cambios
- ⚠️ 10 tests con failures de lógica (documentados abajo)
- ℹ️ 2 tests skipped

**Tests con failures pendientes (No bloqueantes):**
Los siguientes 10 tests fallan por lógica de negocio, NO por el blocker de Serilog:
- VideoIngestionPipelineE2ETests (esperaba 1 job, creó 2)
- [Pendiente documentar otros 9 tests]

**Referencias:**
- Fix: `Program.cs:58`, `CustomWebApplicationFactory.cs:35`
- Commit: `b8c2b8c`

---

## 🟡 Mejoras Técnicas

### TECH-001: Upgrade a .NET 9.0
**Prioridad:** P1 - Alta
**Tipo:** Technical Enhancement
**Esfuerzo estimado:** 1-2 días
**Sprint:** Futura (considerar para Sprint 3)

**Descripción:**
Actualizar el proyecto de .NET 8.0 a .NET 9.0 para aprovechar mejoras de performance, nuevas características y mejor soporte de EF Core.

**Motivación:**
Durante la implementación de Epic 2, se identificó que **EFCore.BulkExtensions 9.0** requiere .NET 9.0, lo cual ofrece:
- **Performance:** Mejoras en garbage collector y JIT compiler
- **EF Core 9.0:** Bulk operations nativas más rápidas
- **C# 13:** Nuevas características del lenguaje
- **JSON:** System.Text.Json mejorado
- **Observability:** Mejor soporte para OpenTelemetry

**Beneficios estimados:**
- 🚀 **Performance:** 10-15% mejora en throughput de bulk insert
- 📦 **Paquetes:** Acceso a versiones más recientes de EFCore.BulkExtensions
- 🔧 **Tooling:** Mejor soporte en Visual Studio y dotnet CLI
- 🛡️ **Seguridad:** Patches de seguridad más recientes

**Tareas requeridas:**

1. **Actualizar Target Framework (0.5 días)**
   - Cambiar `<TargetFramework>net8.0</TargetFramework>` → `net9.0` en todos los `.csproj`
   - Proyectos: Domain, Application, Infrastructure, Api, Tests

2. **Actualizar Paquetes NuGet (0.5 días)**
   - Microsoft.EntityFrameworkCore: 8.0.11 → 9.0.x
   - Microsoft.AspNetCore.*: 8.0.x → 9.0.x
   - EFCore.BulkExtensions: 8.1.1 → 9.0.1
   - Hangfire.*: Verificar compatibilidad
   - Serilog.*: Verificar compatibilidad

3. **Testing & Validación (0.5 días)**
   - Ejecutar suite completa de tests
   - Performance benchmarks
   - Regression testing
   - Verificar producción en staging

4. **Documentación (0.5 días)**
   - Actualizar README.md con requisitos
   - Migration guide para desarrolladores
   - Release notes

**Riesgos:**
- ⚠️ Incompatibilidad con Hangfire (verificar versión compatible)
- ⚠️ Breaking changes en EF Core 9.0
- ⚠️ Dependencias third-party sin soporte .NET 9

**Mitigación:**
- Crear rama feature/net9-upgrade
- Testing exhaustivo antes de merge
- Plan de rollback documentado

**Aceptación:**
- ✅ Todos los proyectos compilan con .NET 9.0
- ✅ Todos los tests pasan (unit + integration + E2E)
- ✅ Performance no degrada (idealmente mejora)
- ✅ No hay breaking changes para usuarios
- ✅ Documentación actualizada

**Referencias:**
- [.NET 9.0 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [EF Core 9.0 What's New](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/whatsnew)
- Discusión inicial: EPIC_2_VALIDATION_REPORT.md (GAP 3)

---

## 🟢 Mejoras de Calidad

### QUALITY-001: Investigar y corregir 10 tests de integración fallidos
**Prioridad:** P2 - Media
**Tipo:** Quality / Testing
**Esfuerzo estimado:** 4-6 horas
**Sprint:** Deuda técnica (post-Epic 2)

**Descripción:**
Después de resolver BLOCKER-001, quedaron 10 tests de integración con failures de lógica de negocio (no relacionados con Serilog).

**Tests afectados:**
1. `VideoIngestionPipelineE2ETests.IngestVideo_ShortVideo_ShouldCreateVideoAndJobInDatabase` - Esperaba 1 job, creó 2
2. [Pendiente documentar otros 9 tests]

**Estado actual:**
- 350/362 tests passing (96.7%)
- 10 tests failing (lógica de negocio)
- 2 tests skipped

**Tareas:**
- Ejecutar suite completa de tests con `--verbosity detailed`
- Documentar los 10 tests que fallan con sus mensajes de error
- Analizar causa raíz de cada failure
- Corregir lógica de negocio o ajustar assertions según corresponda
- Validar que todos los tests pasen (100%)

**Aceptación:**
- ✅ 362/362 tests passing (100%)
- ✅ Documentación de fixes realizados
- ✅ No hay regresión en tests existentes

**Referencias:**
- Test run inicial: 8 de Octubre, 2025 (post BLOCKER-001 fix)
- Comando: `dotnet test YoutubeRag.Tests.Integration/YoutubeRag.Tests.Integration.csproj`

---

### QUALITY-002: Resolver Warnings de Compilación
**Prioridad:** P2 - Media
**Tipo:** Code Quality
**Esfuerzo estimado:** 4 horas
**Sprint:** Continuous improvement

**Descripción:**
Actualmente el proyecto tiene ~26 warnings de compilación que deben resolverse para mantener código limpio.

**Warnings principales:**
1. `CS8604`: Posible argumento de referencia nulo (nullable references)
2. `CS1998`: Método asincrónico sin await
3. `NU1608`: Degradación de paquete Hangfire.SqlServer 1.8.6 vs Hangfire.Core 1.8.21

**Tareas:**
- Habilitar nullable reference types estricto
- Agregar null checks donde corresponda
- Convertir métodos async sin await a sync
- Actualizar Hangfire.SqlServer a 1.8.21

**Aceptación:**
- ✅ Build con 0 warnings
- ✅ Todos los tests pasan
- ✅ No introduce nuevos bugs

---

## 📋 Backlog General

### Feature Ideas (Sin priorizar)

**FT-001:** Soporte para múltiples idiomas en transcripción
**FT-002:** Detección automática de speakers (diarization)
**FT-003:** Export de transcripciones a SRT/VTT
**FT-004:** Dashboard de analytics de transcripciones
**FT-005:** API rate limiting y quotas por usuario
**FT-006:** Webhooks para notificaciones de jobs completados

---

**Nota:** Este backlog se actualiza continuamente. Prioridades pueden cambiar según necesidades del negocio.
