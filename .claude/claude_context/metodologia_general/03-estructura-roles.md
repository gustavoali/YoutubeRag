# 03 - Estructura de Roles

**Versión:** 1.0 (compatible con v2.0)
**Fecha:** 2025-10-20
**Estado:** ACTIVO
**Contexto:** Trabajo individual (desarrollador asume todos los roles)

---

## 🎯 Resumen Ejecutivo

En este proyecto, **una sola persona asume todos los roles de decisión**: Technical Lead, Project Manager, Product Owner y Business Stakeholder. Los **agentes especializados de Claude Code** se utilizan para ejecutar tareas específicas bajo la dirección del desarrollador.

**Principio clave:** TÚ DECIDES, agentes EJECUTAN.

---

## 📊 Estructura de Roles

### Roles Humanos (Asumidos por el Desarrollador)

```
TÚ (Desarrollador)
├── 👔 Technical Lead (decisiones técnicas)
├── 📅 Project Manager (planificación y recursos)
├── 📝 Product Owner (prioridades y backlog)
└── 💼 Business Stakeholder (aprobaciones estratégicas)
```

### Agentes de Claude Code (Herramientas de Ejecución)

```
Specialized Agents
├── dotnet-backend-developer (implementación .NET)
├── test-engineer (testing automatizado)
├── database-expert (diseño y optimización DB)
├── devops-engineer (CI/CD, infraestructura)
├── code-reviewer (revisión de calidad)
├── software-architect (diseño de arquitectura)
├── product-owner (asistencia en backlog)
└── project-manager (asistencia en planificación)
```

---

## 🎭 Roles en Detalle

### 1. Product Owner (TÚ decides, agente asiste)

**Responsabilidades:**
- ✅ Definir historias de usuario
- ✅ Priorizar backlog
- ✅ Escribir criterios de aceptación
- ✅ Validar historias completadas
- ✅ Aceptar/rechazar deliverables

**Proceso:**
1. **TÚ DECIDES** qué historias crear y su prioridad
2. **OPCIONALMENTE** usas el agente `product-owner` para:
   - Redactar historias en formato correcto
   - Organizar backlog con RICE scoring
   - Estimar story points
   - Crear sprint planning

**Cuándo usar el agente:**
```bash
# Estructurar historias
"Ayúdame a crear historias de usuario para implementar búsqueda semántica basado en estos requisitos: [contexto]"

# Priorizar múltiples historias
"Prioriza estas historias usando RICE scoring: [lista de historias]"

# Crear Definition of Ready
"Verifica que estas historias cumplan con Definition of Ready v2.0"
```

---

### 2. Project Manager (TÚ decides, agente asiste)

**Responsabilidades:**
- ✅ Planificar sprints
- ✅ Gestionar timeline y recursos
- ✅ Identificar y mitigar riesgos
- ✅ Coordinar tareas
- ✅ Reportar progreso

**Proceso:**
1. **TÚ DECIDES** el alcance del sprint y deadlines
2. **OPCIONALMENTE** usas el agente `project-manager` para:
   - Crear planes de proyecto detallados
   - Analizar riesgos con probabilidad/impacto
   - Calcular capacity con fórmula v2.0
   - Generar reportes de progreso

**Cuándo usar el agente:**
```bash
# Plan de proyecto estructurado
"Crea un plan de proyecto para implementar Epic 1 (Video Ingestion) en Sprint 11"

# Análisis de riesgos
"Analiza los riesgos de integrar YoutubeExplode y propón mitigaciones"

# Capacity planning
"Calcula la capacity para Sprint 11 usando la fórmula v2.0"
```

---

### 3. Technical Lead (TÚ decides y ejecutas)

**Responsabilidades:**
- ✅ Tomar decisiones técnicas
- ✅ Revisar arquitectura
- ✅ Asignar tareas a agentes
- ✅ Code review crítico
- ✅ Resolver problemas técnicos

