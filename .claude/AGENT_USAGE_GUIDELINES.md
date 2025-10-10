# Agent Usage Guidelines - Directivas Prioritarias

**Versión:** 1.0
**Fecha:** 8 de Octubre, 2025
**Prioridad:** 🔴 CRÍTICA - Aplicar siempre

---

## 🎯 Directiva Principal

> **SIEMPRE delegar tareas a agentes especializados cuando sea posible. El trabajo en paralelo es PRIORITARIO sobre el trabajo secuencial.**

### Regla de Oro
```
SI existe un agente especializado para la tarea
  ENTONCES delegar a ese agente (incluso en paralelo)
  ELSE hacer la tarea directamente
```

---

## 🤖 Agentes Disponibles

### 1. **backend-python-developer** / **backend-python-developer-sonnet**
**Usar para:**
- Implementar servicios backend Python
- FastAPI endpoints
- Database models
- Business logic
- Query optimization
- Caching strategies

**Ejemplo:**
```
❌ MAL: "Voy a leer AudioExtractionService y completar los gaps"
✅ BIEN: "Delegando validación de AudioExtractionService a backend-python-developer"
```

---

### 2. **dotnet-backend-developer** / **dotnet-backend-developer-sonnet**
**Usar para:**
- Implementar servicios .NET/C#
- ASP.NET Core APIs
- Entity Framework operations
- Business logic en C#
- Authentication systems

**Ejemplo:**
```
❌ MAL: "Voy a implementar los gaps de TranscriptionJobProcessor"
✅ BIEN: "Delegando gaps de TranscriptionJobProcessor a dotnet-backend-developer"
```

---

### 3. **test-engineer**
**Usar para:**
- Escribir tests (unit, integration, E2E)
- Revisar cobertura de tests
- Implementar test automation
- Testing strategies
- **✨ NUEVO:** Testing manual execution + documentation

**Ejemplo:**
```
❌ MAL: "Necesitás ejecutar los 6 scenarios de testing manual"
✅ BIEN: "Delegando testing manual de Epic 2 a test-engineer agent"
```

**IMPORTANTE:** El test-engineer puede:
- Ejecutar tests automatizados
- Documentar resultados de tests
- Validar AC mediante testing
- Crear test reports
- Sugerir casos de test adicionales

---

### 4. **code-reviewer**
**Usar para:**
- Code review post-implementación
- Security review
- Performance review
- Best practices validation

**Ejemplo:**
```
✅ BIEN: "Delegando code review de Epic 2 a code-reviewer antes de release"
```

---

### 5. **database-expert**
**Usar para:**
- Schema design
- Query optimization
- Indexing strategies
- Migrations
- Performance tuning

**Ejemplo:**
```
✅ BIEN: "Delegando optimización de queries a database-expert"
```

---

### 6. **frontend-react-developer** / **frontend-angular-developer**
**Usar para:**
- Componentes React/Angular
- UI implementation
- State management
- API integration frontend

---

### 7. **devops-engineer**
**Usar para:**
- CI/CD pipelines
- Docker/Kubernetes
- Infrastructure as Code
- Monitoring setup
- Deployment automation

---

### 8. **software-architect**
**Usar para:**
- System design
- Architecture decisions
- Tech stack evaluation
- Scalability planning

---

### 9. **project-manager**
**Usar para:**
- Project planning
- Resource allocation
- Risk management
- Status reporting
- Timeline creation

---

### 10. **product-owner**
**Usar para:**
- User story creation
- Backlog prioritization
- Sprint planning
- AC validation
- Product roadmap

---

### 11. **business-stakeholder**
**Usar para:**
- Business validation
- Requirements approval
- Budget decisions
- Strategic direction

---

## 🚀 Metodología de Trabajo en Paralelo

### Patrón Correcto: Delegar + Continuar

```javascript
// PATRÓN CORRECTO
1. Identificar tarea A (validar Epic 3)
2. Identificar tarea B (testing Epic 2)
3. Identificar tarea C (preparar Epic 4)

// Ejecutar EN PARALELO
Task A → Delegar a backend-developer
Task B → Delegar a test-engineer
Task C → Yo preparo documentación Epic 4

// Resultado: 3 tareas en paralelo
```

### Patrón Incorrecto: Hacer Todo Secuencial

```javascript
// PATRÓN INCORRECTO ❌
1. Yo valido Epic 3 (2 horas)
2. Yo ejecuto testing Epic 2 (3 horas)
3. Yo preparo Epic 4 (2 horas)

// Resultado: 7 horas secuenciales
```

---

## 📋 Checklist de Delegación

Antes de hacer una tarea, preguntarse:

- [ ] ¿Existe un agente especializado para esto?
- [ ] ¿La tarea toma >30 minutos?
- [ ] ¿Puedo trabajar en otra cosa mientras el agente trabaja?
- [ ] ¿El agente puede hacerlo mejor que yo?

**Si 2+ respuestas son SÍ → DELEGAR**

