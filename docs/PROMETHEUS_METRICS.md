# Prometheus Metrics Documentation

## Overview

YoutubeRag.NET exposes Prometheus metrics at `/metrics` endpoint for monitoring, alerting, and observability.

## Metrics Endpoint

- **URL**: `http://localhost:5000/metrics`
- **Format**: Prometheus text format
- **Authentication**: No authentication required (can be restricted in production)
- **Content-Type**: `text/plain; version=0.0.4`

## Standard HTTP Metrics

These metrics are automatically collected by `prometheus-net.AspNetCore`:

### HTTP Request Metrics

| Metric | Type | Description | Labels |
|--------|------|-------------|--------|
| `http_requests_in_progress` | Gauge | Number of HTTP requests currently being processed | - |
| `http_requests_received_total` | Counter | Total HTTP requests received | `code`, `method`, `controller`, `action` |
| `http_request_duration_seconds` | Histogram | HTTP request duration in seconds | `code`, `method`, `controller`, `action` |

**Example:**
```prometheus
# HELP http_requests_in_progress The number of requests currently being processed
# TYPE http_requests_in_progress gauge
http_requests_in_progress 3

# HELP http_requests_received_total Provides the count of HTTP requests that have been processed
# TYPE http_requests_received_total counter
http_requests_received_total{code="200",method="GET",controller="Videos",action="GetAll"} 1547
http_requests_received_total{code="404",method="GET",controller="Videos",action="GetById"} 23

# HELP http_request_duration_seconds The duration of HTTP requests processed
# TYPE http_request_duration_seconds histogram
http_request_duration_seconds_bucket{code="200",method="GET",controller="Search",action="Semantic",le="0.005"} 234
http_request_duration_seconds_bucket{code="200",method="GET",controller="Search",action="Semantic",le="0.01"} 456
```

## Custom Business Metrics

These metrics track application-specific business logic:

### Video Processing Metrics

#### `youtuberag_videos_processed_total`
- **Type**: Counter
- **Description**: Total number of videos processed
- **Labels**:
  - `status`: `completed`, `failed`, `processing`
  - `source`: `youtube`, `direct_upload`

**Example:**
```prometheus
youtuberag_videos_processed_total{status="completed",source="youtube"} 1247
youtuberag_videos_processed_total{status="failed",source="youtube"} 18
```

**Usage:**
```csharp
PrometheusMetricsMiddleware.VideosProcessedMetric
    .WithLabels("completed", "youtube")
    .Inc();
```

---

#### `youtuberag_video_storage_bytes`
- **Type**: Gauge
- **Description**: Total bytes of video storage used
- **Labels**:
  - `storage_type`: `videos`, `audio`, `models`, `temp`

**Example:**
```prometheus
youtuberag_video_storage_bytes{storage_type="videos"} 524288000000
youtuberag_video_storage_bytes{storage_type="audio"} 12885901824
```

**Usage:**
```csharp
PrometheusMetricsMiddleware.VideoStorageBytesMetric
    .WithLabels("videos")
    .Set(storageBytes);
```

### Transcription Metrics

#### `youtuberag_transcriptions_completed_total`
- **Type**: Counter
- **Description**: Total number of transcriptions completed
- **Labels**:
  - `language`: `en`, `es`, `auto`, etc.
  - `model`: `tiny`, `base`, `small`, `medium`, `large`
  - `status`: `success`, `failed`

**Example:**
```prometheus
youtuberag_transcriptions_completed_total{language="en",model="base",status="success"} 892
youtuberag_transcriptions_completed_total{language="es",model="small",status="success"} 134
youtuberag_transcriptions_completed_total{language="auto",model="base",status="failed"} 7
```

---

#### `youtuberag_transcription_duration_seconds`
- **Type**: Histogram
- **Description**: Duration of transcription jobs in seconds
- **Labels**:
  - `model`: Whisper model used
  - `language`: Language detected/specified
- **Buckets**: 1s, 2s, 4s, 8s, 16s, 32s, 64s, 128s, 256s, 512s, 1024s, 2048s

