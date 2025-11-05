# Definition of Ready (DoR) - Checklist Pre-Development

**Versión:** 2.0
**Fecha:** 2025-10-16
**Estado:** OBLIGATORIO

---

## 🎯 Objetivo

**Prevenir el "empezar y descubrir que falta información"**, la mayor fuente de delays en sprints.

Una historia NO puede comenzarse hasta que pasa el DoR completo.

---

## ✅ Definition of Ready Checklist

### 📋 1. Story Completeness

**La historia debe tener:**

- [ ] **ID único asignado**
  - Formato: `YRUS-XXXX`
  - Ejemplo: `YRUS-0501`

- [ ] **Título descriptivo**
  - Claro y conciso
  - Refleja el objetivo de negocio
  - Ejemplo: "Optimize database queries for video search"

- [ ] **User story format correcto**
  ```markdown
  **As a** [user type]
  **I want** [goal]
  **So that** [benefit]
  ```

- [ ] **Descripción contextual**
  - Por qué es necesario
  - Qué problema resuelve
  - Impacto esperado

- [ ] **Story points estimados**
  - Fibonacci: 1, 2, 3, 5, 8, 13
  - Consensuado con equipo
  - Si >13: dividir en historias más pequeñas

- [ ] **Prioridad asignada**
  - Critical / High / Medium / Low
  - O MoSCoW: Must / Should / Could / Won't

- [ ] **Sprint asignado**
  - Sprint específico identificado
  - Cabe en capacity del sprint

---

### 🎯 2. Acceptance Criteria (AC)

**Cada AC debe ser:**

- [ ] **Específico y medible**
  ```markdown
  ✅ BIEN: "Query completes in <100ms for 10K records"
  ❌ MAL:  "Query should be fast"
  ```

- [ ] **Testeable**
  - Puede verificarse objetivamente
  - Tiene criterio pass/fail claro

- [ ] **Formato Given-When-Then**
  ```markdown
  **AC1: Performance optimization**
  - Given a video search with 10K results
  - When the query executes
  - Then response time is <100ms
  - And database CPU usage <50%
  ```

- [ ] **Completo (happy path + edge cases)**
  ```markdown
  AC1: Happy path (normal flow)
  AC2: Edge case (empty results)
  AC3: Edge case (malformed query)
  AC4: Error case (database unavailable)
  ```

- [ ] **Mínimo 3 AC por historia**
  - Si <3: Probablemente falta cubrir escenarios
  - Si >8: Historia muy grande, considerar dividir

---

### 🔗 3. Dependencies

- [ ] **Dependencias técnicas identificadas**
  ```markdown
  Dependencias:
  - Requiere: US-0450 (indexes creados) ✅ Completo
  - Requiere: Database migration #15 ✅ Deployed
  - Requiere: Cache service configurado ❌ Pendiente
  ```

- [ ] **Dependencias externas resueltas**
  - APIs de terceros disponibles
  - Accesos/credenciales obtenidos
  - Servicios externos funcionando

- [ ] **Dependencias de datos disponibles**
  - Test data preparado
  - Database seeds listos
  - Mock data generado

- [ ] **Bloqueadores identificados**
  - No hay bloqueadores activos
  - O bloqueadores tienen workarounds documentados
  - O bloqueadores tienen fecha de resolución

---

### 🛠️ 4. Technical Requirements

- [ ] **Arquitectura definida**
  - Componentes a modificar/crear identificados
  - Patrones a aplicar documentados
  - Diagramas creados si necesario

- [ ] **API contracts definidos**
  ```json
  POST /api/videos/search
  Request: {
    "query": "string",
    "limit": "int32",
    "offset": "int32"
  }
  Response: {
    "results": [...],
    "total": "int32",
    "took_ms": "int32"
  }
  ```

- [ ] **Database changes identificados**
  - Migrations planeadas
  - Indexes requeridos listados
  - Performance impact estimado

