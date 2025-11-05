# Sprint 11 - Capacity Planning Calculation

**Sprint:** 11
**Date:** 2025-10-20 to 2025-10-31 (10 working days)
**Methodology:** v2.0 Capacity Planning Formula
**Team:** 1 Developer (Technical Lead)

---

## 📐 Capacity Planning Formula (v2.0)

### Formula:

```
Capacity = Team Days × Hours/Day × Efficiency × Availability

Donde:
  Team Days: Number of developers × sprint duration in days
  Hours/Day: Productive hours per day (excluding meetings, breaks)
  Efficiency: Productive work percentage (0.0-1.0)
  Availability: Team availability (1.0 - unexpected absences/interruptions)
```

---

## 🧮 Sprint 11 Calculation

### Input Parameters:

| Parameter | Value | Justification |
|-----------|-------|---------------|
| **Developers** | 1 | Technical Lead (solo developer) |
| **Sprint Duration** | 10 días | Standard 2-week sprint |
| **Hours/Day** | 6 hours | 8h work - 1h lunch - 0.5h standup - 0.5h email/admin |
| **Efficiency** | 0.80 (80%) | Context switching, design time, learning |
| **Availability** | 0.98 (98%) | 2% for unexpected interruptions |

### Calculation:

```
Team Days = 1 developer × 10 días = 10 team-days

Capacity = 10 × 6 × 0.80 × 0.98
         = 10 × 6 × 0.784
         = 60 × 0.784
         = 47.04 hours

Rounded: 47 hours total capacity
```

---

## 📊 Capacity Allocation

### Buffer Strategy (20% buffer):

```
Total Capacity: 47 hours

Allocation:
  - Commitment (80%): 37.6 hours
  - Buffer (20%): 9.4 hours
```

### Buffer Breakdown:

| Buffer Category | Hours | Purpose |
|----------------|-------|---------|
| **Bug Fixes** | 4.0h | Fixing issues discovered during sprint |
| **Code Reviews** | 2.0h | Review cycles and feedback incorporation |
| **Documentation** | 2.0h | API docs, architecture updates |
| **Unexpected Issues** | 1.4h | Unplanned interruptions, blockers |
| **TOTAL** | 9.4h | - |

**Rationale for 20% buffer:**
- First sprint using new methodology (uncertainty)
- New domain (YouTube integration, FFmpeg)
- External dependencies (YoutubeExplode, FFmpeg)
- Complex error handling requirements

---

## 🎯 Story Points to Hours Mapping

### Fibonacci Scale Mapping (Conservative):

| Story Points | Hours | Complexity | Examples |
|-------------|-------|------------|----------|
| 1 pt | 1.5h | Trivial | Simple config change |
| 2 pts | 2.5h | Simple | Add validation rule |
| 3 pts | 4h | Low | Simple service method |
| 5 pts | 7h | Medium | US-101, US-103 |
| 8 pts | 12h | High | US-102 (download with retry) |
| 13 pts | 20h | Very High | Complex feature |

**Mapping Rationale:**
- Conservative estimates (1.8h average per story point)
- Includes implementation + testing + review
- Accounts for unexpected complexity
- Based on Clean Architecture overhead

### Sprint 11 Story Points:

| User Story | Story Points | Hours | % of Commitment |
|------------|-------------|-------|-----------------|
| US-101 | 5 pts | 7h | 18.6% |
| US-102 | 8 pts | 12h | 31.9% |
| US-103 | 5 pts | 7h | 18.6% |
| Buffer Tasks | 3 pts | 4h | 10.6% |
| **TOTAL** | **21 pts** | **30h** | **79.8%** |

**Remaining Commitment Capacity:** 37.6h - 30h = 7.6h (20% slack)

**Total with Buffer:** 30h + 9.4h = 39.4h (within 47h capacity ✅)

---

## ✅ Capacity Validation

### Checks:

1. **Total Load Check:**
   - Planned: 39.4h
   - Capacity: 47h
   - Utilization: 83.8% ✅ (healthy range 75-90%)

2. **Commitment Check:**
   - Committed: 30h
   - Commitment Capacity: 37.6h
   - Utilization: 79.8% ✅ (safe under 80%)

3. **Story Points Check:**
   - Planned: 21 pts
   - Average: 1.8h per point
   - Total: 37.8h ✅ (fits in commitment)

4. **Buffer Check:**
   - Buffer: 9.4h (20%)
   - Target: 15-25% buffer ✅
   - Sufficient for first sprint with unknowns

**Result:** ✅ ALL CHECKS PASSED - Capacity planning is healthy

---

## 📈 Velocity Baseline

### Sprint 11 Velocity Prediction:

**Scenario 1: Optimistic (100% completion)**
- Story Points Delivered: 21 pts
- Velocity: 21 pts/sprint
- Hours/Point: 1.8h

**Scenario 2: Realistic (90% completion)**
- Story Points Delivered: 19 pts (US-101, US-102, US-103 complete, buffer partial)
- Velocity: 19 pts/sprint
- Hours/Point: 2.0h

