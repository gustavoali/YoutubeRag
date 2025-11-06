# Agent Usage Guidelines - Directivas Prioritarias

**Version:** 2.0 - Integrated with Claude Code Best Practices
**Last Updated:** January 2025
**Priority:** 🔴 CRITICAL - Always Apply

---

## 🎯 Primary Directive

> **ALWAYS delegate tasks to specialized agents when possible. Parallel work is PRIORITIZED over sequential work.**

### Golden Rule
```
IF a specialized agent exists for the task
  THEN delegate to that agent (even in parallel)
  ELSE do the task directly

ADDITIONAL: Use /clear between major task switches
            Use subagents to preserve main context
            Specify concrete targets for agent tasks
```

### Integration with Claude Code Methodology

This guide integrates with:
- **[METHODOLOGY.md](METHODOLOGY.md)** - Overall development workflow (Explore → Plan → Code → Commit)
- **[CONTEXT_MANAGEMENT.md](CONTEXT_MANAGEMENT.md)** - Token optimization strategies
- **[CLAUDE.md](../CLAUDE.md)** - Project-specific context (auto-loaded)

**Read these files for complete understanding of the development ecosystem.**

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

## 🎓 Claude Code Best Practices for Agent Delegation

### 1. Provide Concrete Targets

```diff
❌ BAD: "Have the agent review VideoService"

✅ GOOD: "Delegate to dotnet-backend-developer:
         - Review VideoService.cs for SOLID violations
         - Check async/await patterns
         - Verify error handling covers:
           * HttpRequestException
           * UnauthorizedAccessException
           * Custom ValidationException
         - Report findings in structured format"
```

**Why:** Specificity drives results. Vague delegation produces vague output.

### 2. Use Subagents for Context Preservation

```markdown
Scenario: Working on Epic 2, need information from Epic 1

❌ BAD Approach:
- Load Epic 1 files into main context
- Main context now polluted with Epic 1
- Return to Epic 2 with degraded context

✅ GOOD Approach:
- Keep main context on Epic 2
- Spawn subagent to investigate Epic 1
- Subagent reports findings
- Main context remains clean

Implementation:
"Delegating Epic 1 investigation to backend-developer subagent
 to preserve main context on Epic 2"
```

### 3. Enable Extended Thinking for Complex Tasks

```markdown
When delegating complex architectural decisions:

✅ GOOD: "Delegate to software-architect with extended thinking:
         - Use 'think harder' mode
         - Analyze Epic 3 architecture
         - Consider scalability implications
         - Evaluate 3 alternative approaches
         - Provide recommendation with trade-offs"

This enables deeper analysis and better outcomes.
```

### 4. Request Explicit Verification Steps

```markdown
When delegating implementation:

✅ GOOD: "Delegate to dotnet-backend-developer:
         - Implement TranscriptionJobProcessor
         - Verify by:
           1. Running dotnet build (must succeed)
           2. Running affected tests (must pass)
           3. Checking for compiler warnings (must be zero)
           4. Validating Clean Architecture layers
         - Report completion with test results"
```

### 5. Use TDD Delegation Pattern

```markdown
For new features:

Step 1: Delegate test creation
"Delegate to test-engineer:
 - Write tests for VideoService.ProcessVideoAsync
 - Cover success case, invalid URL, disk space error
 - Use real database via TestContainers (NO MOCKS)
 - Verify tests FAIL without implementation"

Step 2: Delegate implementation
"Delegate to dotnet-backend-developer:
 - Implement VideoService.ProcessVideoAsync
 - Make all tests pass
 - Use async/await correctly
 - Follow Clean Architecture"

Step 3: Independent verification
"Delegate to separate code-reviewer subagent:
 - Review implementation WITHOUT seeing tests
 - Check for overfitting
 - Identify missing edge cases"
```

### 6. Parallel Multi-Agent Pattern

```markdown
For Epic implementation:

✅ Optimal: Launch 3 agents in parallel

Agent 1 (dotnet-backend-developer):
  Task: Implement service layer
  Duration: 3 hours

Agent 2 (test-engineer):
  Task: Write comprehensive test suite
  Duration: 2 hours

Agent 3 (database-expert):
  Task: Optimize database schema and queries
  Duration: 2 hours

Total Sequential Time: 7 hours
Total Parallel Time: 3 hours
Savings: 57%

Coordination: Main agent monitors and integrates results
```

---

## 🔄 Context Management for Agents

### Before Delegating

```bash
# Clean context if needed
"Before delegating, I'll clear my context to ensure
 optimal performance for both main and subagent."

/clear

# Then delegate
"Delegating Epic 3 validation to dotnet-backend-developer..."
```

### After Agent Completes

```markdown
Scenario: Subagent completed investigation, reported findings

Main Agent Actions:
1. Review subagent report
2. Extract essential information only
3. Don't load all files subagent used
4. Keep main context lean

Example:
Subagent: "I analyzed 15 files in Epic 1. Key finding:
          Caching uses Redis with 5-minute TTL."

Main Agent: "Thank you. I now know Epic 1 uses Redis caching.
            I won't load those 15 files into my context.
            Continuing with Epic 2..."
```

---

## 📊 Agent Delegation Metrics (Enhanced)

### Excellence Indicators

```markdown
✅ Agent delegation rate: 60-80% of tasks
✅ Parallel agents running: 2-3 frequently
✅ Context management: /clear used 3-5 times per session
✅ Specificity score: Concrete targets in 90%+ delegations
✅ Time reduction: 40-60% via parallelism
✅ Independent verification: Used for critical features
✅ TDD compliance: 100% for new features
```

### Warning Indicators