- [ ] **Performance requirements claros**
  ```markdown
  Performance:
  - Response time: <200ms p95
  - Throughput: 100 req/s
  - Memory: <500MB
  - CPU: <70% average
  ```

- [ ] **Configuración requerida documentada**
  - Nuevas variables de environment
  - Feature flags necesarios
  - appsettings changes

---

### 🎨 5. UX/UI Requirements (si aplica)

- [ ] **Mockups/wireframes disponibles**
  - Diseños aprobados por PO
  - Flujos de usuario claros
  - Estados de UI documentados (loading, error, empty)

- [ ] **Responsive behavior definido**
  - Desktop, tablet, mobile
  - O N/A si es API-only

- [ ] **Accessibility requirements**
  - WCAG compliance level
  - O N/A si no tiene UI

- [ ] **i18n/l10n considerado**
  - Strings externalizados
  - O N/A si single language MVP

---

### 🔒 6. Security & Compliance

- [ ] **Security considerations documentadas**
  - Authentication requerida: Yes/No
  - Authorization rules definidas
  - Input validation especificada
  - Output sanitization requerida

- [ ] **Data privacy verificada**
  - PII identificado
  - Encryption requirements
  - Compliance (GDPR, etc.)

- [ ] **Security threats mitigados**
  - SQL injection considerado
  - XSS considerado
  - CSRF considerado
  - Rate limiting si aplica

---

### 🧪 7. Test Requirements

- [ ] **Test strategy definida**
  ```markdown
  Testing:
  - Unit tests: Services, repositories
  - Integration tests: API endpoints
  - E2E tests: Critical user path
  - Performance tests: Load scenario
  ```

- [ ] **Test data requirements**
  - Data sets identificados
  - Data generators preparados
  - Test users/roles definidos

- [ ] **Test environment disponible**
  - Database test disponible
  - External services mockeable
  - Test isolation posible

- [ ] **Coverage target definido**
  - Unit: >70%
  - Integration: >60%
  - Critical paths: 100%

---

### 📚 8. Documentation Requirements

- [ ] **Documentation scope definido**
  - API docs: Auto-generated (Swagger)
  - Architecture docs: Si cambios significativos
  - User docs: Si feature visible al usuario
  - Developer docs: Si patrón nuevo

- [ ] **Examples preparados**
  - Curl examples para API
  - Code snippets para developers

---

### 👥 9. Team Readiness

- [ ] **Skills requeridas disponibles**
  - Team tiene expertise necesario
  - O training programado
  - O experto externo disponible

- [ ] **Capacity verificada**
  - Sprint tiene capacity para esta historia
  - No hay sobre-compromiso

- [ ] **Agentes identificados**
  - Agentes especializados identificados
  - `dotnet-backend-developer` para implementación
  - `test-engineer` para testing
  - `code-reviewer` para review

- [ ] **Time budget razonable**
  - Estimación alineada con complejidad
  - Buffer considerado
  - No hay presión de tiempo irreal

---

### 📝 10. Approval & Sign-off

- [ ] **Product Owner approval**
  - PO revisó y aprobó la historia
  - AC son aceptables
  - Prioridad confirmada

- [ ] **Technical Lead review**
  - TL revisó factibilidad técnica
  - Arquitectura aprobada
  - Estimación razonable

- [ ] **Business Stakeholder awareness** (si aplica)
  - Stakeholder informado
  - Budget aprobado si necesario

---

## 🚨 Gating Rules

### Rule #1: NO START sin DoR Complete

```
Si DoR no está 100% completo:
  ❌ NO iniciar desarrollo
  ❌ NO asignar a sprint
  ❌ NO comprometer en sprint planning

Acción:
  → Mover a "Needs Refinement" backlog
  → Completar DoR en discovery track
  → Re-evaluar en siguiente sprint
```

### Rule #2: Bloqueador Detectado = PAUSE