**Proceso:**
1. **TÚ TOMAS** todas las decisiones técnicas
2. **TÚ USAS** agentes especializados para ejecutar:
   - `software-architect`: Diseño de arquitectura
   - `dotnet-backend-developer`: Implementación
   - `database-expert`: Diseño de base de datos
   - `code-reviewer`: Revisión de código

**Cuándo usar agentes:**
```bash
# Diseño de arquitectura
"Diseña la arquitectura para el pipeline de video ingestion siguiendo Clean Architecture"

# Implementación de código
"Implementa US-101 (Submit YouTube URL) en VideoService siguiendo la spec en SPRINT_11_PLAN.md"

# Revisión de código
"Revisa el código de VideoService para calidad, security y performance"

# Optimización de DB
"Optimiza las queries de búsqueda de videos para obtener p95 <100ms"
```

---

### 4. Business Stakeholder (TÚ decides)

**Responsabilidades:**
- ✅ Aprobar presupuestos
- ✅ Decisiones GO/NO-GO
- ✅ Validar valor de negocio
- ✅ Definir success criteria

**Proceso:**
1. **TÚ TOMAS** todas las decisiones de negocio
2. **NO SE UTILIZAN** agentes (decisiones puramente humanas)

**Ejemplos de decisiones:**
- "¿Invertimos en OpenAI Whisper o usamos modelo local?"
- "¿Priorizamos búsqueda semántica o batch processing?"
- "¿El MVP está listo para producción?"

---

## 🔄 Flujo de Trabajo Completo

### Fase 1: Definición de Historias

```
1. TÚ (Product Owner) defines qué quieres construir
   Ejemplo: "Necesito un pipeline de video ingestion"

2. OPCIONALMENTE usas agente product-owner:
   Input: "Crea historias de usuario para video ingestion pipeline"
   Output: US-101, US-102, US-103 con AC completos

3. TÚ revisas y apruebas las historias
   - Verificas Definition of Ready (v2.0)
   - Ajustas prioridades según valor de negocio
```

### Fase 2: Planificación de Sprint

```
1. TÚ (PM) decides cuántas historias entran en el sprint
   Ejemplo: "21 story points para Sprint 11"

2. OPCIONALMENTE usas agente project-manager:
   Input: "Planifica Sprint 11 con US-101, US-102, US-103"
   Output: Sprint plan con timeline, capacity calculation, risk analysis

3. TÚ ajustas el plan según tu disponibilidad
   - Verificas capacity con fórmula v2.0
   - Aplicas buffer 20%
   - Confirmas commitment
```

### Fase 3: Creación de Rama Git

```
1. TÚ creas rama siguiendo naming convention:
   git checkout master
   git pull origin master
   git checkout -b feature/epic-1-video-ingestion
```

### Fase 4: Implementación

```
1. TÚ (Technical Lead) asignas tareas a agentes:

   Para arquitectura:
   - Agente: software-architect
   - Tarea: "Diseña la integración de YoutubeExplode para US-101"

   Para desarrollo:
   - Agente: dotnet-backend-developer
   - Tarea: "Implementa IVideoService.SubmitVideoFromUrlAsync() según SPRINT_11_PLAN.md"

   Para base de datos:
   - Agente: database-expert
   - Tarea: "Optimiza índices para búsqueda de videos por YoutubeId"

2. TÚ supervisas y ajustas el trabajo de los agentes
   - Verificas que sigue Clean Architecture
   - Validas performance requirements
   - Aseguras test coverage >80%
```

### Fase 5: Testing y DoD

```
1. Agente test-engineer:
   - Tarea: "Crea unit tests para VideoService.SubmitVideoFromUrlAsync()"
   - Output: Tests con coverage >80%

2. TÚ ejecutas testing manual siguiendo TESTING_METHODOLOGY_RULES.md

3. TÚ verificas que se cumple el DoD (checklist completo)
```

### Fase 6: Code Review

