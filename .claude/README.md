# .claude/ - YoutubeRag Development Methodology

**Version:** 2.0 - Claude Code Best Practices Integration
**Last Updated:** January 2025
**Purpose:** Comprehensive development methodology combining Claude Code best practices with specialized agent workflows

---

## 📋 Quick Navigation

| File | Purpose | When to Read |
|------|---------|--------------|
| **[../CLAUDE.md](../CLAUDE.md)** | 🌟 **Auto-loaded project memory** | Read first - loaded automatically by Claude |
| **[METHODOLOGY.md](METHODOLOGY.md)** | Complete development workflow | Starting any development work |
| **[AGENT_USAGE_GUIDELINES.md](AGENT_USAGE_GUIDELINES.md)** | Agent delegation patterns | Before delegating tasks |
| **[CONTEXT_MANAGEMENT.md](CONTEXT_MANAGEMENT.md)** | Token optimization strategies | When context feels bloated |
| **[commands/](commands/)** | Custom slash commands | Browse for reusable workflows |
| **[settings.local.json](settings.local.json)** | Tool permissions | Rarely - for permission issues |

---

## 🎯 What is This Directory?

The `.claude/` directory contains the **development methodology** for the YoutubeRag project when working with Claude Code. It integrates:

1. **Claude Code Official Best Practices** - From Anthropic's engineering team
2. **Specialized Agent Workflows** - 11+ agent types for parallel work
3. **Context Management Strategies** - 40% token reduction techniques
4. **Custom Commands** - Reusable workflows via slash commands
5. **Project-Specific Patterns** - YoutubeRag Clean Architecture guidelines

---

## 🚀 Quick Start Guide

### For New Team Members

1. **Read CLAUDE.md first** (in project root)
   - Auto-loaded by Claude Code
   - Contains essential project context
   - Concise reference (<100 lines)

2. **Read METHODOLOGY.md second**
   - Complete development workflow
   - Explore → Plan → Code → Commit pattern
   - Quality gates and best practices

3. **Scan AGENT_USAGE_GUIDELINES.md third**
   - Understand agent delegation
   - Learn parallel work patterns
   - Review agent types available

4. **Explore commands/ directory**
   - See available slash commands
   - Try `/review-epic`, `/run-tests`, etc.

5. **Bookmark CONTEXT_MANAGEMENT.md**
   - Reference when context feels wrong
   - Learn /clear usage patterns
   - Optimize token usage

### For Experienced Team Members

Jump directly to relevant sections:
- Working on Epic → `/review-epic <number>`
- Running tests → `/run-tests <category>`
- Creating PR → `/prepare-pr`
- Performance issues → `/analyze-performance`
- General health → `/check-health`

---

## 📚 File Descriptions

### Core Methodology Files

#### [../CLAUDE.md](../CLAUDE.md) ⭐ AUTO-LOADED
**Purpose:** Project memory that Claude reads automatically every session

**Contents:**
- Project overview (architecture, tech stack)
- Core development principles
- Project structure (key directories)
- Common commands
- Common pitfalls & patterns
- Current sprint context

**Keep it:** <100 lines, essential info only, project-specific

**Update when:**
- Architecture changes
- New critical patterns emerge
- Common pitfalls discovered
- Tech stack changes

---

#### [METHODOLOGY.md](METHODOLOGY.md)
**Purpose:** Complete development methodology and workflow

**Contents:**
- Core principles (Explore → Plan → Code → Commit)
- Workflow patterns (feature dev, bug fix, refactoring)
- Agent delegation strategy
- Context management
- Git & GitHub integration
- Testing strategy
- Quality assurance
- Performance optimization
- Team collaboration

**Size:** Comprehensive (~800 lines)

**Read when:**
- Starting development work
- Onboarding new team member
- Establishing workflow patterns
- Need quality gate checklist

---

#### [AGENT_USAGE_GUIDELINES.md](AGENT_USAGE_GUIDELINES.md)
**Purpose:** Detailed agent delegation patterns and best practices

**Contents:**
- 11 specialized agent types
- When/how to delegate
- Parallel work patterns
- Context management for agents
- Delegation templates
- Metrics and troubleshooting
- Advanced patterns (fan-out, pipeline, verification)

**Size:** Comprehensive (~800 lines)

**Read when:**
- Before delegating tasks
- Planning parallel work
- Optimizing development velocity
- Need agent-specific guidance

---

#### [CONTEXT_MANAGEMENT.md](CONTEXT_MANAGEMENT.md)
**Purpose:** Token optimization and context window management

**Contents:**
- Understanding context windows
- Token budget optimization
- File loading strategies
- /clear usage patterns
- Subagent patterns for context preservation
- Multi-session workflows
- Anti-patterns to avoid

**Size:** Comprehensive (~600 lines)

**Read when:**
- Context feels polluted
- Responses slow or degraded
- Token budget warnings
- Working on large tasks
- Need to preserve main context

---

### Custom Commands