---

## 🎯 Casos de Uso Específicos

### Epic Validation
```
❌ MAL:
"Voy a leer AudioExtractionService.cs completo (2 horas)"

✅ BIEN:
"Delegando validación de AudioExtractionService a backend-developer:
- Leer implementación completa
- Verificar AC de YRUS-0103
- Identificar gaps específicos
- Sugerir implementaciones faltantes
- Estimar esfuerzo real"
```

---

### Testing Manual
```
❌ MAL:
"Necesitás ejecutar estos 6 scenarios de testing manual"

✅ BIEN:
"Delegando testing manual a test-engineer:
- Ejecutar 6 scenarios de EPIC_2_MANUAL_TESTING_PLAN.md
- Documentar resultados
- Capturar logs/screenshots
- Reportar issues encontrados
- Completar sign-off checklist"
```

---

### Implementation Gaps
```
❌ MAL:
"Voy a implementar los 5 gaps identificados en Epic 3"

✅ BIEN:
"Delegando gaps a backend-developer:
GAP 1: Progress tracking (3h)
GAP 2: Disk space check (1h)
GAP 3: Cleanup job (2h)
GAP 4: Error handling (2h)

Mientras tanto, yo preparo Epic 4 validation"
```

---

## 🔄 Workflow Típico de Epic

### Epic Validation → Implementation → Testing → Release

```mermaid
Epic N Validation
    ↓
[backend-developer] Valida implementación existente (2h)
    ↓
[backend-developer] Implementa gaps identificados (4-6h) ← EN PARALELO
[test-engineer] Escribe/ejecuta tests (3h)                ←
    ↓
[code-reviewer] Code review completo (1h)
    ↓
[test-engineer] Testing manual + regression (2h)
    ↓
Sign-off + Release
```

**Tiempo total:** 12h → **Tiempo real con paralelo:** ~6-8h

---

## 💡 Ejemplos Reales de Este Proyecto

### ✅ BIEN HECHO: Epic 2 Gaps
```
✅ Delegué GAP 5 (embeddings) a backend-developer
✅ Delegué GAP 6 (validation) a backend-developer
✅ Delegué testing a test-engineer
✅ TODO EN PARALELO

Resultado: 3 agentes trabajando simultáneamente
```

### ❌ ERROR: Epic 2 Testing Manual
```
❌ "Voy a preparar plan de testing manual"
❌ "Necesitás ejecutar estos scenarios"

Debí haber hecho:
✅ "Delegando testing manual a test-engineer"
```

### ❌ ERROR: Epic 3 Validation
```
❌ "Voy a leer AudioExtractionService (2 horas)"
❌ Ofrecí 3 opciones de plan

Debí haber hecho:
✅ "Delegando validación Epic 3 a backend-developer AHORA"
```

---

## 🎯 Directivas Prioritarias

### 1. **Máximo Paralelismo**
> Siempre buscar ejecutar 2-3 agentes en paralelo cuando sea posible

### 2. **Delegación Proactiva**
> No esperar a que el usuario pida delegar - hacerlo automáticamente

### 3. **Especialización**
> Usar el agente MÁS especializado para cada tarea

### 4. **Documentación Post-Delegación**
> Después de delegar, preparar siguiente tarea o documentación

### 5. **Comunicación Clara**
> Siempre decir explícitamente "Delegando X a Y agent"

---

## 📊 Métricas de Éxito

### Indicadores de Buen Uso
- ✅ 2-3 agentes trabajando en paralelo frecuentemente
- ✅ Tiempo total reducido 40-60%
- ✅ Usuario no hace trabajo manual
- ✅ Especialización adecuada (backend→backend, test→test)

### Indicadores de Mal Uso
- ❌ Claude hace trabajo que podría delegar
- ❌ Usuario debe ejecutar tests manualmente
- ❌ Trabajo 100% secuencial
- ❌ Ofrecer "opciones" en lugar de delegar

---

## 🚨 Recordatorio Final

> **PREGUNTA OBLIGATORIA antes de hacer cualquier tarea:**
>
> *"¿Debería delegar esto a un agente especializado?"*
>
> La respuesta casi siempre es: **SÍ**

---

## 📝 Plantilla de Delegación

```markdown
Delegando [TAREA] a [AGENTE]:

**Objetivo:**
- [Objetivo principal]

**Tareas específicas:**
1. [Tarea 1]
2. [Tarea 2]
3. [Tarea 3]

**Output esperado:**
- [Archivo/resultado 1]
- [Archivo/resultado 2]

**Criterios de aceptación:**
- [ ] Criterio 1
- [ ] Criterio 2

**Tiempo estimado:** [X horas]

Mientras tanto, yo trabajaré en: [SIGUIENTE_TAREA]
```

---

**APLICAR ESTAS DIRECTIVAS EN TODO MOMENTO**

**Prioridad:** 🔴 CRÍTICA
**Scope:** Todo el proyecto
**Review:** Cada sprint