```
Si durante check de DoR se detecta bloqueador:
  ⏸️  PAUSE historia
  🔍 Investigar bloqueador
  📋 Crear issue para resolver bloqueador
  ⏭️  Mover historia a sprint futuro

No intentar "workaround" sin documentar.
```

### Rule #3: Estimación >13 pts = SPLIT

```
Si historia estimada en 21, 13+ story points:
  ✂️  SPLIT en historias más pequeñas
  📋 Cada sub-historia debe tener su DoR
  🔗 Documentar dependencias entre sub-historias

Razón: Historias grandes tienen alta probabilidad de fallar.
```

---

## 📋 DoR Review Process

### Timing

**Cuando revisar DoR:**

1. **Durante Backlog Refinement** (continuous)
   - Product Owner + Technical Lead
   - Asegurar historias top del backlog tienen DoR

2. **En Discovery Track** (sprint N-1)
   - Preparar historias para sprint N
   - DoR debe completarse 1 sprint antes

3. **Durante Sprint Planning** (verificación final)
   - Re-verificar DoR antes de commitment
   - No asumir que DoR sigue válido

### Responsabilidades

| Sección DoR | Responsable Primary | Responsable Secondary |
|-------------|---------------------|----------------------|
| Story Completeness | Product Owner | Technical Lead |
| Acceptance Criteria | Product Owner | Test Engineer |
| Dependencies | Technical Lead | DevOps |
| Technical Requirements | Technical Lead | Software Architect |
| UX/UI Requirements | Product Owner | Frontend Dev |
| Security & Compliance | Technical Lead | Security Expert |
| Test Requirements | Test Engineer | Technical Lead |
| Documentation | Technical Lead | Product Owner |
| Team Readiness | Project Manager | Technical Lead |
| Approval & Sign-off | Product Owner | Business Stakeholder |

---

## 📊 DoR Verification Template

```markdown
# DoR Verification: YRUS-0501

**Story:** Optimize database queries for video search
**Reviewer:** Technical Lead
**Date:** 2025-10-16
**Sprint:** 11

## Checklist Results

### ✅ PASSED (8/10)

1. ✅ Story Completeness (100%)
2. ✅ Acceptance Criteria (100%)
3. ✅ Dependencies (100%)
4. ✅ Technical Requirements (100%)
5. ⚠️ UX/UI Requirements (N/A - API only)
6. ✅ Security & Compliance (100%)
7. ✅ Test Requirements (100%)
8. ✅ Documentation Requirements (100%)
9. ✅ Team Readiness (100%)
10. ✅ Approval & Sign-off (100%)

### ⚠️ WARNINGS (0)

None

### ❌ BLOCKERS (0)

None

## Decision

✅ **READY TO START**

Esta historia cumple 100% del DoR y puede comenzar en Sprint 11.

## Notes

- Database expert agent confirmó factibilidad
- Test data ya disponible en test DB
- Performance benchmarks baseline tomados
- Estimated confidence: HIGH

---

**Reviewer:** Technical Lead
**Approved by:** Product Owner
**Date:** 2025-10-16
```

---

## 🎯 Success Metrics

DoR es exitoso si:

1. ✅ **Zero surprises:** No descubrimientos mayores durante desarrollo
2. ✅ **High completion rate:** >90% historias completadas según estimación
3. ✅ **Low rework:** <10% código re-escrito por mala definición
4. ✅ **Team confidence:** High confidence al iniciar historias
5. ✅ **Predictability:** Velocity estable sprint-to-sprint

---

## 🚀 Quick DoR Check (30 seconds)

**Antes de sprint planning, quick check:**

```
❓ Can I start coding immediately?
   → If NO → DoR incomplete

❓ Do I know what "done" looks like?
   → If NO → AC insuficientes

❓ Are dependencies resolved?
   → If NO → Bloqueado

❓ Do I have all access/data needed?
   → If NO → No ready

❓ Is architecture clear?
   → If NO → Needs design work

If ANY answer is NO → ❌ NOT READY
If ALL answers are YES → ✅ READY
```