**Example:**
```prometheus
youtuberag_transcription_duration_seconds_bucket{model="base",language="en",le="32"} 456
youtuberag_transcription_duration_seconds_bucket{model="base",language="en",le="64"} 789
youtuberag_transcription_duration_seconds_sum{model="base",language="en"} 24567.8
youtuberag_transcription_duration_seconds_count{model="base",language="en"} 892
```

**Usage:**
```csharp
using (PrometheusMetricsMiddleware.TranscriptionDurationMetric
    .WithLabels("base", "en")
    .NewTimer())
{
    // Transcription logic
}
```

### Search Metrics

#### `youtuberag_search_queries_total`
- **Type**: Counter
- **Description**: Total number of search queries
- **Labels**:
  - `query_type`: `semantic`, `keyword`, `hybrid`
  - `user_type`: `authenticated`, `anonymous`

**Example:**
```prometheus
youtuberag_search_queries_total{query_type="semantic",user_type="authenticated"} 4523
youtuberag_search_queries_total{query_type="keyword",user_type="anonymous"} 167
```

---

#### `youtuberag_search_query_duration_seconds`
- **Type**: Histogram
- **Description**: Duration of search queries in seconds
- **Labels**:
  - `query_type`: `semantic`, `keyword`, `hybrid`
- **Buckets**: 1ms, 2ms, 4ms, 8ms, 16ms, 32ms, 64ms, 128ms, 256ms, 512ms

**Example:**
```prometheus
youtuberag_search_query_duration_seconds_bucket{query_type="semantic",le="0.064"} 3456
youtuberag_search_query_duration_seconds_sum{query_type="semantic"} 145.67
youtuberag_search_query_duration_seconds_count{query_type="semantic"} 4523
```

**Usage:**
```csharp
using (PrometheusMetricsMiddleware.SearchQueryDurationMetric
    .WithLabels("semantic")
    .NewTimer())
{
    // Search logic
}
```

### Background Jobs Metrics

#### `youtuberag_active_jobs_count`
- **Type**: Gauge
- **Description**: Number of currently active background jobs
- **Labels**:
  - `job_type`: `transcription`, `embedding`, `download`, `cleanup`
  - `priority`: `high`, `normal`, `low`

**Example:**
```prometheus
youtuberag_active_jobs_count{job_type="transcription",priority="high"} 3
youtuberag_active_jobs_count{job_type="embedding",priority="normal"} 7
```

---

#### `youtuberag_background_jobs_executed_total`
- **Type**: Counter
- **Description**: Total number of background jobs executed
- **Labels**:
  - `job_type`: `transcription`, `embedding`, `download`, `cleanup`
  - `status`: `succeeded`, `failed`, `deleted`

**Example:**
```prometheus
youtuberag_background_jobs_executed_total{job_type="transcription",status="succeeded"} 1892
youtuberag_background_jobs_executed_total{job_type="transcription",status="failed"} 23
```

### Authentication Metrics

#### `youtuberag_api_authentication_attempts_total`
- **Type**: Counter
- **Description**: Total number of API authentication attempts
- **Labels**:
  - `result`: `success`, `failed`
  - `auth_type`: `jwt`, `apikey`, `google_oauth`

**Example:**
```prometheus
youtuberag_api_authentication_attempts_total{result="success",auth_type="jwt"} 15678
youtuberag_api_authentication_attempts_total{result="failed",auth_type="jwt"} 234
```

### Database Metrics

#### `youtuberag_database_connection_pool_size`
- **Type**: Gauge
- **Description**: Number of active database connections
- **Labels**:
  - `state`: `active`, `idle`, `total`

**Example:**
```prometheus
youtuberag_database_connection_pool_size{state="active"} 8
youtuberag_database_connection_pool_size{state="idle"} 12
youtuberag_database_connection_pool_size{state="total"} 20
```

### Cache Metrics

#### `youtuberag_cache_hits_total`
- **Type**: Counter
- **Description**: Total number of cache hits
- **Labels**:
  - `cache_type`: `redis`, `memory`
  - `operation`: `get`, `set`, `delete`

**Example:**
```prometheus
youtuberag_cache_hits_total{cache_type="redis",operation="get"} 45678
youtuberag_cache_misses_total{cache_type="redis",operation="get"} 2345
```

## Prometheus Configuration

