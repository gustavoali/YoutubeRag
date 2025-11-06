# YoutubeRag - Claude Project Memory

> **Auto-loaded:** This file is automatically included in Claude's context at the start of every session.
> **Keep it concise:** Focus on the most essential, non-obvious information about this project.

---

## Project Overview

**YoutubeRag** is a RAG (Retrieval-Augmented Generation) system for YouTube video transcriptions with semantic search capabilities.

- **Architecture:** Clean Architecture (.NET 8)
- **Main Tech Stack:** ASP.NET Core, EF Core, Hangfire, MySQL, Redis, Whisper AI
- **Test Coverage:** 99.3% (422/425 tests passing)
- **Development Mode:** Full CI/CD pipeline with GitHub Actions

---

## 🎯 Core Development Principles

### 1. Always Use Agent Delegation
- **CRITICAL:** Read `.claude/AGENT_USAGE_GUIDELINES.md` at session start
- We have 11 specialized agents (dotnet-backend-developer, test-engineer, code-reviewer, etc.)
- **Rule:** If a task takes >30 min OR can run in parallel → DELEGATE to specialized agent
- **Goal:** 40-60% time reduction through parallel agent work

### 2. Follow Clean Architecture Layers
```
Domain/         → Entities, Interfaces (NO external dependencies)
Application/    → Business logic, Services (depends on Domain only)
Infrastructure/ → EF Core, External APIs (implements Application interfaces)
Api/            → Controllers, Startup (thin layer)
```

**NEVER:**
- Reference Infrastructure from Domain
- Put business logic in Controllers
- Mix layers incorrectly

### 3. Test-Driven Development (TDD)
```bash
# For ALL new features and bug fixes:
1. Write failing test FIRST
2. Commit test suite
3. Implement code to pass test
4. Verify all 422+ tests still pass
5. Commit working code

# Run tests:
dotnet test --configuration Release
```

### 4. Explore → Plan → Code → Commit
```bash
# Phase 1: EXPLORE (understand before coding)
- Read relevant files
- Use /clear if context is polluted
- Ask clarifying questions

# Phase 2: PLAN (extended thinking)
- Use "think harder" for complex problems
- Create checklist in TODO.md or issue
- Get approval before implementing

# Phase 3: CODE (TDD + parallel agents)
- Delegate to specialized agents when possible
- Run tests continuously
- Iterate based on feedback

# Phase 4: COMMIT (documentation)
- Generate contextual commit message
- Update docs if needed
- Push to: claude/work-in-progress-<session-id>
```

---

## 📂 Project Structure (Key Files)

```
YoutubeRag/
├── CLAUDE.md                           ← THIS FILE (auto-loaded)
├── .claude/
│   ├── METHODOLOGY.md                  ← Complete dev methodology
│   ├── AGENT_USAGE_GUIDELINES.md       ← Agent delegation rules
│   ├── CONTEXT_MANAGEMENT.md           ← Token optimization
│   └── commands/                       ← Custom slash commands
│
├── YoutubeRag.Domain/
│   ├── Entities/                       ← Core domain models (Video, Transcript, etc.)
│   └── Interfaces/                     ← Service contracts
│
├── YoutubeRag.Application/
│   ├── Services/                       ← Business logic
│   │   ├── VideoProcessingService.cs   ← Video download + processing
│   │   ├── TranscriptionService.cs     ← Whisper integration
│   │   └── SemanticSearchService.cs    ← Vector search
│   └── Configuration/                  ← App configuration (WhisperOptions, etc.)
│
├── YoutubeRag.Infrastructure/
│   ├── Data/                           ← EF Core DbContext
│   ├── Jobs/                           ← Hangfire background jobs
│   ├── Repositories/                   ← Data access
│   └── Services/                       ← External service implementations
│
├── YoutubeRag.Api/
│   ├── Controllers/                    ← REST API endpoints
│   └── Program.cs                      ← Startup + DI configuration
│
└── YoutubeRag.Tests.Integration/       ← 422+ integration tests
    ├── Controllers/                    ← API tests
    ├── Services/                       ← Service tests
    └── E2E/                            ← End-to-end tests
```

---

## 🔧 Common Commands & Tasks

### Development Workflow
```bash
# Start local development
./scripts/dev-setup.sh       # Linux/macOS (first time)
dotnet run --project YoutubeRag.Api

# Start with local environment
ASPNETCORE_ENVIRONMENT=Local dotnet run --no-build --configuration Release

# Run tests
dotnet test --configuration Release
dotnet test --filter "Category=Integration"
```

### Database Migrations
```bash
# Create migration
dotnet ef migrations add MigrationName \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api

# Apply migrations
dotnet ef database update \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api
```

### Git Workflow
```bash
# Current branch (check at session start)
git status
# Should be: claude/work-in-progress-<session-id>

# Commit (Claude generates message)
git add .
git commit -m "..."

# Push (ALWAYS with -u)
git push -u origin claude/work-in-progress-<session-id>
```

