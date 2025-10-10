# Metodología de Roles - YouTube RAG .NET

**Versión:** 1.0
**Fecha:** 2025-10-07
**Estado:** ACTIVO
**Contexto:** Trabajo individual (desarrollador asume todos los roles)

---

## Resumen Ejecutivo

En este proyecto, **una sola persona asume todos los roles de decisión**: Technical Lead, Project Manager, Product Owner y Business Stakeholder. Los **agentes especializados de Claude Code** se utilizan para ejecutar tareas específicas bajo la dirección del desarrollador.

---

## Estructura de Roles

### Roles Humanos (Asumidos por el Desarrollador)

```
TÚ (Desarrollador)
├── Technical Lead
├── Project Manager
├── Product Owner
└── Business Stakeholder
```

### Agentes de Claude Code (Herramientas de Ejecución)

```
Specialized Agents
├── dotnet-backend-developer
├── test-engineer
├── database-expert
├── devops-engineer
├── code-reviewer
├── software-architect
├── product-owner (para asistencia)
└── project-manager (para asistencia)
```

---

## Intervención de Roles en el Workflow

### 1. **Product Owner** (Tú decides, agente asiste)

**Responsabilidades:**
- Definir historias de usuario
- Priorizar backlog
- Escribir criterios de aceptación
- Validar historias completadas
- Aceptar/rechazar deliverables

**Proceso:**
1. **TÚ decides** qué historias crear y su prioridad
2. **Opcionalmente** usas el agente `product-owner` para:
   - Redactar historias en formato correcto
   - Organizar backlog
   - Estimar story points
   - Crear sprint planning

**Cuándo usar el agente:**
```bash
# Cuando necesitas ayuda para estructurar historias
"Ayúdame a crear historias de usuario para [feature] basado en [contexto]"

# Cuando necesitas priorizar múltiples historias
"Prioriza estas historias usando RICE scoring: [lista]"
```

---

### 2. **Project Manager** (Tú decides, agente asiste)

**Responsabilidades:**
- Planificar sprints
- Gestionar timeline y recursos
- Identificar y mitigar riesgos
- Coordinar tareas
- Reportar progreso

**Proceso:**
1. **TÚ decides** el alcance del sprint y deadlines
2. **Opcionalmente** usas el agente `project-manager` para:
   - Crear planes de proyecto detallados
   - Analizar riesgos
   - Estimar recursos
   - Generar reportes

**Cuándo usar el agente:**
```bash
# Cuando necesitas un plan de proyecto estructurado
"Crea un plan de proyecto para implementar [feature] en [timeframe]"

# Cuando necesitas análisis de riesgos
"Analiza los riesgos de [cambio] y propón mitigaciones"
```

---

### 3. **Technical Lead** (Tú decides y ejecutas)

**Responsabilidades:**
- Tomar decisiones técnicas
- Revisar arquitectura
- Asignar tareas a agentes
- Code review crítico
- Resolver problemas técnicos

**Proceso:**
1. **TÚ tomas** todas las decisiones técnicas
2. **TÚ usas** agentes especializados para ejecutar:
   - `software-architect`: Diseño de arquitectura
   - `dotnet-backend-developer`: Implementación
   - `database-expert`: Diseño de base de datos
   - `code-reviewer`: Revisión de código

**Cuándo usar agentes:**
```bash
# Diseño de arquitectura
"Diseña la arquitectura para [feature] siguiendo Clean Architecture"

# Implementación de código
"Implementa [funcionalidad] en [ubicación] siguiendo [especificación]"

# Revisión de código
"Revisa el código de [archivo] para calidad y security"
```

---

### 4. **Business Stakeholder** (Tú decides)

**Responsabilidades:**
- Aprobar presupuestos
- Decisiones GO/NO-GO
- Validar valor de negocio
- Definir success criteria

**Proceso:**
1. **TÚ tomas** todas las decisiones de negocio
2. No se utilizan agentes (decisiones puramente humanas)

---

## Flujo de Trabajo Completo

### **Fase 1: Definición de Historias**

```
1. TÚ (Product Owner) defines qué quieres construir

2. OPCIONALMENTE usas agente product-owner:
   - Input: "Necesito crear historias para [feature]"
   - Output: Historias estructuradas con AC

3. TÚ revisas y apruebas las historias
```

### **Fase 2: Planificación de Sprint**

```
1. TÚ (PM) decides cuántas historias entran en el sprint

2. OPCIONALMENTE usas agente project-manager:
   - Input: "Planifica sprint 2 con estas historias: [lista]"
   - Output: Sprint plan con timeline

3. TÚ ajustas el plan según tu disponibilidad
```

