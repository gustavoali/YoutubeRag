using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using StackExchange.Redis;
using YoutubeRag.Infrastructure.Data;
using YoutubeRag.Api.Configuration;
using YoutubeRag.Api.Authentication;
using Hangfire;
using Hangfire.MySql;
using Hangfire.Dashboard;
using YoutubeRag.Api.Filters;
using YoutubeRag.Infrastructure.Jobs;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

// Configure Serilog early in the application startup
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("Starting YouTube RAG API");

try
{
    // Entry point - Create and run the application
    var app = await Program.CreateWebApplication(args);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make the Program class accessible for testing
public partial class Program
{
    // Factory method to create the WebApplication without running it
    public static async Task<WebApplication> CreateWebApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json - skip in Testing environment to avoid frozen logger issue
    if (builder.Environment.EnvironmentName != "Testing")
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "YoutubeRag.Api")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/youtuberag-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
        );
    }

    // Configuration
    var configuration = builder.Configuration;
    var environment = builder.Environment;

    // Bind configuration sections
    var appSettings = new AppSettings();
    configuration.GetSection(AppSettings.SectionName).Bind(appSettings);
    builder.Services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));

    var rateLimitingSettings = new RateLimitingSettings();
    configuration.GetSection(RateLimitingSettings.SectionName).Bind(rateLimitingSettings);
    builder.Services.Configure<RateLimitingSettings>(configuration.GetSection(RateLimitingSettings.SectionName));

    // Configure Whisper options
    builder.Services.Configure<YoutubeRag.Application.Configuration.WhisperOptions>(
        configuration.GetSection(YoutubeRag.Application.Configuration.WhisperOptions.SectionName));

    // Register IAppConfiguration
    builder.Services.AddSingleton<YoutubeRag.Application.Configuration.IAppConfiguration, YoutubeRag.Api.Configuration.AppConfiguration>();

    // Add services to the container
    builder.Services.AddControllers();

    // Add HttpClient for external API calls
    builder.Services.AddHttpClient();

    // Add Memory Cache
    builder.Services.AddMemoryCache();

    // Add AutoMapper - scan the Application assembly for all mapping profiles
    builder.Services.AddAutoMapper(typeof(YoutubeRag.Application.Mappings.UserMappingProfile).Assembly);

    // Register application services
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.Services.IAuthService,
        YoutubeRag.Application.Services.AuthService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.Services.IUserService,
        YoutubeRag.Application.Services.UserService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.Services.IVideoService,
        YoutubeRag.Application.Services.VideoService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.Services.ISearchService,
        YoutubeRag.Application.Services.SearchService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.Services.IVideoIngestionService,
        YoutubeRag.Application.Services.VideoIngestionService>();

    // Register infrastructure services
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IVideoProcessingService,
        YoutubeRag.Infrastructure.Services.VideoProcessingService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IEmbeddingService,
        YoutubeRag.Infrastructure.Services.LocalEmbeddingService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IYouTubeService,
        YoutubeRag.Infrastructure.Services.YouTubeService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IMetadataExtractionService,
        YoutubeRag.Infrastructure.Services.MetadataExtractionService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.ITranscriptionService,
        YoutubeRag.Infrastructure.Services.LocalWhisperService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IJobService,
        YoutubeRag.Infrastructure.Services.JobService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IAudioExtractionService,
        YoutubeRag.Infrastructure.Services.AudioExtractionService>();

    // Register video download and temp file management services
    builder.Services.AddSingleton<YoutubeRag.Application.Interfaces.ITempFileManagementService,
        YoutubeRag.Infrastructure.Services.TempFileManagementService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IVideoDownloadService,
        YoutubeRag.Infrastructure.Services.VideoDownloadService>();

    // Register application processors
    builder.Services.AddScoped<YoutubeRag.Application.Services.TranscriptionJobProcessor>();
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Services.EmbeddingJobProcessor>();

    // Register pipeline stage processors
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Jobs.DownloadJobProcessor>();
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Jobs.AudioExtractionJobProcessor>();
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Jobs.TranscriptionStageJobProcessor>();
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Jobs.SegmentationJobProcessor>();

    // Register Hangfire background job services
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.Services.IBackgroundJobService,
        YoutubeRag.Infrastructure.Services.HangfireJobService>();
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Jobs.TranscriptionBackgroundJob>();
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Jobs.EmbeddingBackgroundJob>();
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Jobs.VideoProcessingBackgroundJob>();
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Services.JobCleanupService>();
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Services.JobMonitoringService>();

    // Register segmentation service
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.ISegmentationService,
        YoutubeRag.Infrastructure.Services.SegmentationService>();

    // Register Whisper model management services
    builder.Services.AddSingleton<YoutubeRag.Application.Interfaces.IWhisperModelDownloadService,
        YoutubeRag.Infrastructure.Services.WhisperModelDownloadService>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IWhisperModelService,
        YoutubeRag.Application.Services.WhisperModelManager>();
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Jobs.WhisperModelCleanupJob>();

    // Register temp file cleanup job
    builder.Services.AddScoped<YoutubeRag.Infrastructure.Jobs.TempFileCleanupJob>();

    // Register repositories
    builder.Services.AddScoped(typeof(YoutubeRag.Application.Interfaces.IRepository<>),
        typeof(YoutubeRag.Infrastructure.Repositories.Repository<>));
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IUserRepository,
        YoutubeRag.Infrastructure.Repositories.UserRepository>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IVideoRepository,
        YoutubeRag.Infrastructure.Repositories.VideoRepository>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IJobRepository,
        YoutubeRag.Infrastructure.Repositories.JobRepository>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.ITranscriptSegmentRepository,
        YoutubeRag.Infrastructure.Repositories.TranscriptSegmentRepository>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IRefreshTokenRepository,
        YoutubeRag.Infrastructure.Repositories.RefreshTokenRepository>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IDeadLetterJobRepository,
        YoutubeRag.Infrastructure.Repositories.DeadLetterJobRepository>();
    builder.Services.AddScoped<YoutubeRag.Application.Interfaces.IUnitOfWork,
        YoutubeRag.Infrastructure.Repositories.UnitOfWork>();

    // Database Configuration - Conditional based on StorageMode
    if (appSettings.UseDatabaseStorage)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            "Server=localhost;Port=3306;Database=youtube_rag_db;Uid=youtube_rag_user;Pwd=youtube_rag_password;";

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 23))));
    }

    // Redis Configuration
    var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnectionString));

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });


    // Hangfire Configuration - Background job processing
    if (appSettings.EnableBackgroundJobs)
    {
        var hangfireConnectionString = configuration.GetConnectionString("DefaultConnection") ??
            "Server=localhost;Port=3306;Database=youtube_rag_db;Uid=youtube_rag_user;Pwd=youtube_rag_password;";
        
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseStorage(new MySqlStorage(
                hangfireConnectionString,
                new MySqlStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 50000,
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                    TablesPrefix = "Hangfire"
                })));

        builder.Services.AddHangfireServer(options =>
        {
            options.WorkerCount = appSettings.MaxConcurrentJobs ?? 3;
            options.Queues = new[] { "critical", "default", "low" };
            options.ServerName = $"YoutubeRag-{Environment.MachineName}";
        });
    }

    // SignalR Configuration - Real-time updates
    if (appSettings.EnableWebSockets)
    {
        builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = !appSettings.IsProduction;
            options.HandshakeTimeout = TimeSpan.FromSeconds(30);
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
            options.MaximumReceiveMessageSize = 102400; // 100 KB
        });
        // Optional: Configure SignalR with Redis for scaling
        // .AddStackExchangeRedis(redisConnectionString, options =>
        // {
        //     options.Configuration.ChannelPrefix = "YoutubeRag";
        // });

        // Register SignalR progress notification service
        builder.Services.AddSingleton<YoutubeRag.Application.Interfaces.Services.IProgressNotificationService,
            YoutubeRag.Api.Services.SignalRProgressNotificationService>();
    }
    else
    {
        // Use mock notification service if WebSockets are disabled
        builder.Services.AddSingleton<YoutubeRag.Application.Interfaces.Services.IProgressNotificationService,
            YoutubeRag.Infrastructure.Services.Mock.MockProgressNotificationService>();
    }
    // Authentication - Always configure, but with different behavior based on EnableAuth
    if (appSettings.EnableAuth)
    {
        // Real JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

        // Validate JWT SecretKey configuration
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException(
                "JWT SecretKey must be configured in appsettings.json under JwtSettings:SecretKey. " +
                "Generate a secure key with at least 256 bits (32 characters).");
        }

        if (secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                $"JWT SecretKey must be at least 256 bits (32 characters). Current length: {secretKey.Length}");
        }

        var key = Encoding.ASCII.GetBytes(secretKey);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = appSettings.IsProduction;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
    }
    else
    {
        // Ensure we're not in production
        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Mock authentication is disabled in production. EnableAuth must be true in production configuration.");
        }

        // Mock Authentication for development/testing - allows all requests
        builder.Services.AddAuthentication("Mock")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, MockAuthenticationHandler>(
                "Mock", options => { });
    }

    // CORS - Conditional based on EnableCors
    if (appSettings.EnableCors)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowedOrigins", policy =>
            {
                var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ??
                    new[] { "http://localhost:3000" };

                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .WithExposedHeaders("*");
            });
        });
    }

    // Rate Limiting - Always enabled but configurable
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = 429;
        options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(
            context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = rateLimitingSettings.PermitLimit,
                    Window = TimeSpan.FromMinutes(rateLimitingSettings.WindowMinutes)
                }));
    });

    // Health Checks - Comprehensive monitoring of critical components
    var healthChecksBuilder = builder.Services.AddHealthChecks();

    // Database health check - verify MySQL connection
    if (appSettings.UseDatabaseStorage)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            "Server=localhost;Port=3306;Database=youtube_rag_db;Uid=youtube_rag_user;Pwd=youtube_rag_password;";

        healthChecksBuilder.AddMySql(
            connectionString: connectionString,
            name: "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "db", "mysql", "critical" });
    }

    // Hangfire health check - verify background job processing
    if (appSettings.EnableBackgroundJobs)
    {
        healthChecksBuilder.AddHangfire(
            setup =>
            {
                setup.MinimumAvailableServers = 1;
                setup.MaximumJobsFailed = 5;
            },
            name: "hangfire",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "hangfire", "jobs", "critical" });
    }

    // Redis health check - verify cache connection
    healthChecksBuilder.AddRedis(
        redisConnectionString: configuration.GetConnectionString("Redis") ?? "localhost:6379",
        name: "redis",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "redis", "cache" });

    // FFmpeg health check - verify audio extraction capability
    healthChecksBuilder.AddCheck<YoutubeRag.Api.HealthChecks.FFmpegHealthCheck>(
        name: "ffmpeg",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "dependencies", "audio", "critical" });

    // Whisper models health check - verify transcription models available
    healthChecksBuilder.AddCheck<YoutubeRag.Api.HealthChecks.WhisperModelsHealthCheck>(
        name: "whisper_models",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "dependencies", "transcription" });

    // Disk space health check - verify sufficient storage
    healthChecksBuilder.AddCheck<YoutubeRag.Api.HealthChecks.DiskSpaceHealthCheck>(
        name: "disk_space",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "storage", "critical" });

    // Swagger/OpenAPI - Conditional based on EnableDocs
    if (appSettings.EnableDocs)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = $"YouTube RAG API - .NET ({appSettings.Environment})",
                Version = "v1.0.0",
                Description = $"YouTube RAG - Intelligent Video Search and Analysis (.NET Implementation) - Environment: {appSettings.Environment}"
            });

            // Add JWT authentication to Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Organize endpoints by tags
            options.TagActionsBy(api => new[]
            {
                api.GroupName ?? api.ActionDescriptor.RouteValues["controller"]?.ToLower() switch
                {
                    "auth" => "🔐 Authentication",
                    "videos" => "🎥 Videos",
                    "search" => "🔍 Search",
                    "jobs" => "⚙️ Jobs",
                    "users" => "👥 Users",
                    "files" => "📁 Files",
                    "websocket" => "🔄 WebSocket",
                    "health" => "💊 Health",
                    _ => "📋 General"
                }
            });
        });
    }

    var app = builder.Build();

    // Configure the HTTP request pipeline
    // Swagger/OpenAPI - Conditional based on EnableDocs
    if (appSettings.EnableDocs)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "YouTube RAG API v1");
            options.RoutePrefix = "swagger";  // Changed to match test expectations
            options.EnablePersistAuthorization();
            options.DisplayRequestDuration();
        });
    }

    // Security Headers Middleware
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

        if (appSettings.IsProduction)
        {
            context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }

        await next();
    });

    // Apply middleware conditionally
    if (appSettings.EnableCors)
    {
        app.UseCors("AllowedOrigins");
    }

    app.UseRateLimiter();

    // Always use authentication and authorization (either real JWT or mock)
    app.UseAuthentication();
    app.UseAuthorization();

    // Health Check Endpoints
    // Main health endpoint - returns detailed JSON response with all checks
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = WriteHealthCheckResponse,
        AllowCachingResponses = true,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    })
    .WithTags("💊 Health")
    .WithName("HealthCheck")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Comprehensive health check";
        operation.Description = "Returns detailed health status of all system components including database, Hangfire, FFmpeg, Whisper models, and disk space";
        return operation;
    });

    // Readiness probe - for Kubernetes readiness checks
    app.MapHealthChecks("/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("critical"),
        ResponseWriter = WriteHealthCheckResponse,
        AllowCachingResponses = false
    })
    .WithTags("💊 Health")
    .WithName("ReadinessCheck")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Readiness check";
        operation.Description = "Returns 200 if critical components (database, Hangfire, FFmpeg) are healthy";
        return operation;
    });

    // Liveness probe - for Kubernetes liveness checks
    app.MapHealthChecks("/live", new HealthCheckOptions
    {
        Predicate = _ => false, // No checks, just confirm the process is alive
        ResponseWriter = async (context, _) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow
            });
        },
        AllowCachingResponses = false
    })
    .WithTags("💊 Health")
    .WithName("LivenessCheck")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Liveness check";
        operation.Description = "Returns 200 if the application process is alive and responding";
        return operation;
    });

    // API Routes
    app.MapControllers();