#### [commands/](commands/)
**Purpose:** Reusable workflow templates as slash commands

**Available Commands:**

| Command | Purpose | Usage |
|---------|---------|-------|
| `/review-epic` | Comprehensive Epic review | `/review-epic 3` |
| `/run-tests` | Execute test suite | `/run-tests integration` |
| `/prepare-pr` | Create pull request | `/prepare-pr` |
| `/check-health` | Project health check | `/check-health` |
| `/analyze-performance` | Performance analysis | `/analyze-performance api` |

**Creating New Commands:**
1. Create `.md` file in `commands/`
2. Use `$ARGUMENTS` for parameters
3. Document in this README
4. Commit to share with team

**Example:**
```markdown
# commands/my-command.md

Task: Do something with $ARGUMENTS

Steps:
1. ...
2. ...
```

Usage: `/my-command <argument>`

---

### Configuration Files

#### [settings.local.json](settings.local.json)
**Purpose:** Tool permission configuration

**Contents:**
- Allowed Bash commands
- Web search permissions
- Read file permissions
- Tool access control

**Modify when:**
- Need to allowlist new commands
- Permission issues arise
- Security requirements change

**Note:** Personal file, not shared (in `.gitignore`)

---

## 🎓 Learning Path

### Level 1: Beginner (First Week)

**Goal:** Understand basic workflow and project structure

**Read:**
1. ✅ CLAUDE.md (project context)
2. ✅ METHODOLOGY.md sections 1-3 (principles and basic workflows)
3. ✅ Try one custom command (`/check-health`)

**Practice:**
- Fix a simple bug following TDD workflow
- Run tests with `/run-tests`
- Create a small feature using Explore → Plan → Code → Commit

---

### Level 2: Intermediate (First Month)

**Goal:** Master agent delegation and parallel work

**Read:**
1. ✅ AGENT_USAGE_GUIDELINES.md (complete)
2. ✅ CONTEXT_MANAGEMENT.md sections 1-4
3. ✅ All custom commands

**Practice:**
- Delegate task to specialized agent
- Run 2 agents in parallel
- Use `/review-epic` for Epic validation
- Create PR with `/prepare-pr`

---

### Level 3: Advanced (Ongoing)

**Goal:** Optimize velocity and quality through advanced patterns

**Read:**
1. ✅ All methodology files (complete understanding)
2. ✅ Advanced Patterns sections
3. ✅ Troubleshooting sections

**Practice:**
- Fan-out/fan-in pattern for large refactoring
- Independent verification pattern
- Context preservation with subagents
- Create custom commands for team

---

## 🔄 Workflow Integration

### Typical Development Session

```mermaid
graph TB
    A[Start Session] --> B{Context Clean?}
    B -->|No| C[/clear]
    B -->|Yes| D[Load CLAUDE.md]
    C --> D
    D --> E[Read Task/Issue]
    E --> F{Complex Task?}
    F -->|Yes| G[Use 'think harder']
    F -->|No| H[Plan Approach]
    G --> H
    H --> I{Should Delegate?}
    I -->|Yes| J[Delegate to Agent]
    I -->|No| K[Implement Directly]
    J --> L[Parallel Work]
    K --> L
    L --> M[Run Tests]
    M --> N{Tests Pass?}
    N -->|No| O[Debug/Fix]
    O --> M
    N -->|Yes| P[Commit]
    P --> Q{More Work?}
    Q -->|Yes| R{Related Task?}
    R -->|Yes| E
    R -->|No| C
    Q -->|No| S[Create PR]
    S --> T[End Session]
```

### Key Decision Points

**When to use /clear:**
- ✅ Starting new session
- ✅ Switching between unrelated tasks
- ✅ Context feels polluted (Claude references old tasks)
- ✅ Token budget warnings
- ❌ Middle of implementation
- ❌ Debugging specific issue (need history)

**When to delegate:**
- ✅ Task >30 minutes
- ✅ Task can run in parallel
- ✅ Need specialized expertise
- ✅ Want independent verification
- ❌ Simple 5-minute task
- ❌ Need personal context from conversation

**When to use extended thinking:**
- ✅ Complex architectural decisions
- ✅ Multiple valid approaches exist
- ✅ Non-obvious trade-offs
- ✅ Epic planning
- ❌ Simple bug fixes
- ❌ Straightforward implementations

---

## 📊 Methodology Metrics

### Success Indicators

Track these to measure methodology effectiveness:

| Metric | Target | Current | Trend |
|--------|--------|---------|-------|
| Test Coverage | >95% | 99.3% | ✅ Excellent |
| Agent Delegation Rate | 60-80% | - | Track |
| Context /clear Usage | 3-5/session | - | Track |
| Parallel Agent Work | 2-3/session | - | Track |
| PR Quality Score | >8/10 | - | Track |
| Build Success Rate | >95% | - | Track |

### Quality Gates

**Before Commit:**
- [ ] All tests passing
- [ ] No compiler warnings
- [ ] Code formatted (pre-commit hook)
- [ ] Clean Architecture respected

