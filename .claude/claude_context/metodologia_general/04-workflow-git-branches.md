# Metodología de Trabajo - YouTube RAG .NET

**Versión:** 1.0
**Fecha de creación:** 2025-10-07
**Estado:** ACTIVO

---

## Objetivo

Este documento define la metodología de trabajo para el desarrollo del proyecto YouTube RAG .NET, estableciendo el flujo de trabajo desde las historias de usuario hasta su integración en producción.

---

## Flujo de Trabajo

### 1. Punto de Partida: Historias de Usuario

- Todas las historias de usuario deben estar **creadas, organizadas y asignadas** antes de comenzar el desarrollo
- Las historias siguen la metodología establecida en documentos previos
- Cada historia debe tener:
  - ID único (formato: `YRUS-XXXX`)
  - Descripción clara
  - Criterios de aceptación
  - Definition of Done (DoD)
  - Prioridad asignada
  - Sprint asignado

### 2. Creación de Rama para Cada Historia

**Antes de comenzar el desarrollo de una historia:**

```bash
# Asegurarse de estar en develop actualizado
git checkout develop
git pull origin develop

# Crear rama con formato: [ID_HISTORIA]_[descripcion]
git checkout -b YRUS-0003_implementar_busqueda_semantica
```

**Formato de nombre de rama:**
- `[ID_HISTORIA]_[descripcion_en_snake_case]`
- Ejemplos:
  - `YRUS-0003_implementar_busqueda_semantica`
  - `YRUS-0004_optimizar_embeddings`
  - `YRUS-0005_fix_autenticacion_usuarios`

**Reglas:**
- ✅ Una rama por historia de usuario
- ✅ Siempre partir desde `develop` actualizado
- ✅ Usar snake_case para la descripción
- ✅ Mantener nombres concisos pero descriptivos

### 3. Desarrollo y Completitud (Definition of Done)

**Durante el desarrollo:**
- Seguir los principios de Clean Architecture
- Aplicar SOLID
- Escribir tests (unit + integration)
- Documentar cambios significativos

**Definition of Done (DoD) - Checklist:**

- [ ] **Código Implementado**
  - [ ] Toda la funcionalidad implementada
  - [ ] Code review completado
  - [ ] Sin warnings de compilación
  - [ ] Clean Architecture respetada

- [ ] **Testing**
  - [ ] Unit tests escritos (>70% coverage)
  - [ ] Integration tests para paths críticos
  - [ ] Todos los tests pasando
  - [ ] Manual testing completado (según TESTING_METHODOLOGY_RULES.md)
  - [ ] Todos los criterios de aceptación validados manualmente

- [ ] **Documentación**
  - [ ] Código comentado donde necesario
  - [ ] API documentation actualizada
  - [ ] README actualizado si aplica

- [ ] **Build y Deployment**
  - [ ] Build exitoso: `dotnet build --no-incremental`
  - [ ] 0 errores, 0 warnings
  - [ ] Configuración documentada

**Una vez cumplido el DoD:**

```bash
# Asegurarse de que develop está actualizado
git checkout develop
git pull origin develop

# Mergear la rama de la historia a develop
git merge YRUS-0003_implementar_busqueda_semantica

# Push a develop
git push origin develop

# Eliminar rama local (opcional)
git branch -d YRUS-0003_implementar_busqueda_semantica
```

### 4. Validación al Finalizar Sprint

**Al completar TODAS las historias del sprint:**

#### 4.1. Testing Manual Completo

- [ ] Ejecutar flujos end-to-end completos
- [ ] Validar integración entre todas las historias del sprint
- [ ] Probar casos edge y escenarios de error
- [ ] Documentar resultados

#### 4.2. Regresión Automática

```bash
# Matar procesos antiguos
taskkill /F /IM dotnet.exe

# Rebuild completo
dotnet clean
dotnet build --no-incremental --configuration Release

# Ejecutar TODOS los tests
dotnet test --configuration Release --verbosity normal

# Documentar resultados:
# - Total tests
# - Passing (%)
# - Failing (%)
# - Comparar con sprint anterior
```

**Target de calidad:**
- ✅ Tests críticos: 100% passing
- ✅ Tests high priority: >90% passing
- ✅ Tests medium priority: >80% passing
- ✅ Build: 0 errors, 0 warnings

#### 4.3. Gestión de Fallas Detectadas

**Si se detectan fallas durante la validación del sprint:**

1. **Crear Issue por cada falla**
   - Título descriptivo
   - Severidad (P0, P1, P2, P3)
   - Descripción detallada
   - Steps to reproduce
   - Expected vs Actual behavior
   - Screenshots/logs si aplica