```markdown
⚠️ Agent delegation rate: 30-60% of tasks
⚠️ Parallel agents: Only occasionally
⚠️ Context management: /clear rarely used
⚠️ Specificity: Vague delegation ("review this")
⚠️ Time reduction: <30%
⚠️ Verification: Sporadic
⚠️ TDD: Sometimes skipped
```

### Critical Issues

```markdown
❌ Agent delegation rate: <30%
❌ Parallel agents: Never
❌ Context management: Never using /clear
❌ Specificity: Always vague
❌ Sequential work: Everything done one at a time
❌ No verification: Trust first implementation
❌ No TDD: Tests written after (or not at all)
```

---

## 🛠️ Troubleshooting Agent Delegation

### Issue 1: Agent Doesn't Understand Task

```markdown
Problem: Delegated task, agent asks clarifying questions

Root Cause: Insufficient specificity

Fix:
❌ "Review the code"
✅ "Review VideoService.cs focusing on:
   - Method: ProcessVideoAsync (lines 45-120)
   - Check: Async/await usage
   - Verify: Error handling for HttpRequestException
   - Report: Specific issues with line numbers"
```

### Issue 2: Agent Context Polluted Main Context

```markdown
Problem: After subagent completes, main context degraded

Root Cause: Not using proper subagent pattern

Fix:
- Use explicit subagent spawn
- Don't load subagent's files into main context
- Extract summary only from subagent report
- Keep main context focused
```

### Issue 3: Parallel Agents Conflicting

```markdown
Problem: Multiple agents editing same files

Root Cause: Poor task decomposition

Fix:
- Ensure tasks are truly independent
- Different files for different agents
- Or serialize dependent tasks
- Use git branches for isolation if needed
```

---

## 📝 Updated Delegation Template

```markdown
Delegating [TASK] to [AGENT] (using Claude Code best practices):

**Objective:**
[Clear, specific objective with measurable outcome]

**Context:**
[Essential context only - link to files, don't paste entire files]

**Specific Tasks:**
1. [Concrete task 1 with verification]
   - Verify: [How to verify completion]
2. [Concrete task 2 with verification]
   - Verify: [How to verify completion]

**Constraints:**
- [ ] Use TDD (tests first)
- [ ] No mocks (use TestContainers)
- [ ] Follow Clean Architecture
- [ ] Async/await required
- [ ] [Project-specific constraint]

**Expected Output:**
- [Deliverable 1] - [Format]
- [Deliverable 2] - [Format]
- Verification results (test output, build output)

**Acceptance Criteria:**
- [ ] All tests passing
- [ ] No compiler warnings
- [ ] Code coverage >90%
- [ ] [Specific criterion]

**Estimated Time:** [X hours]

**Thinking Mode:** [normal/think harder/ultrathink]

**Context Management:**
- Main agent will: [Continue with X / Clear context / Wait]
- Subagent should: [Report findings / Commit changes / Both]

**Verification:**
Independent verification by: [code-reviewer / test-engineer / none]
```

---

## 🎯 Advanced Patterns

### Pattern 1: Fan-Out / Fan-In

```markdown
Use Case: Large refactoring across multiple files

Fan-Out (Parallel):
├─ Agent 1: Refactor Services layer (files 1-10)
├─ Agent 2: Refactor Controllers layer (files 11-20)
└─ Agent 3: Refactor Repositories layer (files 21-30)

Fan-In (Merge):
Main Agent:
- Collect results from all 3 agents
- Verify integration
- Run full test suite
- Commit all changes

Time: 3x faster than sequential
```

### Pattern 2: Pipeline

```markdown
Use Case: Complex feature with dependencies

Stage 1: software-architect
  → Design architecture
  → Output: Design document

Stage 2: dotnet-backend-developer (consumes Stage 1 output)
  → Implement based on design
  → Output: Implementation

Stage 3: test-engineer (consumes Stage 2 output)
  → Test implementation
  → Output: Test results

Stage 4: code-reviewer (consumes all outputs)
  → Final review
  → Output: Approval or changes needed

Sequential but optimized with handoffs
```

### Pattern 3: Independent Verification

```markdown
Use Case: Ensure quality of critical feature

Primary Agent: dotnet-backend-developer
  → Implements feature with tests
  → All tests pass ✅

Verification Agent: Independent code-reviewer
  → Reviews WITHOUT seeing tests
  → Checks for overfitting
  → Identifies edge cases
  → Reports issues

Result: Higher quality, caught 3 edge cases
```

---

## 📚 Related Documentation

- **[METHODOLOGY.md](METHODOLOGY.md)** - Complete development workflow
- **[CONTEXT_MANAGEMENT.md](CONTEXT_MANAGEMENT.md)** - Token optimization
- **[CLAUDE.md](../CLAUDE.md)** - Project memory (auto-loaded)
- **[README.md](../README.md)** - Project overview

---

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0 | January 2025 | Integrated Claude Code best practices, added context management, enhanced patterns |
| 1.0 | October 2024 | Initial agent delegation methodology |

---

**APPLY THESE DIRECTIVES AT ALL TIMES**

**Priority:** 🔴 CRITICAL
**Scope:** Entire project
**Review:** Each sprint
**Integration:** Claude Code methodology + Agent specialization

---

**Quick Checklist Before Any Task:**
- [ ] Should I delegate this? (If >30 min or parallelizable → YES)
- [ ] Which agent is best suited?
- [ ] Have I provided concrete targets?
- [ ] Should I use /clear first?
- [ ] Can this run in parallel with other work?
- [ ] Do I need independent verification?

**Remember:** Delegation + Specificity + Parallelism = Maximum Velocity & Quality
