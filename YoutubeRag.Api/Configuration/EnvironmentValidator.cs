using System.Text;

namespace YoutubeRag.Api.Configuration;

/// <summary>
/// Validates required environment configuration on application startup.
/// DEVOPS-007: Fail fast with clear error messages when configuration is invalid.
/// </summary>
public static class EnvironmentValidator
{
    /// <summary>
    /// Validates all required environment configuration.
    /// Throws InvalidOperationException with detailed error messages if validation fails.
    /// </summary>
    /// <param name="configuration">The application configuration</param>
    /// <param name="environment">The hosting environment</param>
    /// <exception cref="InvalidOperationException">When required configuration is missing or invalid</exception>
    public static void ValidateConfiguration(IConfiguration configuration, IHostEnvironment environment)
    {
        var errors = new List<string>();

        // Skip validation in Testing environment
        if (environment.EnvironmentName == "Testing")
        {
            return;
        }

        // 1. Database Connection String
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("❌ Database connection string 'DefaultConnection' is required");
        }
        else
        {
            // Validate connection string format
            if (!connectionString.Contains("Server=") || !connectionString.Contains("Database="))
            {
                errors.Add("❌ Database connection string format is invalid (must contain Server= and Database=)");
            }
        }

        // 2. Redis Connection String
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            errors.Add("❌ Redis connection string is required");
        }

        // 3. JWT Secret (must be at least 32 bytes / 256 bits)
        var jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            errors.Add("❌ JWT Secret (Jwt:Secret) is required for authentication");
        }
        else if (jwtSecret.Length < 32)
        {
            errors.Add($"❌ JWT Secret must be at least 32 characters (current: {jwtSecret.Length}). " +
                      "Generate a secure secret with: openssl rand -base64 32");
        }

        // 4. JWT Issuer and Audience
        var jwtIssuer = configuration["Jwt:Issuer"];
        if (string.IsNullOrWhiteSpace(jwtIssuer))
        {
            errors.Add("⚠️  JWT Issuer (Jwt:Issuer) is not set (optional but recommended)");
        }

        var jwtAudience = configuration["Jwt:Audience"];
        if (string.IsNullOrWhiteSpace(jwtAudience))
        {
            errors.Add("⚠️  JWT Audience (Jwt:Audience) is not set (optional but recommended)");
        }

        // 5. Whisper Configuration
        var whisperModelsPath = configuration["Whisper:ModelsPath"];
        if (!string.IsNullOrWhiteSpace(whisperModelsPath))
        {
            try
            {
                var fullPath = Path.GetFullPath(whisperModelsPath);
                // Check if path is writable (parent directory must exist)
                var parentDir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
                {
                    // Try to ensure directory exists
                    Directory.CreateDirectory(fullPath);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"❌ Whisper models path '{whisperModelsPath}' is invalid: {ex.Message}");
            }
        }

        // 6. Application Paths (if configured)
        ValidatePathConfiguration(configuration, "Paths:Temp", errors);
        ValidatePathConfiguration(configuration, "Paths:Uploads", errors);
        ValidatePathConfiguration(configuration, "Paths:Models", errors);

        // 7. Disk Space Warning
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            foreach (var drive in drives)
            {
                var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                if (freeSpaceGB < 10)
                {
                    errors.Add($"⚠️  Low disk space on drive {drive.Name}: {freeSpaceGB:F2} GB free " +
                              "(minimum 10GB recommended for Whisper models)");
                }
            }
        }
        catch
        {
            // Ignore disk space check errors
        }

        // 8. CORS Configuration (warning only)
        var corsOrigins = configuration["Cors:AllowedOrigins"];
        if (string.IsNullOrWhiteSpace(corsOrigins) && !environment.IsDevelopment())
        {
            errors.Add("⚠️  CORS AllowedOrigins not configured (may block frontend access)");
        }

        // Throw if any critical errors found
        var criticalErrors = errors.Where(e => e.StartsWith("❌")).ToList();
        if (criticalErrors.Any())
        {
            var errorMessage = BuildErrorMessage(errors, environment);
            throw new InvalidOperationException(errorMessage);
        }

        // Log warnings
        var warnings = errors.Where(e => e.StartsWith("⚠️")).ToList();
        if (warnings.Any())
        {
            Console.WriteLine();
            Console.WriteLine("⚠️  Configuration Warnings:");
            foreach (var warning in warnings)
            {
                Console.WriteLine($"   {warning}");
            }

            Console.WriteLine();
        }
    }

    private static void ValidatePathConfiguration(IConfiguration configuration, string key, List<string> errors)
    {
        var path = configuration[key];
        if (string.IsNullOrWhiteSpace(path))
        {
            return; // Optional path, skip validation
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            var parentDir = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                errors.Add($"⚠️  Path '{key}' parent directory does not exist: {parentDir}");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"❌ Path configuration '{key}' is invalid: {ex.Message}");
        }
    }

    private static string BuildErrorMessage(List<string> errors, IHostEnvironment environment)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("❌ CONFIGURATION VALIDATION FAILED");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Environment: {environment.EnvironmentName}");
        sb.AppendLine($"Application: YoutubeRag.Api");
        sb.AppendLine();
        sb.AppendLine("The application cannot start due to the following configuration errors:");
        sb.AppendLine();

        foreach (var error in errors)
        {
            sb.AppendLine($"  {error}");
        }

        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("REQUIRED ACTIONS:");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("1. Ensure .env file exists:");
        sb.AppendLine("   cp .env.template .env");
        sb.AppendLine();
        sb.AppendLine("2. Configure required variables in .env:");
        sb.AppendLine("   - DATABASE_HOST, DATABASE_PORT, DATABASE_NAME");
        sb.AppendLine("   - REDIS_HOST, REDIS_PORT");
        sb.AppendLine("   - JWT_SECRET (minimum 32 characters)");
        sb.AppendLine();
        sb.AppendLine("3. Verify infrastructure services are running:");
        sb.AppendLine("   docker-compose ps");
        sb.AppendLine("   docker-compose up -d mysql redis");
        sb.AppendLine();
        sb.AppendLine("4. Run automated setup script:");
        sb.AppendLine("   Windows: .\\scripts\\dev-setup.ps1");
        sb.AppendLine("   Linux:   ./scripts/dev-setup.sh");
        sb.AppendLine();
        sb.AppendLine("For detailed setup instructions, see:");
        sb.AppendLine("  - README.md");
        sb.AppendLine("  - docs/devops/DEVELOPER_SETUP_GUIDE.md");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Displays configuration summary (non-sensitive information)
    /// </summary>
    public static void DisplayConfigurationSummary(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.EnvironmentName == "Testing")
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("✅ CONFIGURATION VALIDATED SUCCESSFULLY");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine($"Environment:        {environment.EnvironmentName}");
        Console.WriteLine($"Application:        YoutubeRag.Api");
        Console.WriteLine($"Content Root:       {environment.ContentRootPath}");
        Console.WriteLine();

        // Database (hide password)
        var connString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connString))
        {
            var safeConnString = MaskPassword(connString);
            Console.WriteLine($"Database:           {safeConnString}");
        }

        // Redis (hide password if present)
        var redisString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisString))
        {
            var safeRedisString = MaskPassword(redisString);
            Console.WriteLine($"Redis:              {safeRedisString}");
        }

        // Whisper
        var whisperModelsPath = configuration["Whisper:ModelsPath"];
        if (!string.IsNullOrWhiteSpace(whisperModelsPath))
        {
            Console.WriteLine($"Whisper Models:     {whisperModelsPath}");
        }

        // JWT (never show secret)
        var jwtIssuer = configuration["Jwt:Issuer"];
        var jwtAudience = configuration["Jwt:Audience"];
        if (!string.IsNullOrWhiteSpace(jwtIssuer))
        {
            Console.WriteLine($"JWT Issuer:         {jwtIssuer}");
        }

        if (!string.IsNullOrWhiteSpace(jwtAudience))
        {
            Console.WriteLine($"JWT Audience:       {jwtAudience}");
        }

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static string MaskPassword(string connectionString)
    {
        // Mask password in connection string for display
        var patterns = new[] { "password=", "pwd=", "Pwd=", "Password=" };
        var result = connectionString;

        foreach (var pattern in patterns)
        {
            var index = result.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var start = index + pattern.Length;
                var end = result.IndexOfAny(new[] { ';', '&' }, start);
                if (end < 0)
                {
                    end = result.Length;
                }

                var password = result.Substring(start, end - start);
                result = result.Replace(password, "****");
            }
        }

        return result;
    }
}
