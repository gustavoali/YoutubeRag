using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using YoutubeRag.Tests.E2E.Configuration;
using YoutubeRag.Tests.E2E.PageObjects;

namespace YoutubeRag.Tests.E2E.Fixtures;

/// <summary>
/// Base class for all E2E tests with Playwright integration
/// </summary>
[TestFixture]
public abstract class E2ETestBase : PageTest
{
    protected E2EConfiguration Config { get; private set; } = null!;
    protected IAPIRequestContext ApiContext { get; private set; } = null!;
    protected AuthApi AuthApi { get; private set; } = null!;
    protected VideosApi VideosApi { get; private set; } = null!;
    protected SearchApi SearchApi { get; private set; } = null!;
    protected string? AuthToken { get; private set; }
    protected string TestRunId { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.E2E.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        Config = new E2EConfiguration();
        configuration.Bind(Config);

        // Generate unique test run ID
        TestRunId = $"e2e_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";

        // Ensure output directories exist
        EnsureDirectoriesExist();

        Console.WriteLine($"Starting E2E Test Run: {TestRunId}");
        Console.WriteLine($"Base URL: {Config.TestSettings.BaseUrl}");
        Console.WriteLine($"API Base URL: {Config.TestSettings.ApiBaseUrl}");
    }

    [SetUp]
    public async Task SetUp()
    {
        // Create API request context
        ApiContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = Config.TestSettings.ApiBaseUrl,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            IgnoreHTTPSErrors = true
        });

        // Initialize Page Objects
        AuthApi = new AuthApi(ApiContext, Config.TestSettings.ApiBaseUrl);
        VideosApi = new VideosApi(ApiContext, Config.TestSettings.ApiBaseUrl);
        SearchApi = new SearchApi(ApiContext, Config.TestSettings.ApiBaseUrl);

        // Configure browser context for screenshots and videos
        if (Context != null)
        {
            await Context.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
        }
    }

    [TearDown]
    public async Task TearDown()
    {
        var testName = TestContext.CurrentContext.Test.Name;
        var testResult = TestContext.CurrentContext.Result.Outcome.Status;

        // Take screenshot on failure
        if (testResult == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            if (Config.TestSettings.ScreenshotOnFailure && Page != null)
            {
                var screenshotPath = Path.Combine(
                    Config.Reporting.ScreenshotsDirectory,
                    $"{testName}_{TestRunId}_failure.png"
                );
                await Page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
                TestContext.Out.WriteLine($"Screenshot saved: {screenshotPath}");
            }

            // Save trace on failure
            if (Context != null)
            {
                var tracePath = Path.Combine(
                    Config.Reporting.OutputDirectory,
                    $"trace_{testName}_{TestRunId}.zip"
                );
                await Context.Tracing.StopAsync(new() { Path = tracePath });
                TestContext.Out.WriteLine($"Trace saved: {tracePath}");
            }
        }
        else if (Context != null)
        {
            await Context.Tracing.StopAsync();
        }

        // Dispose API context
        if (ApiContext != null)
        {
            await ApiContext.DisposeAsync();
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        Console.WriteLine($"E2E Test Run Completed: {TestRunId}");
    }

    /// <summary>
    /// Authenticate and set auth token for all API clients
    /// </summary>
    protected async Task<string> AuthenticateAsync(string? email = null, string? password = null)
    {
        email ??= Config.TestData.TestUser.Email;
        password ??= Config.TestData.TestUser.Password;

        var response = await AuthApi.LoginAsync(email, password);

        if (!response.Ok)
        {
            // Try to register if login fails
            response = await AuthApi.RegisterAsync(
                email,
                password,
                Config.TestData.TestUser.Name
            );
        }

        Assert.That(response.Ok, Is.True, $"Authentication failed with status {response.Status}");

        AuthToken = await AuthApi.ExtractAccessTokenAsync(response);
        Assert.That(AuthToken, Is.Not.Null.And.Not.Empty, "Auth token should not be null or empty");

        // Set auth token for all API clients
        AuthApi.SetAuthToken(AuthToken!);
        VideosApi.SetAuthToken(AuthToken!);
        SearchApi.SetAuthToken(AuthToken!);

        return AuthToken!;
    }

    /// <summary>
    /// Wait for video processing to complete or fail
    /// </summary>
    protected async Task<bool> WaitForVideoProcessingAsync(
        string videoId,
        int timeoutSeconds = 60,
        int pollIntervalSeconds = 2)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        while (DateTime.UtcNow - startTime < timeout)
        {
            var response = await VideosApi.GetVideoProgressAsync(videoId);

            if (response.Ok)
            {
                var progressText = await response.TextAsync();
                Console.WriteLine($"Video {videoId} progress: {progressText}");

                // Check if processing is complete
                if (progressText.Contains("\"status\":\"Completed\"") ||
                    progressText.Contains("\"status\":\"completed\"") ||
                    progressText.Contains("\"progressPercentage\":100"))
                {
                    return true;
                }

                // Check if processing failed
                if (progressText.Contains("\"status\":\"Failed\"") ||
                    progressText.Contains("\"status\":\"failed\""))
                {
                    return false;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds));
        }

        return false;
    }

    /// <summary>
    /// Ensure all required directories exist
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(Config.Reporting.OutputDirectory);
        Directory.CreateDirectory(Config.Reporting.ScreenshotsDirectory);
        Directory.CreateDirectory(Config.Reporting.VideosDirectory);
    }

    /// <summary>
    /// Get test-specific unique identifier
    /// </summary>
    protected string GetTestUniqueId()
    {
        return $"{TestContext.CurrentContext.Test.Name}_{TestRunId}";
    }
}
