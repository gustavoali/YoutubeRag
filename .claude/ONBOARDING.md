# YoutubeRag Developer Onboarding Guide

**Welcome to YoutubeRag!** 🎉

This guide will get you from zero to productive in **~2 hours**.

**Version:** 1.0
**Last Updated:** January 2025
**Audience:** New developers joining the YoutubeRag project

---

## 📋 Table of Contents

1. [Pre-requisites](#pre-requisites)
2. [Day 1: Environment Setup (30 minutes)](#day-1-environment-setup-30-minutes)
3. [Day 1: Understanding the Project (30 minutes)](#day-1-understanding-the-project-30-minutes)
4. [Day 1: Learning the Methodology (30 minutes)](#day-1-learning-the-methodology-30-minutes)
5. [Day 1: First Task (30 minutes)](#day-1-first-task-30-minutes)
6. [Week 1: Progressive Learning](#week-1-progressive-learning)
7. [Resources](#resources)
8. [Getting Help](#getting-help)

---

## Pre-requisites

Before you start, ensure you have:

- [ ] **Git** - Version control
- [ ] **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- [ ] **Docker Desktop** - For MySQL and Redis
- [ ] **Code Editor** - VS Code recommended (or Visual Studio)
- [ ] **Claude Code** - If working with Claude AI
- [ ] **GitHub Account** - With access to repository

**Verify installations:**
```bash
git --version        # Should be 2.x+
dotnet --version     # Should be 8.0.x
docker --version     # Should be 20.x+
```

---

## Day 1: Environment Setup (30 minutes)

### Step 1: Clone Repository (2 minutes)

```bash
# Clone the repository
git clone https://github.com/gustavoali/YoutubeRag.git
cd YoutubeRag

# Check current branch
git status
```

### Step 2: Automated Setup (20 minutes)

**Windows (PowerShell):**
```powershell
.\scripts\dev-setup.ps1
```

**Linux/macOS:**
```bash
chmod +x scripts/dev-setup.sh
./scripts/dev-setup.sh
```

**What this does:**
1. ✅ Validates prerequisites
2. ✅ Creates `.env` file
3. ✅ Starts Docker containers (MySQL, Redis)
4. ✅ Restores NuGet packages
5. ✅ Builds solution
6. ✅ Runs database migrations
7. ✅ Seeds test data

**Expected output:**
```
✅ Prerequisites validated
✅ Environment configured
✅ Database ready
✅ Build successful
✅ Tests passing (422/425)

🎉 Setup complete! Start the API with:
   dotnet run --project YoutubeRag.Api
```

### Step 3: Verify Setup (5 minutes)

```bash
# Start the API
dotnet run --project YoutubeRag.Api

# In another terminal, test endpoints
curl http://localhost:5000/health
# Should return: {"status":"Healthy"}

# Access Swagger UI
# Open browser: http://localhost:5000/swagger

# Stop the API (Ctrl+C)
```

### Step 4: Run Tests (3 minutes)

```bash
# Run all tests
dotnet test --configuration Release

# Expected output:
# Passed: 422
# Failed: 0
# Skipped: 3
# Total: 425
```

**✅ If all tests pass, your environment is ready!**

---

## Day 1: Understanding the Project (30 minutes)

### Architecture Overview (10 minutes)

**Read:** [README.md](../README.md) - Focus on sections:
- ✨ Features
- 🏗️ Project Structure
- ⚙️ Configuration

**Key Concepts:**

1. **Clean Architecture** - 4 layers:
   ```
   Domain/        → Core entities (Video, Transcript, etc.)
   Application/   → Business logic (Services)
   Infrastructure/→ External integrations (DB, Whisper, etc.)
   Api/           → REST endpoints
   ```

2. **Main Features:**
   - YouTube video download
   - AI transcription (Whisper)
   - Semantic search (vector embeddings)
   - Background jobs (Hangfire)

3. **Tech Stack:**
   - .NET 8, ASP.NET Core
   - MySQL, Redis
   - EF Core, Hangfire
   - Whisper AI

### Project Structure (10 minutes)

**Explore key directories:**

```bash
# Domain layer - Core business entities
ls YoutubeRag.Domain/Entities/
# Files: Video.cs, Transcript.cs, TranscriptSegment.cs, User.cs, etc.

# Application layer - Business logic
ls YoutubeRag.Application/Services/
# Files: VideoProcessingService.cs, TranscriptionService.cs, SemanticSearchService.cs

# API layer - REST endpoints
ls YoutubeRag.Api/Controllers/
# Files: VideoController.cs, SearchController.cs, JobsController.cs

# Tests - 422+ tests
ls YoutubeRag.Tests.Integration/
```

### Database Schema (10 minutes)

**Explore database:**

```bash
# Connect to MySQL
docker exec -it mysql mysql -u root -p
# Password: youtuberag_root_password (from .env)

# Show tables
USE youtuberag;
SHOW TABLES;

# Key tables:
# - Videos
# - Transcripts
# - TranscriptSegments
# - BackgroundJobs (Hangfire)
# - Users

# Explore Videos table
DESCRIBE Videos;

# Exit
exit;
```

---

## Day 1: Learning the Methodology (30 minutes)

### Claude Code Methodology (20 minutes)

**If working with Claude Code:**

1. **Read CLAUDE.md** (5 minutes)
   ```bash
   cat CLAUDE.md
   ```
   - Auto-loaded by Claude every session
   - Essential project context
   - Common patterns and pitfalls

2. **Scan .claude/README.md** (10 minutes)
   ```bash
   cat .claude/README.md
   ```
   - Directory overview
   - File descriptions
   - Learning path

3. **Bookmark key files:** (5 minutes)
   - `.claude/METHODOLOGY.md` - Development workflow
   - `.claude/AGENT_USAGE_GUIDELINES.md` - Agent delegation
   - `.claude/CONTEXT_MANAGEMENT.md` - Token optimization

### Development Workflow (10 minutes)

**The Core Pattern: Explore → Plan → Code → Commit**

```markdown
1. EXPLORE (Understand)
   - Read relevant files
   - Understand the problem
   - Ask clarifying questions

2. PLAN (Think)
   - Design the solution
   - Create checklist
   - Get approval if needed

3. CODE (Implement)
   - Follow TDD (tests first)
   - Implement solution
   - Run tests continuously

4. COMMIT (Document)
   - Write clear commit message
   - Update documentation
   - Push to feature branch
```

**Quality Gates:**
- ✅ All tests pass before commit
- ✅ No compiler warnings
- ✅ Clean Architecture respected
- ✅ Code formatted (pre-commit hook)

---

## Day 1: First Task (30 minutes)

### Task: Fix a Simple Bug (Hands-On Practice)

**Objective:** Learn the workflow by fixing a real (or simulated) bug.

**Scenario:** We'll create and fix a simple bug to practice the workflow.

### Step 1: Create a Feature Branch (2 minutes)

```bash
# Create your branch
git checkout -b feature/onboarding-practice-YOUR_NAME

# Verify you're on the new branch
git branch
```

### Step 2: Explore the Code (5 minutes)

```bash
# Let's work on VideoService
# Open in your editor:
code YoutubeRag.Application/Services/VideoService.cs

# Read the ProcessVideoAsync method
# Understand what it does
```

### Step 3: Write a Test (10 minutes)

**Let's add a test for edge case handling:**

```bash
# Open test file
code YoutubeRag.Tests.Integration/Services/VideoServiceTests.cs
```

**Add this test:**

```csharp
[Fact]
public async Task ProcessVideoAsync_WithNullUrl_ShouldThrowArgumentNullException()
{
    // Arrange
    var service = _serviceProvider.GetRequiredService<IVideoProcessingService>();

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(
        () => service.ProcessVideoAsync(null!, "Test Video", "Description")
    );
}
```

### Step 4: Verify Test Fails (2 minutes)

```bash
# Run the specific test
dotnet test --filter "ProcessVideoAsync_WithNullUrl" --configuration Release

# Expected: Test should FAIL (method doesn't validate null yet)
```

### Step 5: Fix the Implementation (5 minutes)

**In VideoService.cs, add validation:**

```csharp
public async Task<Video> ProcessVideoAsync(string url, string title, string? description)
{
    // ADD THIS:
    if (string.IsNullOrWhiteSpace(url))
        throw new ArgumentNullException(nameof(url));

    // ... rest of implementation
}
```

### Step 6: Verify Test Passes (2 minutes)

```bash
# Run the test again
dotnet test --filter "ProcessVideoAsync_WithNullUrl" --configuration Release

# Expected: Test should PASS ✅

# Run all tests to ensure nothing broke
dotnet test --configuration Release
```

### Step 7: Commit Your Changes (4 minutes)

```bash
# Stage changes
git add .

# Commit with descriptive message
git commit -m "test: Add validation test for null URL in VideoService

- Added test case for ProcessVideoAsync with null URL
- Implemented ArgumentNullException validation
- All 423 tests passing

Part of onboarding practice"

# Verify commit
git log -1
```

**✅ Congratulations!** You've completed your first task using TDD workflow!

### Cleanup

```bash
# Switch back to main branch
git checkout main

# Delete practice branch
git branch -D feature/onboarding-practice-YOUR_NAME
```

---

## Week 1: Progressive Learning

### Day 2-3: Deeper Understanding

**Goals:**
- [ ] Read complete METHODOLOGY.md
- [ ] Understand all 4 Clean Architecture layers
- [ ] Explore test suite structure
- [ ] Review recent PRs to see patterns

**Tasks:**
- Fix 2-3 real bugs from issue tracker
- Add tests for existing code
- Practice TDD workflow

### Day 4-5: Advanced Patterns

**Goals:**
- [ ] Learn agent delegation (if using Claude)
- [ ] Practice parallel work patterns
- [ ] Use custom slash commands
- [ ] Contribute to a feature

**Tasks:**
- Implement a small feature from scratch
- Use `/review-epic` command
- Create a PR using `/prepare-pr`

---

## Week 1 Checklist

By end of Week 1, you should have:

**Environment & Setup:**
- [x] Local environment working
- [x] Can run API locally
- [x] Can run all tests
- [x] Can access Swagger UI
- [x] Can connect to MySQL

**Knowledge:**
- [x] Understand Clean Architecture layers
- [x] Know main project components
- [x] Familiar with test structure
- [x] Read core methodology files

**Practical Skills:**
- [x] Fixed at least 3 bugs
- [x] Added at least 5 tests
- [x] Created at least 2 PRs
- [x] Used TDD workflow successfully
- [x] Followed commit message standards

**Team Integration:**
- [x] Attended team standup
- [x] Asked questions in team chat
- [x] Reviewed at least 2 PRs from others
- [x] Pair programmed (if available)

---

## Resources

### Essential Reading

**Priority 1 (First Day):**
- [README.md](../README.md) - Project overview
- [CLAUDE.md](../CLAUDE.md) - Project memory (if using Claude)
- [.claude/README.md](.claude/README.md) - Methodology overview

**Priority 2 (First Week):**
- [METHODOLOGY.md](.claude/METHODOLOGY.md) - Complete workflow
- [AGENT_USAGE_GUIDELINES.md](.claude/AGENT_USAGE_GUIDELINES.md) - Agent patterns
- [DEVELOPER_SETUP_GUIDE.md](../docs/devops/DEVELOPER_SETUP_GUIDE.md) - Detailed setup

**Priority 3 (Ongoing):**
- [CONTEXT_MANAGEMENT.md](.claude/CONTEXT_MANAGEMENT.md) - Optimization
- [TEST_RESULTS_REPORT.md](../TEST_RESULTS_REPORT.md) - Test metrics
- [DEVOPS_IMPLEMENTATION_PLAN.md](../docs/devops/DEVOPS_IMPLEMENTATION_PLAN.md) - DevOps

### Useful Commands

```bash
# Development
dotnet run --project YoutubeRag.Api              # Start API
dotnet test --configuration Release              # Run all tests
dotnet build --configuration Release             # Build solution

# Database
docker-compose up -d                             # Start infrastructure
docker-compose logs -f mysql                     # View MySQL logs
dotnet ef database update                        # Apply migrations

# Testing
dotnet test --filter "Category=Integration"      # Integration tests only
dotnet test --collect:"XPlat Code Coverage"      # With coverage

# Code Quality
dotnet format                                    # Format code
dotnet build --no-incremental                    # Clean build
```

### Custom Commands (If using Claude)

```bash
/check-health          # Verify project health
/run-tests integration # Run specific test category
/review-epic 3         # Review Epic 3
/prepare-pr            # Create pull request
/analyze-performance   # Performance analysis
```

---

## Getting Help

### During Onboarding

**Technical Issues:**
1. Check [Troubleshooting](../README.md#troubleshooting) in README
2. Review [.claude/README.md](.claude/README.md#troubleshooting)
3. Ask in team chat
4. Pair with senior developer

**Methodology Questions:**
1. Re-read relevant methodology file
2. Ask Claude: "How should I handle X according to the methodology?"
3. Ask team lead
4. Check GitHub issues for similar questions

**Project Questions:**
1. Explore code first (hands-on learning)
2. Read documentation
3. Ask in team chat
4. Schedule onboarding session with mentor

### Communication Channels

**Immediate Help:**
- Team Chat/Slack
- Pair programming sessions

**Questions:**
- GitHub Discussions
- Team meetings

**Bug Reports:**
- GitHub Issues

**Feature Ideas:**
- GitHub Discussions → Issues (after discussion)

---

## Common Onboarding Mistakes

### ❌ Mistake 1: Skipping Setup Verification

**Problem:** Assume setup worked without testing

**Solution:**
- Always run `dotnet test` after setup
- Verify API starts successfully
- Test database connectivity
- Check all Docker containers running

### ❌ Mistake 2: Not Following TDD

**Problem:** Writing code before tests

**Solution:**
- ALWAYS write tests first
- Verify tests FAIL without implementation
- Then implement to make tests pass
- This is non-negotiable on this project

### ❌ Mistake 3: Ignoring Clean Architecture

**Problem:** Putting logic in wrong layer

**Solution:**
- Domain: Entities only, no dependencies
- Application: Business logic, depends on Domain
- Infrastructure: External services, implements Application
- Api: Controllers only, thin layer

### ❌ Mistake 4: Large, Monolithic PRs

**Problem:** Trying to do too much in one PR

**Solution:**
- Keep PRs small (< 300 lines changed)
- One feature/bug per PR
- Easier to review
- Faster to merge

### ❌ Mistake 5: Not Using /clear (Claude Users)

**Problem:** Context becomes polluted, responses degrade

**Solution:**
- Use `/clear` between major tasks
- Read CONTEXT_MANAGEMENT.md
- Keep context focused
- Don't load unnecessary files

---

## Your First Week Goals

### Monday
- [x] Complete environment setup
- [x] Read README and CLAUDE.md
- [x] Complete "First Task" exercise
- [x] Attend team standup

### Tuesday
- [ ] Read METHODOLOGY.md
- [ ] Fix 2 simple bugs
- [ ] Add 3+ tests
- [ ] Create first real PR

### Wednesday
- [ ] Review 2 PRs from others
- [ ] Learn agent delegation (if using Claude)
- [ ] Implement small feature
- [ ] Practice parallel work

### Thursday
- [ ] Use custom commands
- [ ] Contribute to larger feature
- [ ] Pair program with team member
- [ ] Attend team meeting

### Friday
- [ ] Complete Week 1 checklist
- [ ] Reflect on learning
- [ ] Ask remaining questions
- [ ] Plan Week 2 goals

---

## Week 1 Reflection

At the end of Week 1, take 15 minutes to reflect:

**What went well?**
- [Your notes]

**What was challenging?**
- [Your notes]

**What questions remain?**
- [Your questions]

**Week 2 goals:**
- [Your goals]

**Share your reflection with your mentor or team lead.**

---

## Welcome Message

You're joining a project that values:
- ✅ **Quality** - 99.3% test coverage, comprehensive CI/CD
- ✅ **Clarity** - Clean Architecture, clear documentation
- ✅ **Efficiency** - Agent delegation, parallel work, automation
- ✅ **Collaboration** - Code review, pair programming, knowledge sharing
- ✅ **Continuous Improvement** - Methodology evolves, team learns together

**We're excited to have you on the team!** 🚀

Your contributions will help make YoutubeRag better. Don't hesitate to ask questions, suggest improvements, and share your ideas.

---

## Next Steps

After completing this onboarding:

1. **Check with your mentor** - Schedule a check-in
2. **Pick your first real task** - From issue tracker
3. **Join the team rhythm** - Standups, planning, retros
4. **Start contributing** - Your ideas and code matter

**Good luck, and happy coding!** 💻

---

**Questions about this onboarding guide?**
- Create an issue: [GitHub Issues](https://github.com/gustavoali/YoutubeRag/issues)
- Ask in team chat
- Contact your mentor

**Want to improve this guide?**
- Submit a PR with improvements
- Share feedback with team lead
- Help make onboarding better for future team members