---

## ⚠️ Common Pitfalls & Non-Obvious Patterns

### 1. Path Resolution (Cross-Platform)
```csharp
// ❌ WRONG: Hardcoded paths
var path = "C:\\Temp\\YoutubeRag";

// ✅ CORRECT: Use IPathProvider
var path = _pathProvider.GetTempPath();
// Automatically resolves:
//   Windows:    C:\Temp\YoutubeRag
//   Linux:      /tmp/youtuberag
//   Container:  /app/temp
```

### 2. Async/Await Pattern
```csharp
// ✅ ALL service methods MUST be async
public async Task<Video> ProcessVideoAsync(string url)
{
    await _repository.AddAsync(video);
    await _unitOfWork.SaveChangesAsync(); // Don't forget!
    return video;
}
```

### 3. Repository Pattern
```csharp
// ✅ ALWAYS use Unit of Work for transactions
await _videoRepository.AddAsync(video);
await _transcriptRepository.AddAsync(transcript);
await _unitOfWork.SaveChangesAsync(); // Single transaction
```

### 4. Background Jobs (Hangfire)
```csharp
// Jobs run in SEPARATE process context
// ✅ MUST re-fetch entities from DB
public async Task ProcessVideoJob(int videoId)
{
    var video = await _repository.GetByIdAsync(videoId); // Re-fetch!
    // Don't rely on cached entities
}
```

### 5. Testing with TestContainers
```csharp
// ✅ Use REAL database via TestContainers (NOT mocks)
public class VideoServiceTests : IntegrationTestBase
{
    // TestContainers automatically creates MySQL container
    // Runs migrations automatically
    // Cleans up after tests
}
```

---

## 🚨 Before You Start ANY Task

**MANDATORY CHECKLIST:**
1. ✅ Read `.claude/AGENT_USAGE_GUIDELINES.md` - Should I delegate this?
2. ✅ Check `git status` - Am I on correct branch?
3. ✅ Run `dotnet test` - Are all tests passing before I start?
4. ✅ Use `/clear` if context feels polluted from previous tasks

---

## 📊 Project Health Metrics

**Test Suite:**
- Total: 425 tests
- Passing: 422 (99.3%)
- Skipped: 3 (optional features)

**CI/CD Pipeline:**
- ✅ Build + Test on every push
- ✅ Security scans (CodeQL, dependencies)
- ✅ E2E tests (90s health checks)
- ✅ Performance tests (k6)

**Code Quality:**
- Pre-commit hooks: Code formatting + build check
- Pre-push hooks: Unit tests
- Code review: Automated via code-reviewer agent

---

## 🎯 Current Sprint Context

**Last Completed:**
- Issue #13: Test coverage improvements (3 critical services >70%)
- Sprint 7: CI/CD stabilization + security scans

**Active Branch:**
- `claude/work-in-progress-011CUrYe3w9LkjVaU1U9Lm8i`

**Main Branch for PRs:**
- Check `git log` for recent commits
- PRs should target `main` or `develop` (confirm with user)

---

## 🔗 Quick Links to Key Documentation

- [METHODOLOGY.md](.claude/METHODOLOGY.md) - Complete development methodology
- [AGENT_USAGE_GUIDELINES.md](.claude/AGENT_USAGE_GUIDELINES.md) - Agent delegation
- [CONTEXT_MANAGEMENT.md](.claude/CONTEXT_MANAGEMENT.md) - Token optimization
- [README.md](README.md) - Project overview + quick start
- [DEVELOPER_SETUP_GUIDE.md](docs/devops/DEVELOPER_SETUP_GUIDE.md) - Detailed setup

---

## 💡 Tips for Effective Claude Sessions

### Use Extended Thinking
```
"think harder" → More analysis
"ultrathink"  → Maximum analysis for complex problems
```

### Clear Context Frequently
```bash
/clear  # Between major tasks
        # Keeps context focused
        # Improves response quality
```

### Delegate Proactively
```markdown
✅ GOOD: "Delegating Epic 3 validation to dotnet-backend-developer"
❌ BAD:  "I'll manually review Epic 3 code" (2+ hours wasted)
```

### Be Specific
```diff
❌ Vague: "Add error handling"
✅ Specific: "Add try-catch in ProcessVideoAsync to catch:
             - HttpRequestException → Return VideoDownloadFailed status
             - UnauthorizedAccessException → Log error, notify admin
             - Re-throw unexpected exceptions for global handler"
```

---

## 🔄 Context Refresh Indicators

**Clear context with `/clear` when you notice:**
- Responses referencing unrelated previous tasks
- Token budget warnings
- Switching between unrelated epics/features
- Starting a new work session

---

**Last Updated:** January 2025
**Methodology Version:** 2.0

---

> **Remember:** This file provides context. For step-by-step workflows, see `.claude/METHODOLOGY.md`.
> For detailed agent usage, see `.claude/AGENT_USAGE_GUIDELINES.md`.
