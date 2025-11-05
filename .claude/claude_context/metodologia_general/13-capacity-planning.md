# Capacity Planning - Cálculo Formal de Sprint Capacity

**Versión:** 2.0
**Fecha:** 2025-10-16
**Estado:** OBLIGATORIO

---

## 🎯 Objetivo

Eliminar el "over-commitment" que lleva a sprints fallidos mediante **cálculo formal de capacity**.

---

## 📐 Fórmula de Capacity

### Formula Base

```
Capacity (horas) = Team Days × Hours/Day × Efficiency × Availability
```

### Definiciones

#### Team Days
```
Team Days = Team Size × Sprint Duration (días laborables)

Ejemplo:
  Team Size: 1 developer
  Sprint: 10 días laborables (2 semanas, 5 días/semana)
  Team Days = 1 × 10 = 10 team-days
```

#### Hours/Day (Horas Efectivas)
```
Hours/Day = Work Hours - Overhead

Cálculo típico:
  Work Hours: 8h/día
  Overhead:
    - Emails, Slack: 0.5h
    - Meetings (stand-ups, reviews): 0.5h
    - Context switching: 0.5h
    - Breaks: 0.5h
  Total Overhead: 2h

Hours/Day efectivas = 8h - 2h = 6h ✅
```

**Regla:** Nunca usar más de 6h/día para planificación.

#### Efficiency (Factor de Eficiencia)
```
Efficiency = % tiempo productivo vs tiempo planificado

Factores que reducen efficiency:
  - Bugs de producción: -5%
  - Interrupciones: -5%
  - Context switching: -5%
  - Imprevistos: -5%

Efficiency típica: 0.80 (80%)
Efficiency conservadora: 0.70 (70%)
Efficiency optimista: 0.90 (90%) ⚠️ Raramente se logra
```

**Regla:** Usar 0.80 por default, ajustar después de 3 sprints.

#### Availability (Disponibilidad del Team)
```
Availability = % team disponible completo

Factores:
  - Vacaciones: -X%
  - Sick days estimados: -2%
  - Training/onboarding: -X%
  - On-call duties: -X%

Cálculo ejemplo:
  Team: 1 persona
  Sprint: 10 días
  Vacation: 0 días
  Sick (estimado): 0.2 días (2% del sprint)

Availability = (10 - 0.2) / 10 = 0.98 (98%)
```

---

## 🧮 Ejemplo Completo de Cálculo

### Escenario: Sprint Típico

**Inputs:**
```
Team Size: 1 developer
Sprint Duration: 10 días laborables
Hours/Day: 6h efectivas (8h - 2h overhead)
Efficiency: 0.80 (80% productivo)
Availability: 0.90 (10% para imprevistos/sick)
```

**Cálculo:**
```
Capacity = 1 × 10 × 6 × 0.80 × 0.90
Capacity = 43.2 horas efectivas de trabajo
```

**Conversión a Story Points:**
```
Suponiendo tu velocidad histórica:
  1 story point = 2 horas (promedio)

Capacity en story points = 43.2h / 2h = 21.6 ≈ 21 story points
```

**Buffer de Seguridad:**
```
Comprometerse al 80% de capacity:
  Sprint commitment = 21 × 0.80 = 16.8 ≈ 17 story points

Reservar 20% para:
  - Bugs urgentes
  - Technical debt crítico
  - Support requests
  - Imprevistos

Buffer = 21 - 17 = 4 story points
```

**Resultado Final:**
```
✅ Comprometer: 17 story points
✅ Buffer: 4 story points
✅ Total capacity: 21 story points
```

---

## 📊 Capacity Planning Worksheet

### Template por Sprint

