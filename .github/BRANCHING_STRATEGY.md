# Branching Strategy - YoutubeRag Project

**Fecha:** 9 de Octubre, 2025
**Versión:** 1.0
**Autor:** Sprint 2 Team

---

## 📋 Lecciones Aprendidas - Sprint 2

### ⚠️ Issue Identificado

Durante Sprint 2, se utilizó una sola rama (`YRUS-0201_gestionar_modelos_whisper`) como **integration branch** para múltiples épicas (Epic 2, 3, 4, 5).

**Problemas:**
- Branch name no refleja el contenido completo
- Dificulta trazabilidad por user story
- Merge a master contiene múltiples épicas en un solo PR

**Causa:**
- Trabajo en paralelo con agentes especializados requirió integración continua
- No se siguió estrategia de feature branches por US
- Se priorizó velocidad sobre organización de branches

**Decisión tomada:**
- ✅ Merge YRUS-0201 → master como "Sprint 2 Integration Branch"
- ✅ Documentar esta estrategia para evitar en futuro

---

## 🎯 Branching Strategy Recomendada

### Estructura de Branches

```
master (main branch, production-ready)
  │
  └─> develop (integration branch, always deployable)
       │
       ├─> epic-2-transcription (epic integration branch)
       │    │
       │    ├─> YRUS-0201-gestionar-modelos-whisper (feature branch)
       │    ├─> YRUS-0202-ejecutar-transcripcion (feature branch)
       │    └─> YRUS-0203-segmentar-y-almacenar (feature branch)
       │
       ├─> epic-3-download-audio (epic integration branch)
       │    │
       │    └─> YRUS-0103-descargar-y-extraer-audio (feature branch)
       │
       ├─> epic-4-background-jobs (epic integration branch)
       │    │
       │    ├─> YRUS-0301-pipeline-orchestration (feature branch)
       │    └─> YRUS-0302-retry-logic (feature branch)
       │
       └─> epic-5-progress-tracking (epic integration branch)
            │
            ├─> YRUS-0401-real-time-progress (feature branch)
            └─> YRUS-0402-error-notifications (feature branch)
```

---

## 🔄 Workflow por Epic

### 1. Inicio de Epic

```bash
# Crear epic branch desde develop
git checkout develop
git pull origin develop
git checkout -b epic-X-nombre-epica

# Push epic branch
git push -u origin epic-X-nombre-epica
```

### 2. Trabajo en User Story

```bash
# Crear feature branch desde epic branch
git checkout epic-X-nombre-epica
git checkout -b YRUS-XXXX-descripcion-corta

# Hacer commits
git add .
git commit -m "feat: descripción del cambio"

# Push feature branch
git push -u origin YRUS-XXXX-descripcion-corta
```

### 3. Merge Feature → Epic

```bash
# Crear PR: YRUS-XXXX → epic-X-nombre-epica
# Una vez aprobado, hacer merge (squash opcional)
git checkout epic-X-nombre-epica
git merge YRUS-XXXX-descripcion-corta
git push origin epic-X-nombre-epica

# Eliminar feature branch (opcional)
git branch -d YRUS-XXXX-descripcion-corta
git push origin --delete YRUS-XXXX-descripcion-corta
```

### 4. Release de Epic

```bash
# Cuando epic está completa y testeada
# Crear PR: epic-X-nombre-epica → develop
# Una vez aprobado, hacer merge

git checkout develop
git merge epic-X-nombre-epica
git push origin develop

# Tag release
git tag -a vX.Y.0-epic-name -m "Release Epic X: Nombre"
git push origin vX.Y.0-epic-name

# Eliminar epic branch (opcional)
git branch -d epic-X-nombre-epica
git push origin --delete epic-X-nombre-epica
```

### 5. Release a Production

```bash
# Periódicamente (cada sprint, por ejemplo)
# Crear PR: develop → master
# Una vez aprobado, hacer merge

git checkout master
git merge develop
git push origin master

# Tag production release
git tag -a vX.Y.0 -m "Sprint X Release"
git push origin vX.Y.0
```

---

## 🚀 Trabajo en Paralelo con Agentes

### Estrategia para Agentes Especializados

Cuando se trabaja con **agentes en paralelo** (backend-developer, test-engineer, etc.), usar esta estrategia:

#### Opción A: Epic Branch como Integration Point (Recomendado)

```bash
# Agentes trabajan en epic branch
git checkout epic-X-nombre

# Agente 1: Implementation
# Agente 2: Testing (paralelo)
# Agente 3: Validation (paralelo)

# Commits van a epic-X-nombre directamente
# NO crear feature branches individuales para cada agente
```

**Pros:**
- Integración continua
- Menos overhead de branches
- Agentes pueden trabajar en paralelo sin conflictos

**Contras:**
- Epic branch acumula muchos commits
- Menos granularidad en trazabilidad

#### Opción B: Feature Branch por Componente (Granular)

```bash
# Agente 1: Backend implementation
git checkout -b YRUS-0301-dead-letter-queue

# Agente 2: Testing
git checkout -b YRUS-0301-testing

# Merge ambos a epic-4-background-jobs cuando completen
```

**Pros:**
- Mejor trazabilidad
- Code review más granular

**Contras:**
- Más overhead
- Posibles conflictos al merge

**Recomendación:** Usar **Opción A** para agentes en paralelo, **Opción B** para user stories independientes.

---

## 📝 Convenciones de Nombres

### Branch Names

**Epic branches:**
```
epic-{número}-{nombre-descriptivo}

Ejemplos:
epic-2-transcription
epic-3-download-audio
epic-4-background-jobs
```

**Feature branches (User Stories):**
```
YRUS-{número}-{descripción-corta}

Ejemplos:
YRUS-0201-gestionar-modelos-whisper
YRUS-0301-pipeline-orchestration
YRUS-0401-real-time-progress
```