// Hangfire Dashboard
if (appSettings.EnableBackgroundJobs && appSettings.EnableHangfireDashboard)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() },
        DashboardTitle = "YouTube RAG - Background Jobs"
    });
}

// SignalR Hubs
if (appSettings.EnableWebSockets)
{
    // Map JobProgressHub for real-time job progress notifications
    app.MapHub<YoutubeRag.Api.Hubs.JobProgressHub>("/hubs/job-progress");

    // SignalR Connection Info Endpoint
    app.MapGet("/api/v1/signalr/connection-info", (HttpContext context) =>
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Results.Ok(new
        {
            hubUrl = "/hubs/job-progress",
            userId,
            instructions = new
            {
                connect = "Use SignalR client library to connect to the hub",
                authentication = "Pass JWT token in accessTokenFactory option",
                subscribeToJob = "Call hub.invoke('SubscribeToJob', jobId) to receive job updates",
                subscribeToVideo = "Call hub.invoke('SubscribeToVideo', videoId) to receive video updates",
                unsubscribeFromJob = "Call hub.invoke('UnsubscribeFromJob', jobId) to stop receiving job updates",
                getJobProgress = "Call hub.invoke('GetJobProgress', jobId) to get current job status",
                getVideoProgress = "Call hub.invoke('GetVideoProgress', videoId) to get current video status"
            },
            events = new[]
            {
                "JobProgressUpdate - Fired when job progress updates",
                "JobCompleted - Fired when job completes successfully",
                "JobFailed - Fired when job fails",
                "VideoProgressUpdate - Fired when video progress updates",
                "UserNotification - Fired for user-specific notifications",
                "BroadcastNotification - Fired for system-wide notifications",
                "Error - Fired when an error occurs"
            },
            exampleUsage = new
            {
                javascript = "const connection = new signalR.HubConnectionBuilder().withUrl('/hubs/job-progress', { accessTokenFactory: () => 'your-jwt-token' }).build(); await connection.start(); connection.on('JobProgressUpdate', (progress) => console.log(progress));"
            }
        });
    })
    .WithTags("🔄 WebSocket")
    .WithName("GetSignalRConnectionInfo")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get SignalR connection information";
        operation.Description = "Returns information about how to connect to and use the SignalR hub for real-time updates";
        return operation;
    });
}
    // Root endpoint
    app.MapGet("/", () => new
    {
        message = "YouTube RAG API - .NET",
        version = "1.0.0",
        status = "healthy",
        docs_url = appSettings.EnableDocs ? "/docs" : null,
        environment = appSettings.Environment,
        processing_mode = appSettings.ProcessingMode,
        storage_mode = appSettings.StorageMode,
        features = new
        {
            auth_enabled = appSettings.EnableAuth,
            websockets_enabled = appSettings.EnableWebSockets,
            metrics_enabled = appSettings.EnableMetrics,
            real_processing_enabled = appSettings.EnableRealProcessing
        }
    })
    .WithTags("📋 General");

    // Database initialization - Only if using database storage
    if (appSettings.UseDatabaseStorage)
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine($"Database connection successful - Storage Mode: {appSettings.StorageMode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
                if (appSettings.IsDevelopment)
                {
                    Console.WriteLine("Development mode: Continuing without database...");
                }
                else
                {
                    throw; // In production, fail fast if database is not available
                }
            }
        }
    }
    else
    {
        Console.WriteLine($"Database initialization skipped - Storage Mode: {appSettings.StorageMode}");
    }

    // Configure recurring Hangfire jobs if background jobs are enabled
    if (appSettings.EnableBackgroundJobs)
    {
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
                RecurringJobsSetup.ConfigureRecurringJobs(recurringJobManager);
                Console.WriteLine("Configured Hangfire recurring jobs");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to configure recurring jobs: {ex.Message}");
                if (!appSettings.IsDevelopment)
                {
                    throw; // In production, fail fast if recurring jobs can't be configured
                }
            }
        }
    }

    return app; // Return without calling Run()
    }

    /// <summary>
    /// Custom health check response writer that formats the response as JSON
    /// according to the specification in AC4
    /// </summary>
    private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var checks = new Dictionary<string, object>();

        foreach (var entry in report.Entries)
        {
            checks[entry.Key] = new
            {
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                data = entry.Value.Data.Any() ? entry.Value.Data : null,
                exception = entry.Value.Exception?.Message
            };
        }

        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks,
            timestamp = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        return context.Response.WriteAsJsonAsync(result, options);
    }
}