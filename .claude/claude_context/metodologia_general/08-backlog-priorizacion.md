# 08 - Backlog y Priorización

**Versión:** 1.0
**Fecha:** 2025-10-20
**Estado:** ACTIVO

---

## 🎯 Framework de Priorización

### RICE Scoring

```
RICE Score = (Reach × Impact × Confidence) / Effort

Donde:
  Reach: 1-10 (usuarios afectados por quarter)
  Impact: 0.25=Minimal, 0.5=Low, 1=Medium, 2=High, 3=Massive
  Confidence: 50%=Low, 80%=Medium, 100%=High
  Effort: Person-days required
```

### MoSCoW Classification

- **Must Have:** Core MVP features
- **Should Have:** Important but not critical
- **Could Have:** Nice to have
- **Won't Have:** Out of scope for MVP

---

## 📋 User Story Format

```markdown
**As a** [user type]
**I want** [goal]
**So that** [benefit]

### Acceptance Criteria:

**AC1:** [Title]
- Given [context]
- When [action]
- Then [expected result]
- And [additional condition]

**AC2:** [Title]
...
```

---

## 🎯 Story Points

**Fibonacci:** 1, 2, 3, 5, 8, 13

**Mapping:**
- 1 pt = Trivial (1-2 hours)
- 2 pts = Simple (2-4 hours)
- 3 pts = Low complexity (4-6 hours)
- 5 pts = Medium complexity (6-10 hours)
- 8 pts = High complexity (10-16 hours)
- 13 pts = Very high (split recommended)

---

## ✅ Definition of Ready (v2.0)

Ver `14-definition-of-ready.md` para checklist completo de 50+ items.

**Mínimo obligatorio:**
1. ✅ Story completeness (ID, título, formato correcto)
2. ✅ 3+ Acceptance Criteria (Given-When-Then)
3. ✅ Dependencies identificadas y resueltas
4. ✅ Technical requirements definidos
5. ✅ Test requirements claros
6. ✅ Team readiness verificada
7. ✅ Approval & sign-off obtenidos

---

## 📊 Backlog Structure

```
Product Backlog
├── Epic 1: Video Ingestion
│   ├── US-101: Submit YouTube URL (5 pts)
│   ├── US-102: Download Video (8 pts)
│   └── US-103: Extract Audio (5 pts)
│
├── Epic 2: Transcription Pipeline
│   ├── US-201: Whisper Model Management (5 pts)
│   ├── US-202: Execute Transcription (8 pts)
│   └── US-203: Store Segments (5 pts)
│
└── Epic 3: Background Jobs
    ├── US-301: Configure Hangfire (3 pts)
    ├── US-302: Pipeline Orchestration (8 pts)
    └── US-303: Retry Logic (3 pts)
```

---

## 🔄 Backlog Refinement

**Frequency:** Continuous (Two-Track Agile)

**Activities:**
1. Review top 20% of backlog
2. Update priorities based on business value
3. Complete Definition of Ready for next sprint
4. Break down large stories (>13 pts)
5. Update story points estimates
6. Identify dependencies
7. Get stakeholder approval

---

**Referencia Completa:** Ver `PRODUCT_BACKLOG.md` en raíz del proyecto

**Estado:** ACTIVO