**Before PR:**
- [ ] All commits have good messages
- [ ] Documentation updated
- [ ] Performance validated (if applicable)
- [ ] Security reviewed

**Before Merge:**
- [ ] CI pipeline green
- [ ] Code reviewed
- [ ] Acceptance criteria met
- [ ] No breaking changes (or documented)

---

## 🛠️ Troubleshooting

### Common Issues

#### Issue: "Claude doesn't seem to remember project structure"

**Solution:** Claude auto-loads CLAUDE.md each session. Ensure:
1. File exists at project root: `/home/user/YoutubeRag/CLAUDE.md`
2. File is <100 lines (Claude loads it fully)
3. Essential info is documented there

#### Issue: "Responses are slow or degraded"

**Diagnosis:** Context window likely full

**Solution:**
1. Use `/clear` to reset context
2. Load only essential files
3. Review CONTEXT_MANAGEMENT.md
4. Check if you should use subagent instead

#### Issue: "Agent didn't understand delegation"

**Diagnosis:** Insufficient specificity

**Solution:**
1. Review AGENT_USAGE_GUIDELINES.md section on specificity
2. Provide concrete targets, not vague requests
3. Include verification steps
4. Specify expected output format

#### Issue: "Tests failing after changes"

**Diagnosis:** TDD not followed or breaking change

**Solution:**
1. Use `/run-tests` to get detailed output
2. Review test failures
3. Fix implementation or update tests
4. Ensure all tests pass before commit

---

## 🔗 External Resources

### Official Claude Code Documentation
- [Best Practices](https://www.anthropic.com/engineering/claude-code-best-practices)
- [CLAUDE.md Guide](https://docs.claude.com) - Check for latest docs

### YoutubeRag Project Documentation
- [README.md](../README.md) - Project overview
- [DEVELOPER_SETUP_GUIDE.md](../docs/devops/DEVELOPER_SETUP_GUIDE.md)
- [DEVOPS_IMPLEMENTATION_PLAN.md](../docs/devops/DEVOPS_IMPLEMENTATION_PLAN.md)
- [TEST_RESULTS_REPORT.md](../TEST_RESULTS_REPORT.md)

---

## 🔄 Maintaining This Methodology

### When to Update

**CLAUDE.md:**
- New critical patterns discovered
- Architecture changes
- Tech stack additions
- Common pitfalls identified
- Sprint context changes

**METHODOLOGY.md:**
- Workflow improvements identified
- New quality gates needed
- Integration points change
- Best practices evolve

**AGENT_USAGE_GUIDELINES.md:**
- New agent types added
- Delegation patterns improve
- Team learns new techniques
- Metrics show issues

**CONTEXT_MANAGEMENT.md:**
- New optimization techniques
- Token usage patterns change
- New anti-patterns discovered

**commands/:**
- Repetitive workflows identified
- Team requests new commands
- Existing commands improved

### How to Update

1. **Propose Change:**
   - Create issue or discuss with team
   - Explain rationale
   - Show examples

2. **Draft Update:**
   - Edit relevant file(s)
   - Maintain structure and style
   - Update version history

3. **Review:**
   - Code review like any PR
   - Validate with examples
   - Test if applicable (for commands)

4. **Commit:**
   - Descriptive commit message
   - Reference issue/discussion
   - Update "Last Updated" date

5. **Communicate:**
   - Announce to team
   - Add to sprint notes
   - Update onboarding if major change

---

## 📝 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0 | January 2025 | Complete rewrite: Integrated Claude Code best practices, added comprehensive documentation, created custom commands |
| 1.0 | October 2024 | Initial methodology with agent guidelines |

---

## 🎯 Methodology Philosophy

This methodology is built on these principles:

1. **Specificity Drives Results** - Concrete targets produce better outcomes than vague requests
2. **Context is Precious** - Manage token budget aggressively with /clear and subagents
3. **Parallelism Wins** - 2-3 agents working simultaneously beats sequential work
4. **Quality Gates Matter** - TDD, code review, and testing are non-negotiable
5. **Continuous Improvement** - Methodology evolves based on team learning

**Remember:** These guidelines exist to help you work more effectively with Claude Code. They're not rigid rules - adapt them to your specific needs while maintaining the core principles.

---

## 🆘 Getting Help

**For methodology questions:**
1. Re-read relevant section in this directory
2. Ask Claude: "How should I handle X according to the methodology?"
3. Discuss with team
4. Update methodology if answer is non-obvious

**For technical issues:**
1. Check [Troubleshooting](#troubleshooting) section
2. Review project documentation in `docs/`
3. Use `/check-health` to diagnose
4. Create issue if needed

**For Claude Code issues:**
1. Check official Claude Code documentation
2. Use `/help` command
3. Report at https://github.com/anthropics/claude-code/issues

---

**Welcome to the YoutubeRag development methodology!** 🚀

This is a living methodology that improves through your feedback and contributions. Help make it better by suggesting improvements when you discover better patterns or approaches.
