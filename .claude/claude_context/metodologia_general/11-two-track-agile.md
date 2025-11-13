# Two-Track Agile - Discovery + Delivery Paralelos

**Versión:** 2.0
**Fecha:** 2025-10-16
**Estado:** OBLIGATORIO

---

## 🎯 Problema que Resuelve

**Problema tradicional:**
```
Sprint N:
  Week 1: Trabajar en historias
  Week 2: Trabajar en historias
  End: Sprint review
  Gap: 2-3 días sin trabajo mientras planeas Sprint N+1 ❌
```

**Solución Two-Track:**
```
Sprint N:
  Track 2 (Delivery): Ejecutando Epic 5
  Track 1 (Discovery): Preparando Epic 6

Sprint N+1 comienza inmediatamente con Epic 6 ya listo ✅
```

---

## 📊 Los Dos Tracks

### Track 1: Discovery (Adelantado N+1 sprints)

**Fases incluidas:**
- ✅ Fase 1: Diagnóstico Técnico
- ✅ Fase 2: Planificación (Project Manager)
- ✅ Fase 3: Product Backlog (Product Owner)
- ✅ Fase 4: Validación de Negocio (Business Stakeholder)

**Output:** Epic completamente preparado con:
- Diagnostic report
- Project plan
- User stories con AC
- Business approval GO

### Track 2: Delivery (Sprint actual)

**Fases incluidas:**
- ✅ Fase 5: Ejecución con Agentes
- ✅ Fase 6: Validación y Cierre

**Output:** Software funcionando en producción

---

## 🔄 Workflow Paralelo

### Ejemplo Concreto:

```
┌─────────────────────────────────────────────────────────┐
│ SPRINT 10 (2 semanas)                                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ Track 2 - DELIVERY:                                     │
│   Epic 5: Search Optimization                           │
│   ├─ US-501: Query optimization (5pts)                  │
│   ├─ US-502: Caching strategy (5pts)                    │
│   └─ US-503: Index optimization (3pts)                  │
│   Status: 🟢 IN PROGRESS                                │
│                                                         │
│ Track 1 - DISCOVERY (paralelo):                         │
│   Epic 6: Real-time Notifications                       │
│   Week 1: Diagnóstico + Planning (4-8h)                 │
│   Week 2: Backlog + Business Decision (3-6h)            │
│   Status: 🟡 PREPARING for Sprint 11                    │
│                                                         │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ SPRINT 11 (2 semanas)                                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ Track 2 - DELIVERY:                                     │
│   Epic 6: Real-time Notifications (YA PREPARADO ✅)     │
│   ├─ US-601: SignalR setup (3pts)                       │
│   ├─ US-602: Notification service (5pts)                │
│   └─ US-603: UI notifications (8pts)                    │
│   Status: 🟢 READY TO START immediately                 │
│                                                         │
│ Track 1 - DISCOVERY (paralelo):                         │
│   Epic 7: Video Analytics Dashboard                     │
│   Week 1: Diagnóstico + Planning                        │
│   Week 2: Backlog + Business Decision                   │
│   Status: 🟡 PREPARING for Sprint 12                    │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## ⏱️ Distribución de Tiempo

### Sprint de 10 días (80 horas efectivas)

#### Track 2 - Delivery: 70 horas (87.5%)
```
- Implementación: 45 horas (56%)
- Testing: 15 horas (19%)
- Code review: 5 horas (6%)
- Sprint ceremonies: 5 horas (6%)
```

#### Track 1 - Discovery: 10 horas (12.5%)
```
Week 1:
  - Diagnóstico técnico: 4-6 horas
  - Project planning: 2-3 horas

Week 2:
  - Product backlog: 2-3 horas
  - Business decision: 1-2 horas
```

**Total:** 80 horas = 100% capacity utilizada eficientemente

---

## 📋 Proceso Detallado

### Inicio del Sprint N

**Day 1 - Sprint Planning:**
```
09:00-10:00 | Sprint Planning para Track 2 (Delivery)
            | - Presentar Epic N (ya preparado en Sprint N-1)
            | - Confirmar historias y capacity
            | - Asignar tareas a agentes
            | - Sprint Goal definido

10:00-11:00 | Discovery Kickoff para Track 1
            | - Iniciar Diagnóstico de Epic N+1
            | - Asignar investigación inicial
            | - Identificar stakeholders a consultar
```

### Week 1 del Sprint N

**Track 2 (Delivery) - Tiempo completo:**
- Implementar historias de Epic N
- Testing continuo
- Code reviews

**Track 1 (Discovery) - 4-8 horas distribuidas:**
- Lunes/Martes: Technical Lead hace diagnóstico (puede delegar investigación a agentes)
- Miércoles: Project Manager crea plan
- Jueves: Review intermedio, ajustar si necesario

### Week 2 del Sprint N

**Track 2 (Delivery) - Tiempo completo:**
- Completar historias
- Regresión completa
- Sprint review
- Retrospectiva

**Track 1 (Discovery) - 3-6 horas distribuidas:**
- Lunes: Product Owner crea backlog
- Martes: Business Stakeholder revisa
- Miércoles: Decision GO/NO-GO
- Jueves: Epic N+1 READY para Sprint N+1 ✅

### Transición Sprint N → Sprint N+1

**Viernes (último día Sprint N):**
```
14:00-15:00 | Sprint N Review + Retrospectiva
15:00-16:00 | Sprint N+1 Planning (Epic N+1 ya listo)
            | - Sin delays
            | - Sin "tengo que investigar primero"
            | - Comenzar desarrollo el Lunes
