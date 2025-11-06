# Git Flow Branching Strategy - YoutubeRag

**Created:** November 6, 2025
**Status:** ✅ ACTIVE
**Strategy:** Modified Git Flow for CI/CD workflows

---

## 📋 Overview

This document describes the Git Flow branching strategy implemented for the YoutubeRag project. The strategy ensures clean separation between development work and production releases.

---

## 🌳 Branch Structure

### Main Branches

| Branch | Purpose | Protection | Lifetime |
|--------|---------|------------|----------|
| **`master`** | Production-ready code | Protected | Permanent |
| **`develop`** (local) | Integration branch for features | Unprotected | Permanent |

### Supporting Branches

| Type | Naming Convention | Base Branch | Merge To | Lifetime |
|------|-------------------|-------------|----------|----------|
| **Feature** | `claude/feature-name-{sessionId}` | `develop` (local) | `master` via PR | Temporary |
| **Epic** | `claude/epic-name-{sessionId}` | `develop` (local) | `master` via PR | Temporary |
| **Hotfix** | `claude/hotfix-name-{sessionId}` | `master` | `master` via PR | Temporary |
| **Release** | `claude/release-v{version}-{sessionId}` | `develop` (local) | `master` via PR | Temporary |

---

## 🔄 Workflow

### Standard Development Flow

```
1. Update local develop from master
   ├─ git checkout master
   ├─ git pull origin master
   ├─ git checkout develop
   └─ git merge master

2. Create feature branch from develop (local)
   └─ git checkout -b claude/feature-name-011CUs6tC5tzHwSgPbMwLwsS develop

3. Work on feature
   ├─ Make changes
   ├─ Commit locally
   └─ git commit -m "feat: Implement feature X"

4. Push to origin for PR
   └─ git push -u origin claude/feature-name-011CUs6tC5tzHwSgPbMwLwsS

5. Create Pull Request to master
   └─ PR: claude/feature-name-... → master

6. After PR merged, update local develop
   ├─ git checkout master
   ├─ git pull origin master
   ├─ git checkout develop
   └─ git merge master
```

### Epic Completion Flow

```
1. Complete all user stories in epic
   └─ Multiple feature PRs merged to master

2. Update local develop from master
   ├─ git checkout master
   ├─ git pull origin master
   ├─ git checkout develop
   └─ git merge master

3. Create release branch
   └─ git checkout -b claude/release-v2.6.0-011CUs6tC5tzHwSgPbMwLwsS develop

4. Prepare release
   ├─ Update CHANGELOG.md
   ├─ Update version in .csproj files
   ├─ Update README.md
   └─ git commit -m "chore: Prepare release v2.6.0"

5. Push and create release PR
   ├─ git push -u origin claude/release-v2.6.0-011CUs6tC5tzHwSgPbMwLwsS
   └─ PR: claude/release-v2.6.0-... → master

6. After merge, create Git tag
   ├─ git checkout master
   ├─ git pull origin master
   ├─ git tag -a v2.6.0 -m "Release v2.6.0 - Epic N completion"
   └─ git push origin v2.6.0

7. Update local develop
   ├─ git checkout develop
   └─ git merge master
```

### Hotfix Flow

```
1. Critical bug found in production (master)
   └─ Create hotfix branch from master

2. Create hotfix branch
   ├─ git checkout master
   ├─ git pull origin master
   └─ git checkout -b claude/hotfix-critical-bug-011CUs6tC5tzHwSgPbMwLwsS

3. Fix the bug
   ├─ Make changes
   └─ git commit -m "fix: Critical bug in production"

4. Push and create hotfix PR
   ├─ git push -u origin claude/hotfix-critical-bug-011CUs6tC5tzHwSgPbMwLwsS
   └─ PR: claude/hotfix-... → master

5. After merge, update local develop
   ├─ git checkout master
   ├─ git pull origin master
   ├─ git checkout develop
   └─ git merge master
```

---

## 🎯 Key Principles

### 1. Local `develop` Branch

**Why local only?**
- CI/CD system requires branch names starting with `claude/` and ending with session ID
- `develop` branch serves as local integration point
- Actual pushes use `claude/*-{sessionId}` format

**How it works:**
```bash
# Local develop always synced with master
git checkout develop
git merge master

# Feature branches created from local develop
git checkout -b claude/feature-name-{sessionId} develop

# After feature merged to master, sync develop
git checkout develop
git merge master
```

### 2. All PRs Target `master`

- Individual features → master (via PR)
- Epic completions → master (via release PR)
- Hotfixes → master (via hotfix PR)

### 3. Version Releases

**Trigger:** When an epic is completed

**Process:**
1. All user stories in epic merged to master
2. Create release branch from local develop
3. Update version numbers and changelogs
4. Create release PR to master
5. After merge, create Git tag

**Versioning scheme:** Semantic Versioning (SemVer)
- **Major (v3.0.0)**: Breaking changes
- **Minor (v2.6.0)**: New features (epic completion)
- **Patch (v2.5.1)**: Bug fixes (hotfixes)

