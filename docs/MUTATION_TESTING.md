# Mutation Testing with Stryker.NET

## Table of Contents

- [What is Mutation Testing?](#what-is-mutation-testing)
- [Why Mutation Testing Matters](#why-mutation-testing-matters)
- [How Stryker.NET Works](#how-strykernetnet-works)
- [Installation](#installation)
- [Running Mutation Tests](#running-mutation-tests)
- [Understanding Mutation Reports](#understanding-mutation-reports)
- [Mutation Score Explained](#mutation-score-explained)
- [Common Mutation Types](#common-mutation-types)
- [Improving Surviving Mutants](#improving-surviving-mutants)
- [Best Practices](#best-practices)
- [CI/CD Integration](#cicd-integration)
- [Troubleshooting](#troubleshooting)
- [Performance Optimization](#performance-optimization)

---

## What is Mutation Testing?

**Mutation testing** is a technique to evaluate the quality and effectiveness of your test suite. Unlike traditional code coverage metrics (which only tell you if code is executed), mutation testing tells you if your tests actually catch bugs.

### The Concept

Stryker.NET introduces small, deliberate bugs (mutations) into your source code and then runs your test suite. If a test fails, the mutant is "killed" (good). If all tests pass, the mutant "survived" (bad - indicates a gap in test coverage).

### Example

```csharp
// Original code
public bool IsPositive(int number)
{
    return number > 0;
}

// Mutated code (Stryker changes > to >=)
public bool IsPositive(int number)
{
    return number >= 0;  // BUG: 0 is not positive!
}
```

If your tests pass with this mutation, it means you don't have a test that checks `IsPositive(0) == false`.

---

## Why Mutation Testing Matters

### Traditional Coverage is Not Enough

```csharp
// Code with 100% line coverage
public int Divide(int a, int b)
{
    return a / b;  // Covered by tests
}

// Test that gives 100% line coverage
[Fact]
public void TestDivide()
{
    var result = Divide(10, 2);
    // No assertion! Test passes but doesn't verify behavior
}
```

This test gives 100% coverage but doesn't actually test anything. Mutation testing would reveal this weakness.

### Benefits

1. **Measures Test Quality**: Not just code coverage, but test effectiveness
2. **Finds Weak Tests**: Identifies tests that don't actually verify behavior
3. **Reveals Edge Cases**: Discovers missing boundary and error condition tests
4. **Validates Assertions**: Ensures tests have meaningful assertions
5. **Improves Confidence**: Higher mutation scores = more reliable tests

---

## How Stryker.NET Works

### Process Flow

```
┌─────────────────────┐
│ 1. Analyze Code     │ - Parse source files
│                     │ - Identify mutation points
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│ 2. Generate Mutants │ - Create code mutations
│                     │ - One mutation per mutant
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│ 3. Run Tests        │ - Execute test suite
│                     │ - For each mutant
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│ 4. Analyze Results  │ - Killed: Test failed (good)
│                     │ - Survived: All passed (bad)
│                     │ - Timeout: Test took too long
│                     │ - No Coverage: Not tested
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│ 5. Generate Report  │ - HTML, JSON, Console
│                     │ - Mutation score
└─────────────────────┘
```

### Mutation Strategies

Stryker.NET uses several mutation strategies:

- **Arithmetic**: `+` → `-`, `*` → `/`
- **Relational**: `>` → `>=`, `==` → `!=`
- **Logical**: `&&` → `||`, `!condition` → `condition`
- **Assignment**: `+=` → `-=`, `++` → `--`
- **Block**: Remove entire statements
- **String**: Change string literals

---

## Installation

### Prerequisites

- .NET 8.0 or later
- xUnit test project
- Code to test (Domain/Application layers)

### Install Stryker.NET

Stryker.NET is already configured as a .NET local tool:

```bash
# Restore all .NET tools (including Stryker)
dotnet tool restore

# Verify installation
dotnet stryker --version
```

### Manual Installation (if needed)

```bash
# Install globally
dotnet tool install -g dotnet-stryker

# Or install as local tool
dotnet new tool-manifest
dotnet tool install dotnet-stryker
```

---

## Running Mutation Tests

### Using Helper Scripts (Recommended)

#### Windows (PowerShell)

```powershell
# Run on Domain layer (default)
.\scripts\run-mutation-tests.ps1

# Run on Application layer
.\scripts\run-mutation-tests.ps1 -Project Application

# Run on all projects
.\scripts\run-mutation-tests.ps1 -Project All

# Set threshold and open report
.\scripts\run-mutation-tests.ps1 -Project Domain -Threshold 80 -Open

# Run only on changed files (faster)
.\scripts\run-mutation-tests.ps1 -Project Domain -DiffOnly
```

#### Linux/macOS (Bash)

```bash
# Run on Domain layer (default)
./scripts/run-mutation-tests.sh domain

# Run on Application layer
./scripts/run-mutation-tests.sh application 80 --open

# Run on all projects
./scripts/run-mutation-tests.sh all

# Run only on changed files
./scripts/run-mutation-tests.sh domain --diff
```

### Using Stryker CLI Directly

```bash
# Basic run on Domain layer
dotnet stryker \
  --project YoutubeRag.Domain/YoutubeRag.Domain.csproj \
  --test-project YoutubeRag.Tests.Unit/YoutubeRag.Tests.Unit.csproj

# With custom thresholds
dotnet stryker \
  --project YoutubeRag.Application/YoutubeRag.Application.csproj \
  --test-project YoutubeRag.Tests.Unit/YoutubeRag.Tests.Unit.csproj \
  --threshold-high 80 \
  --threshold-low 60 \
  --threshold-break 50

# With output directory and reporters
dotnet stryker \
  --project YoutubeRag.Domain/YoutubeRag.Domain.csproj \
  --test-project YoutubeRag.Tests.Unit/YoutubeRag.Tests.Unit.csproj \
  --reporter html \
  --reporter json \
  --output StrykerOutput/Domain
```

### Configuration File

Stryker.NET can be configured via `stryker-config.json` at the repository root:

```json
{
  "stryker-config": {
    "project": "YoutubeRag.Domain/YoutubeRag.Domain.csproj",
    "test-projects": ["YoutubeRag.Tests.Unit/YoutubeRag.Tests.Unit.csproj"],
    "reporters": ["html", "json", "cleartext", "progress"],
    "thresholds": {
      "high": 80,
      "low": 60,
      "break": 50
    },
    "mutation-level": "standard",
    "concurrency": 4,
    "timeout-ms": 60000
  }
}
```

---

## Understanding Mutation Reports

### Viewing Reports

```bash
# Windows
.\scripts\view-mutation-report.ps1

# Linux/macOS
./scripts/view-mutation-report.sh
```

### Report Components

#### 1. Mutation Score

```
Mutation Score: 75.5%
```

Percentage of mutants killed by your tests. Higher is better.

#### 2. Mutant Status

- **Killed** (Green): A test failed for this mutant. Good!
- **Survived** (Red): All tests passed. Bad - missing test!
- **Timeout** (Yellow): Test took too long. May indicate infinite loop.
- **No Coverage** (Gray): Code not covered by tests.

#### 3. File-Level View

```
├── Entities/
│   ├── User.cs (85%)
│   ├── Video.cs (72%)
│   └── Job.cs (91%)
```

Shows mutation score per file.

#### 4. Code-Level View

```csharp
public bool IsValid()
{
    return Age > 0 &&     // Killed: Changed > to >=
           Name != null;  // Survived: Changed != to ==
}
```

Shows exact mutations and their status.

---

## Mutation Score Explained

### What is a Good Score?

| Score | Grade | Meaning |
|-------|-------|---------|
| 80-100% | Excellent | Very high test quality |
| 60-79% | Good | Acceptable test quality |
| 50-59% | Fair | Consider improvement |
| 0-49% | Poor | Needs significant work |

### Calculation

```
Mutation Score = (Killed Mutants / Total Mutants) × 100

Example:
- Total Mutants: 200
- Killed: 150
- Survived: 40
- Timeout: 5
- No Coverage: 5

Score = (150 / 200) × 100 = 75%
```

### Why Not 100%?

Some mutants are equivalent (functionally identical to original) or testing them provides diminishing returns. Aiming for 80% is typically excellent.

---

## Common Mutation Types

### 1. Arithmetic Mutators

```csharp
// Original
int result = a + b;

// Mutations
int result = a - b;  // Addition to subtraction
int result = a * b;  // Addition to multiplication
int result = a / b;  // Addition to division
```

**Tests to Kill**: Verify actual calculations with specific values.

### 2. Relational Mutators

```csharp
// Original
if (age > 18)

// Mutations
if (age >= 18)  // Greater than to greater-or-equal
if (age < 18)   // Greater than to less than
if (age <= 18)  // Greater than to less-or-equal
```

**Tests to Kill**: Test boundary values (18, 19, 17).

### 3. Logical Mutators

```csharp
// Original
if (isActive && hasPermission)

// Mutations
if (isActive || hasPermission)  // AND to OR
if (!isActive && hasPermission) // Remove negation
if (isActive && !hasPermission) // Remove negation
```

**Tests to Kill**: Test all boolean combinations.

### 4. Equality Mutators

```csharp
// Original
if (status == "active")

// Mutations
if (status != "active")  // Equal to not-equal
```

**Tests to Kill**: Test both equal and not-equal cases.

### 5. Assignment Mutators

```csharp
// Original
counter++;

// Mutations
counter--;  // Increment to decrement
```

**Tests to Kill**: Verify actual incremented values.

### 6. Block Mutators

```csharp
// Original
public void Validate()
{
    if (value < 0)
        throw new ArgumentException();
}

// Mutation: Remove entire block
public void Validate()
{
    // Block removed!
}
```

**Tests to Kill**: Verify exceptions are thrown.

### 7. String Mutators

```csharp
// Original
var message = "Success";

// Mutations
var message = "";         // String to empty
var message = "Stryker";  // String to "Stryker"
```

**Tests to Kill**: Verify exact string values.

---

## Improving Surviving Mutants

### Step 1: Identify Survivors

Run mutation testing and check the HTML report for red (survived) mutants.

### Step 2: Analyze Why They Survived

```csharp
// Code
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;  // Mutated to: a - b (SURVIVED)
    }
}

// Weak test
[Fact]
public void TestAdd()
{
    var calc = new Calculator();
    var result = calc.Add(0, 0);  // 0 + 0 = 0, but 0 - 0 = 0 too!
    Assert.Equal(0, result);
}
```

**Problem**: Test uses values where mutation doesn't change result.

### Step 3: Add Targeted Tests

```csharp
[Theory]
[InlineData(5, 3, 8)]
[InlineData(10, 20, 30)]
[InlineData(-5, 5, 0)]
public void Add_ShouldReturnCorrectSum(int a, int b, int expected)
{
    var calc = new Calculator();
    var result = calc.Add(a, b);
    Assert.Equal(expected, result);
}
```

Now the mutant `a - b` will be killed because `5 - 3 ≠ 8`.

### Step 4: Verify

Re-run mutation testing to confirm the mutant is now killed.

---

## Best Practices

### 1. Run Regularly (But Not Always)

```bash
# Run locally before major commits
./scripts/run-mutation-tests.sh domain

# CI runs weekly (too slow for every PR)
```

### 2. Focus on High-Value Code

Prioritize mutation testing on:
- Business logic (Domain/Application)
- Complex algorithms
- Critical security code
- Frequently changing code

Skip mutation testing on:
- DTOs without logic
- Generated code
- Simple getters/setters
- Infrastructure code

### 3. Use Incremental Mode

```bash
# Only test changed files (much faster)
./scripts/run-mutation-tests.sh domain --diff
```

### 4. Set Realistic Thresholds

```bash
# Start with achievable goals
--threshold-break 50  # Initial
--threshold-break 60  # Improvement
--threshold-break 80  # Excellence
```

### 5. Don't Chase 100%

Some surviving mutants are:
- **Equivalent mutants**: Functionally identical to original
- **Low value**: Testing provides minimal benefit
- **Edge cases**: Extremely rare scenarios

Aim for 80% as an excellent target.

### 6. Combine with Code Coverage

```bash
# Run both metrics
./scripts/test-coverage.sh
./scripts/run-mutation-tests.sh domain
```

Code coverage (36.3%) + Mutation score (75%) = Complete picture

### 7. Review Reports as a Team

- Schedule periodic reviews of mutation reports
- Identify patterns in surviving mutants
- Share knowledge about test quality
- Celebrate improvements

---

## CI/CD Integration

### GitHub Actions Workflow

Mutation testing runs automatically:

**Schedule**: Every Monday at 2:00 AM UTC

**Manual Trigger**: Via GitHub Actions UI

**Workflow**: `.github/workflows/mutation-tests.yml`

### Manual Trigger

1. Go to **Actions** tab on GitHub
2. Select **Mutation Testing** workflow
3. Click **Run workflow**
4. Choose project and threshold
5. Click **Run workflow** button

### Viewing Results

1. **Job Summary**: View mutation scores in workflow summary
2. **Artifacts**: Download HTML reports from artifacts
3. **Logs**: Check detailed logs for mutant details

### Why Not on Every PR?

Mutation testing is slow:
- Domain layer: ~5-10 minutes
- Application layer: ~10-15 minutes
- All projects: ~20-30 minutes

Running on every PR would slow development. Weekly runs provide insights without blocking progress.

---

## Troubleshooting

### Issue: Stryker.NET Not Found

```bash
Error: Could not find 'dotnet-stryker'
```

**Solution**:
```bash
dotnet tool restore
dotnet stryker --version
```

### Issue: Tests Fail During Mutation

```bash
Error: Tests failed on original code
```

**Solution**:
1. Ensure tests pass without mutations
2. Run: `dotnet test YoutubeRag.Tests.Unit`
3. Fix failing tests
4. Re-run Stryker

### Issue: Timeout for All Mutants

```bash
All mutants timed out after 60000ms
```

**Solution**:
Increase timeout in `stryker-config.json`:
```json
{
  "timeout-ms": 120000
}
```

### Issue: Out of Memory

```bash
System.OutOfMemoryException
```

**Solution**:
Reduce concurrency:
```bash
./scripts/run-mutation-tests.sh domain --concurrency 2
```

### Issue: Mutation Takes Too Long

```bash
# Already running for 30+ minutes
```

**Solution**:
- Run on smaller scope (single project)
- Use `--diff` mode for changed files only
- Exclude large files with little logic
- Increase concurrency (if RAM available)

### Issue: Report Not Generated

```bash
# No HTML report found
```

**Solution**:
Check output directory:
```bash
# Windows
ls StrykerOutput/Domain/reports/

# Linux
ls StrykerOutput/Domain/reports/
```

Verify reporters in config:
```json
{
  "reporters": ["html", "json", "cleartext"]
}
```

---

## Performance Optimization

### 1. Use Diff Mode

Only mutate changed files since last commit:

```bash
./scripts/run-mutation-tests.sh domain --diff
```

### 2. Adjust Concurrency

```bash
# More CPU cores = higher concurrency
./scripts/run-mutation-tests.sh domain --concurrency 8

# Low RAM = lower concurrency
./scripts/run-mutation-tests.sh domain --concurrency 2
```

### 3. Exclude Non-Critical Files

In `stryker-config.json`:

```json
{
  "exclude-files": [
    "**/Migrations/**/*.cs",
    "**/DTOs/**/*.cs",
    "**/obj/**/*.cs",
    "**/bin/**/*.cs"
  ]
}
```

### 4. Use Mutation Level

```json
{
  "mutation-level": "basic"    // Fewer mutations, faster
  // or
  "mutation-level": "standard" // Balanced (default)
  // or
  "mutation-level": "complete" // All mutations, slowest
}
```

### 5. Limit File Scope

Test specific files:

```bash
dotnet stryker \
  --project YoutubeRag.Domain/YoutubeRag.Domain.csproj \
  --mutate "**/Entities/User.cs" \
  --mutate "**/Entities/Video.cs"
```

### 6. Use Baseline

Store baseline and only test changes:

```json
{
  "baseline": {
    "provider": "disk"
  },
  "since": {
    "enabled": true,
    "target": "master"
  }
}
```

---

## Additional Resources

### Official Documentation

- [Stryker.NET Documentation](https://stryker-mutator.io/docs/stryker-net/introduction/)
- [Mutation Testing Guide](https://stryker-mutator.io/docs/General/what-is-mutation-testing/)

### YoutubeRag.NET Documentation

- [Test Coverage Guide](./TEST_COVERAGE.md)
- [Development Methodology](./DEVELOPMENT_METHODOLOGY.md)
- [Quick Reference](./QUICK_REFERENCE.md)

### Team Resources

- **Baseline Report**: `MUTATION_SCORE_BASELINE.md` (repository root)
- **Scripts**:
  - `scripts/run-mutation-tests.ps1` (Windows)
  - `scripts/run-mutation-tests.sh` (Linux/macOS)
  - `scripts/view-mutation-report.ps1` (Windows)
  - `scripts/view-mutation-report.sh` (Linux/macOS)

---

## Summary

Mutation testing with Stryker.NET provides deep insights into test quality:

1. **Install**: `dotnet tool restore`
2. **Run**: `./scripts/run-mutation-tests.sh domain`
3. **View**: `./scripts/view-mutation-report.sh`
4. **Analyze**: Review surviving mutants
5. **Improve**: Add targeted tests
6. **Verify**: Re-run to confirm improvements

**Key Metrics**:
- Current code coverage: **36.3%**
- Target mutation score: **60%** (low), **80%** (high)
- Focus: Domain and Application layers

**Remember**: Mutation testing is a quality tool, not a gate. Use it to gain insights and improve test effectiveness, but don't let it block development progress.

---

**Last Updated**: 2025-10-11
**Version**: 1.0.0
**Maintainer**: YoutubeRag.NET Team