**Hotfix branches:**
```
hotfix-{descripción}

Ejemplo:
hotfix-serilog-frozen-logger
```

**Release branches (opcional):**
```
release-{versión}

Ejemplo:
release-2.3.0
```

### Commit Messages

Seguir [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

Types:
- feat: nueva funcionalidad
- fix: bug fix
- docs: documentación
- test: tests
- refactor: refactoring
- chore: tareas de mantenimiento
- perf: mejoras de performance

Ejemplos:
feat(epic-4): implement dead letter queue
fix(epic-2): resolve bulk insert timestamps issue
docs(sprint-2): update status report
test(epic-3): add integration tests for video download
```

### PR Titles

```
<Epic/Component>: <Description>

Ejemplos:
Epic 2: Transcription Pipeline Implementation
Epic 4: Background Jobs P0+P1 Gaps
Sprint 2: Epics 2-5 Integration
```

---

## 🔍 PR Strategy

### Feature PR (US → Epic)
- **From:** `YRUS-XXXX`
- **To:** `epic-X-nombre`
- **Reviewers:** 1-2 team members
- **Squash:** Opcional
- **Delete branch after merge:** Sí

### Epic PR (Epic → Develop)
- **From:** `epic-X-nombre`
- **To:** `develop`
- **Reviewers:** Product Owner + Tech Lead
- **Squash:** No (preserve commit history)
- **Delete branch after merge:** Opcional

### Release PR (Develop → Master)
- **From:** `develop`
- **To:** `master`
- **Reviewers:** All team + stakeholders
- **Squash:** No
- **Delete branch after merge:** No (keep develop)

---

## 📊 Ejemplo Completo - Epic 4

### Timeline

**Day 1: Inicio Epic**
```bash
git checkout develop
git checkout -b epic-4-background-jobs
git push -u origin epic-4-background-jobs
```

**Day 2: US 1 - Pipeline Orchestration**
```bash
git checkout epic-4-background-jobs
git checkout -b YRUS-0301-pipeline-orchestration

# Implementation by backend-developer agent
git add .
git commit -m "feat(epic-4): implement Hangfire job orchestration"
git push -u origin YRUS-0301-pipeline-orchestration

# Create PR: YRUS-0301 → epic-4-background-jobs
# Review + Merge
```

**Day 3: US 2 - Retry Logic (Paralelo con Testing)**
```bash
# Agente 1: Implementation
git checkout epic-4-background-jobs
git checkout -b YRUS-0302-retry-logic

# Agente 2: Testing (parallel)
git checkout epic-4-background-jobs
git checkout -b YRUS-0302-testing

# Ambos hacen commits, luego merge a epic-4-background-jobs
```

**Day 4: Testing & Validation**
```bash
git checkout epic-4-background-jobs

# test-engineer agent commits directly to epic branch
git add .
git commit -m "test(epic-4): add integration tests"
git push origin epic-4-background-jobs
```

**Day 5: Release Epic**
```bash
# Epic completa y testeada
# Create PR: epic-4-background-jobs → develop
# Review + Approve + Merge

git checkout develop
git merge epic-4-background-jobs
git tag -a v2.4.0-background-jobs -m "Release Epic 4"
git push origin develop
git push origin v2.4.0-background-jobs
```

---

## ✅ Checklist de Branch Hygiene

### Antes de Merge
- [ ] Build passing
- [ ] Tests passing (coverage > 80%)
- [ ] Code review completado
- [ ] No merge conflicts
- [ ] Commit messages descriptivos
- [ ] PR description completa

### Después de Merge
- [ ] Tag release (si es epic/sprint merge)
- [ ] Delete feature branch (si no se necesita más)
- [ ] Update documentation
- [ ] Notify team

### Limpieza Periódica
- [ ] Delete merged feature branches (monthly)
- [ ] Archive old epic branches (after sprint review)
- [ ] Clean up stale branches (no commits in 30 days)

---

## 🚨 Casos Especiales

### Hotfix Urgente en Production

```bash
# Branch desde master
git checkout master
git checkout -b hotfix-descripcion

# Fix + test
git commit -m "fix: critical bug in production"

# Merge a master Y develop
git checkout master
git merge hotfix-descripcion
git push origin master

git checkout develop
git merge hotfix-descripcion
git push origin develop

# Tag
git tag -a v2.3.1-hotfix -m "Hotfix: descripción"
git push origin v2.3.1-hotfix
```

### Conflictos en Epic Branch

```bash
# Rebase feature branch con epic branch
git checkout YRUS-XXXX
git fetch origin
git rebase origin/epic-X-nombre

# Resolver conflictos
git add .
git rebase --continue

# Force push (solo si branch no compartido)
git push -f origin YRUS-XXXX
```

### Rollback de Epic

```bash
# Si epic merge a develop causó problemas
git checkout develop
git revert -m 1 <merge-commit-hash>
git push origin develop

# Investigar, fix en epic branch, re-merge
```

---

## 🎯 Métricas de Éxito

### KPIs de Branching

- **Branch lifetime:** < 5 días (feature), < 15 días (epic)
- **PR review time:** < 24 horas
- **Merge conflicts:** < 10% de PRs
- **Deleted branches after merge:** > 90%

### Sprint 2 Metrics (Actual)

- ❌ Branch lifetime: 3 días (YRUS-0201, pero contenía 4 épicas)
- ✅ PR review time: N/A (no PRs yet)
- ❌ Branch organization: 1 branch para múltiples épicas
- **Lesson learned:** Crear epic branches desde inicio

---

**Última actualización:** 9 de Octubre, 2025
**Próxima revisión:** Sprint 3 Planning (post Sprint 2 Review)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
