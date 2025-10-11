# Test Coverage Guide

This document provides comprehensive information about code coverage in the YoutubeRag.NET project.

## Table of Contents

1. [Coverage Tools](#coverage-tools)
2. [Running Coverage Locally](#running-coverage-locally)
3. [Interpreting Coverage Reports](#interpreting-coverage-reports)
4. [Writing Tests for Uncovered Code](#writing-tests-for-uncovered-code)
5. [Best Practices](#best-practices)
6. [CI/CD Integration](#cicd-integration)
7. [Troubleshooting](#troubleshooting)

---

## Coverage Tools

### Coverlet

We use **Coverlet** for collecting code coverage data. Coverlet is a cross-platform code coverage framework for .NET that supports:

- Line coverage
- Branch coverage
- Method coverage
- Multiple output formats (Cobertura, OpenCover, JSON)

**Installation:**
```bash
dotnet add package coverlet.collector
dotnet add package coverlet.msbuild
```

### ReportGenerator

**ReportGenerator** converts raw coverage data into human-readable reports. It supports:

- HTML reports with detailed line-by-line coverage
- JSON summary reports
- SVG badges
- Markdown summaries for GitHub

**Installation:**
```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
```

---

## Running Coverage Locally

### Quick Start

**Windows PowerShell:**
```powershell
.\scripts\test-coverage.ps1
```

**Linux/Mac Bash:**
```bash
./scripts/test-coverage.sh
```

### Script Options

**PowerShell:**
```powershell
# Run with Release configuration
.\scripts\test-coverage.ps1 -Configuration Release

# Skip build step (faster if already built)
.\scripts\test-coverage.ps1 -SkipBuild

# Open report automatically after generation
.\scripts\test-coverage.ps1 -OpenReport
```

**Bash:**
```bash
# Run with Release configuration
./scripts/test-coverage.sh Release

# Skip build
./scripts/test-coverage.sh Debug true

# Open report
./scripts/test-coverage.sh Debug false true
```

### Manual Coverage Execution

If you prefer to run coverage manually:

```bash
# Run all tests with coverage collection
dotnet test --settings .runsettings \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults

# Generate HTML report
reportgenerator \
    -reports:"./TestResults/**/coverage.cobertura.xml" \
    -targetdir:"./TestResults/CoverageReport" \
    -reporttypes:"Html;JsonSummary;Badges"
```

### Viewing Reports

**Windows:**
```powershell
.\scripts\view-coverage.ps1
```

**Linux/Mac:**
```bash
./scripts/view-coverage.sh
```

Or manually open: `TestResults/CoverageReport/index.html`

---

## Interpreting Coverage Reports

### Coverage Metrics

1. **Line Coverage** (Target: ≥90%)
   - Percentage of executable lines that were executed during tests
   - Most commonly used metric
   - Shows which lines of code are tested

2. **Branch Coverage** (Target: ≥85%)
   - Percentage of conditional branches (if/else, switch) that were executed
   - More strict than line coverage
   - Ensures all code paths are tested

3. **Method Coverage** (Target: ≥90%)
   - Percentage of methods that were called during tests
   - Helps identify untested methods

### HTML Report Navigation

The HTML report provides:

- **Summary Page**: Overview of coverage by assembly and namespace
- **Assembly Details**: Coverage breakdown by class
- **Class Details**: Method-level coverage with line-by-line highlighting
- **Risk Hotspots**: Areas with low coverage that need attention

**Color Coding:**
- 🟢 **Green**: Well covered (≥80%)
- 🟡 **Yellow**: Partially covered (50-79%)
- 🔴 **Red**: Poorly covered (<50%)
- **Gray**: Not covered at all

### JSON Summary

Location: `TestResults/CoverageReport/Summary.json`

Contains:
- Overall line, branch, and method coverage percentages
- Per-assembly breakdown
- Number of covered/uncovered lines
- Historical comparison data

---

## Writing Tests for Uncovered Code

### Identifying Uncovered Code

1. Open the HTML coverage report
2. Navigate to assemblies with low coverage
3. Click through to individual classes
4. Look for red/gray highlighted lines

### Test Writing Guidelines

#### 1. Follow AAA Pattern

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var service = new MyService();
    var input = "test";

    // Act - Execute the method under test
    var result = service.ProcessData(input);

    // Assert - Verify the outcome
    result.Should().Be("expected");
}
```

#### 2. Use Meaningful Test Names

**Good:**
```csharp
public void ValidateEmail_WithInvalidFormat_ReturnsFalse()
public void ProcessPayment_WhenInsufficientFunds_ThrowsException()
```

**Bad:**
```csharp
public void Test1()
public void TestValidation()
```

#### 3. Test Edge Cases

Always test:
- **Null/Empty inputs**: `null`, `""`, `[]`
- **Boundary values**: `0`, `-1`, `int.MaxValue`
- **Invalid states**: Unauthorized, expired, locked
- **Error conditions**: Network failures, timeouts, exceptions

#### 4. Use Theory for Parameterized Tests

```csharp
[Theory]
[InlineData("test@example.com", true)]
[InlineData("invalid-email", false)]
[InlineData("", false)]
[InlineData(null, false)]
public void ValidateEmail_VariousInputs_ReturnsExpected(string email, bool expected)
{
    var result = EmailValidator.IsValid(email);
    result.Should().Be(expected);
}
```

#### 5. Mock External Dependencies

```csharp
[Fact]
public async Task GetUser_CallsRepository()
{
    // Arrange
    var mockRepo = new Mock<IUserRepository>();
    mockRepo.Setup(r => r.GetByIdAsync("123"))
            .ReturnsAsync(new User { Id = "123", Name = "Test" });

    var service = new UserService(mockRepo.Object);

    // Act
    var user = await service.GetUserAsync("123");

    // Assert
    user.Should().NotBeNull();
    user.Name.Should().Be("Test");
    mockRepo.Verify(r => r.GetByIdAsync("123"), Times.Once);
}
```

### Coverage vs. Quality

⚠️ **Important**: 100% coverage doesn't mean perfect code!

**Focus on:**
- ✅ Testing business logic thoroughly
- ✅ Testing error handling
- ✅ Testing edge cases
- ✅ Meaningful assertions

**Avoid:**
- ❌ Testing framework code (ASP.NET, EF Core internals)
- ❌ Testing auto-generated code
- ❌ Writing tests just to increase percentages
- ❌ Testing getters/setters without logic

---

## Best Practices

### 1. Test Organization

```
YoutubeRag.Tests.Unit/
├── Domain/
│   ├── Entities/
│   │   ├── UserTests.cs
│   │   └── VideoTests.cs
│   └── Enums/
│       └── VideoStatusTests.cs
├── Application/
│   ├── Services/
│   ├── Validators/
│   └── Utilities/
└── Infrastructure/
    ├── Repositories/
    └── Services/
```

### 2. Test Independence

Each test should:
- Run independently
- Not depend on execution order
- Clean up after itself
- Not share state with other tests

### 3. Fast Tests

Unit tests should:
- Complete in <100ms each
- Not access database (use in-memory or mocks)
- Not access network
- Not access file system (unless necessary)

### 4. Clear Assertions

```csharp
// Good - Specific assertions
result.Should().NotBeNull();
result.Id.Should().Be("expected-id");
result.Status.Should().Be(VideoStatus.Completed);

// Bad - Vague assertions
Assert.True(result != null);
```

### 5. Test Data Builders

For complex objects, use builders or Bogus:

```csharp
public class VideoBuilder
{
    private string _title = "Default Title";
    private VideoStatus _status = VideoStatus.Pending;

    public VideoBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public VideoBuilder WithStatus(VideoStatus status)
    {
        _status = status;
        return this;
    }

    public Video Build() => new Video
    {
        Title = _title,
        Status = _status
    };
}

// Usage
var video = new VideoBuilder()
    .WithTitle("Test Video")
    .WithStatus(VideoStatus.Completed)
    .Build();
```

---

## CI/CD Integration

### GitHub Actions Workflow

Coverage is automatically collected on every push and pull request.

**Location:** `.github/workflows/ci.yml`

**Features:**
- Runs all tests with coverage collection
- Generates coverage reports
- Uploads reports as artifacts
- Fails build if coverage drops below threshold
- Posts coverage summary to PR comments

### Coverage Thresholds

The build fails if:
- Line coverage < 90%
- Branch coverage < 85%
- Method coverage < 90%

### Viewing Coverage in CI

1. Go to GitHub Actions run
2. Download "Coverage Report" artifact
3. Extract and open `index.html`

### Coverage Badge

The README displays real-time coverage:

![Coverage](https://img.shields.io/badge/coverage-90%25-brightgreen)

Badge updates automatically after each push to main branch.

---

## Troubleshooting

### Issue: No coverage files generated

**Symptoms:**
```
No coverage files found!
```

**Solutions:**
1. Ensure coverlet.collector is installed:
   ```bash
   dotnet add package coverlet.collector
   ```

2. Verify .runsettings file exists
3. Check test projects build successfully:
   ```bash
   dotnet build
   ```

### Issue: Coverage shows 0% for all files

**Symptoms:**
All files show 0% coverage despite running tests.

**Solutions:**
1. Ensure tests are actually running:
   ```bash
   dotnet test --logger "console;verbosity=detailed"
   ```

2. Check .runsettings excludes don't block everything
3. Verify test project references the code project

### Issue: Report Generator fails

**Symptoms:**
```
ReportGenerator command not found
```

**Solutions:**
1. Install globally:
   ```bash
   dotnet tool install --global dotnet-reportgenerator-globaltool
   ```

2. Or restore local tools:
   ```bash
   dotnet tool restore
   ```

### Issue: Tests fail in CI but pass locally

**Possible Causes:**
1. **Environment differences**: Check appsettings, connection strings
2. **Timing issues**: Add explicit waits for async operations
3. **File paths**: Use Path.Combine for cross-platform compatibility
4. **Dependencies**: Ensure all NuGet packages are restored

**Debug in CI:**
```yaml
- name: List test files
  run: |
    find . -name "*Tests.cs"
    dotnet test --list-tests
```

### Issue: Slow coverage generation

**Solutions:**
1. Run in parallel:
   ```bash
   dotnet test --parallel
   ```

2. Skip integration tests if only checking unit coverage:
   ```bash
   dotnet test --filter Category!=Integration
   ```

3. Use incremental build:
   ```bash
   dotnet test --no-build
   ```

---

## Additional Resources

- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator Wiki](https://github.com/danielpalme/ReportGenerator/wiki)
- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [FluentAssertions Docs](https://fluentassertions.com/introduction)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)

---

## Questions?

For coverage-related questions or issues:
1. Check this documentation
2. Review existing test examples in the codebase
3. Ask in team chat
4. Create an issue in the project repository

---

**Last Updated:** 2025-10-11
**Minimum Coverage Target:** Line: 90% | Branch: 85% | Method: 90%