```markdown
# Sprint N - Capacity Planning

**Sprint Duration:** 10 días (2025-10-20 to 2025-10-31)
**Team Size:** 1 developer

## 1. Base Capacity Calculation

### Team Days
- Team Size: 1
- Sprint Duration: 10 días laborables
- **Team Days:** 1 × 10 = 10

### Hours per Day (Effective)
- Work Hours: 8h
- Overhead:
  * Daily standup: 0.25h
  * Email/Slack: 0.5h
  * Sprint ceremonies: 0.5h (amortized)
  * Breaks/context switching: 0.75h
- **Hours/Day:** 8 - 2 = 6h

### Efficiency Factor
- Baseline: 0.85
- Adjustments:
  * Production bugs last sprint: -0.05
  * No major blockers expected: +0.00
- **Efficiency:** 0.80 (80%)

### Availability
- Total days: 10
- Planned vacation: 0 días
- Estimated sick: 0.2 días (2%)
- Training: 0 días
- **Availability:** (10 - 0.2) / 10 = 0.98 (98%)

## 2. Capacity Calculation

```
Capacity = Team Days × Hours/Day × Efficiency × Availability
Capacity = 10 × 6 × 0.80 × 0.98
Capacity = 47.04 horas
```

## 3. Two-Track Allocation

### Track 1 - Discovery (Next Epic)
- Time allocation: 12% of sprint
- **Discovery hours:** 47.04 × 0.12 = 5.6h
- Activities: Diagnosis + Planning + Backlog (Epic N+1)

### Track 2 - Delivery (Current Epic)
- Time allocation: 88% of sprint
- **Delivery hours:** 47.04 × 0.88 = 41.4h

## 4. Story Points Conversion

### Historical Velocity
- Last 3 sprints average: 2.1 hours/story point
- **Conversion factor:** 2 hours/point (rounded)

### Delivery Capacity
```
Delivery capacity = 41.4h / 2h per point = 20.7 story points
```

### Commitment with Buffer
```
Commitment (80%): 20.7 × 0.80 = 16.5 ≈ 17 story points
Buffer (20%):     20.7 × 0.20 = 4.1 ≈ 4 story points
```

## 5. Final Sprint Plan

### Committed Stories (17 points)
- US-501: Query optimization (5 pts)
- US-502: Caching strategy (5 pts)
- US-503: Index creation (3 pts)
- US-504: Performance testing (4 pts)
- **TOTAL COMMITTED:** 17 story points ✅

### Buffer Reserved (4 points)
- Technical debt items
- Bug fixes
- Urgent requests
- **BUFFER:** 4 story points

### Discovery Work (5.6 hours)
- Epic 6: Real-time Notifications
  * Diagnosis: 3h
  * Planning: 2h
  * Backlog: (next week)

## 6. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Production bug requires hotfix | Medium | -8h | Use buffer, de-scope US-504 |
| Discovery takes longer | Low | -2h | Defer backlog to Week 2 |
| US-501 more complex than estimated | Medium | -4h | Pair with database-expert agent |

## 7. Validation

- [ ] Total committed (17) < Capacity with buffer (21) ✅
- [ ] Discovery time (5.6h) < 15% threshold (7h) ✅
- [ ] Buffer (4 pts) ≥ 15% of capacity ✅
- [ ] Risks identified with mitigations ✅
- [ ] Team consensus on commitment ✅

## 8. Tracking

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Story points delivered | 17 | TBD | - |
| Actual hours spent | 41.4h | TBD | - |
| Buffer used | 4 pts | TBD | - |
| Discovery completed | 100% | TBD | - |

---

**Sign-off:**
- Technical Lead: ✅ [Date]
- Product Owner: ✅ [Date]
```

---

## 📈 Ajuste de Velocity Histórica

### Tracking Velocity

Mantener log de últimos sprints:

