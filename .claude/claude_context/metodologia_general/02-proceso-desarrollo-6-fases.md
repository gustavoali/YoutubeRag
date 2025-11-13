# Proceso de Desarrollo - 6 Fases Obligatorias

**Versión:** 1.0
**Fuente:** `DEVELOPMENT_PROCESS_FRAMEWORK.md`
**Estado:** OBLIGATORIO

---

## 🔄 Visión General del Proceso

```
┌────────────────────────────────────────────────────────────┐
│                   PROCESO DE 6 FASES                       │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  Fase 0: Detección del Trabajo                            │
│     ↓                                                      │
│  Fase 1: Diagnóstico Técnico (Technical Lead)             │
│     ↓                                                      │
│  Fase 2: Planificación (Project Manager)                  │
│     ↓                                                      │
│  Fase 3: Product Backlog (Product Owner)                  │
│     ↓                                                      │
│  Fase 4: Validación de Negocio (Business Stakeholder)     │
│     ↓                                                      │
│  Fase 5: Ejecución con Agentes                            │
│     ↓                                                      │
│  Fase 6: Validación y Cierre                              │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## 📝 Fase 0: Detección del Trabajo Necesario

**Trigger:** Se identifica necesidad de desarrollo (feature, bug, mejora)

**Responsable:** Cualquier miembro del equipo

**Acción:** Crear Issue/Ticket con:
- Contexto y motivación
- Problema a resolver o feature a implementar
- Impacto esperado en usuarios/negocio
- Estimación preliminar (S/M/L/XL)

**Output:** Ticket creado y asignado a Technical Lead

---

## 🔍 Fase 1: Diagnóstico Técnico

**Responsable:** Technical Lead (YO)

**Duración:** 2-8 horas

### Actividades:

#### 1. Investigación Técnica
- Revisar código existente relacionado
- Identificar componentes afectados
- Analizar dependencias y acoplamiento
- Buscar soluciones previas o patrones

#### 2. Identificación de Problemas
- Listar bugs o limitaciones actuales
- Clasificar por severidad (P0/P1/P2/P3)
- Documentar causa raíz
- Evaluar impacto en sistema completo

#### 3. Propuesta de Soluciones
- Diseñar al menos 2 alternativas
- Evaluar pros/contras de cada una
- Estimar esfuerzo y complejidad
- Identificar riesgos técnicos

### Output:

**Archivo:** `DIAGNOSTIC_REPORT_[FEATURE_NAME].md`

**Contenido mínimo:**
```markdown
# Diagnostic Report: [Feature Name]

## Executive Summary
[Qué problema, por qué crítico, solución recomendada]

## Current State Analysis
### What Works
- [Funcionalidad OK]

### What Doesn't Work
- [Bugs/limitaciones con evidencia]

## Root Cause Analysis
[Causa raíz de cada problema]

## Proposed Solutions
### Option A: [Nombre]
- **Pros**: [Lista]
- **Cons**: [Lista]
- **Effort**: [Horas]

### Option B: [Nombre]
- **Pros**: [Lista]
- **Cons**: [Lista]
- **Effort**: [Horas]

## Recommendation
[Cuál opción y por qué]

## Technical Risks
[Top 3-5 riesgos]
```

### Gate 1: ¿Diagnóstico Completo?

**Checklist:**
- [ ] Problemas identificados con evidencia
- [ ] Al menos 2 soluciones propuestas
- [ ] Estimaciones de esfuerzo incluidas
- [ ] Riesgos técnicos documentados
- [ ] Revisado por Technical Lead

✅ PASS → Fase 2 | ❌ FAIL → Completar diagnóstico

---

## 📊 Fase 2: Planificación

**Responsable:** Project Manager (YO)

**Duración:** 1-4 horas

### Actividades:

#### 1. Revisión del Diagnóstico
- Validar estimaciones técnicas
- Identificar dependencias entre tareas
- Evaluar impacto en cronograma

#### 2. Creación del Plan de Proyecto
- Descomponer en fases/milestones
- Asignar recursos (quien hace qué)
- Definir cronograma realista
- Identificar riesgos de cronograma/recursos

#### 3. Análisis de Riesgos
- Crear matriz de riesgos (probabilidad × impacto)
- Definir mitigaciones para top 3-5 riesgos
- Establecer plan de contingencia
- Definir procedimiento de escalación

### Output:

**Archivo:** `PROJECT_PLAN_[FEATURE_NAME].md`

**Contenido mínimo:**
```markdown
# Project Plan: [Feature Name]

## Timeline
- Start Date: [YYYY-MM-DD]
- End Date: [YYYY-MM-DD]
- Duration: [X days]

