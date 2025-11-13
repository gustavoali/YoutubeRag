# Technical Debt Management - Sistema Cuantitativo

**Versión:** 2.0
**Fecha:** 2025-10-16
**Estado:** OBLIGATORIO

---

## 🎯 Objetivo

Convertir la deuda técnica de un "feeling" a **números objetivos** que permiten tomar decisiones basadas en ROI.

---

## 📊 Technical Debt Register

### Ubicación

**Archivo:** `TECHNICAL_DEBT_REGISTER.md` (raíz del proyecto)

### Estructura

```markdown
# Technical Debt Register - YouTube RAG .NET

**Última actualización:** 2025-10-16
**Total TD Items:** 12
**Total Interest Cost:** 25h/sprint
**Total Fix Cost:** 85h

---

## Active Technical Debt

| ID | Descripción | Severidad | Interest Rate | Fix Cost | ROI | Status | Owner |
|----|-------------|-----------|---------------|----------|-----|--------|-------|
| TD-001 | Falta validación input en VideoController | Medium | 2h/sprint | 4h | 10.0x | Open | TL |
| TD-002 | No hay indexes en TranscriptSegments | High | 5h/sprint | 8h | 12.5x | Planned | DB |
| TD-003 | Tests E2E sin cleanup | Low | 1h/sprint | 6h | 3.3x | Open | QA |
| TD-004 | Hardcoded config en WhisperService | Medium | 3h/sprint | 5h | 12.0x | In Progress | Dev |
| TD-005 | No hay circuit breaker en YouTube API | High | 8h/sprint | 12h | 13.3x | Open | Arch |

## Paid Technical Debt (History)

| ID | Descripción | Paid Date | Cost Actual | Value Delivered |
|----|-------------|-----------|-------------|-----------------|
| TD-101 | Refactor VideoService DI | 2025-10-10 | 6h | +15h/sprint saved |
| TD-102 | Add connection pooling | 2025-10-05 | 4h | +10h/sprint saved |
```

---

## 📐 Métricas Clave

### 1. Interest Rate (Tasa de Interés)

**Definición:** Cuánto tiempo PIERDES cada sprint por NO arreglar el debt.

**Cómo calcular:**

```
Ejemplos:

TD-002: No hay indexes en TranscriptSegments
- Query lento: 5 segundos
- Query ejecutado: 100 veces/día en desarrollo
- Tiempo perdido: 100 × 5s = 500s = 8.3 min/día
- En sprint de 10 días: 83 min ≈ 1.4h
- Pero causa re-runs de tests: +2h
- Debugging de "lentitud": +1.5h
- TOTAL Interest Rate: 5h/sprint ⏰
```

```
TD-004: Hardcoded config en WhisperService
- Cada cambio de ambiente requiere rebuild: 5 min
- Cambios de ambiente: 10 veces/sprint
- Tests fallan por config: 2h/sprint debug
- TOTAL Interest Rate: 3h/sprint ⏰
```

**Regla:** Sé conservador. Si dudas entre 2h y 4h, usa 2h.

### 2. Fix Cost (Costo de Arreglo)

**Definición:** Cuántas horas toma eliminar completamente el debt.

**Incluye:**
- Tiempo de implementación
- Tests
- Code review
- Deployment
- Documentación

**Ejemplo:**

```
TD-002: Agregar indexes
- Análisis de queries: 1h
- Crear migration: 0.5h
- Testing: 1h
- Verificar performance: 0.5h
- Code review: 0.5h
- Docs: 0.5h
- TOTAL Fix Cost: 4h (round up)
```

### 3. ROI (Return on Investment)

**Fórmula:**

```
ROI = (Interest Rate × Sprints Remaining) / Fix Cost

Donde:
  Sprints Remaining = Sprints hasta release o fin de año
```

**Ejemplo:**

```
TD-002: No hay indexes
- Interest Rate: 5h/sprint
- Sprints Remaining: 20 (hasta fin de año)
- Fix Cost: 8h

ROI = (5h × 20) / 8h = 100h / 8h = 12.5x

Interpretación:
  Por cada 1 hora invertida en arreglarlo,
  ganas 12.5 horas de productividad.

  ✅ ARREGLAR INMEDIATAMENTE
```

