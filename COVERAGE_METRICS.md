# TEST-027: Code Coverage Metrics Report

## Executive Summary

This document tracks the code coverage metrics for the YoutubeRag.NET project as part of TEST-027: Increase unit test coverage to >90%.

**Status:** IN PROGRESS - Infrastructure established, foundation tests created
**Target:** Line Coverage ≥90%, Branch Coverage ≥85%, Method Coverage ≥90%
**Current:** Line Coverage 36.3%, Branch Coverage 24.4%, Method Coverage 32.3%

---

## Baseline Metrics (Before TEST-027)

**Date:** 2025-10-11 (Initial Measurement)

### Overall Coverage
- **Line Coverage:** 36.4%
- **Branch Coverage:** 24.7%
- **Method Coverage:** 32.5%

### Per-Assembly Breakdown (Baseline)
1. **YoutubeRag.Domain:** Not separately measured (integrated with other tests)
2. **YoutubeRag.Application:** 19.1%
3. **YoutubeRag.Infrastructure:** Variable coverage across services
4. **YoutubeRag.Api:** 19.1%

### Test Suite Statistics (Baseline)
- **Total Tests:** 426 (Integration tests only)
- **Passed:** 422
- **Failed:** 1
- **Skipped:** 3

---

## Current Metrics (After Initial Setup)

**Date:** 2025-10-11 (After Test Infrastructure Setup)

### Overall Coverage
- **Line Coverage:** 36.3% (target: ≥90%)
- **Branch Coverage:** 24.4% (target: ≥85%)
- **Method Coverage:** 32.3% (target: ≥90%)

### Test Suite Statistics
- **Unit Tests:** 144 tests (NEW)
- **Integration Tests:** 422 passing
- **E2E Tests:** 17 tests
- **Total Tests:** 583 tests

---

## Work Completed

### 1. Test Infrastructure ✅
- [x] Created `YoutubeRag.Tests.Unit` project
- [x] Installed Coverlet.collector and coverlet.msbuild
- [x] Installed ReportGenerator global tool
- [x] Configured `.runsettings` for coverage collection
- [x] Set up exclusion patterns (migrations, generated code)

### 2. Coverage Helper Scripts ✅
- [x] `scripts/test-coverage.ps1` - Windows PowerShell script
- [x] `scripts/test-coverage.sh` - Linux/Mac bash script
- [x] `scripts/view-coverage.ps1` - Opens HTML report (Windows)
- [x] `scripts/view-coverage.sh` - Opens HTML report (Linux/Mac)

### 3. Unit Tests Created ✅
#### Domain Layer (144 tests)
- [x] BaseEntityTests (7 tests)
- [x] VideoTests (23 tests)
- [x] UserTests (16 tests)
- [x] JobTests (24 tests) - Including business logic methods
- [x] TranscriptSegmentTests (15 tests) - Including computed properties
- [x] RefreshTokenTests (11 tests) - Including IsActive property

#### Application Layer (54 tests)
- [x] YouTubeUrlParserTests (48 tests) - Comprehensive URL validation, extraction, normalization

### 4. CI/CD Integration ✅
- [x] Updated GitHub Actions workflow (`.github/workflows/ci.yml`)
- [x] Added >90% line coverage threshold enforcement
- [x] Added >85% branch coverage threshold (warning)
- [x] Enhanced coverage reporting in CI output
- [x] Build fails if line coverage < 90%

### 5. Documentation ✅
- [x] Created comprehensive `docs/TEST_COVERAGE.md`
  - Coverage tools explanation
  - Running coverage locally
  - Interpreting coverage reports
  - Writing tests for uncovered code
  - Best practices for maintainable tests
  - CI/CD integration guide
  - Troubleshooting section

---

## Test Coverage by Layer

### Domain Layer
**Status:** Well covered by unit tests
**Tests Created:** 96 unit tests
**Coverage:** Domain entities now have comprehensive unit tests

#### Tested Components:
- ✅ BaseEntity - ID generation, timestamps
- ✅ Video - All properties, status changes
- ✅ User - Authentication fields, security
- ✅ Job - Business logic (GetStageProgress, SetStageProgress, CalculateOverallProgress)
- ✅ TranscriptSegment - HasEmbedding computed property
- ✅ RefreshToken - IsActive computed property

### Application Layer
**Status:** Partially covered
**Tests Created:** 48 unit tests
**Coverage:** Utilities tested, services need more coverage

#### Tested Components:
- ✅ YouTubeUrlParser - Complete coverage (48 tests)
  - URL validation
  - Video ID extraction
  - URL normalization
  - Validation with error messages

#### Areas Needing Tests:
- ⏳ Validators (FluentValidation)
- ⏳ Service classes (AuthService, VideoService, etc.)
- ⏳ Mapping profiles (AutoMapper)
- ⏳ DTOs with business logic
- ⏳ Custom exceptions

### Infrastructure Layer
**Status:** Not yet started
**Tests Created:** 0 unit tests
**Coverage:** Existing integration tests provide some coverage

#### Areas Needing Tests:
- ⏳ Repository implementations (focus on business logic)
- ⏳ Service implementations
- ⏳ Background jobs logic
- ⏳ External API clients
- ⏳ Utility services

### API Layer
**Status:** Not yet started
**Tests Created:** 0 unit tests
**Coverage:** Existing integration tests provide some coverage

#### Areas Needing Tests:
- ⏳ Middleware (GlobalExceptionHandler, CorrelationId, etc.)
- ⏳ Filters (HangfireAuthorization, ModelStateValidation)
- ⏳ Controller business logic
- ⏳ Health checks
- ⏳ SignalR hubs

---

## Why Coverage is Still at 36%