**Scenario 3: Pessimistic (75% completion)**
- Story Points Delivered: 16 pts (US-101, US-102 complete, US-103 partial)
- Velocity: 16 pts/sprint
- Hours/Point: 2.4h

**Expected Velocity:** 19 pts (Realistic scenario)

**Usage:** This velocity will be baseline for Sprint 12 capacity planning.

---

## 🎯 Capacity Assumptions

### Assumptions Made:

1. **No major blockers:** External dependencies (YouTube API, FFmpeg) work as expected
2. **Stable environment:** Development environment remains functional
3. **Knowledge availability:** Documentation and examples for libraries exist
4. **Test infrastructure:** Current 99.3% coverage means test setup is solid
5. **Single-tasking:** Developer focuses on Sprint 11 work only (no production support)

### Risks to Capacity:

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| **Learning curve** (YoutubeExplode) | Medium | -4h | Use during buffer time |
| **FFmpeg integration issues** | Medium | -3h | Document in setup guide early |
| **YouTube rate limiting** | Low | -2h | Implement retry immediately |
| **Large file handling bugs** | Medium | -3h | Test with realistic file sizes |
| **Unexpected dependency conflicts** | Low | -2h | Use stable NuGet versions |

**Total Risk Exposure:** -14h maximum
**Buffer Available:** 9.4h + 7.6h slack = 17h ✅ Sufficient coverage

---

## 📊 Capacity Tracking

### Daily Capacity Tracking:

**Format:** Track actual hours spent vs. planned

| Day | Planned | Actual | Variance | Notes |
|-----|---------|--------|----------|-------|
| Day 1 | 4.7h | - | - | - |
| Day 2 | 4.7h | - | - | - |
| Day 3 | 4.7h | - | - | - |
| Day 4 | 4.7h | - | - | - |
| Day 5 | 4.7h | - | - | - |
| Day 6 | 4.7h | - | - | - |
| Day 7 | 4.7h | - | - | - |
| Day 8 | 4.7h | - | - | - |
| Day 9 | 4.7h | - | - | - |
| Day 10 | 4.7h | - | - | - |
| **TOTAL** | **47h** | **-** | **-** | - |

**Update:** End of each day in `SPRINT_11_DAILY_LOG.md`

---

## 🎓 Lessons for Sprint 12

### Capacity Planning Improvements:

After Sprint 11, review:

1. **Was 80% efficiency accurate?**
   - If actual < 80%: Increase buffer or reduce commitment
   - If actual > 80%: Consider reducing buffer to 15%

2. **Was 6h/day realistic?**
   - Track actual productive hours
   - Adjust for Sprint 12 if needed

3. **Was story point mapping accurate?**
   - Compare estimated vs. actual hours
   - Adjust mapping if systematic error

4. **Was 20% buffer sufficient?**
   - Track buffer usage
   - Adjust for Sprint 12 (likely reduce to 15%)

5. **Did Two-Track Agile add overhead?**
   - Track Discovery track time (should be ~12% = 5-6h)
   - Validate benefit for Sprint 12 planning

---

## ✅ Capacity Planning Checklist

**Before Sprint Start:**
- [x] Capacity calculated using v2.0 formula
- [x] Story points mapped to hours
- [x] Buffer allocated (20%)
- [x] Risks identified and quantified
- [x] Velocity scenarios defined
- [x] Assumptions documented
- [x] Tracking template prepared

**During Sprint:**
- [ ] Track actual hours daily
- [ ] Update capacity utilization
- [ ] Monitor buffer usage
- [ ] Escalate if capacity exceeded

**After Sprint:**
- [ ] Calculate actual velocity
- [ ] Compare planned vs. actual
- [ ] Identify capacity planning improvements
- [ ] Update formula for Sprint 12
- [ ] Document lessons learned

---

## 📝 Summary

### Sprint 11 Capacity:

```
Total Capacity: 47 hours
  - Commitment: 37.6 hours (21 story points)
  - Buffer: 9.4 hours (20%)
  - Slack: 7.6 hours (within commitment)

Utilization: 83.8% (healthy)
Confidence: HIGH (90%)
Risk Coverage: Sufficient (17h available vs. 14h exposure)
```

### Key Takeaways:

1. ✅ **Conservative planning:** 1.8h per story point leaves room for unknowns
2. ✅ **Healthy buffer:** 20% buffer + 20% slack = 40% safety margin
3. ✅ **Risk-aware:** All identified risks covered by buffer + slack
4. ✅ **Trackable:** Daily tracking enables mid-sprint adjustments
5. ✅ **Predictable:** Velocity baseline for Sprint 12 planning

**Status:** APPROVED for Sprint 11 execution

---

**Created:** 2025-10-20
**Sprint:** 11
**Methodology:** v2.0
**Next Review:** End of Sprint 11 (validate actual vs. planned)