### prometheus.yml Example

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'youtuberag-api'
    static_configs:
      - targets: ['api:5000']
    metrics_path: '/metrics'
    scrape_interval: 10s
```

### Docker Compose Integration

```yaml
services:
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
```

## Grafana Integration

### Adding Data Source

1. Navigate to Grafana (http://localhost:3001)
2. Configuration → Data Sources → Add data source
3. Select "Prometheus"
4. Set URL: `http://prometheus:9090`
5. Click "Save & Test"

### Example Queries

**API Request Rate (requests/sec)**
```promql
rate(http_requests_received_total[5m])
```

**95th Percentile Response Time**
```promql
histogram_quantile(0.95,
  rate(http_request_duration_seconds_bucket[5m])
)
```

**Video Processing Success Rate**
```promql
rate(youtuberag_videos_processed_total{status="completed"}[5m])
/
rate(youtuberag_videos_processed_total[5m])
```

**Active Jobs by Type**
```promql
sum by (job_type) (youtuberag_active_jobs_count)
```

**Search Query Latency**
```promql
histogram_quantile(0.99,
  rate(youtuberag_search_query_duration_seconds_bucket{query_type="semantic"}[5m])
)
```

## Alerting Rules

### Example Alert Rules

```yaml
groups:
  - name: youtuberag_alerts
    interval: 30s
    rules:
      # High error rate
      - alert: HighErrorRate
        expr: |
          rate(http_requests_received_total{code=~"5.."}[5m]) > 10
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "High error rate detected"
          description: "API error rate is {{ $value }} req/s"

      # Transcription failures
      - alert: TranscriptionFailures
        expr: |
          rate(youtuberag_transcriptions_completed_total{status="failed"}[10m]) > 5
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "High transcription failure rate"

      # Slow search queries
      - alert: SlowSearchQueries
        expr: |
          histogram_quantile(0.95,
            rate(youtuberag_search_query_duration_seconds_bucket[5m])
          ) > 2
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Search queries are slow (p95 > 2s)"

      # Database connection pool exhausted
      - alert: DatabaseConnectionsHigh
        expr: |
          youtuberag_database_connection_pool_size{state="active"}
          /
          youtuberag_database_connection_pool_size{state="total"} > 0.9
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "Database connection pool near capacity"
```

## Best Practices

1. **Label Cardinality**: Avoid high-cardinality labels (e.g., user IDs, video IDs)
2. **Naming Convention**: Use snake_case and descriptive names
3. **Units**: Include units in metric names (`_seconds`, `_bytes`, `_total`)
4. **Types**:
   - Counter: Monotonically increasing values (requests, errors)
   - Gauge: Values that can go up and down (active jobs, memory)
   - Histogram: Distributions (latencies, sizes)

5. **Recording in Code**:
```csharp
// Counter
PrometheusMetricsMiddleware.VideosProcessedMetric
    .WithLabels("completed", "youtube")
    .Inc();

// Gauge
PrometheusMetricsMiddleware.ActiveJobsMetric
    .WithLabels("transcription", "high")
    .Set(activeCount);

// Histogram with timer
using (PrometheusMetricsMiddleware.TranscriptionDurationMetric
    .WithLabels("base", "en")
    .NewTimer())
{
    // Operation being timed
}
```

## Troubleshooting

### Metrics not appearing

1. Check endpoint: `curl http://localhost:5000/metrics`
2. Verify middleware order in Program.cs
3. Check Prometheus scrape config
4. Review Prometheus logs: `docker logs prometheus`

### High cardinality warnings

Reduce unique label combinations. Instead of:
```csharp
// BAD: High cardinality (unique per video)
counter.WithLabels(videoId).Inc();

// GOOD: Low cardinality (grouped)
counter.WithLabels("youtube").Inc();
```

## Further Reading

- [Prometheus Documentation](https://prometheus.io/docs/)
- [prometheus-net Documentation](https://github.com/prometheus-net/prometheus-net)
- [Grafana Documentation](https://grafana.com/docs/)
- [PromQL Tutorial](https://prometheus.io/docs/prometheus/latest/querying/basics/)

---

**Last Updated**: 2025-10-10
**Maintained By**: DevOps Team