```
TD-003: Tests sin cleanup
- Interest Rate: 1h/sprint
- Sprints Remaining: 20
- Fix Cost: 6h

ROI = (1h × 20) / 6h = 20h / 6h = 3.3x

Interpretación:
  ROI positivo pero bajo.
  ⚠️ ARREGLAR en sprint con capacity extra
```

### 4. Severidad

**Clasificación:**

| Severidad | Criterio | Acción |
|-----------|----------|--------|
| **Critical** | Security vuln, data loss risk, crashes | Fix IMMEDIATELY (hotfix) |
| **High** | Major performance issue, blocks features, ROI >10x | Fix in current sprint |
| **Medium** | Maintainability issue, ROI 5-10x | Fix in next 2-3 sprints |
| **Low** | Minor inconvenience, ROI <5x | Fix when capacity available |

---

## 🚨 Decision Rules (Cuándo Pagar Debt)

### Rule #1: ROI > 10x = FIX IMMEDIATELY

```
Si ROI > 10x:
  - Agregar a sprint actual como P1
  - Justificación: Cada hora invertida devuelve 10+ horas
  - Pagar este debt es MÁS valioso que features nuevos
```

### Rule #2: ROI 5-10x = FIX IN NEXT SPRINT

```
Si 5x < ROI <= 10x:
  - Planear para próximo sprint
  - Comunicar a stakeholders
  - Balance con features
```

### Rule #3: ROI < 5x = FIX WHEN CAPACITY

```
Si ROI < 5x:
  - Mantener en backlog
  - Arreglar en sprints con capacity extra
  - No priorizar sobre features
```

### Rule #4: Critical Severity = IGNORE ROI

```
Si Severity = Critical:
  - Ignorar ROI
  - Fix IMMEDIATELY
  - Hotfix si es necesario
```

---

## 📋 Workflow de Technical Debt

### 1. Identificación

**¿Cuándo se crea un TD item?**

Durante:
- Code review (reviewer identifica)
- Retrospectiva (team identifica)
- Debugging (se descubre)
- Performance analysis (se detecta)

**¿Quién lo crea?**

Cualquiera, pero Technical Lead aprueba.

**Template:**

```markdown
## TD-XXX: [Título descriptivo]

**Fecha creación:** 2025-10-16
**Identificado por:** [Nombre]
**Ubicación:** [File:Line o Componente]

### Descripción
[¿Qué es el debt? ¿Por qué existe?]

### Impacto Actual
[¿Qué problemas causa HOY?]

### Interest Rate Calculation
- [Problema 1]: Xh/sprint
- [Problema 2]: Yh/sprint
- **TOTAL:** Zh/sprint

### Fix Cost Estimate
- Implementación: Xh
- Testing: Yh
- Review: Zh
- **TOTAL:** Wh

### ROI
```
ROI = (Zh × 20 sprints) / Wh = [resultado]x
```

### Severidad
[Critical / High / Medium / Low]

### Propuesta de Solución
[¿Cómo arreglarlo?]

### Dependencies
[¿Qué debe hacerse antes?]
```

### 2. Priorización

**Reunión:** Durante Sprint Planning

**Proceso:**

1. Review TD Register
2. Calcular ROI actualizado (sprints remaining cambia cada sprint)
3. Clasificar por ROI
4. Aplicar decision rules
5. Asignar TD items a sprint

**Output:** TD items seleccionados para sprint

### 3. Ejecución

**Tratamiento:** TD items son historias de usuario normales

```markdown
### US-TD-002: Agregar indexes a TranscriptSegments

**Story Points:** 3
**Priority:** High (ROI 12.5x)
**Type:** Technical Debt

**As a** developer
**I want** proper database indexes
**So that** queries run in <100ms instead of 5s

**Acceptance Criteria:**
- Given a query on TranscriptSegments by VideoId
- When index is applied
- Then query completes in <100ms
- And test suite runs 5h faster per sprint
```

**DoD adicional para TD:**
- [ ] ROI real medido post-fix
- [ ] Interest Rate eliminado verificado
- [ ] TD item movido a "Paid" section