```
1. Agente code-reviewer:
   - Tarea: "Revisa código de US-101"
   - Output: Feedback de calidad, security, performance

2. TÚ decides qué feedback aplicar
   - Crítico: Aplicas inmediatamente
   - Sugerencia: Evalúas vs. timeline
   - Nice-to-have: Creas technical debt item
```

### Fase 7: Merge a Master

```
1. TÚ verificas DoD completo (100%)

2. TÚ ejecutas merge:
   git checkout master
   git merge --no-ff feature/epic-1-video-ingestion
   git push origin master

3. TÚ creas PR en GitHub con descripción completa
```

### Fase 8: Validación de Sprint

```
1. TÚ ejecutas regresión automática (CI/CD pipeline)

2. TÚ ejecutas testing manual completo

3. TÚ (Product Owner) validas las historias contra AC

4. TÚ (Business Stakeholder) das sign-off

5. TÚ (PM) creas sprint retrospective
```

---

## 📋 Matriz de Decisiones

| Decisión | Quién Decide | Agente que Asiste | Obligatorio/Opcional |
|----------|--------------|-------------------|----------------------|
| **Qué construir** | TÚ (PO) | product-owner | Opcional |
| **Prioridad de historias** | TÚ (PO) | product-owner | Opcional |
| **Definition of Ready** | TÚ (PO + TL) | product-owner | Recomendado |
| **Cuándo construir** | TÚ (PM) | project-manager | Opcional |
| **Alcance del sprint** | TÚ (PM) | project-manager | Opcional |
| **Capacity planning** | TÚ (PM) | project-manager | Recomendado |
| **Cómo construir (arquitectura)** | TÚ (TL) | software-architect | Recomendado |
| **Implementación de código** | TÚ (TL) | dotnet-backend-developer | **Obligatorio** |
| **Diseño de base de datos** | TÚ (TL) | database-expert | Recomendado |
| **Testing automatizado** | TÚ (TL) | test-engineer | Recomendado |
| **Testing manual** | TÚ (TL) | Ninguno | **TÚ ejecutas** |
| **Code review** | TÚ (TL) | code-reviewer | Recomendado |
| **Aprobar sprint** | TÚ (PO + Stakeholder) | Ninguno | **TÚ decides** |
| **Technical Debt decisions** | TÚ (TL) | Ninguno | **TÚ decides** (ROI) |

---

## ✅ Cuándo SÍ usar agentes

### 1. Tareas de Ejecución Repetitivas
- ✅ Implementar código según spec
- ✅ Crear tests unitarios e integración
- ✅ Generar documentación (Swagger, README)
- ✅ Refactorizar código siguiendo patrones

### 2. Tareas que Requieren Expertise Específico
- ✅ Diseño de arquitectura compleja
- ✅ Optimización de queries SQL
- ✅ Security review (OWASP checklist)
- ✅ Performance profiling

### 3. Tareas de Análisis Estructurado
- ✅ Priorización de historias (RICE scoring)
- ✅ Planificación de sprints (capacity formula)
- ✅ Análisis de riesgos (probabilidad × impacto)
- ✅ Code review sistemático

---

## ❌ Cuándo NO usar agentes

### 1. Decisiones de Negocio
- ❌ Prioridades estratégicas
- ❌ Aprobaciones de presupuesto
- ❌ Definición de valor de negocio
- ❌ GO/NO-GO decisions

### 2. Decisiones Creativas
- ❌ Naming de features
- ❌ UX/UI design decisions
- ❌ Branding y messaging
- ❌ User experience strategy

### 3. Testing Manual Exploratorio
- ❌ Pruebas de usabilidad
- ❌ Validación end-to-end
- ❌ Aceptación de usuario
- ❌ Edge cases no documentados

---

## 📝 Templates de Prompts por Agente

### product-owner