### **Fase 3: Creación de Rama Git**

```
1. TÚ creas rama siguiendo WORKFLOW_METHODOLOGY.md:
   git checkout develop
   git pull origin develop
   git checkout -b YRUS-XXXX_descripcion
```

### **Fase 4: Implementación**

```
1. TÚ (Technical Lead) asignas tareas a agentes:

   Para arquitectura:
   - Agente: software-architect
   - Tarea: "Diseña [componente]"

   Para desarrollo:
   - Agente: dotnet-backend-developer
   - Tarea: "Implementa [feature] según [spec]"

   Para base de datos:
   - Agente: database-expert
   - Tarea: "Diseña schema para [entidad]"

2. TÚ supervisas y ajustas el trabajo de los agentes
```

### **Fase 5: Testing y DoD**

```
1. TÚ ejecutas testing manual (TESTING_METHODOLOGY_RULES.md)

2. Agente test-engineer:
   - Tarea: "Crea tests para [feature]"
   - Output: Suite de tests automatizados

3. TÚ verificas que se cumple el DoD
```

### **Fase 6: Code Review**

```
1. Agente code-reviewer:
   - Tarea: "Revisa código de [historia]"
   - Output: Feedback de calidad, security, performance

2. TÚ decides qué feedback aplicar
```

### **Fase 7: Merge a Develop**

```
1. TÚ verificas DoD completo

2. TÚ ejecutas merge:
   git checkout develop
   git merge --no-ff YRUS-XXXX_descripcion
   git push origin develop
```

### **Fase 8: Validación de Sprint**

```
1. TÚ ejecutas regresión automática

2. TÚ ejecutas testing manual completo

3. TÚ (Product Owner) validas las historias

4. TÚ (Business Stakeholder) das sign-off

5. TÚ (PM) creas sprint report
```

---

## Matriz de Decisiones

| Decisión | Quién Decide | Agente que Asiste | Obligatorio/Opcional |
|----------|--------------|-------------------|----------------------|
| **Qué construir** | TÚ (PO) | product-owner | Opcional |
| **Prioridad de historias** | TÚ (PO) | product-owner | Opcional |
| **Cuándo construir** | TÚ (PM) | project-manager | Opcional |
| **Alcance del sprint** | TÚ (PM) | project-manager | Opcional |
| **Cómo construir (arquitectura)** | TÚ (TL) | software-architect | Recomendado |
| **Implementación de código** | TÚ (TL) | dotnet-backend-developer | Obligatorio |
| **Diseño de base de datos** | TÚ (TL) | database-expert | Recomendado |
| **Testing automatizado** | TÚ (TL) | test-engineer | Recomendado |
| **Testing manual** | TÚ (TL) | Ninguno | TÚ ejecutas |
| **Code review** | TÚ (TL) | code-reviewer | Recomendado |
| **Aprobar sprint** | TÚ (PO + Stakeholder) | Ninguno | TÚ decides |

---

## Uso de Agentes: Cuándo y Cómo

### ✅ **Cuándo SÍ usar agentes:**

1. **Tareas de ejecución repetitivas**
   - Implementar código según spec
   - Crear tests
   - Generar documentación

2. **Tareas que requieren expertise específico**
   - Diseño de arquitectura compleja
   - Optimización de queries SQL
   - Security review

3. **Tareas de análisis estructurado**
   - Priorización de historias (RICE)
   - Planificación de sprints
   - Análisis de riesgos

### ❌ **Cuándo NO usar agentes:**

1. **Decisiones de negocio**
   - Prioridades estratégicas
   - Aprobaciones de presupuesto
   - Definición de valor

2. **Decisiones creativas**
   - Naming de features
   - UX/UI decisions
   - Branding

3. **Testing manual exploratorio**
   - Pruebas de usabilidad
   - Validación end-to-end
   - Aceptación de usuario

---

## Comandos de Agentes por Fase

### **Creación de Historias de Usuario**

```markdown
Prompt para product-owner:

"Basándome en el estado actual de la rama develop, crea historias de usuario
para el Sprint 2. El alcance es [descripción]. Las historias deben:
- Seguir formato: As a [user], I want [goal], so that [benefit]
- Incluir criterios de aceptación detallados
- Estar priorizadas usando MoSCoW
- Incluir estimación en story points
- Estar organizadas en epics lógicos"
```

### **Planificación de Sprint**