### 4. Tracking

**Métricas por Sprint:**

```markdown
## Sprint 10 - Technical Debt Metrics

### Debt Paid This Sprint:
- TD-002: Indexes (ROI 12.5x) ✅
- TD-004: Config refactor (ROI 12.0x) ✅
- Total cost: 13h
- Value delivered: 160h over 20 sprints

### New Debt Created:
- TD-006: No monitoring on new service
- Interest Rate: 2h/sprint

### Net Debt Position:
- Previous interest: 25h/sprint
- Paid: -8h/sprint (TD-002 + TD-004)
- New: +2h/sprint (TD-006)
- **Current interest: 19h/sprint** (improved 24% ✅)
```

---

## 📊 Technical Debt Dashboard

### Visualización Recomendada

```
┌─────────────────────────────────────────────────────────┐
│ TECHNICAL DEBT DASHBOARD - Sprint 10                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ 📊 TOTAL DEBT BURDEN                                   │
│ Total Interest: 19h/sprint (was 25h, improved 24%)     │
│ Total Fix Cost: 72h (was 85h)                          │
│ Items: 11 active (was 12)                              │
│                                                         │
│ 🎯 HIGH ROI ITEMS (Fix Immediately)                    │
│ TD-005: Circuit breaker (ROI 13.3x) - 12h fix          │
│ TD-007: Query optimization (ROI 11.2x) - 6h fix        │
│                                                         │
│ ⚠️  CRITICAL ITEMS (Ignore ROI)                         │
│ TD-008: Security: SQL injection risk - 8h fix          │
│                                                         │
│ 📈 TREND (Last 5 Sprints)                              │
│ Interest Rate:                                          │
│ Sprint 6:  30h ████████████████░░░░                    │
│ Sprint 7:  28h ██████████████░░░░░░                    │
│ Sprint 8:  25h ████████████░░░░░░░░                    │
│ Sprint 9:  22h ███████████░░░░░░░░░                    │
│ Sprint 10: 19h █████████░░░░░░░░░░░ ✅ IMPROVING       │
│                                                         │
│ 💰 VALUE DELIVERED THIS SPRINT                         │
│ Debt paid: 13h investment                              │
│ Value gained: 160h over 20 sprints                     │
│ ROI: 12.3x average                                     │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 🎯 Goals y Targets

### Por Sprint:

**Target:** Reducir interest rate en 10-20% cada sprint

```
Sprint N:   25h interest
Sprint N+1: 20-22.5h interest (10-20% reduction ✅)
Sprint N+2: 16-20h interest
...
Sprint N+X: <5h interest (maintenance level ✅)
```

### Por Año:

**Target:** Interest rate <5h/sprint (maintenance level)

```
Start of year: 30h/sprint
Target by end: <5h/sprint

Required: Reducción de 25h
En 48 sprints: 0.5h/sprint reduction rate
Achievable: ✅ SÍ (con 10-20% reduction/sprint)
```

### ROI Mínimo:

**Policy:** No pagar debt con ROI <3x (excepto Critical)

```
Razón:
  Si ROI <3x, tu tiempo probablemente está mejor invertido
  en features que entregan valor directo al usuario.

Excepción:
  Severity = Critical → Fix sin importar ROI
```

---

## 🚀 Preventing New Debt

### Code Review Checklist

Reviewer debe verificar:

- [ ] **No hardcoded config** → Use appsettings
- [ ] **No N+1 queries** → Use eager loading
- [ ] **No missing indexes** → Check query performance
- [ ] **No duplicate code** → DRY principle
- [ ] **No missing tests** → 70%+ coverage
- [ ] **No TODO comments** → Convert to TD items
- [ ] **No disabled tests** → Fix or document
- [ ] **No commented code** → Delete or document why

**Si alguno falla:** Puede mergear SOLO si crea TD item.

### Pre-Merge Hook

```bash
# .husky/pre-commit

# Check for TODO comments
if grep -r "TODO" src/ --exclude-dir=node_modules; then
  echo "⚠️  Found TODO comments. Create TD item or remove."
  echo "To bypass: git commit --no-verify"
  exit 1
