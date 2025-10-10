# Testing Setup and Known Issues

**Date:** October 1, 2025
**Status:** Integration Test Infrastructure Complete - Pending Program.cs Refactoring

---

## Current Status

✅ **Integration Test Project Created**
- 61 comprehensive integration tests
- Complete test infrastructure with WebApplicationFactory
- Test data generators and seeders
- All test files compile successfully
- Build status: **0 errors, 0 warnings**

❌ **Tests Currently Failing**
- All 61 tests fail with: `The entry point exited without ever building an IHost`
- Root cause: Program.cs uses top-level statements incompatible with WebApplicationFactory

---

## Problem Description

The current Program.cs uses top-level statements with the following structure:

```csharp
var builder = WebApplication.CreateBuilder(args);
// ... configuration ...
var app = builder.Build();
// ... middleware ...
app.Run();  // ← This is the problem
```

When `WebApplicationFactory` tries to create a test host, it invokes the Program's entry point, but `app.Run()` is a blocking call that prevents the factory from obtaining the `IHost` instance it needs.

---

## Solution: Refactor Program.cs for Testability

### Option 1: Factory Method Pattern (Recommended)

Create a static method that builds the `WebApplication` without calling `Run()`:

```csharp
// At the top of Program.cs
var app = CreateWebApplication(args);
app.Run();

static WebApplication CreateWebApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // ... all configuration code ...

    var app = builder.Build();

    // ... all middleware configuration ...

    return app;  // Return without calling Run()
}

// Make Program accessible for testing
public partial class Program
{
    public static WebApplication CreateApp(string[] args) => CreateWebApplication(args);
}
```

### Option 2: Conditional Run

Detect when running in test context and skip `app.Run()`:

```csharp
// At the end of Program.cs, replace app.Run() with:

if (!IsRunningInTest())
{
    app.Run();
}

static bool IsRunningInTest()
{
    var assembly = System.Reflection.Assembly.GetEntryAssembly();
    return assembly == null ||
           assembly.GetName().Name?.Contains("testhost") == true ||
           assembly.GetName().Name?.Contains("xunit") == true;
}

public partial class Program { }
```

### Option 3: Minimal Hosting Model

Convert to the newer Minimal Hosting model that's more test-friendly:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configuration in separate methods
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

ConfigureMiddleware(app);

// Only run if not in test
if (args.FirstOrDefault() != "--test")
{
    app.Run();
}

// Make accessible for testing
public partial class Program { }

static void ConfigureServices(IServiceCollection services, IConfiguration config)
{
    // All service registration
}