```markdown
Prompt para project-manager:

"Crea un plan de sprint para implementar estas historias: [lista].
El sprint debe:
- Durar [N] días
- Incluir estimaciones realistas
- Identificar dependencias
- Listar riesgos y mitigaciones
- Proporcionar un timeline día a día"
```

### **Implementación de Código**

```markdown
Prompt para dotnet-backend-developer:

"Implementa la user story US-XXX: [título].

Acceptance Criteria:
[lista de AC]

Especificaciones técnicas:
- Ubicación: [carpeta/archivo]
- Seguir Clean Architecture
- Implementar [patrón específico]
- Tests requeridos: [tipos]

Definition of Done:
[checklist de DoD]"
```

### **Revisión de Código**

```markdown
Prompt para code-reviewer:

"Revisa el código implementado para US-XXX en [archivos].

Enfócate en:
- Cumplimiento de Clean Architecture
- SOLID principles
- Security vulnerabilities
- Performance issues
- Test coverage
- Code smells

Proporciona feedback accionable."
```

---

## Checklist por Fase

### **Al Crear Historias de Usuario**

- [ ] TÚ defines el objetivo del sprint
- [ ] TÚ describes el alcance general
- [ ] Agente product-owner crea historias estructuradas
- [ ] TÚ revisas y ajustas prioridades
- [ ] TÚ apruebas el backlog final

### **Al Planificar Sprint**

- [ ] TÚ decides cuántas historias incluir
- [ ] TÚ defines deadlines
- [ ] Agente project-manager crea plan detallado
- [ ] TÚ ajustas según tu disponibilidad
- [ ] TÚ apruebas el sprint plan

### **Al Implementar Historia**

- [ ] TÚ creas rama git
- [ ] TÚ defines especificaciones técnicas
- [ ] Agente implementa según spec
- [ ] TÚ supervisas progreso
- [ ] TÚ ejecutas testing manual
- [ ] Agente test-engineer crea tests automatizados
- [ ] Agente code-reviewer revisa calidad
- [ ] TÚ verificas DoD completo
- [ ] TÚ mergeas a develop

### **Al Cerrar Sprint**

- [ ] TÚ ejecutas regresión automática
- [ ] TÚ ejecutas testing manual completo
- [ ] TÚ (PO) validas acceptance criteria
- [ ] TÚ (Stakeholder) das sign-off
- [ ] TÚ (PM) creas sprint report
- [ ] TÚ (TL) identificas technical debt

---

## Integración con Otras Metodologías

Este documento se integra con:

- **WORKFLOW_METHODOLOGY.md**: Workflow de git y branches
- **TESTING_METHODOLOGY_RULES.md**: Reglas de testing
- **DEVELOPMENT_PROCESS_FRAMEWORK.md**: Proceso completo de 6 fases

---

## Ejemplo Completo: De Idea a Deploy

```
1. IDEA: "Necesito búsqueda semántica de videos"

2. TÚ (PO) decides construirlo

3. Agente product-owner:
   Input: "Crea historias para búsqueda semántica"
   Output: 3 historias con AC

4. TÚ priorizas: US-301, US-302, US-303

5. TÚ (PM) decides sprint de 5 días

6. Agente project-manager:
   Input: "Plan para implementar US-301-303 en 5 días"
   Output: Timeline detallado

7. TÚ creas rama: git checkout -b YRUS-0003_busqueda_semantica

8. Agente software-architect:
   Input: "Diseña arquitectura para búsqueda semántica"
   Output: Diagrama y spec técnica

9. TÚ apruebas diseño

10. Agente dotnet-backend-developer:
    Input: "Implementa US-301 según spec"
    Output: Código implementado

11. TÚ ejecutas testing manual ✅

12. Agente test-engineer:
    Input: "Crea tests para US-301"
    Output: Suite de tests

13. Agente code-reviewer:
    Input: "Revisa código de US-301"
    Output: Feedback

14. TÚ aplicas feedback

15. TÚ verificas DoD ✅

16. TÚ mergeas a develop

17. Repites 10-16 para US-302 y US-303

18. TÚ ejecutas regresión del sprint ✅

19. TÚ (PO) validas el sprint ✅

20. TÚ (Stakeholder) das sign-off ✅

21. DEPLOY
```

---

**Aprobado por:** Desarrollador (Todos los roles)
**Fecha efectiva:** 2025-10-07
**Próxima revisión:** Fin de Sprint 2
**Estado:** ACTIVO

---

**FIN DE METODOLOGÍA DE ROLES**