```markdown
# Velocity History

| Sprint | Committed | Delivered | Actual Hours | Hours/Point | Notes |
|--------|-----------|-----------|--------------|-------------|-------|
| 7      | 20 pts    | 18 pts    | 42h          | 2.3h        | 2 stories carried over |
| 8      | 18 pts    | 18 pts    | 39h          | 2.2h        | Clean sprint ✅ |
| 9      | 19 pts    | 17 pts    | 38h          | 2.2h        | 1 story blocked |
| 10     | 17 pts    | 17 pts    | 41h          | 2.4h        | Complex stories |
| **Average** | **18.5** | **17.5** | **40h** | **2.3h/pt** | **Last 4 sprints** |

**Recommendation for Sprint 11:**
- Use 2.5h/point (conservative, accounts for complexity trend)
- Capacity: 41.4h / 2.5h = 16.5 ≈ 17 story points ✅
```

### Calculating Hours per Point

```
Hours/Point = Total Actual Hours / Total Delivered Points

Example:
  Sprint 10: 41h actual / 17 pts delivered = 2.4h/point
  Sprint 9:  38h actual / 17 pts delivered = 2.2h/point
  Sprint 8:  39h actual / 18 pts delivered = 2.2h/point

  Average: (2.4 + 2.2 + 2.2) / 3 = 2.3h/point

  Use for next sprint: 2.3h/point (or round to 2.5 for safety)
```

---

## 🚨 Red Flags de Over-Commitment

### Warning Signs

```
🔴 CRITICAL: Commitment > 100% capacity
   Example: Planning 25 story points when capacity is 21
   Action: Immediately de-scope

🟡 WARNING: Buffer < 15%
   Example: Committing 20 pts of 21 capacity (only 1 pt buffer)
   Action: Reduce commitment to create buffer

🟡 WARNING: Efficiency < 0.70
   Example: Team only 60% productive last sprint
   Action: Investigate blockers, reduce commitment

🟡 WARNING: Velocity dropping trend
   Example: 20 → 18 → 16 delivered over last 3 sprints
   Action: Diagnose root cause (complexity, tech debt, burnout)
```

### Emergency De-Scoping

Si durante sprint planning te das cuenta de over-commitment:

**Step 1: Identify non-critical stories**
```
Stories by priority:
  1. US-501: Query optimization (5 pts) - MUST HAVE
  2. US-502: Caching (5 pts) - MUST HAVE
  3. US-503: Indexes (3 pts) - SHOULD HAVE
  4. US-504: Perf testing (4 pts) - COULD HAVE ← De-scope this

De-scope US-504 → Frees 4 points
```

**Step 2: Move to backlog**
```
US-504 moved to:
  - Sprint N+1 backlog
  - With higher priority
  - Documented reason: Capacity constraint
```

---

## 📊 Capacity Planning Dashboard

```
┌─────────────────────────────────────────────────────────┐
│ SPRINT 10 - CAPACITY PLANNING                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ 📊 CAPACITY CALCULATION                                │
│ Team Days:      10 days                                │
│ Hours/Day:      6h effective                           │
│ Efficiency:     80% (0.80)                             │
│ Availability:   98% (0.98)                             │
│ ────────────────────────────────────                   │
│ TOTAL CAPACITY: 47.0 hours                             │
│                                                         │
│ 📈 TWO-TRACK ALLOCATION                                │
│ Track 1 (Discovery):  5.6h (12%) │████░░░░░░░░░░░░░░   │
│ Track 2 (Delivery):  41.4h (88%) │███████████████████  │
│                                                         │
│ 🎯 DELIVERY BREAKDOWN                                  │
│ Capacity:        41.4h = 20.7 story points             │
│ Commitment:      16.8h = 17 story points (80%)         │
│ Buffer:           4.1h =  4 story points (20%)         │
│                                                         │
│ ✅ STORIES COMMITTED (17 points)                       │
│ US-501: Query optimization     [█████] 5 pts           │
│ US-502: Caching strategy       [█████] 5 pts           │
│ US-503: Index creation         [███  ] 3 pts           │
│ US-504: Performance testing    [████ ] 4 pts           │
│                                                         │
│ 🛡️  BUFFER (4 points)                                  │
│ Reserved for: Bugs, Tech Debt, Urgent requests         │
│                                                         │
│ ⚠️  RISK LEVEL: 🟢 LOW                                 │
│ Commitment vs Capacity: 17/21 (81%) ✅                 │
│ Buffer adequate: 19% ✅                                │
│ Discovery time: 5.6h < 7h threshold ✅                 │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 🎯 Capacity Types

### 1. Planned Capacity

```
Planned = Formula calculation (as above)

