namespace YoutubeRag.Api.Configuration;

public class AppSettings
{
    public const string SectionName = "AppSettings";

    public string Environment { get; set; } = "Development";
    public string ProcessingMode { get; set; } = "Mock";
    public string StorageMode { get; set; } = "Database";
    public bool EnableAuth { get; set; } = true;
    public bool EnableWebSockets { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableRealProcessing { get; set; } = false;
    public bool EnableDocs { get; set; } = true;
    public bool EnableCors { get; set; } = true;

    // Helper properties
    public bool IsDevelopment => Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
    public bool IsProduction => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
    public bool IsTesting => Environment.Equals("Testing", StringComparison.OrdinalIgnoreCase);

    public bool UseMockProcessing => ProcessingMode.Equals("Mock", StringComparison.OrdinalIgnoreCase);
    public bool UseRealProcessing => ProcessingMode.Equals("Real", StringComparison.OrdinalIgnoreCase);
    public bool UseHybridProcessing => ProcessingMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase);

    public bool UseMemoryStorage => StorageMode.Equals("Memory", StringComparison.OrdinalIgnoreCase);
    public bool UseDatabaseStorage => StorageMode.Equals("Database", StringComparison.OrdinalIgnoreCase);
    public bool UseHybridStorage => StorageMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase);
}

public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; set; } = 100;
    public int WindowMinutes { get; set; } = 1;
}