### 4. Branch Naming Conventions

**Required format:** `claude/{type}-{description}-{sessionId}`

**Examples:**
- `claude/us-001-youtube-url-submission-011CUs6tC5tzHwSgPbMwLwsS`
- `claude/epic-8-monitoring-and-observability-011CUs6tC5tzHwSgPbMwLwsS`
- `claude/hotfix-transcription-timeout-011CUs6tC5tzHwSgPbMwLwsS`
- `claude/release-v2.6.0-011CUs6tC5tzHwSgPbMwLwsS`

---

## 📊 Branch Lifecycle Example

```
master (v2.5.0)
  │
  ├─ develop (local) ← synced with master
  │    │
  │    ├─ claude/us-101-feature-A → PR #25 → master
  │    │                               ↓
  │    │                            master (updated)
  │    │                               ↓
  │    ├─ develop (merge master) ← updated
  │    │
  │    ├─ claude/us-102-feature-B → PR #26 → master
  │    │                               ↓
  │    │                            master (updated)
  │    │                               ↓
  │    ├─ develop (merge master) ← updated
  │    │
  │    └─ claude/release-v2.6.0 → PR #27 → master (v2.6.0)
  │                                           ↓
  │                                        Git tag v2.6.0
  │                                           ↓
  └─ develop (merge master) ← synced with v2.6.0
```

---

## 🛡️ Branch Protection Rules

### Master Branch

**Required:**
- ✅ Pull request before merging
- ✅ At least 1 approval (manual or auto)
- ✅ CI/CD pipeline must pass
  - Build successful
  - All tests passing (422 tests)
  - Code coverage ≥ 35%
  - Security scans passing
  - E2E tests passing
  - Performance smoke tests passing

**Prohibited:**
- ❌ Direct pushes to master
- ❌ Force pushes
- ❌ Branch deletion

---

## 📝 Commit Message Conventions

Following [Conventional Commits](https://www.conventionalcommits.org/):

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

| Type | Purpose | Example |
|------|---------|---------|
| **feat** | New feature | `feat(api): Add YouTube URL validation endpoint` |
| **fix** | Bug fix | `fix(transcription): Handle timeout errors gracefully` |
| **docs** | Documentation | `docs: Update Git Flow strategy guide` |
| **style** | Code formatting | `style: Apply dotnet format to Controllers` |
| **refactor** | Code refactoring | `refactor(services): Extract common validation logic` |
| **test** | Test additions | `test(api): Add integration tests for VideoController` |
| **chore** | Maintenance | `chore: Update NuGet packages to latest versions` |
| **perf** | Performance | `perf(search): Optimize semantic search query` |
| **ci** | CI/CD changes | `ci: Add performance test threshold checks` |
| **build** | Build system | `build: Update to .NET 9.0` |
| **revert** | Revert commit | `revert: Revert "feat: Add feature X"` |

### Examples

```bash
# Feature
git commit -m "feat(video): Implement YouTube metadata extraction"

# Bug fix
git commit -m "fix(jobs): Resolve Hangfire job duplication issue"

# Multiple lines
git commit -m "feat(api): Add semantic search pagination

- Add page and pageSize query parameters
- Return total count in response
- Update Swagger documentation
- Add integration tests

Closes #42"

# Breaking change
git commit -m "feat(api)!: Change authentication to JWT tokens

BREAKING CHANGE: Cookie-based auth is removed.
Clients must use Bearer tokens in Authorization header."
```

---

## 🔧 Maintenance

### Updating Local Develop

**Daily or before starting new work:**

```bash
git checkout master
git pull origin master
git checkout develop
git merge master --no-ff
```

### Cleaning Up Old Branches

**After PR merged:**

```bash
# Delete local branch
git branch -d claude/feature-name-011CUs6tC5tzHwSgPbMwLwsS

# Delete remote branch (optional, GitHub does this automatically)
git push origin --delete claude/feature-name-011CUs6tC5tzHwSgPbMwLwsS
```

### Resolving Conflicts

**If develop diverges from master:**

```bash
git checkout develop
git fetch origin
git merge origin/master

# If conflicts
git status
# Fix conflicts in files
git add .
git commit -m "chore: Resolve merge conflicts with master"
```

---

## 📚 References

- [Git Flow Original](https://nvie.com/posts/a-successful-git-branching-model/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)
- [GitHub Flow](https://guides.github.com/introduction/flow/)

---

## 🎯 Next Steps

### Immediate Actions

1. ✅ Create local `develop` branch from `master`
2. ✅ Document Git Flow strategy (this document)
3. ⏳ Configure branch protection rules on GitHub
4. ⏳ Update CI/CD pipelines to handle release branches
5. ⏳ Create CHANGELOG.md for version tracking

### Future Enhancements

- Automated version bumping in CI/CD
- Release notes generation from commit messages
- Automated CHANGELOG.md updates
- Branch naming validation in pre-commit hooks

---

**Last Updated:** November 6, 2025
**Next Review:** Sprint 9 Planning
**Owner:** DevOps Team