fi

# Check for hardcoded strings
if grep -r "http://localhost" src/ --exclude-dir=node_modules; then
  echo "⚠️  Found hardcoded URLs. Use configuration."
  exit 1
fi
```

---

## 📝 Examples

### Example 1: High ROI Debt

```markdown
## TD-002: No indexes on TranscriptSegments table

**Created:** 2025-10-01
**Identified by:** Technical Lead (during performance testing)
**Location:** Database schema, TranscriptSegments table

### Description
TranscriptSegments table has no indexes on VideoId column.
Queries filtering by VideoId perform full table scan.

### Current Impact
- Query time: 5 seconds (with 10K segments)
- Affects: Search feature, video detail page, admin dashboard
- Test suite slow: Adds 5h to each test run

### Interest Rate Calculation
- Slow queries in development: 1h/sprint
- Test suite overhead: 3h/sprint
- Debugging "why slow": 1h/sprint
- **TOTAL: 5h/sprint**

### Fix Cost Estimate
- Create migration: 1h
- Test performance: 1h
- Code review: 0.5h
- Deploy + verify: 0.5h
- **TOTAL: 3h**

### ROI
```
ROI = (5h × 20 sprints) / 3h = 33.3x ⭐⭐⭐
```

### Severity
**High** - Major performance impact

### Proposed Solution
```sql
CREATE INDEX idx_transcriptsegments_videoid
ON TranscriptSegments(VideoId);

CREATE INDEX idx_transcriptsegments_videoid_segmentindex
ON TranscriptSegments(VideoId, SegmentIndex);
```

### Dependencies
None - can fix immediately

### Decision
✅ **FIX IN CURRENT SPRINT** (ROI > 10x)
```

### Example 2: Low ROI Debt

```markdown
## TD-003: Tests don't cleanup temp files

**Created:** 2025-10-05
**Identified by:** Test Engineer
**Location:** Tests/Integration/BaseIntegrationTest.cs

### Description
Integration tests create temp files but don't clean them up.
Over time, /tmp fills with test artifacts.

### Current Impact
- Manual cleanup: 10 minutes/week = 0.5h/sprint
- Disk space: 2GB used (not critical, have 500GB)

### Interest Rate Calculation
- Manual cleanup: 0.5h/sprint
- **TOTAL: 0.5h/sprint**

### Fix Cost Estimate
- Implement cleanup: 2h
- Test all scenarios: 1h
- **TOTAL: 3h**

### ROI
```
ROI = (0.5h × 20 sprints) / 3h = 3.3x
```

### Severity
**Low** - Minor inconvenience

### Proposed Solution
```csharp
public class BaseIntegrationTest : IDisposable
{
    protected string TempDir { get; private set; }

    public BaseIntegrationTest()
    {
        TempDir = CreateTempDirectory();
    }

    public void Dispose()
    {
        if (Directory.Exists(TempDir))
            Directory.Delete(TempDir, recursive: true);
    }
}
```

### Dependencies
None

### Decision
⚠️ **FIX WHEN CAPACITY AVAILABLE** (ROI 3.3x < 5x threshold)
```

---

## ✅ Success Criteria

Technical Debt Management es exitoso si:

1. ✅ **TD Register actualizado** cada sprint
2. ✅ **Interest rate trending down** (-10% o más por sprint)
3. ✅ **High ROI items (>10x) paid** dentro de 2 sprints
4. ✅ **Zero Critical items** open >1 sprint
5. ✅ **Team awareness** of debt cost vs feature value

---

## 🔄 Review Cycle

**Frecuencia:** Cada Sprint Planning

**Agenda:**
1. Review active TD items (10 min)
2. Update ROI (sprints remaining changed)
3. Identify items to pay this sprint
4. Review new debt created last sprint
5. Verify paid debt delivered expected value

**Owner:** Technical Lead

---

**Estado:** OBLIGATORIO
**Archivo requerido:** `TECHNICAL_DEBT_REGISTER.md` (raíz proyecto)
**Review:** Cada Sprint Planning
**Versión:** 2.0