```markdown
"Basándome en el PRODUCT_BACKLOG.md, crea historias de usuario para Sprint 11.

Alcance:
- Epic 1: Video Ingestion Pipeline
- Features: Submit URL, Download Video, Extract Audio

Las historias deben:
- Seguir formato: As a [user], I want [goal], so that [benefit]
- Incluir 4 AC en formato Given-When-Then
- Cumplir Definition of Ready v2.0
- Estar priorizadas usando MoSCoW
- Incluir estimación en story points

Referencia: SPRINT_11_PLAN.md para contexto técnico"
```

### project-manager

```markdown
"Crea un sprint plan para Sprint 11 con estas historias:
- US-101: Submit YouTube URL (5 pts)
- US-102: Download Video (8 pts)
- US-103: Extract Audio (5 pts)

El sprint debe:
- Durar 10 días
- Usar capacity planning formula v2.0
- Incluir 20% buffer
- Identificar dependencias (US-102 depends on US-101)
- Listar riesgos y mitigaciones
- Proporcionar timeline día a día

Referencia: SPRINT_11_CAPACITY_CALCULATION.md"
```

### dotnet-backend-developer

```markdown
"Implementa US-101: Submit YouTube URL for Processing.

Acceptance Criteria (de SPRINT_11_PLAN.md):
- AC1: URL validation (youtube.com and youtu.be formats)
- AC2: Duplicate detection by YoutubeId
- AC3: Metadata extraction (title, duration, author, thumbnail)
- AC4: Job creation with "Pending" status

Especificaciones técnicas:
- Ubicación: YoutubeRag.Application/Services/VideoService.cs
- Método: IVideoService.SubmitVideoFromUrlAsync(SubmitVideoDto dto)
- Seguir Clean Architecture
- Usar YoutubeExplode NuGet package
- Retry logic: 3 attempts con exponential backoff (Polly)
- Transaction: Atomic Video + Job creation
- Tests requeridos: Unit (>80%) + Integration

Definition of Done:
[Ver SPRINT_11_PLAN.md sección US-101 DoD]"
```

### code-reviewer

```markdown
"Revisa el código implementado para US-101 en estos archivos:
- YoutubeRag.Application/Services/VideoService.cs
- YoutubeRag.Api/Controllers/VideosController.cs
- YoutubeRag.Tests.Unit/Application/Services/VideoServiceTests.cs

Enfócate en:
- ✅ Cumplimiento de Clean Architecture
- ✅ SOLID principles
- ✅ Security vulnerabilities (SQL injection, XSS)
- ✅ Performance issues (N+1 queries, memory leaks)
- ✅ Test coverage >80%
- ✅ Error handling comprehensivo
- ✅ Code smells y anti-patterns

Referencia:
- DEVELOPMENT_GUIDELINES_NET.md para estándares
- TESTING_METHODOLOGY_RULES.md para testing

Proporciona feedback accionable con prioridad (Critical/High/Medium/Low)."
```

---

## 🎯 Checklist por Fase

### Al Crear Historias de Usuario

- [ ] TÚ defines el objetivo del sprint
- [ ] TÚ describes el alcance general
- [ ] Agente product-owner crea historias estructuradas
- [ ] TÚ revisas y ajustas prioridades
- [ ] TÚ verificas Definition of Ready (100%)
- [ ] TÚ apruebas el backlog final

### Al Planificar Sprint

- [ ] TÚ decides cuántas historias incluir
- [ ] TÚ defines deadlines
- [ ] Agente project-manager calcula capacity (formula v2.0)
- [ ] Agente project-manager crea plan detallado
- [ ] TÚ ajustas según tu disponibilidad
- [ ] TÚ aplicas buffer 20%
- [ ] TÚ apruebas el sprint plan

### Al Implementar Historia

- [ ] TÚ creas rama git
- [ ] TÚ defines especificaciones técnicas
- [ ] Agente software-architect diseña arquitectura (si necesario)
- [ ] Agente dotnet-backend-developer implementa según spec
- [ ] TÚ supervisas progreso (daily check)
- [ ] TÚ ejecutas testing manual
- [ ] Agente test-engineer crea tests automatizados
- [ ] Agente code-reviewer revisa calidad
- [ ] TÚ aplicas feedback crítico
- [ ] TÚ verificas DoD completo (100%)
- [ ] TÚ mergeas a master

