# Performance Testing Guide

Comprehensive guide for performance testing the YoutubeRag.NET application using k6.

## Table of Contents

1. [Overview](#overview)
2. [Getting Started](#getting-started)
3. [k6 Installation](#k6-installation)
4. [Test Scenarios](#test-scenarios)
5. [Running Tests](#running-tests)
6. [Interpreting Results](#interpreting-results)
7. [Writing New Tests](#writing-new-tests)
8. [Performance Thresholds & SLOs](#performance-thresholds--slos)
9. [CI/CD Integration](#cicd-integration)
10. [Grafana Integration](#grafana-integration)
11. [Best Practices](#best-practices)
12. [Troubleshooting](#troubleshooting)

## Overview

This project uses [k6](https://k6.io/) by Grafana for performance testing. k6 is a modern, open-source load testing tool that:

- Uses JavaScript for test scripts
- Provides comprehensive metrics
- Integrates with CI/CD pipelines
- Exports results to multiple formats
- Scales from local testing to cloud

### Why k6?

- **Developer-friendly**: Write tests in JavaScript
- **Accurate**: Go-based runtime ensures accurate load generation
- **Flexible**: Support for various protocols (HTTP, WebSocket, gRPC)
- **Observable**: Built-in metrics and custom metrics
- **Integrable**: Works with Prometheus, Grafana, InfluxDB

### Performance Testing Goals

1. **Validate performance requirements** before production
2. **Detect regressions** in performance
3. **Find system limits** and breaking points
4. **Optimize** based on data
5. **Build confidence** in system scalability

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- MySQL 8.0+ (or Docker)
- Redis 7+ (or Docker)
- k6 installed
- Running instance of YoutubeRag API

### Quick Start

1. **Install k6** (see [k6 Installation](#k6-installation))

2. **Start dependencies**:
   ```bash
   docker-compose up -d mysql redis
   ```

3. **Start the API**:
   ```bash
   cd YoutubeRag.Api
   dotnet run
   ```

4. **Run smoke test**:
   ```bash
   cd performance-tests/k6
   bash scripts/run-smoke.sh
   ```

## k6 Installation

### Windows

**Using Chocolatey:**
```powershell
choco install k6
```

**Using Windows Installer:**
1. Download from [k6 releases](https://github.com/grafana/k6/releases)
2. Run the installer
3. Verify: `k6 version`

### macOS

**Using Homebrew:**
```bash
brew install k6
```

### Linux (Debian/Ubuntu)

```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

### Docker

```bash
docker pull grafana/k6:latest
```

**Run with Docker:**
```bash
docker run --rm -v $(pwd)/performance-tests/k6:/tests grafana/k6 run /tests/tests/smoke.js
```

### Verification

```bash
k6 version
```

Expected output:
```
k6 v0.48.0 (go1.21.3, linux/amd64)
```

## Test Scenarios

### 1. Smoke Test (`smoke.js`)

**Purpose**: Quick validation that all critical endpoints are functional.

**Configuration**:
- Duration: 30 seconds
- VUs: 10
- Endpoints: Health, Auth, Videos, Search

**When to run**:
- Before running other tests
- After deployments
- On every PR (automated)

**Command**:
```bash
bash scripts/run-smoke.sh
```

### 2. Video Ingestion Load Test (`video-ingestion-load.js`)

**Purpose**: Test video ingestion endpoint under realistic load.

**Configuration**:
- Duration: 8 minutes
- VUs: 0 → 50 → 50 → 0
- Target: `/api/v1/videos/ingest`

**Stages**:
1. Ramp up to 50 VUs over 2 minutes
2. Sustain 50 VUs for 5 minutes
3. Ramp down to 0 VUs over 1 minute

**Thresholds**:
- p95 response time < 2s
- p99 response time < 3s
- Error rate < 1%

**Command**:
```bash
bash scripts/run-load.sh video-ingestion-load
```

### 3. Search Performance Test (`search-load.js`)

**Purpose**: Test semantic search performance under load.

**Configuration**:
- Duration: 3 minutes
- VUs: 100
- Target: `/api/v1/search/semantic`
- Query complexity: Simple, Medium, Complex

**Thresholds**:
- p95 response time < 500ms
- p99 response time < 1s
- Error rate < 1%

**Command**:
```bash
bash scripts/run-load.sh search-load
```

### 4. Stress Test (`stress.js`)

**Purpose**: Find the system's breaking point.

**Configuration**:
- Duration: 15 minutes
- VUs: 0 → 200
- Progressive load increase

**Goals**:
- Identify maximum capacity
- Observe degradation patterns
- Find resource bottlenecks

**Command**:
```bash
bash scripts/run-load.sh stress
```

**Warning**: May impact system resources. Monitor during execution.

### 5. Spike Test (`spike.js`)

**Purpose**: Test system behavior under sudden traffic surge.

**Configuration**:
- Duration: ~3 minutes
- VUs: 10 → 200 (instant) → 10
- Simulates flash traffic

**Validates**:
- Auto-scaling response
- Error handling
- Recovery capability

**Command**:
```bash
bash scripts/run-load.sh spike
```

### 6. Endurance Test (`endurance.js`)

**Purpose**: Detect memory leaks and performance degradation over time.

**Configuration**:
- Duration: 30 minutes
- VUs: 30 (constant)
- Mixed operations

**Detects**:
- Memory leaks
- Connection leaks
- Performance degradation
- Resource exhaustion

**Command**:
```bash
bash scripts/run-load.sh endurance
```

**Note**: Run this test in isolation for accurate results.

## Running Tests

### Local Execution

#### Single Test

```bash
# Linux/macOS
bash scripts/run-load.sh <test-name> [base-url]

# Windows
.\scripts\run-load.ps1 -TestName <test-name> -BaseUrl <base-url>
```

**Examples**:
```bash
bash scripts/run-load.sh smoke
bash scripts/run-load.sh video-ingestion-load http://localhost:5000
```

#### All Tests

```bash
# Linux/macOS
bash scripts/run-all.sh [base-url]

# Windows
.\scripts\run-all.ps1 -BaseUrl <base-url>
```

### Custom Configuration

**Environment Variables**:
```bash
export BASE_URL=https://staging.youtuberag.com
export ENVIRONMENT=staging
export EXPORT_JSON=./my-report.json

bash scripts/run-smoke.sh
```

**Direct k6 Command**:
```bash
k6 run \
  -e BASE_URL=http://localhost:5000 \
  -e ENVIRONMENT=local \
  --out json=results.json \
  tests/smoke.js
```

### Advanced Options

**Increase VUs**:
```bash
k6 run --vus 200 --duration 5m tests/stress.js
```

**Custom Thresholds**:
```bash
k6 run \
  --threshold 'http_req_duration{p(95)}<1000' \
  --threshold 'http_req_failed<0.01' \
  tests/smoke.js
```

## Interpreting Results

### Console Output

k6 provides real-time and summary metrics:

```
================================================================================
SMOKE TEST SUMMARY
================================================================================
Total Requests:        1,250
Failed Requests:       5
Success Rate:          99.60%
Average Duration:      345.23ms
p95 Duration:          890.12ms
p99 Duration:          1,245.67ms
Throughput:            41.67 req/s
Error Rate:            0.40%
================================================================================
Test Result: ✅ PASSED
================================================================================
```

### Key Metrics Explained

#### HTTP Metrics

| Metric | Description | Target |
|--------|-------------|--------|
| `http_req_duration` | Response time | p95 < 2s |
| `http_req_failed` | Error rate | < 1% |
| `http_reqs` | Total requests | - |
| `http_req_blocked` | Time waiting for connection | < 10ms |
| `http_req_connecting` | Time establishing TCP | < 50ms |
| `http_req_tls_handshaking` | TLS handshake time | < 100ms |
| `http_req_sending` | Time sending request | < 5ms |
| `http_req_waiting` | Time waiting for response | - |
| `http_req_receiving` | Time receiving response | < 10ms |

#### Response Time Percentiles

- **p50 (Median)**: Half of requests are faster
- **p90**: 90% of requests are faster
- **p95**: 95% of requests are faster (common SLO)
- **p99**: 99% of requests are faster (tail latency)
- **max**: Slowest request

#### Throughput

- **req/s**: Requests per second
- Target varies by operation:
  - Read operations: 100+ req/s
  - Write operations: 10+ req/s
  - Complex queries: 5+ req/s

### Pass/Fail Criteria

Tests pass when all thresholds are met:

```javascript
thresholds: {
  'http_req_duration': ['p(95)<2000', 'p(99)<3000'],
  'http_req_failed': ['rate<0.01'],
  'http_reqs': ['rate>10'],
}
```

### JSON Reports

Reports are saved to `performance-tests/k6/reports/`:

```json
{
  "metrics": {
    "http_req_duration": {
      "values": {
        "avg": 345.23,
        "p(95)": 890.12,
        "p(99)": 1245.67
      }
    },
    "http_req_failed": {
      "values": {
        "rate": 0.004
      }
    }
  }
}
```

## Writing New Tests

### Test Template

```javascript
/**
 * Test Name - Brief Description
 *
 * Purpose: Detailed purpose
 * Duration: X minutes
 * Load: Y VUs
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Configuration
const config = JSON.parse(open('../config/default.json'));
const BASE_URL = __ENV.BASE_URL || config.baseUrl;

// Custom metrics
const errorRate = new Rate('errors');
const customMetric = new Trend('custom_metric');

// Test options
export const options = {
  vus: 10,
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.01'],
    errors: ['rate<0.01'],
  },
  tags: {
    test_type: 'custom',
  },
};

// Setup (runs once)
export function setup() {
  console.log('Starting test...');
  return {
    baseUrl: BASE_URL,
    // Additional setup data
  };
}

// Main test (runs per VU)
export default function (data) {
  const { baseUrl } = data;

  // Your test logic here
  const response = http.get(`${baseUrl}/api/endpoint`);

  check(response, {
    'status is 200': (r) => r.status === 200,
  });

  sleep(1);
}

// Teardown (runs once)
export function teardown(data) {
  console.log('Test complete');
}

// Custom summary
export function handleSummary(data) {
  return {
    stdout: formatSummary(data),
  };
}

function formatSummary(data) {
  return `Test Results: ${data.metrics.http_reqs?.values.count} requests`;
}
```

### Best Practices for Test Scripts

1. **Use realistic scenarios**: Simulate actual user behavior
2. **Include think time**: Add `sleep()` between requests
3. **Handle errors gracefully**: Check responses, log failures
4. **Use custom metrics**: Track business-specific KPIs
5. **Parameterize tests**: Use environment variables
6. **Clean up resources**: Implement proper teardown
7. **Document thresholds**: Explain why thresholds are set

### Custom Metrics

```javascript
import { Trend, Rate, Counter, Gauge } from 'k6/metrics';

// Trend: Track timing metrics
const processingTime = new Trend('video_processing_time');
processingTime.add(duration);

// Rate: Track success/failure rates
const successRate = new Rate('operation_success');
successRate.add(success ? 1 : 0);

// Counter: Count events
const totalVideos = new Counter('videos_processed');
totalVideos.add(1);

// Gauge: Track current value
const activeConnections = new Gauge('active_connections');
activeConnections.add(count);
```

## Performance Thresholds & SLOs

### Service Level Objectives (SLOs)

| Operation | p95 | p99 | Error Rate | Availability |
|-----------|-----|-----|------------|--------------|
| Health Check | 100ms | 200ms | 0.01% | 99.99% |
| Authentication | 500ms | 1s | 0.1% | 99.9% |
| Video List | 1s | 2s | 0.5% | 99.9% |
| Video Ingestion | 2s | 3s | 1% | 99.5% |
| Semantic Search | 500ms | 1s | 1% | 99.5% |

### Threshold Configuration

Thresholds in k6 define pass/fail criteria:

```javascript
export const options = {
  thresholds: {
    // 95th percentile must be below 2 seconds
    'http_req_duration': ['p(95)<2000'],

    // 99th percentile must be below 3 seconds
    'http_req_duration': ['p(99)<3000'],

    // Error rate must be below 1%
    'http_req_failed': ['rate<0.01'],

    // Minimum throughput: 10 requests/second
    'http_reqs': ['rate>10'],

    // Average iteration duration
    'iteration_duration': ['avg<5000'],

    // Custom metric threshold
    'video_ingestion_success': ['rate>0.99'],
  },
};
```

### When to Adjust Thresholds

- **More strict**: Production-critical paths
- **More lenient**: Complex operations, initial testing
- **Context-specific**: Different endpoints have different requirements

## CI/CD Integration

### GitHub Actions Workflow

Performance tests run automatically via `.github/workflows/performance-tests.yml`:

#### On Pull Requests

- **Smoke tests** run on every PR
- Results posted as PR comment
- Fails PR if thresholds not met

#### Nightly Schedule

- **Full test suite** runs nightly at 2 AM UTC
- Includes load, stress, and spike tests
- Reports saved as artifacts

#### Manual Triggers

- **workflow_dispatch** allows manual test execution
- Select specific test type
- Customize base URL

### Running in CI

```yaml
- name: Run k6 tests
  run: |
    cd performance-tests/k6
    bash scripts/run-smoke.sh
  env:
    BASE_URL: http://localhost:5000
    EXPORT_JSON: reports/results.json
```

### Artifacts

Test results are uploaded as GitHub Actions artifacts:
- Available for 30 days
- Download from Actions tab
- JSON and HTML reports

### Performance Regression Detection

The CI workflow compares results against baseline:
- Fails if p95 increases > 20%
- Fails if error rate increases > 5%
- Alerts on throughput decrease > 10%

## Grafana Integration

### Importing the Dashboard

1. **Open Grafana** (typically `http://localhost:3000`)

2. **Navigate** to Dashboards → Import

3. **Upload** `monitoring/grafana/dashboards/k6-performance-dashboard.json`

4. **Select** Prometheus datasource

5. **Import**

### Dashboard Panels

The k6 dashboard includes:

1. **Request Duration (Percentiles)**: p50, p95, p99 over time
2. **Success Rate**: Percentage of successful requests
3. **Request Throughput**: Requests per second
4. **HTTP Status Codes**: 2xx, 4xx, 5xx breakdown
5. **Requests by Endpoint**: Traffic distribution
6. **Error Rate**: Errors over time

### Exporting k6 Metrics to Prometheus

k6 doesn't natively export to Prometheus, but you can:

1. **Use k6 Cloud** (paid service)
2. **Use k6 Prometheus remote write** extension
3. **Parse JSON outputs** and push to Prometheus

**Example with Prometheus remote write**:
```bash
k6 run \
  --out experimental-prometheus-rw \
  -e K6_PROMETHEUS_RW_SERVER_URL=http://localhost:9090/api/v1/write \
  tests/smoke.js
```

### Real-time Monitoring

While tests run, monitor:
- Application metrics in Grafana
- System resources (CPU, Memory)
- Database connections
- Redis operations

## Best Practices

### Test Design

1. **Start small**: Run smoke tests before load tests
2. **Realistic scenarios**: Model actual user behavior
3. **Gradual load**: Ramp up, don't start at max VUs
4. **Think time**: Add realistic pauses between actions
5. **Error handling**: Gracefully handle failures

### Test Execution

1. **Isolate tests**: Run performance tests in isolation
2. **Consistent environment**: Same infrastructure for baseline comparisons
3. **Warm-up period**: Allow system to stabilize before measuring
4. **Monitor system**: Watch CPU, memory, disk during tests
5. **Document baselines**: Record baseline metrics for comparison

### Test Maintenance

1. **Keep tests updated**: Sync with API changes
2. **Review thresholds**: Adjust based on production data
3. **Clean test data**: Remove or reset test data between runs
4. **Version test scripts**: Commit to git, track changes
5. **Regular execution**: Run tests frequently to catch regressions

### Common Pitfalls to Avoid

1. **Testing in production**: Always use dedicated test environments
2. **Ignoring think time**: Unrealistic load patterns
3. **Not monitoring system**: Missing root causes
4. **Hardcoded values**: Use configuration files
5. **Overly aggressive thresholds**: Causing false failures
6. **Testing cold start**: Results don't reflect steady state

## Troubleshooting

### Application Not Responding

**Symptom**: Connection refused errors

```
Error: Connection refused at http://localhost:5000
```

**Solutions**:
1. Verify application is running: `curl http://localhost:5000/health`
2. Check application logs
3. Ensure correct URL and port
4. Verify firewall/network settings

### Authentication Failures

**Symptom**: 401 Unauthorized responses

```
Error: Login failed with status 401
```

**Solutions**:
1. Verify test user credentials in `fixtures/users.json`
2. Check JWT secret key configuration
3. Ensure database has test users
4. Review authentication middleware

### High Error Rates

**Symptom**: Error rate exceeds threshold

```
Test Result: ❌ FAILED
http_req_failed rate: 5.2% (threshold: < 1%)
```

**Solutions**:
1. Check application logs for errors
2. Monitor database connection pool
3. Verify Redis connectivity
4. Review resource usage (CPU, memory)
5. Reduce VU count to find stable load level

### Slow Response Times

**Symptom**: p95 exceeds threshold

```
http_req_duration p(95): 3500ms (threshold: < 2000ms)
```

**Solutions**:
1. Profile application code
2. Check database query performance
3. Review cache hit rates
4. Analyze network latency
5. Optimize resource-intensive operations

### Memory Leaks

**Symptom**: Performance degrades over time

**Detection**: Run endurance test, monitor memory usage

**Solutions**:
1. Review connection disposal
2. Check for unmanaged resources
3. Profile with memory profiler
4. Review caching strategies
5. Check for circular references

### k6 Installation Issues

**Windows**: Use administrator PowerShell for Chocolatey

**Linux**: Verify GPG key import successful

**macOS**: Update Homebrew: `brew update`

**Docker**: Ensure Docker daemon is running

### Script Errors

**Syntax errors**: Validate JavaScript syntax

**Module not found**: Check relative paths in `open()`

**Undefined variables**: Verify environment variables are set

## Additional Resources

### Official Documentation

- [k6 Documentation](https://k6.io/docs/)
- [k6 GitHub](https://github.com/grafana/k6)
- [k6 Community Forum](https://community.k6.io/)

### Tutorials

- [Getting Started with k6](https://k6.io/docs/getting-started/running-k6/)
- [Test Types](https://k6.io/docs/test-types/introduction/)
- [Metrics](https://k6.io/docs/using-k6/metrics/)
- [Thresholds](https://k6.io/docs/using-k6/thresholds/)

### Best Practices Guides

- [Performance Testing Best Practices](https://k6.io/docs/testing-guides/performance-testing/)
- [Load Testing Your API](https://k6.io/docs/testing-guides/api-load-testing/)

### Community

- [k6 Slack](https://k6.io/community/)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/k6)

## Next Steps

1. **Run your first smoke test**
2. **Establish performance baselines**
3. **Integrate into CI/CD pipeline**
4. **Set up Grafana dashboard**
5. **Define SLOs for your application**
6. **Schedule regular performance tests**
7. **Document performance requirements**

## Support

For questions or issues:
- Review this documentation
- Check test output and logs
- Consult k6 documentation
- Create an issue in the repository

---

**Document Version**: 1.0
**Last Updated**: 2025-10-11
**Maintained By**: Performance Engineering Team