## Phases
### Phase 1: [Name] (Duration: X hours)
- Task 1.1: [Description] - [Owner] - [Hours]
- Task 1.2: [Description] - [Owner] - [Hours]

## Resource Allocation
| Resource | Allocation % | Total Hours |
|----------|--------------|-------------|
| [Name]   | [%]          | [Hours]     |

## Risk Register
| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| [Description] | [H/M/L] | [H/M/L] | [Strategy] |

## Milestones
| Milestone | Date | Success Criteria |
|-----------|------|------------------|
| M1        | [Date] | [Criteria]     |
```

### Gate 2: ¿Plan Viable?

**Checklist:**
- [ ] Timeline realista con fechas específicas
- [ ] Recursos identificados y disponibles
- [ ] Riesgos con mitigaciones definidas
- [ ] Milestones con criterios de éxito
- [ ] Presupuesto calculado

✅ PASS → Fase 3 | ❌ FAIL → Ajustar plan

---

## 📋 Fase 3: Product Backlog

**Responsable:** Product Owner (YO)

**Duración:** 2-4 horas

### Actividades:

#### 1. Traducción a User Stories
- Convertir tasks técnicos en historias de usuario
- Formato: "As a [user], I want [goal], so that [benefit]"
- Definir acceptance criteria específicos y medibles
- Asignar story points (Fibonacci: 1/2/3/5/8/13)

#### 2. Priorización
- Aplicar framework MoSCoW (Must/Should/Could/Won't)
- Definir MVP (Minimum Viable Product)
- Separar P0 (crítico) vs P1/P2
- Considerar valor de negocio vs costo técnico

#### 3. Organización en Backlog
- Agrupar en Epics lógicos
- Ordenar backlog por prioridad
- Definir Sprint(s) necesarios
- Establecer Definition of Done

### Output:

**Archivo:** `PRODUCT_BACKLOG_[FEATURE_NAME].md`

**Contenido mínimo:**
```markdown
# Product Backlog: [Feature Name]

## Product Vision
[Objetivo de producto]

## Epics

### Epic 1: [Name]
**Goal**: [Objetivo del epic]

#### US-001: [User Story Title]
**Story**: As a [user type], I want [goal], so that [benefit]

**Acceptance Criteria**:
- Given [context]
- When [action]
- Then [outcome]