```

**RESULTADO: 0 días de gap entre sprints**

---

## 🎯 Beneficios Cuantificables

### Antes (Single Track):

```
Sprint duration: 10 días trabajo + 2-3 días planning = 13 días
Sprints por año: 365 / 13 ≈ 28 sprints

Time lost: 2-3 días × 28 = 56-84 días/año sin entregar valor
```

### Después (Two-Track):

```
Sprint duration: 10 días trabajo + 0 días gap = 10 días
Sprints por año: 365 / 10 ≈ 36 sprints

Time gained: (36 - 28) × 10 = 80 días adicionales de delivery/año
```

**GANANCIA: ~25% más sprints completados por año**

---

## 📊 Métricas de Éxito Two-Track

### Track 1 (Discovery) Metrics:

| Métrica | Target | Medición |
|---------|--------|----------|
| **Epic Ready Rate** | 100% | Épics listos a tiempo para siguiente sprint |
| **Discovery Time** | <10h | Horas invertidas en discovery por epic |
| **Business Approval Time** | <2 días | Tiempo desde backlog hasta GO decision |
| **Discovery Quality** | >90% | % épics que no requieren re-planning |

### Track 2 (Delivery) Metrics:

| Métrica | Target | Medición |
|---------|--------|----------|
| **Sprint Goal Success** | 100% | % sprints que logran el goal |
| **Story Completion** | >90% | % historias completadas vs comprometidas |
| **Zero-Gap Transition** | 100% | % sprints que comienzan sin delay |

---

## 🚨 Reglas y Restricciones

### Rule #1: Discovery es Optional para Sprint +1 ONLY

**Correcto:**
```
Sprint 10: Preparando Sprint 11 (next) ✅
Sprint 10: NO preparando Sprint 12 (next+1) ❌
```

**Razón:** Evitar sobre-planificación. El roadmap puede cambiar.

### Rule #2: Discovery no Consume más del 15% del Sprint

**Límite estricto:**
```
Sprint de 80 horas:
- Track 2 (Delivery): Mínimo 68 horas (85%)
- Track 1 (Discovery): Máximo 12 horas (15%)
```

**Si discovery necesita más:** Es señal de que el epic es muy grande, dividir.

### Rule #3: Discovery puede Cancelarse si Urgencias

**Prioridad:**
```
P0 Bug en producción > Discovery

Si surge P0:
  1. Pausar discovery
  2. Todo el equipo a resolver P0
  3. Reanudar discovery después
```

### Rule #4: Epic Preparado DEBE ser Ejecutado

**No waste:**
```
Si Epic 6 está preparado (GO decision):
  - DEBE ejecutarse en Sprint N+1
  - NO puede saltarse por "nueva prioridad"
  - Excepción: Decisión de Business Stakeholder