---

## 📝 Examples

### ❌ BAD Example (Incomplete DoR)

```markdown
### US-0999: Make search faster

**Story:** Search is slow, make it faster.

**AC:**
- Search should be fast
- Users should be happy

**Estimación:** 5 pts
```

**Problems:**
- ❌ No Given-When-Then format
- ❌ "Fast" no es medible
- ❌ No technical requirements
- ❌ No dependencies identified
- ❌ No performance target
- ❌ No test strategy

### ✅ GOOD Example (Complete DoR)

```markdown
### US-0501: Optimize database queries for video search

**As a** content creator
**I want** video search results in <100ms
**So that** I can quickly find relevant videos

**Context:**
Current search takes 3-5 seconds for 10K videos. User feedback indicates this is too slow. Analytics show 40% of searches are abandoned.

**Business Value:**
- Reduce search abandonment by 50%
- Improve user satisfaction score
- Enable real-time search experience

#### Acceptance Criteria

**AC1: Performance optimization**
- Given a search query with 10K video results
- When the optimized query executes
- Then response time is <100ms at p95
- And database CPU usage is <50%

**AC2: Correctness maintained**
- Given the optimized queries
- When compared to current results
- Then result sets are identical
- And ranking order is preserved

**AC3: Index utilization**
- Given the optimized queries
- When EXPLAIN ANALYZE is run
- Then all queries use indexes
- And no full table scans occur

**AC4: Monitoring added**
- Given the optimization is deployed
- When monitoring the queries
- Then Prometheus metrics track query time
- And alerts fire if p95 >150ms

#### Dependencies

**Resolved:**
- ✅ US-0450: Database indexes created (Sprint 10)
- ✅ Prometheus setup complete (Sprint 9)

**Pending:**
- None

#### Technical Requirements

**Architecture:**
- Optimize VideoService.SearchAsync()
- Add query result caching (IMemoryCache)
- Use EF Core query splitting

**Database Changes:**
```sql
-- Already applied in Sprint 10
CREATE INDEX idx_videos_title ON Videos(Title);
CREATE INDEX idx_videos_tags ON Videos(Tags);
```

**Performance Targets:**
- Response time: <100ms p95, <50ms p50
- Cache hit ratio: >70%
- Database queries: max 2 per search
- Memory usage: <50MB for cache

**Configuration:**
```json
"CacheSettings": {
  "SearchResultsTTL": "300",  // 5 minutes
  "MaxCacheSize": "1000"      // entries
}
```

#### Security

- ✅ Input validation: Search query max 200 chars
- ✅ SQL injection: Using parameterized EF queries
- ✅ Rate limiting: 10 searches/sec per user

#### Testing

**Strategy:**
- Unit tests: SearchService optimization logic
- Integration tests: Database query performance
- Load tests: 100 concurrent users

**Test Data:**
- 10K test videos (already seeded)
- Various search patterns prepared

**Coverage Target:**
- Unit: >80%
- Integration: >70%

#### Documentation

- Update API docs (Swagger auto-gen)
- Add performance benchmarks to README
- Document caching strategy in arch docs

#### Team Readiness

**Skills:**
- EF Core query optimization: ✅ Available
- Caching patterns: ✅ Known
- Load testing: ⚠️ Need database-expert agent

**Capacity:**
- Estimated 5 pts
- Sprint 11 has 17 pts committed, 4 pts buffer
- ✅ Fits in capacity

**Agents:**
- dotnet-backend-developer: Implementation
- database-expert: Query optimization review
- test-engineer: Load testing
- code-reviewer: Performance review

#### Approval

- ✅ Product Owner: Approved 2025-10-15
- ✅ Technical Lead: Reviewed 2025-10-15
- ✅ Ready for Sprint 11
```

---

**Estado:** OBLIGATORIO
**Check:** Antes de cada historia en sprint planning
**Owner:** Product Owner + Technical Lead
**Versión:** 2.0
