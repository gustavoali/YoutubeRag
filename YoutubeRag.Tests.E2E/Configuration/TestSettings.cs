namespace YoutubeRag.Tests.E2E.Configuration;

/// <summary>
/// Configuration settings for E2E tests
/// </summary>
public class TestSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public string ApiBaseUrl { get; set; } = "http://localhost:5000/api/v1";
    public int TestTimeout { get; set; } = 30000;
    public int DefaultWaitTimeout { get; set; } = 5000;
    public bool ScreenshotOnFailure { get; set; } = true;
    public bool VideoRecording { get; set; } = true;
    public bool Headless { get; set; } = true;
    public int SlowMo { get; set; } = 0;
    public string BrowserType { get; set; } = "chromium";
}

/// <summary>
/// Test data configuration
/// </summary>
public class TestData
{
    public string ValidYouTubeUrl { get; set; } = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
    public string InvalidYouTubeUrl { get; set; } = "https://invalid-url.com/video";
    public TestUserData TestUser { get; set; } = new();
}

/// <summary>
/// Test user data
/// </summary>
public class TestUserData
{
    public string Email { get; set; } = "e2etest@youtuberag.com";
    public string Password { get; set; } = "E2ETest@123!";
    public string Name { get; set; } = "E2E Test User";
}

/// <summary>
/// Browser configuration
/// </summary>
public class BrowsersConfig
{
    public bool Chromium { get; set; } = true;
    public bool Firefox { get; set; } = false;
    public bool WebKit { get; set; } = false;
}

/// <summary>
/// Reporting configuration
/// </summary>
public class ReportingConfig
{
    public string OutputDirectory { get; set; } = "TestResults";
    public string ScreenshotsDirectory { get; set; } = "TestResults/Screenshots";
    public string VideosDirectory { get; set; } = "TestResults/Videos";
    public string HtmlReportPath { get; set; } = "TestResults/report.html";
    public string JUnitReportPath { get; set; } = "TestResults/junit.xml";
}

/// <summary>
/// Database configuration for tests
/// </summary>
public class DatabaseConfig
{
    public string ConnectionString { get; set; } = "Server=localhost;Port=3306;Database=youtube_rag_test_e2e;Uid=youtube_rag_user;Pwd=youtube_rag_password;";
    public bool CleanupAfterTests { get; set; } = true;
}

/// <summary>
/// Root configuration class
/// </summary>
public class E2EConfiguration
{
    public TestSettings TestSettings { get; set; } = new();
    public TestData TestData { get; set; } = new();
    public BrowsersConfig Browsers { get; set; } = new();
    public ReportingConfig Reporting { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
}