### Al Cerrar Sprint

- [ ] TÚ ejecutas regresión automática (CI/CD)
- [ ] TÚ ejecutas testing manual completo
- [ ] TÚ (PO) validas acceptance criteria
- [ ] TÚ (Stakeholder) das sign-off
- [ ] TÚ (PM) creas sprint retrospective
- [ ] TÚ (TL) actualizas Technical Debt Register
- [ ] TÚ (TL) documentas lessons learned

---

## 🔗 Integración con Otras Metodologías

Este documento se integra con:

- **04-workflow-git-branches.md**: Workflow de git y branches
- **05-reglas-testing.md**: Reglas de testing obligatorias
- **02-proceso-desarrollo-6-fases.md**: Proceso completo
- **14-definition-of-ready.md**: Checklist pre-desarrollo (v2.0)
- **12-technical-debt-management.md**: ROI-based TD decisions (v2.0)

---

## 📚 Ejemplo Completo: De Idea a Deploy

```
1. IDEA: "Necesito pipeline de video ingestion"

2. TÚ (PO) decides construirlo → Sprint 11 priority

3. Agente product-owner:
   Input: "Crea historias para video ingestion pipeline"
   Output: US-101, US-102, US-103 con AC completos

4. TÚ priorizas: US-101 (5 pts), US-102 (8 pts), US-103 (5 pts)

5. TÚ verificas Definition of Ready (100%) ✅

6. TÚ (PM) decides sprint de 10 días

7. Agente project-manager:
   Input: "Plan Sprint 11 con US-101-103, capacity formula v2.0"
   Output: Timeline detallado, 21 pts, 20% buffer

8. TÚ creas rama: git checkout -b feature/epic-1-video-ingestion

9. Agente software-architect:
   Input: "Diseña integración de YoutubeExplode para US-101"
   Output: Diagrama y spec técnica

10. TÚ apruebas diseño ✅

11. Agente dotnet-backend-developer:
    Input: "Implementa US-101 según SPRINT_11_PLAN.md"
    Output: VideoService.SubmitVideoFromUrlAsync() implementado

12. TÚ ejecutas testing manual ✅

13. Agente test-engineer:
    Input: "Crea unit tests para US-101, coverage >80%"
    Output: VideoServiceTests.cs con 15 tests

14. Agente code-reviewer:
    Input: "Revisa código de US-101"
    Output: Feedback (2 Critical, 3 High, 5 Medium)

15. TÚ aplicas feedback Critical y High ✅

16. TÚ verificas DoD completo ✅

17. TÚ mergeas a master

18. Repites 11-17 para US-102 y US-103

19. TÚ ejecutas regresión del sprint ✅

20. TÚ (PO) validas el sprint contra AC ✅

21. TÚ (Stakeholder) das sign-off ✅

22. TÚ actualizas Technical Debt Register (0 new items) ✅

23. DEPLOY 🚀
```

---

## ✅ Success Criteria

Este documento es exitoso si:

1. ✅ **Claridad de roles**: Sabes exactamente cuándo TÚ decides vs. agente ejecuta
2. ✅ **Eficiencia**: Usas agentes para tareas repetitivas, liberas tiempo para decisiones
3. ✅ **Calidad**: Code reviews sistemáticos con agentes mejoran calidad
4. ✅ **Velocidad**: Paralelismo con agentes acelera desarrollo
5. ✅ **Control**: TÚ mantienes control total de decisiones críticas

---

**Aprobado por:** Technical Lead (Todos los roles)
**Fecha efectiva:** 2025-10-20
**Próxima revisión:** End of Sprint 11
**Estado:** ACTIVO
**Versión:** 1.0 (compatible con v2.0 methodology)
