# YoutubeRag E2E Tests

End-to-End testing suite for YoutubeRag.NET using Playwright for .NET.

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Playwright browsers
- Running API (localhost:5000)

### Install Playwright

```bash
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

### Run Tests

```bash
# Run all E2E tests
dotnet test

# Run specific category
dotnet test --filter Category=VideoIngestion
dotnet test --filter Category=Search

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Configuration

Edit `appsettings.E2E.json` to configure:
- API URLs
- Test data
- Browser settings
- Reporting options

## Test Coverage

### Video Ingestion (7 tests)
- ✅ Submit YouTube URL successfully
- ✅ Metadata extraction
- ✅ Processing status updates
- ✅ Invalid URL handling
- ✅ Duplicate detection
- ✅ Video list appearance
- ✅ Video deletion

### Search Flow (10 tests)
- ✅ Semantic search
- ✅ Search with filters
- ✅ Pagination
- ✅ Empty results handling
- ✅ Keyword search
- ✅ Advanced search
- ✅ Search suggestions
- ✅ Trending searches
- ✅ Search history
- ✅ Validation

**Total: 17 E2E tests**

## Documentation

See [docs/E2E_TESTING.md](../docs/E2E_TESTING.md) for:
- Detailed setup instructions
- Writing new tests guide
- Debugging techniques
- CI/CD integration
- Best practices
- Troubleshooting

## Project Structure

```
YoutubeRag.Tests.E2E/
├── Configuration/       # Test configuration models
├── Fixtures/           # Base test classes
├── PageObjects/        # API endpoint abstractions
├── Tests/             # Test implementations
├── appsettings.E2E.json
└── .runsettings
```

## Features

- **Page Object Model**: Clean API abstraction
- **Automatic Screenshots**: On test failure
- **Trace Recording**: Full execution traces
- **Multiple Browsers**: Chromium, Firefox, WebKit support
- **Parallel Execution**: Fast test runs
- **Rich Reporting**: HTML, TRX, JUnit formats
- **CI/CD Ready**: GitHub Actions integration

## Support

For issues or questions:
1. Check [E2E_TESTING.md](../docs/E2E_TESTING.md)
2. Review test logs in `TestResults/`
3. Analyze Playwright traces for failures
