# Marco de Proceso de Desarrollo - YoutubeRag .NET
## Development Process Framework

**Versión**: 1.0
**Fecha de Adopción**: 2025-10-06
**Estado**: OFICIAL - Obligatorio para todo el equipo
**Aprobado por**: Technical Lead, Project Manager, Product Owner, Business Stakeholder

---

## 📜 Propósito del Documento

Este documento establece el **proceso oficial y obligatorio** para todos los avances incrementales en el desarrollo del producto YoutubeRag .NET. Ningún desarrollo significativo debe iniciarse sin seguir este proceso.

### Objetivos
1. ✅ Asegurar alineación entre técnica, producto y negocio
2. ✅ Prevenir trabajo no validado o de bajo valor
3. ✅ Garantizar calidad y revisión adecuada
4. ✅ Mantener visibilidad completa del progreso
5. ✅ Evitar sorpresas y re-trabajos costosos

---

## 🎯 Alcance

Este proceso aplica a:
- ✅ Nuevas features o funcionalidades
- ✅ Bugs críticos o de alta prioridad
- ✅ Refactorizaciones significativas
- ✅ Cambios en arquitectura o diseño
- ✅ Actualizaciones de dependencias mayores
- ✅ Cambios que afecten a usuarios o clientes

Este proceso NO aplica a:
- ❌ Fixes triviales (typos, logging menor)
- ❌ Actualizaciones de documentación interna
- ❌ Ajustes de configuración sin impacto funcional

---

## 🔄 Proceso Oficial de Desarrollo Incremental

### Fase 0: Detección del Trabajo Necesario

**Trigger**: Se identifica necesidad de desarrollo (nuevo feature, bug, mejora)

**Responsable**: Cualquier miembro del equipo (Dev, QA, PM, PO)

**Acción**: Crear un **Issue/Ticket** describiendo:
- Contexto y motivación
- Problema a resolver o feature a implementar
- Impacto esperado en usuarios/negocio
- Estimación preliminar (T-shirt size: S/M/L/XL)

**Output**: Ticket creado y asignado a Technical Lead para triaje

---

### Fase 1: Diagnóstico y Análisis Técnico

**Responsable**: Technical Lead (o Developer Senior asignado)

**Duración**: 2-8 horas dependiendo complejidad

**Actividades**:
1. **Investigación Técnica**
   - Revisar código existente relacionado
   - Identificar componentes afectados
   - Analizar dependencias y acoplamiento
   - Buscar soluciones previas o patrones aplicables

2. **Identificación de Problemas**
   - Listar bugs o limitaciones actuales
   - Clasificar por severidad (P0/P1/P2/P3)
   - Documentar causa raíz cuando aplique
   - Evaluar impacto en sistema completo

3. **Propuesta de Soluciones**
   - Diseñar al menos 2 alternativas
   - Evaluar pros/contras de cada una
   - Estimar esfuerzo y complejidad
   - Identificar riesgos técnicos

**Output**: Documento de Diagnóstico Técnico
- Archivo: `DIAGNOSTIC_REPORT_[FEATURE_NAME].md`
- Ubicación: Raíz del proyecto
- Contenido mínimo:
  - Resumen ejecutivo
  - Problemas identificados (con logs/evidencia)
  - Soluciones propuestas (pros/contras/estimación)
  - Riesgos técnicos
  - Recomendación del Technical Lead

**Ejemplo**: `DIAGNOSTIC_REPORT_VIDEO_INGESTION.md`

---

### Fase 2: Planificación con Project Manager

**Responsable**: Project Manager

**Input**: Documento de Diagnóstico Técnico

**Duración**: 1-4 horas

**Actividades**:
1. **Revisión del Diagnóstico**
   - Validar estimaciones técnicas
   - Identificar dependencias entre tareas
   - Evaluar impacto en cronograma actual

2. **Creación del Plan de Proyecto**
   - Descomponer en fases/milestones
   - Asignar recursos (quien hace qué)
   - Definir cronograma realista
   - Identificar riesgos de cronograma/recursos