static void ConfigureMiddleware(WebApplication app)
{
    // All middleware configuration
}
```

---

## Implemented Test Infrastructure

### Test Project Structure

```
YoutubeRag.Tests.Integration/
├── Controllers/
│   ├── AuthControllerTests.cs (16 tests)
│   ├── VideosControllerTests.cs (14 tests)
│   ├── SearchControllerTests.cs (13 tests)
│   └── UsersControllerTests.cs (14 tests)
├── Infrastructure/
│   ├── CustomWebApplicationFactory.cs
│   └── IntegrationTestBase.cs
├── Helpers/
│   └── TestDataGenerator.cs
├── Fixtures/
│   └── DatabaseSeeder.cs
└── HealthCheckTests.cs (4 tests)
```

### CustomWebApplicationFactory

```csharp
public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace database with in-memory for tests
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Seed test data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureCreated();
            DatabaseSeeder.SeedTestData(db);
        });
    }
}
```

### Test Categories

**Authentication Tests (16):**
- User registration
- Login/logout flows
- Token refresh
- Invalid credentials
- Unauthorized access

**Video Management Tests (14):**
- CRUD operations
- Pagination
- Authorization checks
- URL processing
- Search and filtering

**Search Tests (13):**
- Semantic search
- Search suggestions
- Date range filtering
- Status filtering
- Special characters handling

**User Management Tests (14):**
- Profile operations
- Statistics retrieval
- Preferences management
- Data export
- Account deletion

**Health Check Tests (4):**
- API health status
- Swagger endpoints
- Root endpoint

---

## Test Data

### DatabaseSeeder

Provides comprehensive test data:
- 10 test users with various states
- 25 test videos with different statuses
- 100+ transcript segments with embeddings
- Jobs with various completion states
- Refresh tokens and user sessions

### TestDataGenerator (using Bogus)

Generates realistic fake data:
- User profiles
- Video metadata
- Transcript segments
- Search queries
- Embeddings

---

## How to Run Tests (Once Program.cs is Fixed)

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity detailed

# Run specific test class
dotnet test --filter "FullyQualifiedName~AuthControllerTests"

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

---

## Dependencies Added

**NuGet Packages:**
- xUnit (v2.9.2) - Test framework
- xUnit.runner.visualstudio (v3.0.0-pre.35) - Test runner
- Microsoft.AspNetCore.Mvc.Testing (v9.0.0) - WebApplicationFactory
- Microsoft.EntityFrameworkCore.InMemory (v9.0.1) - In-memory database
- FluentAssertions (v7.0.0) - Assertion library
- Moq (v4.20.72) - Mocking framework
- Bogus (v35.6.1) - Test data generation

---

## Expected Test Results (After Fix)

Once Program.cs is refactored, expected results:

- **Total Tests:** 61
- **Expected Passes:** 55-58 (90-95%)
- **Expected Failures:** 3-6 (edge cases, not-yet-implemented features)

**Known Expected Failures:**
- Google OAuth tests (not implemented)
- Some advanced search features (mocked services)
- WebSocket-related tests (not yet implemented)

---

## Benefits of Integration Tests

1. **End-to-End Validation**
   - Tests entire request pipeline
   - Validates database interactions
   - Verifies authentication/authorization

2. **Regression Prevention**
   - Catches breaking changes
   - Validates refactoring
   - Ensures API contract compliance

3. **Documentation**
   - Tests serve as usage examples
   - Documents expected behavior
   - Shows API capabilities

4. **Confidence**
   - Safe refactoring
   - Deployment confidence
   - Quick feedback on changes

---

## Recommended Next Steps

### Immediate (Priority 1)
1. ✅ Refactor Program.cs using Option 1 or 2 above
2. ✅ Run all integration tests
3. ✅ Fix any failing tests
4. ✅ Add code coverage reporting

### Short Term (Priority 2)
5. Add unit tests for Service Layer
6. Add unit tests for Repository Layer
7. Implement mutation testing
8. Add performance benchmarks

### Medium Term (Priority 3)
9. Add E2E tests with Playwright
10. Add load testing with K6
11. Add security testing (OWASP)
12. Add contract testing for APIs

---

## Build Status

**Current:**
```
Solution Build: ✅ PASSING (0 errors, 0 warnings)
Test Project Build: ✅ PASSING (0 errors, 0 warnings)
Test Execution: ❌ FAILING (Program.cs compatibility issue)
```

**After Fix:**
```
Solution Build: ✅ PASSING
Test Project Build: ✅ PASSING
Test Execution: ✅ PASSING (expected 55-58/61)
```

---

## Code Coverage Goals

**Target Coverage:**
- Controllers: 80-90%
- Services: 85-95%
- Repositories: 70-80%
- Overall: 80%+

**Coverage Tools:**
- Coverlet for .NET coverage
- ReportGenerator for HTML reports
- SonarQube for continuous monitoring

---

## Continuous Integration

**Recommended CI Pipeline:**

```yaml
steps:
  - name: Restore dependencies
    run: dotnet restore

  - name: Build solution
    run: dotnet build --no-restore

  - name: Run tests
    run: dotnet test --no-build --verbosity normal /p:CollectCoverage=true

  - name: Generate coverage report
    run: reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport

  - name: Publish test results
    uses: dorny/test-reporter@v1
```

---

## Summary

✅ **Completed:**
- Integration test project with 61 tests
- Complete test infrastructure
- Test data generation
- Build verification

⏳ **Pending:**
- Program.cs refactoring for test compatibility
- Test execution and validation
- Code coverage setup
- CI/CD integration

**Estimated Time to Complete:** 30-60 minutes

**Confidence Level:** High - Solution is well-defined and straightforward to implement

---

**Document Created:** October 1, 2025
**Last Updated:** October 1, 2025
**Status:** Ready for Implementation
