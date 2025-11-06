# Check Health

**Description:** Verify project health across all dimensions

**Usage:** `/check-health` or `/check-health <aspect>` where aspect is:
- `tests` - Test suite health
- `build` - Build status
- `coverage` - Code coverage
- `security` - Security scan status
- `performance` - Performance metrics
- `ci` - CI/CD pipeline status

---

## Task: Comprehensive Health Check

### 1. Test Suite Health

```bash
# Run all tests
dotnet test --configuration Release --verbosity minimal

# Analyze results
```

**Report:**
```markdown
### 🧪 Test Suite Health

- **Total Tests:** [count]
- **Passing:** [count] ([percentage]%)
- **Failing:** [count]
- **Skipped:** [count]

**Status:** [✅ HEALTHY | ⚠️ WARNING | ❌ CRITICAL]

**Issues:**
[List any failing tests with suggestions]
```

### 2. Build Health

```bash
# Clean and rebuild
dotnet clean
dotnet build --configuration Release

# Check for warnings
```

**Report:**
```markdown
### 🔨 Build Health

- **Status:** [✅ SUCCESS | ❌ FAILED]
- **Warnings:** [count]
- **Errors:** [count]

**Build Time:** [duration]

**Issues:**
[List warnings/errors with file locations]
```

### 3. Code Coverage

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --configuration Release

# Parse coverage report
```

**Report:**
```markdown
### 📊 Code Coverage

- **Line Coverage:** [percentage]%
- **Branch Coverage:** [percentage]%
- **Target:** >95%
- **Status:** [✅ MEETS TARGET | ⚠️ BELOW TARGET]

**Low Coverage Areas:**
- [File/Class name]: [percentage]%
- [File/Class name]: [percentage]%

**Recommendations:**
[Suggest areas to improve]
```

### 4. Security Status

```bash
# Check for security issues
git log --all --full-history --grep="security"

# Review dependencies for vulnerabilities
# (Manual check or CI scan results)
```

**Report:**
```markdown
### 🔒 Security Health

**Recent Security Changes:**
[List security-related commits]

**Known Vulnerabilities:**
[List any known security issues]

**Security Checklist:**
- [ ] No hardcoded secrets
- [ ] Dependencies up-to-date
- [ ] Input validation in place
- [ ] Authentication/authorization configured

**Status:** [✅ SECURE | ⚠️ REVIEW NEEDED | ❌ CRITICAL ISSUES]
```

### 5. Performance Metrics

```bash
# Check if application is running
# If yes, curl health endpoint
curl http://localhost:5000/health

# Check recent performance test results
# Look for performance test reports
```

**Report:**
```markdown
### ⚡ Performance Health

**API Health:** [✅ UP | ❌ DOWN]

**Recent Metrics:**
- Response Time (p95): [ms]
- Response Time (p50): [ms]
- Throughput: [req/s]

**Performance Tests:**
- Last Run: [date]
- Status: [PASS/FAIL]

**Status:** [✅ PERFORMING WELL | ⚠️ DEGRADED | ❌ CRITICAL]
```

### 6. CI/CD Pipeline Status

```bash
# Check recent CI runs
gh run list --limit 5

# Get latest run status
gh run view --log-failed
```

**Report:**
```markdown
### 🚀 CI/CD Health

**Recent Runs:**
1. [Run #1]: [Status] - [Date]
2. [Run #2]: [Status] - [Date]
3. [Run #3]: [Status] - [Date]

**Latest Pipeline Status:** [✅ PASSING | ❌ FAILING]

**Failed Jobs (if any):**
[List failed jobs with reasons]

**Status:** [✅ HEALTHY | ❌ BROKEN]
```

---

## Overall Health Report

After all checks, generate summary:

```markdown
# 🏥 YoutubeRag Project Health Report

**Generated:** [Date & Time]
**Branch:** [Current branch]

## Health Score: [Score/100]

### Summary

| Aspect | Status | Score |
|--------|--------|-------|
| Tests | [✅/⚠️/❌] | [/100] |
| Build | [✅/⚠️/❌] | [/100] |
| Coverage | [✅/⚠️/❌] | [/100] |
| Security | [✅/⚠️/❌] | [/100] |
| Performance | [✅/⚠️/❌] | [/100] |
| CI/CD | [✅/⚠️/❌] | [/100] |

### Overall Status
[✅ HEALTHY | ⚠️ NEEDS ATTENTION | ❌ CRITICAL ISSUES]

### Priority Actions
1. [Most urgent action]
2. [Second priority]
3. [Third priority]

### Recommendations
- [Long-term improvement 1]
- [Long-term improvement 2]

### Metrics Trend
- Tests: [Improving/Stable/Declining]
- Coverage: [Improving/Stable/Declining]
- Performance: [Improving/Stable/Declining]
```

---

## Quick Health Check (Default)

If no argument provided, run quick health check:

```bash
# Just verify core health
1. dotnet build --configuration Release
2. dotnet test --configuration Release --verbosity minimal
3. git status (check for uncommitted changes)

# Report brief summary
```

---

**Notes:**
- Use this before creating PRs
- Run after major changes
- Schedule weekly comprehensive checks
- Delegate specific health checks to specialized agents if needed
