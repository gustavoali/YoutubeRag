# Analyze Performance

**Description:** Analyze performance metrics and identify bottlenecks

**Usage:** `/analyze-performance` or `/analyze-performance <component>`
- `api` - API endpoint performance
- `database` - Database query performance
- `jobs` - Background job performance
- `transcription` - Transcription performance

---

## Task: Performance Analysis

Component: $ARGUMENTS (or ALL if not specified)

### Step 1: Gather Performance Data

#### A. API Performance

```bash
# Check if API is running
curl http://localhost:5000/health

# If running, check metrics endpoint (if Prometheus enabled)
curl http://localhost:5000/metrics

# Review recent logs for slow requests
# (grep for request duration in logs)
```

**Analyze:**
- Response times (p50, p95, p99)
- Endpoint-specific metrics
- Error rates
- Throughput

#### B. Database Performance

```bash
# Connect to MySQL
docker exec -it mysql mysql -u root -p

# Run in MySQL:
# Check slow queries
SELECT * FROM mysql.slow_log LIMIT 10;

# Check table sizes
SELECT
  table_name AS 'Table',
  round(((data_length + index_length) / 1024 / 1024), 2) AS 'Size (MB)'
FROM information_schema.TABLES
WHERE table_schema = 'youtuberag'
ORDER BY (data_length + index_length) DESC;

# Check index usage
SHOW INDEX FROM Videos;
SHOW INDEX FROM Transcripts;
```

**Analyze:**
- Slow queries (>100ms)
- Missing indexes
- Table sizes
- Query patterns

#### C. Background Jobs Performance

```bash
# Access Hangfire dashboard
# http://localhost:5000/hangfire

# Review job statistics
# - Average execution time
# - Failed job rate
# - Queue lengths
```

**Analyze:**
- Job execution times
- Failed jobs
- Queue backlogs
- Resource usage

#### D. Transcription Performance

```bash
# Search logs for transcription metrics
# grep for "Transcription completed" or similar

# Analyze:
# - Time per minute of audio
# - Model performance (tiny/base/small)
# - Success/failure rates
```

---

### Step 2: Performance Report

Generate comprehensive report:

