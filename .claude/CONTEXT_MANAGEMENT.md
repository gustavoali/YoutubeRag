# Context Management Guide - Optimizing Claude's Token Usage

**Version:** 1.0
**Last Updated:** January 2025
**Goal:** Achieve ~40% token reduction while maintaining or improving code quality

---

## 📋 Table of Contents

1. [Understanding Context Windows](#understanding-context-windows)
2. [Token Budget Optimization](#token-budget-optimization)
3. [File Loading Strategies](#file-loading-strategies)
4. [Context Clearing Best Practices](#context-clearing-best-practices)
5. [Subagent Pattern for Context Preservation](#subagent-pattern-for-context-preservation)
6. [Multi-Session Workflows](#multi-session-workflows)
7. [Anti-Patterns to Avoid](#anti-patterns-to-avoid)

---

## Understanding Context Windows

### What is Context?

**Context** = All information Claude has available when generating a response:
- Conversation history (all previous messages)
- Loaded files (via @ references or file reads)
- Auto-loaded files (CLAUDE.md, project structure)
- System prompts and instructions

### Why Context Management Matters

```markdown
❌ Poor Context Management:
- Loads entire directories into context
- Never uses /clear between tasks
- Keeps 2 hours of conversation history for 5-minute task
- Result: Slower responses, higher costs, reduced accuracy

✅ Good Context Management:
- Loads only relevant files
- Uses /clear between unrelated tasks
- Focused context on current task
- Result: 40% token reduction, better quality responses
```

### Token Budget

Claude has a **finite context window**. Think of it like RAM:
- Limited capacity
- More context = slower processing
- Irrelevant context = noise that reduces accuracy

**Analogy:**
```
Poor context management = Running 50 browser tabs on 4GB RAM
Good context management = Running 3 focused tabs on 4GB RAM
```

---

## Token Budget Optimization

### Context Size Guidelines by Task Type

| Task Type | Context Size | Files to Load | Strategy |
|-----------|-------------|---------------|----------|
| **Bug Fix** | 🟢 Small | 1-3 files | Load only failing file + related test |
| **Feature (Small)** | 🟡 Medium | 5-10 files | Load incrementally as needed |
| **Feature (Large)** | 🔴 Large | 10+ files | Use subagents for exploration |
| **Architecture Review** | 🔴 Very Large | Project-wide | Use multiple subagents in parallel |
| **Refactoring** | 🟡 Medium | Affected files only | Work in small batches, commit frequently |
| **Code Review** | 🟢 Small | Changed files only | Load git diff output |

### Calculating Approximate Token Usage

**Rule of Thumb:**
- 1 token ≈ 4 characters
- Average C# file (200 lines) ≈ 1,000-1,500 tokens
- Conversation message ≈ 100-500 tokens

**Example:**
```bash
Current Context Estimate:
- CLAUDE.md (auto-loaded): ~800 tokens
- Conversation (10 messages): ~2,000 tokens
- 3 loaded files (200 lines each): ~4,000 tokens
- Total: ~6,800 tokens

After /clear:
- CLAUDE.md (auto-loaded): ~800 tokens
- New conversation: ~200 tokens
- Total: ~1,000 tokens

Savings: 85% token reduction!
```

---

## File Loading Strategies

### Strategy 1: Explicit File Loading (Recommended)

```markdown
✅ GOOD: Request specific files only when needed

Example conversation:
User: "Fix the bug in VideoService where videos aren't processing"

Claude: "I'll need to see the VideoService implementation.
         Please let me read the file."
[Reads YoutubeRag.Application/Services/VideoService.cs only]

Claude: "I see the issue. The ProcessVideoAsync method isn't
         handling HttpRequestException. Let me also check the tests."
[Reads YoutubeRag.Tests.Integration/Services/VideoServiceTests.cs]

Claude: "Now I have enough context. Here's the fix..."
```

**Advantages:**
- Minimal token usage
- Focused context
- Faster responses
- Easy to understand what's in context

### Strategy 2: @ References (Selective Loading)

```markdown
Use tab-completion to reference specific files:

@src/Services/VideoService.cs
@tests/Integration/VideoServiceTests.cs

This tells Claude: "These files are relevant, load them if needed"
```

### Strategy 3: Directory Exploration (Use Sparingly)

```markdown
❌ AVOID: Loading entire directories

# Bad:
@src/Services/

# Good:
@src/Services/VideoService.cs
@src/Services/TranscriptionService.cs

Only load directories when:
- Architecture review needed
- Refactoring multiple related files
- Initial exploration of unknown codebase
```

### Strategy 4: Incremental Loading

```markdown
✅ Best Practice: Load files incrementally as investigation deepens

1. Start: Load only the entry point
   └─ @src/Api/Controllers/VideoController.cs

2. Dependency: Load service used by controller
   └─ @src/Application/Services/VideoService.cs

3. Deep Dive: Load repository if needed
   └─ @src/Infrastructure/Repositories/VideoRepository.cs

4. Testing: Load test file
   └─ @tests/Integration/VideoServiceTests.cs

Total: 4 files loaded over 5 minutes
Alternative: Load all 50 files upfront → 90% waste
```

---

## Context Clearing Best Practices

### When to Use `/clear`

**ALWAYS clear context when:**
1. ✅ Switching between unrelated tasks
   - Example: Finished Epic 2, starting Epic 3
2. ✅ Starting a new work session
   - Example: Came back after lunch break
3. ✅ Context feels polluted
   - Example: Claude references unrelated previous tasks
4. ✅ Receiving token budget warnings
5. ✅ Changing problem domains
   - Example: Switching from API work to database optimization

**NEVER clear context when:**
1. ❌ In the middle of implementing a feature
2. ❌ Debugging an issue (need conversation history)
3. ❌ Iterating on code review feedback

### How to Use `/clear` Effectively

```bash
# Scenario 1: Completed Epic 2, starting Epic 3
User: "Epic 2 is done. Let's start Epic 3"
Claude: "Great! Before we start Epic 3, I'll clear the context
         to ensure optimal performance."
[Uses /clear]
Claude: "Context cleared. CLAUDE.md reloaded. Ready for Epic 3."

# Scenario 2: Context feels bloated
User: "Fix the bug in TranscriptionService"
Claude: [References something from 30 minutes ago about VideoService]
User: "/clear"
Claude: "Context cleared. Please describe the bug in TranscriptionService again."

# Scenario 3: Proactive clearing
Claude: "I've completed the code review. Before starting the next task,
         I'll clear context to maintain optimal performance."
[Uses /clear]
```

### What Happens After `/clear`?

```markdown
Cleared:
❌ Conversation history (all previous messages)
❌ Loaded files (via @ or file reads)
❌ Current task context

Preserved:
✅ CLAUDE.md (automatically reloaded)
✅ Project structure knowledge (from CLAUDE.md)
✅ Your git working directory state

Result:
- Fresh start with essential project knowledge
- ~80-90% token reduction
- Faster, more focused responses
```

---

## Subagent Pattern for Context Preservation

### What are Subagents?

**Subagents** = Separate Claude instances running in parallel

**Use Cases:**
1. Preserve main context while exploring
2. Run multiple tasks in parallel
3. Get independent verification (avoid overfitting)

### Pattern 1: Exploratory Subagent (Context Preservation)

```markdown
Scenario: You're implementing Feature A, but need to understand Feature B

❌ BAD Approach:
1. Load Feature A files (5 files in context)
2. Load Feature B files (10 more files → 15 total)
3. Context now polluted with Feature B
4. Return to Feature A implementation (Feature B noise reduces quality)

✅ GOOD Approach (Subagent):
1. Main Agent: Working on Feature A (5 files in context)
2. Spawn Subagent: "Explore Feature B and report back"
3. Subagent: Loads Feature B files independently
4. Subagent: Reports findings to main agent
5. Main Agent: Context still clean (5 files only)
```

**Example:**
```bash
Main Agent Context:
- Epic 2: Implementing semantic search
- Files: SemanticSearchService.cs, SearchController.cs
- Status: In progress

User: "How does the caching work in Epic 1?"

Main Agent: "I'll delegate this investigation to a subagent
             to preserve my current context on Epic 2."

[Spawns Subagent]
Subagent: [Loads caching files, investigates, reports back]
Main Agent: [Receives report, continues Epic 2 work]

Result: Main agent context preserved, question answered
```

### Pattern 2: Parallel Implementation (Velocity)

```markdown
Scenario: Need to implement 3 independent features

❌ SEQUENTIAL Approach:
Feature A → Feature B → Feature C
Time: 3 + 3 + 3 = 9 hours

✅ PARALLEL Approach (Multi-Subagent):
Subagent 1 → Feature A (3 hours) ┐
Subagent 2 → Feature B (3 hours) ├─ Run in parallel
Subagent 3 → Feature C (3 hours) ┘
Time: 3 hours

Result: 66% time reduction
```

### Pattern 3: Independent Verification (Quality)

```markdown
Scenario: Ensure implementation isn't overfitting to tests

Main Agent:
1. Wrote tests for VideoService
2. Implemented VideoService to pass tests
3. All tests green ✅

Subagent (Independent Verification):
"Review VideoService implementation without seeing the tests.
 Does it look correct? Any edge cases missed?"

Result: Catch overfitting, identify missing edge cases
```

### How to Spawn Subagents

```bash
# Via Task delegation (see AGENT_USAGE_GUIDELINES.md)
"Delegating investigation of caching mechanism to
 backend-developer subagent"

# Via explicit request
"Please spawn a subagent to explore the authentication
 flow while I continue working on video processing"

# Via parallel work
"I need these 3 features implemented in parallel:
 - Feature A: Video upload validation
 - Feature B: Transcript export to PDF
 - Feature C: User notification preferences

 Please delegate to 3 separate subagents"
```

---

## Multi-Session Workflows

### Git Worktrees for Parallel Work

```bash
# Create separate worktrees for independent tasks
git worktree add ../youtuberag-epic2 feature/epic2
git worktree add ../youtuberag-epic3 feature/epic3
git worktree add ../youtuberag-epic4 feature/epic4

# Launch Claude in each worktree (separate terminals)
cd ../youtuberag-epic2 && claude  # Terminal 1
cd ../youtuberag-epic3 && claude  # Terminal 2
cd ../youtuberag-epic4 && claude  # Terminal 3

# Each Claude instance has:
- Separate context (no cross-contamination)
- Separate git branch
- Separate file state

# Cleanup when done
git worktree remove ../youtuberag-epic2
```

**Advantages:**
- 100% isolated contexts
- Work on multiple branches simultaneously
- No context switching overhead
- True parallel development

### Session Handoff Strategy

```markdown
When you need to pause and resume later:

Before Pause:
1. Commit current work
2. Create a session note:

   SESSION_NOTES.md:
   ```
   ## 2025-01-15 14:30 - Epic 3 Implementation

   **Current Status:**
   - Implemented AudioExtractionService (committed)
   - In progress: TranscriptionJobProcessor
   - Next: Add progress tracking

   **Context:**
   - Files in context: TranscriptionJobProcessor.cs, IProgressTracker.cs
   - Test status: 10/12 passing (2 failing expected until complete)

   **To Resume:**
   1. Load TranscriptionJobProcessor.cs
   2. Load IProgressTracker.cs
   3. Continue implementing progress tracking
   ```

After Resume:
1. Use /clear to start fresh
2. Read SESSION_NOTES.md
3. Load only files mentioned in notes
4. Continue work

Result: Fast context restoration, minimal token waste
```

---

## Anti-Patterns to Avoid

### ❌ Anti-Pattern 1: Directory Dumping

```bash
# BAD: Loading entire directories
@src/
@tests/

Result: 100+ files in context, 99% irrelevant
Token usage: 50,000+ tokens
Response quality: Poor (too much noise)

# GOOD: Selective loading
@src/Services/VideoService.cs
@tests/Integration/VideoServiceTests.cs

Result: 2 files in context, 100% relevant
Token usage: 2,000 tokens
Response quality: Excellent
```

### ❌ Anti-Pattern 2: Never Clearing Context

```markdown
Symptom:
- 2-hour conversation history
- Discussed 5 different epics
- Currently working on Epic 6
- Claude references Epic 1 details (irrelevant)

Problem:
- Context window 90% full
- Responses slow
- Quality degraded

Solution:
- Use /clear between epics
- Keep conversations focused
- Start fresh for new major tasks
```

### ❌ Anti-Pattern 3: Premature Exploration

```bash
# BAD: Explore entire codebase before starting task
"Show me how all services work, all repositories,
 all controllers, and all jobs"

Result:
- 1 hour of exploration
- 50 files in context
- Still don't know where to start
- Context polluted before task begins

# GOOD: Just-in-time exploration
"I need to fix bug in VideoService"
1. Read VideoService.cs only
2. Identify issue
3. Read test file
4. Fix bug
5. Done in 10 minutes
```

### ❌ Anti-Pattern 4: Copy-Paste Entire Files

```markdown
# BAD:
User: [Pastes 500 lines of VideoService.cs]
      "What's wrong with this?"

Result: Wasted tokens on entire file

# GOOD:
User: "VideoService.ProcessVideoAsync throws NullReferenceException
       when url is null. Please investigate."

Claude: "Let me read the VideoService.cs file"
[Reads file selectively, finds issue]

Result: Optimal token usage
```

---

## Quick Reference: Context Management Checklist

### Before Starting a Task
- [ ] Is my context clear? (If not: `/clear`)
- [ ] Do I have leftover files from previous task? (If yes: `/clear`)
- [ ] Is this task related to current context? (If no: `/clear`)

### During Task Execution
- [ ] Am I loading only necessary files?
- [ ] Can I defer loading some files until needed?
- [ ] Is this exploration better done by a subagent?

### After Completing a Task
- [ ] Should I clear context before next task?
- [ ] Did I learn any patterns to add to CLAUDE.md?
- [ ] Should I document session state if pausing?

### When Context Feels Wrong
- [ ] Is Claude referencing unrelated previous tasks? → `/clear`
- [ ] Are responses slower than usual? → `/clear`
- [ ] Getting token budget warnings? → `/clear`
- [ ] Context window >50% full? → Consider `/clear`

---

## Metrics to Track

### Token Efficiency Metrics

```markdown
Good Context Management Indicators:
✅ Average context window: <30% full
✅ Files loaded per task: <10
✅ /clear used: 3-5 times per work session
✅ Response time: Fast (<5 seconds)
✅ Subagents used: 2-3 per session

Poor Context Management Indicators:
❌ Average context window: >70% full
❌ Files loaded per task: >20
❌ /clear used: Never
❌ Response time: Slow (>10 seconds)
❌ Subagents used: Never
```

---

## Advanced Techniques

### Technique 1: Context Snapshots

```markdown
Create savepoints during complex tasks:

# Checkpoint 1: After architecture planning
git commit -m "arch: Design for Epic 3"
Create: CONTEXT_SNAPSHOT_1.md (current context state)

# Checkpoint 2: After implementation
git commit -m "feat: Implement Epic 3"
Create: CONTEXT_SNAPSHOT_2.md

# Restore later:
Read: CONTEXT_SNAPSHOT_2.md
Load: Files mentioned in snapshot
Continue: From checkpoint
```

### Technique 2: Context Budgeting

```markdown
Allocate token budget before starting:

Task: Implement Epic 3 (estimated 4 hours)
Token Budget: 20,000 tokens

Allocation:
- CLAUDE.md (auto): 800 tokens
- Conversation: 5,000 tokens (reserved)
- Files: 14,200 tokens (max ~10-14 files)

Monitor during task:
- Check context window usage
- If approaching limit → /clear and restart
- If under budget → continue
```

### Technique 3: Context Rotation

```markdown
For large refactoring tasks:

Batch 1: Refactor files 1-5
  - Load files 1-5
  - Refactor
  - Commit
  - /clear

Batch 2: Refactor files 6-10
  - Load files 6-10
  - Refactor
  - Commit
  - /clear

Result: Keep context fresh, maintain quality throughout large task
```

---

## Related Documentation

- [CLAUDE.md](../CLAUDE.md) - Auto-loaded project memory
- [METHODOLOGY.md](METHODOLOGY.md) - Complete development methodology
- [AGENT_USAGE_GUIDELINES.md](AGENT_USAGE_GUIDELINES.md) - Agent delegation patterns

---

**Remember:** Context is like RAM - precious and finite. Use it wisely, clear it frequently, delegate to subagents when appropriate.