**Story Points**: [1/2/3/5/8/13]
**Priority**: [Must/Should/Could/Won't]
**Dependencies**: [List]
```

### Gate 3: ¿Backlog Aceptado?

**Checklist:**
- [ ] User stories en formato correcto
- [ ] Acceptance criteria específicos y medibles
- [ ] Priorización MoSCoW aplicada
- [ ] MVP claramente definido
- [ ] Story points asignados

✅ PASS → Fase 4 | ❌ FAIL → Refinar backlog

---

## 💼 Fase 4: Validación de Negocio

**Responsable:** Business Stakeholder (YO)

**Duración:** 1-2 horas

### Actividades:

#### 1. Evaluación de Impacto de Negocio
- Analizar costo de NO hacer el trabajo
- Evaluar oportunidad de negocio
- Considerar compromisos con clientes
- Revisar alineación con estrategia

#### 2. Decisión de Aprobación
- Revisar presupuesto propuesto
- Validar timeline contra necesidades de negocio
- Aprobar/Rechazar/Modificar el plan
- Definir success criteria de negocio

#### 3. Gestión de Riesgos de Negocio
- Identificar riesgos desde perspectiva de negocio
- Definir tolerancia al riesgo
- Aprobar trade-offs (velocidad vs calidad)
- Establecer métricas de negocio a trackear

### Output:

**Archivo:** `BUSINESS_STAKEHOLDER_DECISION_[FEATURE_NAME].md`

**Contenido mínimo:**
```markdown
# Business Stakeholder Decision: [Feature Name]

## Decision: [GO / NO-GO / CONDITIONAL]

## Business Impact
- Revenue Impact: [$X]
- Customer Impact: [X customers]
- Market Impact: [Description]

## Approved Budget
- Total: [$X]
- Resources: [List]

## Success Criteria
- Metric 1: [Target]
- Metric 2: [Target]

## Accepted Risks
- [Risk 1]: [Acceptance rationale]
```

### Gate 4: Decisión de Negocio

**Checklist:**
- [ ] Decisión documentada (GO/NO-GO/CONDITIONAL)
- [ ] Presupuesto aprobado
- [ ] Timeline aceptable para negocio
- [ ] Success criteria de negocio definidos
- [ ] Riesgos de negocio aceptados

**Resultado:**
- ✅ **GO** → Fase 5 (Ejecución)
- ⚠️ **CONDITIONAL** → Implementar condiciones y proceder
- ❌ **NO-GO** → DETENER trabajo, re-evaluar o cancelar

---

## ⚙️ Fase 5: Ejecución con Agentes

**Responsable:** Technical Lead (coordinador) + Agentes Especializados

**Duración:** Según Plan de Proyecto

### Matriz de Asignación:

| Tipo de Trabajo | Agente Responsable |
|-----------------|-------------------|
| Backend .NET | `dotnet-backend-developer` |
| Testing | `test-engineer` |
| Database | `database-expert` |
| Infrastructure | `devops-engineer` |
| Code Review | `code-reviewer` |
| Architecture | `software-architect` |

### Actividades por Sprint:

#### 1. Sprint Planning
- Product Owner presenta stories
- Team estima y confirma capacity
- Technical Lead asigna stories a agentes
- Se establece Sprint Goal

#### 2. Daily Execution (Development Loop)
- Agentes trabajan en tareas asignadas
- Código según acceptance criteria
- Build exitoso verificado
- **TESTING INMEDIATO OBLIGATORIO:**
  * Manual testing con escenarios reales
  * Validación de cada AC
  * Verificación de no-regression
  * Documentación de resultados

#### 3. Code Review
- Cada PR revisado por `code-reviewer` agent
- Checks automáticos: build, tests, linting
- Aprobación requerida antes de merge

#### 4. Testing Continuo
- `test-engineer` valida cada feature
- Tests E2E por cada milestone
- Bugs reportados y priorizados inmediatamente

#### 5. Sprint Review
- Demo de funcionalidad completada
- Product Owner valida AC
- Business Stakeholder valida valor
- Decisión: deploy o iterar

### Gate 5: ¿Sprint Goal Alcanzado?

**Checklist:**
- [ ] Todas las stories completadas o justificadas
- [ ] Tests E2E pasando
- [ ] Code reviews aprobados
- [ ] No P0 bugs abiertos
- [ ] Demo exitoso a stakeholders

✅ PASS → Siguiente sprint | ❌ FAIL → Extender/Re-planear

---

## ✅ Fase 6: Validación y Cierre

**Responsable:** Product Owner + Business Stakeholder (YO)

**Duración:** 1-2 horas

### Actividades:

#### 1. Validation Testing
- Ejecutar tests E2E completos
- Validar AC de todas las stories
- Verificar métricas técnicas (coverage, performance)
- Confirmar no hay P0/P1 bugs abiertos

#### 2. Business Acceptance
- Business Stakeholder valida contra success criteria
- Product Owner confirma Definition of Done
- Decisión: Aceptar / Rechazar / Iterar

#### 3. Retrospectiva
- ¿Qué salió bien?
- ¿Qué salió mal?
- ¿Qué mejorar para próximo sprint?
- Documentar lecciones aprendidas

#### 4. Documentación
- Actualizar README y docs de usuario
- Documentar cambios en CHANGELOG.md
- Actualizar diagramas de arquitectura
- Crear release notes

### Output:
- `SPRINT_REVIEW_[SPRINT_NUMBER].md`
- `LESSONS_LEARNED_[FEATURE_NAME].md`
- Release notes publicados
- Backlog actualizado

### Gate 6: ¿Deploy Ready?

**Checklist:**
- [ ] 100% acceptance criteria completados
- [ ] Test coverage >= 80%
- [ ] Performance tests pasando
- [ ] Security scan sin P0/P1
- [ ] Documentación actualizada
- [ ] Business Stakeholder sign-off
- [ ] Product Owner sign-off

✅ PASS → Deploy a producción | ❌ FAIL → Iterar

---

## 🚨 Excepciones al Proceso

### Hotfixes de Producción (P0 Bugs)

**Proceso Acelerado:**
1. Detección (5 min): Confirmar severidad P0
2. Diagnóstico Rápido (30 min): Root cause
3. Fix Implementación (1-2 hrs): Solución mínima
4. Testing Rápido (30 min): Smoke tests críticos
5. Deploy Inmediato: Con post-mortem posterior

**Post-Mortem Obligatorio** (dentro de 24 horas)

### Experimentos Técnicos (Spikes)

**Proceso Simplificado:**
1. Propuesta de Spike (30 min)
2. Time-boxed Execution (máximo 1 día)
3. Findings Report (1 hora)
4. Decision: Proceder o descartar

**Límites:**
- Máximo 1 día de esfuerzo
- No commit a main branch
- Resultados documentados

---

**Estado:** VIGENTE y OBLIGATORIO
**Última Actualización:** 2025-10-16
**Versión:** 1.0