```markdown
# Performance Analysis Report

**Date:** [Date]
**Component:** $ARGUMENTS
**Analyzer:** Claude Code

---

## Executive Summary

**Overall Performance:** [EXCELLENT/GOOD/FAIR/POOR]

**Key Findings:**
- [Finding 1]
- [Finding 2]
- [Finding 3]

**Critical Issues:** [count]
**Recommendations:** [count]

---

## Detailed Analysis

### API Performance

| Endpoint | p50 | p95 | p99 | Status |
|----------|-----|-----|-----|--------|
| GET /api/videos | [ms] | [ms] | [ms] | [✅/⚠️/❌] |
| POST /api/videos/from-url | [ms] | [ms] | [ms] | [✅/⚠️/❌] |
| POST /api/search/semantic | [ms] | [ms] | [ms] | [✅/⚠️/❌] |

**Threshold:** <200ms (p95)

**Issues:**
- [Slow endpoint 1]: [reason]
- [Slow endpoint 2]: [reason]

**Recommendations:**
1. [Optimization 1]
2. [Optimization 2]

---

### Database Performance

**Slow Queries:** [count]

| Query | Execution Time | Table | Recommendation |
|-------|----------------|-------|----------------|
| [Query 1] | [ms] | [Table] | Add index on [column] |
| [Query 2] | [ms] | [Table] | Optimize JOIN |

**Index Analysis:**
- ✅ Well-indexed: Videos.UserId, Videos.Status
- ⚠️ Missing indexes: [list]
- ❌ Unused indexes: [list]

**Table Size Analysis:**
| Table | Size (MB) | Growth Rate | Status |
|-------|-----------|-------------|--------|
| Videos | [size] | [rate] | [✅/⚠️] |
| Transcripts | [size] | [rate] | [✅/⚠️] |
| TranscriptSegments | [size] | [rate] | [✅/⚠️] |

---

### Background Jobs Performance

**Job Statistics:**
- Average Execution Time: [duration]
- Failed Jobs (24h): [count]
- Queue Length: [count]
- Throughput: [jobs/hour]

**Slow Jobs:**
| Job Type | Avg Duration | Status |
|----------|--------------|--------|
| TranscriptionJob | [duration] | [✅/⚠️/❌] |
| VideoProcessingJob | [duration] | [✅/⚠️/❌] |
| EmbeddingGenerationJob | [duration] | [✅/⚠️/❌] |

**Issues:**
[List job-specific issues]

---

### Transcription Performance

**Metrics:**
- Average Time per Audio Minute: [seconds]
- Success Rate: [percentage]%
- Model Distribution:
  - tiny: [percentage]%
  - base: [percentage]%
  - small: [percentage]%

**Performance by Model:**
| Model | Time/Min | Accuracy | Recommendation |
|-------|----------|----------|----------------|
| tiny | [s] | Good | Use for quick transcripts |
| base | [s] | Better | Balanced choice |
| small | [s] | Best | Use for accuracy-critical |

---

### Resource Usage

**CPU:**
- Average: [percentage]%
- Peak: [percentage]%
- Status: [✅/⚠️/❌]

**Memory:**
- Average: [MB/GB]
- Peak: [MB/GB]
- Status: [✅/⚠️/❌]

**Disk:**
- Usage: [GB]
- Growth: [GB/day]
- Status: [✅/⚠️/❌]

**Database Connections:**
- Active: [count]
- Max: [count]
- Status: [✅/⚠️/❌]

---

## Bottleneck Analysis

### Top 5 Bottlenecks

1. **[Bottleneck 1]**
   - Component: [API/DB/Job/etc]
   - Impact: [High/Medium/Low]
   - Current Performance: [metric]
   - Expected Performance: [metric]
   - Root Cause: [explanation]
   - Fix Complexity: [Easy/Medium/Hard]

2. **[Bottleneck 2]**
   [Same structure]

[Continue for all 5]

---

## Optimization Recommendations

### High Priority (Immediate Action)
1. **[Optimization 1]**
   - Expected Improvement: [percentage]% faster
   - Effort: [hours/days]
   - Complexity: [Low/Medium/High]
   - Implementation: [brief description]

2. **[Optimization 2]**
   [Same structure]

### Medium Priority (This Sprint)
[List medium priority optimizations]

### Low Priority (Backlog)
[List low priority optimizations]

---

## Performance Testing Recommendations

1. **Load Testing:**
   - Tool: k6
   - Scenarios: [list scenarios]
   - Target: [users/requests]

2. **Stress Testing:**
   - Identify breaking point
   - Test resource limits
   - Verify graceful degradation

3. **Endurance Testing:**
   - Duration: 24 hours
   - Monitor: Memory leaks, connection pools
   - Verify: Stability over time

---

## Action Plan

### Immediate (Today)
- [ ] [Action 1]
- [ ] [Action 2]

### Short-term (This Week)
- [ ] [Action 3]
- [ ] [Action 4]

### Long-term (This Month)
- [ ] [Action 5]
- [ ] [Action 6]

---

## Metrics to Monitor

**Track these metrics:**
1. API response time (p95): Target <200ms
2. Database query time: Target <50ms
3. Job execution time: Target <5min
4. Error rate: Target <1%
5. Throughput: Target [req/s]

**Dashboard:** http://localhost:3001 (Grafana)
**Alerts:** Configure for thresholds
```

---

### Step 3: Delegate Deep Analysis

For complex performance issues, delegate to specialized agents:

```markdown
Delegating to database-expert:
- Optimize slow queries identified
- Review index strategy
- Suggest schema improvements

Delegating to backend-developer:
- Implement caching for slow endpoints
- Optimize async patterns
- Review algorithm complexity
```

---

### Step 4: Generate Performance Test Plan

If performance issues found:

```markdown
# Performance Test Plan

## Objectives
- Verify optimizations work
- Establish baseline metrics
- Identify regression thresholds

## Test Scenarios
1. [Scenario 1]
2. [Scenario 2]

## Tools
- k6 for load testing
- Prometheus for metrics
- Grafana for visualization

## Success Criteria
- p95 response time <200ms
- Error rate <1%
- Throughput >100 req/s
```

---

**Notes:**
- Run during off-peak hours if testing production
- Establish baseline before optimizations
- Re-run tests after optimizations to verify improvements
- Document findings for future reference