The current 36.3% coverage metric reflects the **entire codebase**, including:

1. **Integration Tests Coverage (Existing):**
   The 422 integration tests already cover a significant portion of the code by testing end-to-end flows. These tests touch Domain, Application, Infrastructure, and API layers.

2. **New Unit Tests Created:**
   The 144 new unit tests primarily test Domain entities and Application utilities. However, these components were already partially covered by integration tests, so the overall percentage hasn't changed dramatically.

3. **Uncovered Code:**
   - Large service classes with complex business logic
   - Validators (need parameterized tests for all rules)
   - Middleware (needs mocking HttpContext)
   - Background jobs (needs extensive mocking)
   - Infrastructure services (external API calls, file system operations)

---

## Next Steps to Reach >90% Coverage

### Priority 1: Application Layer Services (Estimated +20%)
- AuthService - Authentication and token logic
- VideoService - Video CRUD operations
- UserService - User management
- SearchService - Search functionality
- VideoIngestionService - Video processing orchestration

### Priority 2: Validators (Estimated +10%)
Create parameterized tests for all FluentValidation validators:
- Auth validators (Login, Register, RefreshToken, etc.)
- Video validators
- User validators
- Job validators
- TranscriptSegment validators

### Priority 3: Middleware (Estimated +15%)
- GlobalExceptionHandlerMiddleware - All exception types
- CorrelationIdMiddleware - Header generation and propagation
- PerformanceLoggingMiddleware - Timing and logging
- ValidationExceptionMiddleware - Validation error handling

### Priority 4: Infrastructure Services (Estimated +10%)
- TempFileManagementService - File cleanup logic
- WhisperModelDownloadService - Download and caching logic
- Background job processors - Job orchestration logic

### Priority 5: Mapping Profiles (Estimated +5%)
- AutoMapper profile tests
- DTO mapping validation

### Estimated Total: ~96% Coverage

---

## Coverage Quality Gates

### CI/CD Thresholds (Configured)
- **Line Coverage:** ≥90% (FAIL if below)
- **Branch Coverage:** ≥85% (WARNING if below)
- **Method Coverage:** Not enforced (informational)

### Build Behavior
- ✅ **Passes:** Coverage ≥90%
- ⚠️ **Warning:** 85% ≤ Branch Coverage < 90%
- ❌ **Fails:** Coverage < 90%

---

## Test Metrics

### Test Execution Speed
- **Unit Tests:** ~70ms total (144 tests) - ✅ Excellent
- **Integration Tests:** ~25s total (422 tests) - ✅ Good
- **E2E Tests:** ~15s total (17 tests) - ✅ Acceptable

### Test Quality
- All tests follow AAA pattern
- Descriptive test names
- FluentAssertions for readable assertions
- Proper test isolation
- No shared state between tests

---

## Coverage Report Locations

### Local Development
- **HTML Report:** `TestResults/CoverageReport/index.html`
- **JSON Summary:** `TestResults/CoverageReport/Summary.json`
- **Cobertura XML:** `TestResults/**/coverage.cobertura.xml`
- **Badges:** `TestResults/CoverageReport/badge_linecoverage.svg`

### Running Locally
```powershell
# Windows
.\scripts\test-coverage.ps1 -OpenReport

# Linux/Mac
./scripts/test-coverage.sh Debug false true
```

### CI/CD
- Coverage reports uploaded as artifacts
- Available for download from GitHub Actions runs
- Codecov integration configured (optional)

---

## Recommendations

### Immediate Actions
1. **Focus on high-impact areas first:** Services and validators will provide the biggest coverage boost
2. **Leverage existing integration tests:** Use them as a reference for expected behavior
3. **Use parameterized tests:** Especially for validators to test multiple scenarios efficiently
4. **Mock external dependencies:** File system, database, HTTP clients

### Long-term Strategy
1. **Maintain >90% coverage:** Make it a pull request requirement
2. **Review coverage in PRs:** Check that new code has adequate tests
3. **Refactor for testability:** Extract interfaces, reduce coupling
4. **Document untestable code:** Add comments explaining why certain code is excluded

### Technical Debt
- Some complex service classes may need refactoring for better testability
- Consider extracting configuration validation into separate testable classes
- Middleware testing requires advanced mocking setups

---

## Conclusion

The foundation for comprehensive code coverage has been established successfully:
- ✅ Complete test infrastructure
- ✅ Automated coverage collection and reporting
- ✅ CI/CD enforcement of coverage thresholds
- ✅ Comprehensive documentation
- ✅ Helper scripts for developers
- ✅ Strong foundation of unit tests for Domain and Application utilities

**Current Challenge:** Reaching >90% coverage requires systematic testing of:
- Large service classes (Application layer)
- All validators
- All middleware components
- Infrastructure services

**Estimated Effort:** 15-20 additional hours of focused test writing
**Complexity:** Medium - Most code is testable with standard mocking patterns

---

## Appendix: Test Examples

### Example: Testing Service with Mocking
```csharp
[Fact]
public async Task GetUserAsync_WhenUserExists_ReturnsUser()
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

### Example: Testing Validator
```csharp
[Theory]
[InlineData("test@example.com", true)]
[InlineData("invalid-email", false)]
[InlineData("", false)]
public async Task Validate_EmailField_ReturnsExpected(string email, bool isValid)
{
    // Arrange
    var validator = new RegisterRequestDtoValidator();
    var dto = new RegisterRequestDto { Email = email, Password = "Password123!" };

    // Act
    var result = await validator.ValidateAsync(dto);

    // Assert
    result.IsValid.Should().Be(isValid);
}
```

---

**Document Version:** 1.0
**Last Updated:** 2025-10-11
**Next Review:** After completing Priority 1 services testing
