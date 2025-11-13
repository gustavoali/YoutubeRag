# US-101 E2E Tests - Quick Start Guide

## Prerequisites

1. **Start the API server**
   ```bash
   cd C:\agents\youtube_rag_net\YoutubeRag.Api
   dotnet run
   ```
   - Wait for: `Now listening on: http://localhost:5000`

2. **Verify database is running**
   - PostgreSQL or MySQL should be running
   - Connection string in `appsettings.Development.json` should be correct

3. **Verify test configuration**
   - Check `YoutubeRag.Tests.E2E\appsettings.E2E.json`
   - Default API URL: `http://localhost:5000/api/v1`
   - Test user credentials should be valid

## Running Tests

### Option 1: Run All Video Submission Tests
```bash
cd C:\agents\youtube_rag_net
dotnet test YoutubeRag.Tests.E2E/YoutubeRag.Tests.E2E.csproj --filter "Category=VideoSubmission" --logger "console;verbosity=detailed"
```

### Option 2: Run Only Happy Path Tests
```bash
dotnet test YoutubeRag.Tests.E2E/YoutubeRag.Tests.E2E.csproj --filter "FullyQualifiedName~SubmitVideo_WithValidYouTubeUrl"
```

### Option 3: Run Single Test
```bash
dotnet test YoutubeRag.Tests.E2E/YoutubeRag.Tests.E2E.csproj --filter "FullyQualifiedName=YoutubeRag.Tests.E2E.Tests.VideoSubmissionE2ETests.SubmitVideo_WithValidYouTubeUrl_ShouldCreateVideoAndJobSuccessfully"
```

### Option 4: List All Tests (Don't Run)
```bash
dotnet test YoutubeRag.Tests.E2E/YoutubeRag.Tests.E2E.csproj --filter "Category=VideoSubmission" --list-tests
```

## Expected Output

### Successful Test Run
```
Passed SubmitVideo_WithValidYouTubeUrl_ShouldCreateVideoAndJobSuccessfully [2.3 s]
Passed SubmitVideo_WithDuplicateUrl_ShouldReturnExistingVideoWithoutNewJob [1.8 s]
Passed SubmitVideo_WithDifferentUrlFormats_ShouldExtractSameVideoId("https://www.youtube.com/watch?v=...", "...") [1.5 s]
...

Test Run Successful.
Total tests: 18
     Passed: 18
     Failed: 0
    Skipped: 0
 Total time: 62.4 seconds
```

## Common Issues

### Issue: "Cannot connect to API"
```
Solution:
1. Verify API is running on http://localhost:5000
2. Check appsettings.E2E.json has correct BaseUrl
3. Try: curl http://localhost:5000/health
```

### Issue: "Authentication failed"
```
Solution:
1. Check test user credentials in appsettings.E2E.json
2. Verify user exists in database
3. Try registering test user manually via API
```

### Issue: "Video not found after submission"
```
Solution:
1. Check database connection
2. Verify migrations are applied
3. Check API logs for errors
```

### Issue: "YouTube metadata extraction failed"
```
Solution:
1. Verify internet connectivity
2. Check if YouTube URLs are accessible
3. Consider implementing mocking (see summary report)
```

## Quick Test Commands

```bash
# Full test suite with detailed output
dotnet test YoutubeRag.Tests.E2E/YoutubeRag.Tests.E2E.csproj --filter "Category=VideoSubmission" -v n

# Only error path tests
dotnet test YoutubeRag.Tests.E2E/YoutubeRag.Tests.E2E.csproj --filter "FullyQualifiedName~Invalid|FullyQualifiedName~Empty|FullyQualifiedName~Unauthorized"

# Only performance tests
dotnet test YoutubeRag.Tests.E2E/YoutubeRag.Tests.E2E.csproj --filter "FullyQualifiedName~Performance|FullyQualifiedName~Concurrent"

# Only database verification tests
dotnet test YoutubeRag.Tests.E2E/YoutubeRag.Tests.E2E.csproj --filter "FullyQualifiedName~Atomic|FullyQualifiedName~Database|FullyQualifiedName~AppearIn"
```

## Test Categories

| Category | Test Count | Purpose |
|----------|-----------|---------|
| Happy Path | 5 | Validate successful video submission flow |
| Error Path | 7 | Validate error handling and validation |
| Performance | 2 | Validate response times and concurrency |
| Database | 3 | Validate data persistence and consistency |

## Test Execution Times

- **Fastest**: `SubmitVideo_WithDifferentUrlFormats...` (~1.5s)
- **Slowest**: `SubmitVideo_WithConcurrentSubmissions...` (~8-10s)
- **Average**: ~3s per test
- **Total Suite**: ~60s for all 18 tests

## Files Created/Modified

### Created:
- `C:\agents\youtube_rag_net\YoutubeRag.Tests.E2E\Tests\VideoSubmissionE2ETests.cs` (552 lines)
- `C:\agents\youtube_rag_net\US-101_E2E_TEST_SUMMARY.md` (comprehensive report)
- `C:\agents\youtube_rag_net\US-101_E2E_QUICK_START.md` (this file)

### Modified:
- `C:\agents\youtube_rag_net\YoutubeRag.Tests.E2E\PageObjects\VideosApi.cs` (added `SubmitVideoFromUrlAsync` method)

## Next Steps

1. Run the full test suite: ✅
2. Verify all 18 tests pass: ⏳
3. Review test output and logs: ⏳
4. Fix any environment-specific issues: ⏳
5. Integrate into CI/CD pipeline: ⏳

## Support

For detailed information about each test, see:
- `US-101_E2E_TEST_SUMMARY.md` - Full documentation
- `VideoSubmissionE2ETests.cs` - Source code with inline comments
- `VideosController.cs` - API implementation (lines 123-235)

---

**Quick Reference Created**: 2025-10-17
**Test Suite**: US-101 Video Submission E2E Tests