```

**Razón:** Evitar waste de las 10 horas de discovery.

---

## 🔄 Casos Especiales

### Caso 1: Sprint N+1 No Necesita Discovery

**Ejemplo:** Continuación del mismo epic

**Solución:**
- Track 1 se usa para refinamiento del backlog actual
- O preparar sprint N+2 (excepción a Rule #1)
- O pagar technical debt
- O spikes de investigación

### Caso 2: Discovery Identifica que Epic no es Viable

**Ejemplo:** Business Decision = NO-GO

**Solución:**
```
Week 1: Epic 6 diagnosis → Resulta no viable
Week 2: Iniciar discovery de Epic 7 (backup)
Resultado: Sprint N+1 tiene Epic 7 preparado
```

**Por eso es importante tener backlog priorizado.**

### Caso 3: Múltiples Épics Pequeños en un Sprint

**Ejemplo:** Sprint 10 ejecuta Epic 5A + Epic 5B (ambos pequeños)

**Discovery Track 1:**
- Week 1: Epic 6 (principal para Sprint 11)
- Week 2: Epic 7 (secundario para Sprint 11)

**Resultado:** Sprint 11 tiene 2 épics preparados

---

## 📝 Templates y Checklists

### Discovery Checklist (Track 1)

#### Week 1 - Diagnóstico y Planning

- [ ] **Day 1-2: Technical Diagnosis**
  - [ ] Código existente revisado
  - [ ] Problemas identificados
  - [ ] Soluciones propuestas (mínimo 2)
  - [ ] Riesgos técnicos documentados
  - [ ] `DIAGNOSTIC_REPORT_[EPIC].md` creado

- [ ] **Day 3-4: Project Planning**
  - [ ] Timeline estimado
  - [ ] Recursos asignados
  - [ ] Riesgos identificados con mitigaciones
  - [ ] `PROJECT_PLAN_[EPIC].md` creado

#### Week 2 - Backlog y Business Decision

- [ ] **Day 5-6: Product Backlog**
  - [ ] User stories escritas
  - [ ] Acceptance criteria definidos
  - [ ] Story points estimados
  - [ ] Priorización aplicada
  - [ ] `PRODUCT_BACKLOG_[EPIC].md` creado

- [ ] **Day 7-8: Business Validation**
  - [ ] Presentación a Business Stakeholder
  - [ ] Decision: GO / NO-GO / CONDITIONAL
  - [ ] Budget aprobado
  - [ ] Success criteria definidos
  - [ ] `BUSINESS_STAKEHOLDER_DECISION_[EPIC].md` creado

- [ ] **Day 9: Epic Ready Verification**
  - [ ] Todos los docs completos
  - [ ] GO decision obtenida
  - [ ] Backlog importado a Sprint N+1
  - [ ] Equipo notificado

### Delivery Checklist (Track 2)

- [ ] Sprint comienza con Epic preparado
- [ ] Zero time wasted en "¿qué hacemos?"
- [ ] Historias claras con AC
- [ ] Team capacity confirmada
- [ ] Sprint goal definido

---

## 🎓 Capacitación Two-Track

### Para Technical Lead (YO):

**Nuevas responsabilidades:**
1. Balancear tiempo entre Track 1 y Track 2
2. Delegar discovery research a agentes
3. Mantener buffer de 1 epic siempre preparado

**Tiempo requerido en Track 1:** 6-8 horas/sprint

### Para Agentes:

**Nuevos patterns:**
1. Pueden recibir tareas de discovery durante delivery
2. Investigaciones pueden ser asíncronas
3. Output de discovery debe ser documentado

---

## 📈 Ejemplo Real - 3 Sprints Consecutivos

### Sprint 8 (Actual)

**Track 2 - Delivery:**
- Epic 4: Background Jobs (Hangfire)
- Status: 🟢 80% completado

**Track 1 - Discovery:**
- Epic 5: Search Optimization
- Week 1: ✅ Diagnosis done, Planning done
- Week 2: 🔄 Backlog in progress

### Sprint 9 (Siguiente)

**Track 2 - Delivery:**
- Epic 5: Search Optimization (PREPARADO ✅)
- Comienza inmediatamente, zero delay

**Track 1 - Discovery:**
- Epic 6: Real-time Notifications
- Comenzará en Week 1 de Sprint 9

### Sprint 10 (Futuro)

**Track 2 - Delivery:**
- Epic 6: Real-time Notifications (se preparará en Sprint 9)

**Track 1 - Discovery:**
- Epic 7: Analytics Dashboard (se preparará en Sprint 10)

---

## ✅ Criterios de Éxito

### Un sprint es exitoso en Two-Track si:

1. ✅ **Track 2:** Sprint goal alcanzado, historias completadas
2. ✅ **Track 1:** Epic N+1 preparado con GO decision
3. ✅ **Transición:** Sprint N+1 comienza sin delays
4. ✅ **Balance:** <15% tiempo en discovery, >85% en delivery
5. ✅ **Calidad:** Epic preparado no requiere re-planning

---

## 🚀 Implementación Gradual

### Fase 1: Piloto (1 sprint)

```
Sprint 10:
  - Track 2: Ejecutar normalmente
  - Track 1: EXPERIMENT con Epic 11
  - Objetivo: Validar que funciona
```

### Fase 2: Adopción (2-3 sprints)

```
Sprints 11-13:
  - Ambos tracks activos
  - Ajustar tiempos según aprendizajes
  - Refinar proceso
```

### Fase 3: Optimización (ongoing)

```
Sprint 14+:
  - Two-track como estándar
  - Métricas recolectadas
  - Mejora continua
```

---

## 📊 Dashboard Two-Track (Recomendado)

```
┌─────────────────────────────────────────────────────┐
│ SPRINT 10 - TWO-TRACK DASHBOARD                    │
├─────────────────────────────────────────────────────┤
│                                                     │
│ 🚀 TRACK 2: DELIVERY (Epic 5)                      │
│ ▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░ 75% complete                     │
│ Stories: 6/8 done                                   │
│ Days remaining: 3                                   │
│ Risk: 🟢 LOW (on track)                            │
│                                                     │
│ 🔍 TRACK 1: DISCOVERY (Epic 6)                     │
│ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ 100% Week 1 (Diagnosis + Plan)    │
│ ▓▓▓▓▓▓▓▓░░░░░░░░ 50% Week 2 (Backlog in progress)  │
│ Next: Business Decision (2 days)                    │
│ Risk: 🟢 LOW (on track for Sprint 11)              │
│                                                     │
│ ⏱️  TIME ALLOCATION THIS WEEK:                      │
│ Track 2: 34h (85%)                                  │
│ Track 1:  6h (15%)                                  │
│ Total:   40h (100% capacity)                        │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

**Estado:** OBLIGATORIO para Sprint 11+
**Fecha de Adopción:** Sprint 11 (próximo sprint)
**Revisión:** Después de 3 sprints
**Versión:** 2.0
