# End-to-End Testing with Playwright for .NET

## Overview

This document provides comprehensive guidance on running and maintaining End-to-End (E2E) tests for the YoutubeRag.NET project using Playwright for .NET.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Setup Instructions](#setup-instructions)
- [Running Tests](#running-tests)
- [Test Configuration](#test-configuration)
- [Writing New Tests](#writing-new-tests)
- [Debugging Tests](#debugging-tests)
- [CI/CD Integration](#cicd-integration)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Prerequisites

- .NET 8.0 SDK or later
- Playwright browsers installed
- API server running (localhost:5000 by default)
- MySQL and Redis containers running (for integration tests)

## Project Structure

```
YoutubeRag.Tests.E2E/
├── Configuration/
│   └── TestSettings.cs          # Configuration models
├── Fixtures/
│   └── E2ETestBase.cs          # Base test class with setup/teardown
├── PageObjects/
│   ├── ApiClient.cs            # Base API client
│   ├── AuthApi.cs              # Authentication endpoints
│   ├── VideosApi.cs            # Video management endpoints
│   └── SearchApi.cs            # Search endpoints
├── Tests/
│   ├── VideoIngestionE2ETests.cs  # Video ingestion flow tests
│   └── SearchE2ETests.cs          # Search flow tests
├── appsettings.E2E.json        # Test configuration
├── .runsettings                # Test runner settings
└── YoutubeRag.Tests.E2E.csproj
```

## Setup Instructions

### 1. Install Playwright Browsers

After building the E2E project for the first time, install Playwright browsers:

```bash
# Install Playwright CLI globally (if not already installed)
dotnet tool install --global Microsoft.Playwright.CLI

# Install browsers
playwright install

# Or install specific browser
playwright install chromium
```

### 2. Configure Test Settings

Edit `appsettings.E2E.json` to match your environment:

```json
{
  "TestSettings": {
    "BaseUrl": "http://localhost:5000",
    "ApiBaseUrl": "http://localhost:5000/api/v1",
    "TestTimeout": 30000,
    "Headless": true
  },
  "TestData": {
    "ValidYouTubeUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
    "TestUser": {
      "Email": "e2etest@youtuberag.com",
      "Password": "E2ETest@123!",
      "Name": "E2E Test User"
    }
  }
}
```

### 3. Start Required Services

Ensure the API and dependencies are running:

```bash
# Start MySQL and Redis (if using Docker)
docker-compose up -d mysql redis

# Run the API
cd YoutubeRag.Api
dotnet run
```

## Running Tests

### Run All E2E Tests

```bash
# From solution root
dotnet test YoutubeRag.Tests.E2E

# With detailed output
dotnet test YoutubeRag.Tests.E2E --logger "console;verbosity=detailed"

# Run specific category
dotnet test YoutubeRag.Tests.E2E --filter Category=VideoIngestion
dotnet test YoutubeRag.Tests.E2E --filter Category=Search
```

### Run with Settings File

```bash
dotnet test YoutubeRag.Tests.E2E --settings YoutubeRag.Tests.E2E/.runsettings
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~IngestVideo_WithValidYouTubeUrl"
```

### Run in Headed Mode (Show Browser)

Update `appsettings.E2E.json`:

```json
{
  "TestSettings": {
    "Headless": false,
    "SlowMo": 500
  }
}
```

## Test Configuration

### Environment Variables

Override configuration using environment variables:

```bash
export TestSettings__BaseUrl=http://localhost:5000
export TestSettings__Headless=false
dotnet test YoutubeRag.Tests.E2E
```

### Test Timeouts

- **Test Timeout**: 30 seconds (default)
- **API Timeout**: 30 seconds
- **Wait Timeout**: 5 seconds

### Browsers

Configure which browsers to test:

```json
{
  "Browsers": {
    "Chromium": true,
    "Firefox": false,
    "WebKit": false
  }
}
```

## Writing New Tests

### 1. Create Test Class

Inherit from `E2ETestBase`:

```csharp
using YoutubeRag.Tests.E2E.Fixtures;
using NUnit.Framework;
using FluentAssertions;

namespace YoutubeRag.Tests.E2E.Tests;

[TestFixture]
[Category("E2E")]
[Category("MyFeature")]
public class MyFeatureE2ETests : E2ETestBase
{
    [SetUp]
    public async Task TestSetUp()
    {
        // Authenticate before each test
        await AuthenticateAsync();
    }

    [Test]
    [Order(1)]
    public async Task MyTest_WithValidInput_ShouldSucceed()
    {
        // Arrange
        var input = "test data";

        // Act
        var response = await VideosApi.GetVideosAsync();

        // Assert
        response.Status.Should().Be(200);
    }
}
```

### 2. Use Page Objects

Page Objects are available in the base class:

- `AuthApi` - Authentication operations
- `VideosApi` - Video management operations
- `SearchApi` - Search operations

### 3. Follow AAA Pattern

```csharp
[Test]
public async Task TestName_Condition_ExpectedResult()
{
    // Arrange - Setup test data and preconditions
    var testData = PrepareTestData();

    // Act - Execute the operation being tested
    var result = await SomeOperation(testData);

    // Assert - Verify the outcome
    result.Should().Be(expectedValue);
}
```

### 4. Add Documentation

Always add XML documentation to tests:

```csharp
/// <summary>
/// Test: Verify video ingestion with valid YouTube URL
/// </summary>
[Test]
public async Task IngestVideo_WithValidUrl_ShouldCreateVideo()
{
    // Test implementation
}
```

## Debugging Tests

### 1. Enable Headed Mode

Set `Headless: false` in configuration to see browser actions.

### 2. Add Breakpoints

Use standard .NET debugging:

```csharp
[Test]
public async Task MyTest()
{
    var response = await VideosApi.GetVideosAsync();
    System.Diagnostics.Debugger.Break(); // Add breakpoint
    Assert.That(response.Ok);
}
```

### 3. Screenshots and Traces

Screenshots are automatically captured on test failure:

- Location: `TestResults/Screenshots/`
- Traces: `TestResults/trace_*.zip`

View traces using Playwright:

```bash
playwright show-trace TestResults/trace_TestName_*.zip
```

### 4. Verbose Logging

```bash
dotnet test YoutubeRag.Tests.E2E --logger "console;verbosity=detailed"
```

## CI/CD Integration

### GitHub Actions

The E2E tests are integrated into the CI/CD pipeline:

```yaml
- name: Install Playwright Browsers
  run: |
    dotnet tool install --global Microsoft.Playwright.CLI
    playwright install --with-deps chromium

- name: Run E2E Tests
  run: dotnet test YoutubeRag.Tests.E2E --logger "trx;LogFileName=e2e-results.trx"

- name: Upload Test Results
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: e2e-test-results
    path: TestResults/
```

### Test Reports

Reports are generated in multiple formats:

- **HTML**: `TestResults/report.html`
- **TRX**: `TestResults/results.trx`
- **JUnit XML**: `TestResults/junit.xml`

## Best Practices

### 1. Test Independence

- Each test should be independent
- Use unique test data (e.g., `GetTestUniqueId()`)
- Clean up after tests if needed

### 2. Explicit Waits

Use explicit waits instead of fixed delays:

```csharp
// Good
await WaitForVideoProcessingAsync(videoId, timeoutSeconds: 60);

// Avoid
await Task.Delay(5000);
```

### 3. Meaningful Assertions

```csharp
// Good
response.Status.Should().Be(200, "Video ingestion should succeed");

// Less informative
Assert.That(response.Status == 200);
```

### 4. Page Object Pattern

Always use Page Objects for API interactions:

```csharp
// Good
var response = await VideosApi.IngestVideoAsync(url, title);

// Avoid direct HTTP calls
var response = await HttpClient.PostAsync(...);
```

### 5. Test Organization

- Use `[Order]` attribute for dependent tests
- Group related tests in same class
- Use descriptive test names: `Method_Condition_ExpectedResult`

### 6. Error Handling

```csharp
[Test]
public async Task ShouldHandleInvalidInput()
{
    var response = await VideosApi.IngestVideoAsync("invalid-url");

    response.Status.Should().BeOneOf(400, 422);
    var body = await response.TextAsync();
    body.Should().Contain("error");
}
```

## Troubleshooting

### Common Issues

#### 1. Playwright Browsers Not Installed

**Error**: "Executable doesn't exist at..."

**Solution**:
```bash
playwright install chromium
```

#### 2. API Not Running

**Error**: Connection refused on localhost:5000

**Solution**:
```bash
cd YoutubeRag.Api
dotnet run
```

#### 3. Authentication Fails

**Error**: 401 Unauthorized

**Solution**: Check test user credentials in `appsettings.E2E.json`

#### 4. Tests Timeout

**Error**: Test exceeded timeout

**Solution**: Increase timeout in configuration:
```json
{
  "TestSettings": {
    "TestTimeout": 60000
  }
}
```

#### 5. Database Connection Issues

**Error**: Cannot connect to MySQL

**Solution**:
```bash
docker-compose up -d mysql
```

### Debug Logs

Enable detailed Playwright logs:

```bash
export DEBUG=pw:api
dotnet test YoutubeRag.Tests.E2E
```

### Test Isolation

If tests interfere with each other:

```csharp
[OneTimeSetUp]
public async Task OneTimeSetup()
{
    // Clean test database
    await CleanupTestDataAsync();
}
```

## Performance Tips

1. **Parallel Execution**: Configure in `.runsettings`:
   ```xml
   <NUnit>
     <NumberOfTestWorkers>4</NumberOfTestWorkers>
   </NUnit>
   ```

2. **Reuse Authentication**: Authenticate once in `SetUp` instead of per test

3. **Optimize Waits**: Use appropriate poll intervals:
   ```csharp
   await WaitForVideoProcessingAsync(videoId,
       timeoutSeconds: 60,
       pollIntervalSeconds: 2);
   ```

## Additional Resources

- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [NUnit Documentation](https://docs.nunit.org/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Project Test Strategy](./TESTING_METHODOLOGY_RULES.md)

## Support

For issues or questions:
1. Check this documentation
2. Review test logs in `TestResults/`
3. Check Playwright traces for failures
4. Review the existing test implementations

## Test Coverage

Current E2E test coverage:

- **Video Ingestion**: 7 tests
  - Submit URL successfully
  - Metadata extraction
  - Processing status updates
  - Invalid URL handling
  - Duplicate detection
  - Video list appearance
  - Video deletion

- **Search Flow**: 10 tests
  - Semantic search
  - Search with filters
  - Pagination
  - Empty results handling
  - Keyword search
  - Advanced search
  - Search suggestions
  - Trending searches
  - Search history
  - Validation

**Total**: 17 E2E tests covering critical user journeys