2. **Evaluar capacidad del sprint**
   - ¿Queda tiempo en el sprint?
   - ¿Hay recursos disponibles?
   - ¿Es crítica (P0) la falla?

3. **Decisión:**

   **OPCIÓN A: Resolver en el sprint actual**
   - Si hay capacidad
   - Si es falla crítica (P0)
   - Crear rama desde develop: `FIX-[issue-id]_[descripcion]`
   - Seguir DoD
   - Mergear a develop
   - Re-validar

   **OPCIÓN B: Derivar al backlog**
   - Si NO hay capacidad
   - Si es falla no crítica (P1-P3)
   - Agregar al backlog con prioridad
   - Planificar en sprint futuro
   - Documentar en Sprint Report

4. **Documentar decisión**
   - En el issue
   - En el Sprint Report
   - En el Planning del siguiente sprint (si se deriva)

---

## Estructura de Ramas

```
master (production)
  ↑
develop (integration)
  ↑
  ├── YRUS-0003_implementar_busqueda_semantica
  ├── YRUS-0004_optimizar_embeddings
  ├── YRUS-0005_fix_autenticacion_usuarios
  └── FIX-123_corregir_fk_constraint
```

**Ramas principales:**
- `master` - Código en producción (releases)
- `develop` - Integración de desarrollo

**Ramas de trabajo:**
- `YRUS-XXXX_descripcion` - Historias de usuario
- `FIX-XXX_descripcion` - Corrección de bugs/issues

---

## Reglas de Merge

### Merge a Develop

**Requisitos:**
- ✅ Definition of Done completo
- ✅ Code review aprobado
- ✅ Tests passing
- ✅ Build exitoso
- ✅ Sin conflictos con develop

**Proceso:**
```bash
git checkout develop
git pull origin develop
git merge --no-ff YRUS-XXXX_descripcion
git push origin develop
```

### Merge a Master (Release)

**Requisitos:**
- ✅ Sprint completado y validado
- ✅ Regresión automática >85% passing
- ✅ Testing manual completado
- ✅ Product Owner sign-off
- ✅ Documentación actualizada

**Proceso:**
```bash
git checkout master
git merge --no-ff develop
git tag -a v1.x.x -m "Release Sprint X"
git push origin master --tags
```

---

## Integración con Otras Metodologías

Este flujo de trabajo se integra con:

- **TESTING_METHODOLOGY_RULES.md** - Reglas de testing y validación
- **User Stories Backlog** - Historias de usuario organizadas
- **Sprint Planning** - Planificación de sprints
- **Definition of Done** - Criterios de completitud

---

## Métricas de Sprint

Al finalizar cada sprint, documentar:

- **Historias completadas:** X/Y (%)
- **Tests passing:** X/Y (%)
- **Issues encontrados:** X
- **Issues resueltos en sprint:** X
- **Issues derivados a backlog:** X
- **Velocity:** Story points completados
- **Quality:** % tests passing

---

## Checklist de Inicio de Historia

Antes de empezar a codear:

- [ ] Historia asignada y en sprint actual
- [ ] Criterios de aceptación claros
- [ ] Definition of Done revisado
- [ ] Rama creada desde develop actualizado: `YRUS-XXXX_descripcion`
- [ ] Ambiente de desarrollo listo
- [ ] Dependencias verificadas

---

## Checklist de Fin de Historia

Antes de mergear a develop:

- [ ] DoD completo (ver sección 3)
- [ ] Code review aprobado
- [ ] Tests passing (unit + integration + manual)
- [ ] Build exitoso sin warnings
- [ ] Documentación actualizada
- [ ] Develop actualizado localmente
- [ ] Sin conflictos

---

## Checklist de Fin de Sprint

Antes de cerrar el sprint:

- [ ] Todas las historias mergeadas a develop
- [ ] Testing manual completo ejecutado
- [ ] Regresión automática ejecutada (>85% passing)
- [ ] Issues de fallas creados
- [ ] Decisión tomada por cada issue (resolver/derivar)
- [ ] Sprint Report documentado
- [ ] Product Owner sign-off obtenido
- [ ] Retrospectiva completada
- [ ] Planning de siguiente sprint iniciado

---

**Aprobado por:** Technical Lead
**Fecha efectiva:** 2025-10-07
**Próxima revisión:** Fin de Sprint 2
**Estado:** ACTIVO

---

**FIN DE METODOLOGÍA DE TRABAJO**
