using Prometheus;

namespace YoutubeRag.Api.Middleware;

/// <summary>
/// Middleware for custom Prometheus business metrics
/// Tracks application-specific metrics beyond standard HTTP metrics
/// </summary>
public class PrometheusMetricsMiddleware
{
    private readonly RequestDelegate _next;

    // Custom Metrics - Business KPIs
    private static readonly Counter VideosProcessed = Metrics.CreateCounter(
        "youtuberag_videos_processed_total",
        "Total number of videos processed",
        new CounterConfiguration
        {
            LabelNames = new[] { "status", "source" }
        });

    private static readonly Counter TranscriptionsCompleted = Metrics.CreateCounter(
        "youtuberag_transcriptions_completed_total",
        "Total number of transcriptions completed",
        new CounterConfiguration
        {
            LabelNames = new[] { "language", "model", "status" }
        });

    private static readonly Counter SearchQueries = Metrics.CreateCounter(
        "youtuberag_search_queries_total",
        "Total number of search queries",
        new CounterConfiguration
        {
            LabelNames = new[] { "query_type", "user_type" }
        });

    private static readonly Histogram SearchQueryDuration = Metrics.CreateHistogram(
        "youtuberag_search_query_duration_seconds",
        "Duration of search queries in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "query_type" },
            Buckets = Histogram.ExponentialBuckets(start: 0.001, factor: 2, count: 10)
        });

    private static readonly Histogram TranscriptionDuration = Metrics.CreateHistogram(
        "youtuberag_transcription_duration_seconds",
        "Duration of transcription jobs in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "model", "language" },
            Buckets = Histogram.ExponentialBuckets(start: 1, factor: 2, count: 12)
        });

    private static readonly Gauge ActiveJobs = Metrics.CreateGauge(
        "youtuberag_active_jobs_count",
        "Number of currently active background jobs",
        new GaugeConfiguration
        {
            LabelNames = new[] { "job_type", "priority" }
        });

    private static readonly Gauge VideoStorageBytes = Metrics.CreateGauge(
        "youtuberag_video_storage_bytes",
        "Total bytes of video storage used",
        new GaugeConfiguration
        {
            LabelNames = new[] { "storage_type" }
        });

    private static readonly Counter BackgroundJobsExecuted = Metrics.CreateCounter(
        "youtuberag_background_jobs_executed_total",
        "Total number of background jobs executed",
        new CounterConfiguration
        {
            LabelNames = new[] { "job_type", "status" }
        });

    private static readonly Counter ApiAuthenticationAttempts = Metrics.CreateCounter(
        "youtuberag_api_authentication_attempts_total",
        "Total number of API authentication attempts",
        new CounterConfiguration
        {
            LabelNames = new[] { "result", "auth_type" }
        });

    private static readonly Gauge DatabaseConnectionPoolSize = Metrics.CreateGauge(
        "youtuberag_database_connection_pool_size",
        "Number of active database connections",
        new GaugeConfiguration
        {
            LabelNames = new[] { "state" }
        });

    private static readonly Counter CacheHits = Metrics.CreateCounter(
        "youtuberag_cache_hits_total",
        "Total number of cache hits",
        new CounterConfiguration
        {
            LabelNames = new[] { "cache_type", "operation" }
        });

    private static readonly Counter CacheMisses = Metrics.CreateCounter(
        "youtuberag_cache_misses_total",
        "Total number of cache misses",
        new CounterConfiguration
        {
            LabelNames = new[] { "cache_type", "operation" }
        });

    // Static properties to allow external access to metrics
    public static Counter VideosProcessedMetric => VideosProcessed;
    public static Counter TranscriptionsCompletedMetric => TranscriptionsCompleted;
    public static Counter SearchQueriesMetric => SearchQueries;
    public static Histogram SearchQueryDurationMetric => SearchQueryDuration;
    public static Histogram TranscriptionDurationMetric => TranscriptionDuration;
    public static Gauge ActiveJobsMetric => ActiveJobs;
    public static Gauge VideoStorageBytesMetric => VideoStorageBytes;
    public static Counter BackgroundJobsExecutedMetric => BackgroundJobsExecuted;
    public static Counter ApiAuthenticationAttemptsMetric => ApiAuthenticationAttempts;
    public static Gauge DatabaseConnectionPoolSizeMetric => DatabaseConnectionPoolSize;
    public static Counter CacheHitsMetric => CacheHits;
    public static Counter CacheMissesMetric => CacheMisses;

    public PrometheusMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Pass through to next middleware
        // Actual metrics are recorded by services using the static properties
        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering Prometheus metrics middleware
/// </summary>
public static class PrometheusMetricsMiddlewareExtensions
{
    public static IApplicationBuilder UsePrometheusMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PrometheusMetricsMiddleware>();
    }
}