Used for:
  - Sprint planning
  - Commitment decisions
```

### 2. Available Capacity (During Sprint)

```
Available = Planned - Hours Already Spent

Example mid-sprint:
  Planned: 47h
  Spent: 25h (after 5 days)
  Available: 47h - 25h = 22h remaining

Compare to stories remaining:
  If remaining stories = 30h worth → AT RISK ⚠️
  If remaining stories = 20h worth → ON TRACK ✅
```

### 3. Actual Capacity (Post-Sprint)

```
Actual = Sum of all hours logged

Used for:
  - Velocity adjustment
  - Retrospective analysis
  - Future planning
```

---

## 🔄 Capacity Adjustments During Sprint

### When to Adjust

**Adjust capacity if:**
- Team member unavailable (sick)
- Production emergency consumes significant time
- Major blocker discovered

**Process:**

1. **Recalculate remaining capacity**
```
Example:
  Original: 47h capacity
  Day 3: Developer sick 1 day = -6h
  Remaining capacity: 47h - 6h = 41h
```

2. **Compare to remaining work**
```
Stories remaining: 18h (3 stories)
Remaining capacity: 41h
Status: ✅ Still on track
```

3. **De-scope if needed**
```
If remaining work > remaining capacity:
  - Identify lowest priority story
  - Move to next sprint
  - Notify stakeholders
```

---

## ✅ Success Criteria

Capacity Planning es exitoso si:

1. ✅ **Predictability:** Delivered within ±10% of committed
2. ✅ **Buffer Used:** 50-100% of buffer consumed (healthy)
3. ✅ **No Overtime:** Team works sustainable pace
4. ✅ **Velocity Stable:** ±15% variation sprint-to-sprint
5. ✅ **Team Confidence:** High confidence in commitments

---

## 📋 Checklist para Sprint Planning

Antes de confirmar commitment:

- [ ] Base capacity calculado con formula
- [ ] Two-track allocation definida (Track 1 ≤ 15%)
- [ ] Historical velocity consultada (last 3 sprints)
- [ ] Conversion factor actualizado (hours/point)
- [ ] Commitment ≤ 85% de capacity
- [ ] Buffer ≥ 15% reservado
- [ ] Riesgos identificados
- [ ] Stories priorizadas (MUST/SHOULD/COULD)
- [ ] Team consensus obtenido
- [ ] Fallback plan si capacity reduces

---

## 🎓 Calibración Inicial

### Primeros 3 Sprints

Si no tienes velocity histórica:

**Sprint 1: Conservative Estimate**
```
Use 3h/story point (muy conservador)
Capacity = 41.4h / 3h = 13.8 ≈ 14 story points
Commitment = 14 × 0.80 = 11 story points
```

**Sprint 2: Adjust Based on Actual**
```
Sprint 1 actual: 2.5h/point
Use 2.5h/point for Sprint 2
Capacity = 41.4h / 2.5h = 16.5 story points
Commitment = 16.5 × 0.80 = 13 story points
```

**Sprint 3: Stabilize**
```
Average Sprint 1-2: 2.6h/point
Use 2.6h/point for Sprint 3
Continue tracking and adjusting
```

**Sprint 4+: Use Rolling Average**
```
Average last 3 sprints
Adjust quarterly based on trends
```

---

**Estado:** OBLIGATORIO
**Responsable:** Technical Lead + Product Owner
**Review:** Cada Sprint Planning
**Versión:** 2.0