3. **Análisis de Riesgos**
   - Crear matriz de riesgos (probabilidad × impacto)
   - Definir mitigaciones para top 3-5 riesgos
   - Establecer plan de contingencia
   - Definir procedimiento de escalación

**Output**: Plan de Proyecto
- Archivo: `PROJECT_PLAN_[FEATURE_NAME].md`
- Contenido mínimo:
  - Timeline con fechas específicas
  - Matriz de asignación de recursos (RACI)
  - Registro de riesgos
  - Milestones con criterios go/no-go
  - Presupuesto estimado (horas × recurso)

**Ejemplo**: `PROJECT_PLAN_VIDEO_INGESTION_RECOVERY.md`

---

### Fase 3: Definición de Producto con Product Owner

**Responsable**: Product Owner

**Input**: Diagnóstico Técnico + Plan de Proyecto

**Duración**: 2-4 horas

**Actividades**:
1. **Traducción a User Stories**
   - Convertir tasks técnicos en historias de usuario
   - Escribir en formato: "As a [user], I want [goal], so that [benefit]"
   - Definir acceptance criteria específicos y medibles
   - Asignar story points (Fibonacci: 1/2/3/5/8/13)

2. **Priorización**
   - Aplicar framework MoSCoW (Must/Should/Could/Won't)
   - Definir MVP (Minimum Viable Product)
   - Separar P0 (crítico) vs P1/P2 (importante/deseable)
   - Considerar valor de negocio vs costo técnico

3. **Organización en Backlog**
   - Agrupar en Epics lógicos
   - Ordenar backlog por prioridad
   - Definir Sprint(s) necesarios
   - Establecer Definition of Done

**Output**: Product Backlog
- Archivo: `PRODUCT_BACKLOG_[FEATURE_NAME].md`
- Contenido mínimo:
  - Visión de producto
  - Epics con objetivos
  - User stories priorizadas
  - Acceptance criteria por story
  - Propuesta de sprint(s)
  - Definition of Done

**Ejemplo**: `PRODUCT_BACKLOG_VIDEO_INGESTION.md`

---

### Fase 4: Validación de Negocio con Business Stakeholder

**Responsable**: Business Stakeholder

**Input**: Diagnóstico + Plan de Proyecto + Product Backlog

**Duración**: 1-2 horas

**Actividades**:
1. **Evaluación de Impacto de Negocio**
   - Analizar costo de NO hacer el trabajo
   - Evaluar oportunidad de negocio
   - Considerar compromisos con clientes
   - Revisar alineación con estrategia

2. **Decisión de Aprobación**
   - Revisar presupuesto propuesto
   - Validar timeline contra necesidades de negocio
   - Aprobar/Rechazar/Modificar el plan
   - Definir success criteria de negocio

3. **Gestión de Riesgos de Negocio**
   - Identificar riesgos desde perspectiva de negocio
   - Definir tolerancia al riesgo
   - Aprobar trade-offs (velocidad vs calidad)
   - Establecer métricas de negocio a trackear

**Output**: Decisión de Stakeholder
- Archivo: `BUSINESS_STAKEHOLDER_DECISION_[FEATURE_NAME].md`
- Contenido mínimo:
  - Decisión: GO / NO-GO / CONDITIONAL
  - Presupuesto aprobado
  - Must-have vs nice-to-have
  - Timeline límite
  - Métricas de negocio
  - Riesgos aceptados

**Ejemplo**: `BUSINESS_STAKEHOLDER_DECISION_VIDEO_INGESTION.md`

**Criterios de Decisión**:
- ✅ **GO**: Aprobar plan completo, proceder inmediatamente
- ⚠️ **CONDITIONAL**: Aprobar con modificaciones, condiciones específicas
- ❌ **NO-GO**: Rechazar, no proceder (proveer razones y alternativas)

---

### Fase 5: Ejecución con Agentes Especializados

**Responsable**: Technical Lead (coordinador) + Agentes Especializados

**Input**: Plan de Proyecto aprobado + Product Backlog + Decisión de Negocio

**Duración**: Según lo definido en el Plan de Proyecto

**Proceso de Asignación**:

```
┌─────────────────────────────────────────────────────────────┐
│ MATRIZ DE ASIGNACIÓN DE AGENTES                            │
├─────────────────────────────────────────────────────────────┤
│ Tipo de Trabajo          → Agente Responsable              │
├─────────────────────────────────────────────────────────────┤
│ Backend API (C#/.NET)    → dotnet-backend-developer         │
│ Frontend (React/Angular) → frontend-react-developer         │
│ Database (Schema/Query)  → database-expert                  │
│ Testing (Unit/E2E)       → test-engineer                    │
│ Infrastructure (CI/CD)   → devops-engineer                  │
│ Code Review              → code-reviewer                    │
│ Architecture Design      → software-architect               │
├─────────────────────────────────────────────────────────────┤
│ Coordinación             → project-manager                  │
│ Priorización             → product-owner                    │
│ Validación Negocio       → business-stakeholder             │
└─────────────────────────────────────────────────────────────┘
```

**Actividades por Sprint/Fase**:

1. **Sprint Planning**
   - Product Owner presenta stories del sprint
   - Team estima y confirma capacity
   - Technical Lead asigna stories a agentes
   - Se establece Sprint Goal

2. **Daily Execution (Development Loop)**
   - Agentes trabajan en sus tareas asignadas
   - Código implementado según acceptance criteria
   - Compilación y verificación de build exitoso
   - **TESTING INMEDIATO OBLIGATORIO** (antes de marcar como Done):
     * Manual testing con escenarios reales
     * Validación de cada acceptance criterion
     * Verificación de no-regression
     * Documentación de resultados de testing
   - Technical Lead coordina dependencias
   - Daily standups virtuales (status update)
   - Bloqueadores escalados inmediatamente

3. **Code Review**
   - Cada PR revisado por `code-reviewer` agent
   - Checks automáticos: build, tests, linting
   - Aprobación requerida antes de merge
   - Technical Lead revisa cambios críticos

4. **Testing Continuo**
   - `test-engineer` valida cada feature completado
   - Tests E2E ejecutados por cada milestone
   - Bugs reportados y priorizados inmediatamente
   - Quality gates enforcement

5. **Sprint Review**
   - Demo de funcionalidad completada
   - Product Owner valida acceptance criteria
   - Business Stakeholder valida valor de negocio
   - Decisión: deploy o iterar

**Métricas de Seguimiento**:
- Velocity (story points completados/sprint)
- Burn-down chart (trabajo restante vs tiempo)
- Bug count (P0/P1/P2 abiertos)
- Test coverage (% código cubierto)
- Success rate (features completados vs planeados)

---

### Fase 6: Validación y Cierre

**Responsable**: Product Owner + Business Stakeholder

**Duración**: 1-2 horas

**Actividades**:

1. **Validation Testing**
   - Ejecutar tests E2E completos
   - Validar acceptance criteria de todas las stories
   - Verificar métricas técnicas (coverage, performance)
   - Confirmar no hay P0/P1 bugs abiertos

2. **Business Acceptance**
   - Business Stakeholder valida contra success criteria
   - Product Owner confirma Definition of Done
   - Decisión: Aceptar / Rechazar / Iterar

3. **Retrospectiva**
   - ¿Qué salió bien?
   - ¿Qué salió mal?
   - ¿Qué mejorar para próximo sprint?
   - Documentar lecciones aprendidas

4. **Documentación**
   - Actualizar README y docs de usuario
   - Documentar cambios en CHANGELOG.md
   - Actualizar diagramas de arquitectura
   - Crear release notes

**Output**:
- `SPRINT_REVIEW_[SPRINT_NUMBER].md`
- `LESSONS_LEARNED_[FEATURE_NAME].md`
- Release notes publicados
- Backlog actualizado para próximo sprint

---

## 📊 Templates Obligatorios

### 1. Diagnostic Report Template

```markdown
# Diagnostic Report: [Feature/Bug Name]

## Executive Summary
[1-2 párrafos: qué problema, por qué crítico, solución recomendada]

## Current State Analysis
### What Works
- [Lista de funcionalidad existente OK]

### What Doesn't Work
- [Lista de bugs/limitaciones con evidencia]

## Root Cause Analysis
[Causa raíz de cada problema principal]

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
[Top 3-5 riesgos identificados]
```

### 2. Project Plan Template

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

### 3. Product Backlog Template

```markdown
# Product Backlog: [Feature Name]

## Product Vision
[1-2 párrafos sobre el objetivo de producto]

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

### 4. Business Decision Template

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

---

## 🚦 Quality Gates (Go/No-Go)

En cada fase, existen criterios obligatorios para avanzar.

### NUEVO: Validation Gate (Después de cada User Story)
**Criterio**: User Story validada con testing real

**Checklist OBLIGATORIO**:
- [ ] Código compila exitosamente
- [ ] Testing manual ejecutado con escenarios reales
- [ ] TODOS los acceptance criteria validados mediante pruebas
- [ ] Capturas de pantalla o logs como evidencia
- [ ] No hay regresiones (funcionalidad existente sigue funcionando)
- [ ] Resultados de testing documentados

**Resultado**:
- ✅ PASS → User Story marcada como DONE, proceder a siguiente
- ❌ FAIL → User Story vuelve a IN PROGRESS, fix bugs encontrados

**IMPORTANTE**: Una User Story NO puede marcarse como DONE sin pasar este gate.

### Gate 1: Después de Diagnóstico Técnico
**Criterio**: Documento de diagnóstico completo y revisado

**Checklist**:
- [ ] Problemas identificados con evidencia
- [ ] Al menos 2 soluciones propuestas
- [ ] Estimaciones de esfuerzo incluidas
- [ ] Riesgos técnicos documentados
- [ ] Revisado por Technical Lead

**Resultado**:
- ✅ PASS → Proceder a Fase 2 (PM Planning)
- ❌ FAIL → Completar diagnóstico antes de continuar

---

### Gate 2: Después de Project Plan
**Criterio**: Plan de proyecto viable y aprobado por PM

**Checklist**:
- [ ] Timeline realista con fechas específicas
- [ ] Recursos identificados y disponibles
- [ ] Riesgos con mitigaciones definidas
- [ ] Milestones con criterios de éxito
- [ ] Presupuesto calculado

**Resultado**:
- ✅ PASS → Proceder a Fase 3 (PO Backlog)
- ❌ FAIL → Ajustar plan hasta que sea viable

---

### Gate 3: Después de Product Backlog
**Criterio**: Backlog priorizado y aceptado por PO

**Checklist**:
- [ ] User stories en formato correcto
- [ ] Acceptance criteria específicos y medibles
- [ ] Priorización MoSCoW aplicada
- [ ] MVP claramente definido
- [ ] Story points asignados

**Resultado**:
- ✅ PASS → Proceder a Fase 4 (Business Validation)
- ❌ FAIL → Refinar backlog hasta cumplir estándares

---

### Gate 4: Después de Business Decision
**Criterio**: Aprobación de negocio obtenida

**Checklist**:
- [ ] Decisión documentada (GO/NO-GO/CONDITIONAL)
- [ ] Presupuesto aprobado
- [ ] Timeline aceptable para negocio
- [ ] Success criteria de negocio definidos
- [ ] Riesgos de negocio aceptados

**Resultado**:
- ✅ GO → Proceder a Fase 5 (Execution)
- ⚠️ CONDITIONAL → Implementar condiciones y proceder
- ❌ NO-GO → DETENER trabajo, re-evaluar o cancelar

---

### Gate 5: Después de cada Sprint
**Criterio**: Sprint goal alcanzado

**Checklist**:
- [ ] Todas las stories completadas o justificadas
- [ ] Tests E2E pasando
- [ ] Code review aprobados
- [ ] No P0 bugs abiertos
- [ ] Demo exitoso a stakeholders

**Resultado**:
- ✅ PASS → Proceder a siguiente sprint
- ❌ FAIL → Extender sprint o re-planear

---

### Gate 6: Antes de Deploy a Producción
**Criterio**: Feature completo y validado

**Checklist**:
- [ ] 100% acceptance criteria completados
- [ ] Test coverage >= 80%
- [ ] Performance tests pasando
- [ ] Security scan sin P0/P1
- [ ] Documentación actualizada
- [ ] Business Stakeholder sign-off
- [ ] Product Owner sign-off

**Resultado**:
- ✅ PASS → Aprobar deploy a producción
- ❌ FAIL → Iterar hasta cumplir criterios

---

## 🎭 Roles y Responsabilidades

### Technical Lead
**Responsabilidades**:
- Liderar diagnóstico técnico
- Coordinar agentes especializados
- Revisar código crítico
- Tomar decisiones técnicas
- Asegurar calidad del código

**Autoridad**:
- Aprobar/rechazar soluciones técnicas
- Asignar tareas a agentes
- Definir estándares de código
- Escalación técnica

---

### Project Manager
**Responsabilidades**:
- Crear y mantener plan de proyecto
- Gestionar recursos y timeline
- Identificar y mitigar riesgos
- Coordinar entre equipos
- Reportar progreso a stakeholders

**Autoridad**:
- Aprobar cambios de scope
- Re-asignar recursos
- Ajustar timeline (dentro de límites)
- Escalación de recursos/timeline

---

### Product Owner
**Responsabilidades**:
- Definir y priorizar backlog
- Escribir user stories
- Validar acceptance criteria
- Tomar decisiones de producto
- Maximizar valor de negocio

**Autoridad**:
- Aprobar/rechazar features
- Re-priorizar backlog
- Definir MVP
- Aceptar/rechazar deliverables

---

### Business Stakeholder
**Responsabilidades**:
- Validar valor de negocio
- Aprobar presupuestos
- Definir success criteria
- Aceptar riesgos de negocio
- Dar sign-off final

**Autoridad**:
- GO/NO-GO decisiones
- Aprobar presupuestos
- Definir prioridades de negocio
- Cambiar timeline por razones de negocio

---

### Specialized Agents
**Responsabilidades**:
- Implementar features asignados
- Escribir tests
- Documentar código
- Reportar progreso diario
- Escalar bloqueadores

**Autoridad**:
- Decisiones de implementación detallada
- Proponer mejoras técnicas
- Rechazar tareas mal definidas

---

## 📈 Métricas de Éxito del Proceso

### Métricas de Calidad
- **Test Coverage**: >= 80% para código nuevo
- **Bug Escape Rate**: < 5% de bugs llegan a producción
- **Code Review Rejection**: < 10% de PRs rechazados
- **Tech Debt Ratio**: < 20% del código clasificado como tech debt

### Métricas de Velocidad
- **Velocity Consistency**: +/- 15% entre sprints
- **Cycle Time**: Promedio días desde start a deploy
- **Lead Time**: Promedio días desde idea a producción
- **Deployment Frequency**: >= 1 por semana

### Métricas de Proceso
- **Planning Accuracy**: Estimación vs real < 20% error
- **Process Adherence**: 100% de features siguen proceso
- **Documentation Coverage**: 100% de features documentados
- **Stakeholder Satisfaction**: >= 8/10 en encuestas

---

## 🚨 Excepciones al Proceso

### Hotfixes de Producción (P0 Bugs)

**Cuando Aplica**:
- Sistema caído completamente
- Pérdida de datos en curso
- Vulnerabilidad de seguridad crítica
- Bug que afecta >50% de usuarios

**Proceso Acelerado**:
1. **Detección** (5 min): Confirmar severidad P0
2. **Diagnóstico Rápido** (30 min): Root cause
3. **Fix Implementación** (1-2 hrs): Solución mínima
4. **Testing Rápido** (30 min): Smoke tests críticos
5. **Deploy Inmediato**: Con post-mortem posterior

**Post-Mortem Obligatorio** (dentro de 24 horas):
- Documento de análisis de causa raíz
- Plan para prevenir recurrencia
- Actualización de runbooks
- Mejoras de proceso identificadas

---

### Experimentos Técnicos (Spikes)

**Cuando Aplica**:
- Investigar nueva tecnología
- Probar feasibility de approach
- Benchmark de performance
- Prototipos descartables

**Proceso Simplificado**:
1. **Propuesta de Spike** (30 min): Qué investigar y por qué
2. **Time-boxed Execution** (máximo 1 día): Investigar
3. **Findings Report** (1 hora): Documentar resultados
4. **Decision**: Proceder con proceso completo o descartar

**Límites**:
- Máximo 1 día de esfuerzo
- No commit a main branch
- No cambios en producción
- Resultados documentados obligatoriamente

---

## 📚 Documentos de Referencia

### Documentos Creados en Este Proyecto
- ✅ `DIAGNOSTIC_REPORT_FOR_STAKEHOLDERS.md`
- ✅ `PROJECT_PLAN_VIDEO_INGESTION_RECOVERY.md`
- ✅ `PRODUCT_BACKLOG_VIDEO_INGESTION.md`
- ✅ `BUSINESS_STAKEHOLDER_DECISION.md`

### Templates Disponibles
- ✅ Diagnostic Report Template (ver arriba)
- ✅ Project Plan Template (ver arriba)
- ✅ Product Backlog Template (ver arriba)
- ✅ Business Decision Template (ver arriba)

### Procesos Relacionados
- Code Review Guidelines: `docs/CODE_REVIEW_GUIDE.md` (TODO)
- Testing Strategy: `docs/TESTING_STRATEGY.md` (TODO)
- CI/CD Pipeline: `.github/workflows/README.md` (TODO)

---

## 🔄 Actualización de Este Documento

Este documento es **vivo** y debe actualizarse:

**Frecuencia**: Revisión trimestral o cuando sea necesario

**Proceso de Actualización**:
1. Propuesta de cambio discutida en retrospectiva
2. Technical Lead redacta cambios propuestos
3. Revisión con PM, PO, Business Stakeholder
4. Aprobación por mayoría (3/4 roles)
5. Actualización de versión y fecha
6. Comunicación a todo el equipo

**Control de Versiones**:
- Versión actual: 1.0
- Fecha última actualización: 2025-10-06
- Próxima revisión programada: 2025-12-06

---

## ✅ Checklist de Adopción

Para asegurar que el proceso se sigue correctamente:

### Para cada Nuevo Trabajo
- [ ] Se creó ticket/issue inicial
- [ ] Se pasó por triaje con Technical Lead
- [ ] Se creó Diagnostic Report
- [ ] Se creó Project Plan
- [ ] Se creó Product Backlog
- [ ] Se obtuvo Business Decision
- [ ] Se ejecutó con agentes asignados
- [ ] Se pasaron quality gates
- [ ] Se hizo sprint review
- [ ] Se documentaron lecciones aprendidas

### Para cada Sprint
- [ ] Sprint planning realizado
- [ ] Stories asignadas a agentes
- [ ] Daily standups ejecutados
- [ ] Code reviews completados
- [ ] Tests E2E ejecutados
- [ ] Sprint review con demo
- [ ] Retrospectiva documentada

---

## 📞 Contacto y Soporte

**Preguntas sobre el Proceso**:
- Technical Lead: Responsable final del proceso técnico
- Project Manager: Soporte en planificación y recursos
- Product Owner: Clarificación de prioridades

**Propuestas de Mejora**:
- Enviar propuesta en retrospectiva de sprint
- O crear issue: `[PROCESS] Mejora propuesta: [descripción]`

**Escalación**:
- Bloqueadores del proceso → Technical Lead
- Conflictos de prioridad → Product Owner
- Restricciones de recursos → Project Manager
- Decisiones de negocio → Business Stakeholder

---

## 🎓 Capacitación

### Para Nuevos Miembros del Equipo
1. Leer este documento completo
2. Revisar ejemplos reales (archivos creados)
3. Observar 1 ciclo completo (diagnóstico → deploy)
4. Ejecutar 1 ciclo con supervisión
5. Autonomía completa

### Recursos de Aprendizaje
- Templates en este documento
- Ejemplos reales en el repositorio
- Retrospectivas documentadas
- Lecciones aprendidas de sprints anteriores

---

## 📜 Aprobaciones

Este documento ha sido revisado y aprobado por:

| Rol | Nombre | Fecha | Firma |
|-----|--------|-------|-------|
| Technical Lead | Claude Code | 2025-10-06 | ✅ Aprobado |
| Project Manager | project-manager agent | 2025-10-06 | ✅ Aprobado |
| Product Owner | product-owner agent | 2025-10-06 | ✅ Aprobado |
| Business Stakeholder | business-stakeholder agent | 2025-10-06 | ✅ Aprobado |

---

**Estado**: VIGENTE y OBLIGATORIO

**Última Actualización**: 2025-10-06

**Versión**: 1.0

---

## Anexo A: Diagrama de Flujo del Proceso

```
┌─────────────────────────────────────────────────────────────────┐
│                    INICIO: Trabajo Identificado                 │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│  FASE 1: Diagnóstico Técnico                                    │
│  Owner: Technical Lead                                          │
│  Output: DIAGNOSTIC_REPORT_[NAME].md                            │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
                    [Gate 1: ¿Diagnóstico Completo?]
                         │
              ┌──────────┴──────────┐
              │ NO                  │ SÍ
              ▼                     ▼
      [Completar Diagnóstico]     [Continuar]
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│  FASE 2: Planificación                                          │
│  Owner: Project Manager                                         │
│  Output: PROJECT_PLAN_[NAME].md                                 │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
                    [Gate 2: ¿Plan Viable?]
                         │
              ┌──────────┴──────────┐
              │ NO                  │ SÍ
              ▼                     ▼
      [Ajustar Plan]              [Continuar]
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│  FASE 3: Product Backlog                                        │
│  Owner: Product Owner                                           │
│  Output: PRODUCT_BACKLOG_[NAME].md                              │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
                    [Gate 3: ¿Backlog Aceptado?]
                         │
              ┌──────────┴──────────┐
              │ NO                  │ SÍ
              ▼                     ▼
      [Refinar Backlog]           [Continuar]
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│  FASE 4: Validación de Negocio                                  │
│  Owner: Business Stakeholder                                    │
│  Output: BUSINESS_STAKEHOLDER_DECISION_[NAME].md                │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
              [Gate 4: Decisión de Negocio]
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
      [NO-GO]      [CONDITIONAL]      [GO]
         │               │               │
         ▼               ▼               │
    [DETENER]   [Implementar          [Continuar]
                 Condiciones]            │
                         │               │
                         └───────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│  FASE 5: Ejecución con Agentes                                  │
│  Owner: Technical Lead + Specialized Agents                     │
│  Output: Working Software + Tests + Docs                        │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
                  [Gate 5: ¿Sprint Goal?]
                         │
              ┌──────────┴──────────┐
              │ NO                  │ SÍ
              ▼                     ▼
      [Extender/Re-planear]      [Continuar]
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│  FASE 6: Validación y Cierre                                    │
│  Owner: PO + Business Stakeholder                               │
│  Output: Sprint Review + Lessons Learned                        │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
              [Gate 6: ¿Deploy Ready?]
                         │
              ┌──────────┴──────────┐
              │ NO                  │ SÍ
              ▼                     ▼
         [Iterar]            [Deploy a Producción]
                                    │
                                    ▼
                            [FIN: Feature Live]
```

---

**FIN DEL DOCUMENTO**
