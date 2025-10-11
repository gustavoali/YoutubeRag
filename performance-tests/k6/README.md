# k6 Performance Testing

Quick start guide for running performance tests with k6.

## Prerequisites

### Install k6

**Windows (using Chocolatey):**
```powershell
choco install k6
```

**macOS (using Homebrew):**
```bash
brew install k6
```

**Linux (Debian/Ubuntu):**
```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

**Docker:**
```bash
docker pull grafana/k6:latest
```

## Quick Start

### 1. Start the Application

Ensure the YoutubeRag API is running:
```bash
cd ../..  # Navigate to repository root
dotnet run --project YoutubeRag.Api
```

The API should be available at `http://localhost:5000`

### 2. Run Smoke Test

**Linux/macOS:**
```bash
bash scripts/run-smoke.sh
```

**Windows (PowerShell):**
```powershell
.\scripts\run-smoke.ps1
```

### 3. Run Specific Test

**Linux/macOS:**
```bash
bash scripts/run-load.sh video-ingestion-load
bash scripts/run-load.sh search-load
```

**Windows (PowerShell):**
```powershell
.\scripts\run-load.ps1 -TestName video-ingestion-load
.\scripts\run-load.ps1 -TestName search-load
```

### 4. Run All Tests

**Linux/macOS:**
```bash
bash scripts/run-all.sh
```

**Windows (PowerShell):**
```powershell
.\scripts\run-all.ps1
```

## Available Tests

| Test | Duration | VUs | Purpose |
|------|----------|-----|---------|
| **smoke** | 30s | 10 | Quick validation of all endpoints |
| **video-ingestion-load** | 8m | 0→50→0 | Video ingestion performance |
| **search-load** | 3m | 100 | Search endpoint performance |
| **stress** | 15m | 0→200 | Find system breaking point |
| **spike** | 3m | 10→200→10 | Sudden traffic surge handling |
| **endurance** | 30m | 30 | Long-running stability test |

## Test Structure

```
performance-tests/k6/
├── tests/           # Test scripts
│   ├── smoke.js
│   ├── video-ingestion-load.js
│   ├── search-load.js
│   ├── stress.js
│   ├── spike.js
│   ├── endurance.js
│   └── utils.js     # Shared utilities
├── config/          # Configuration files
│   ├── default.json
│   ├── local.json
│   └── production.json
├── fixtures/        # Test data
│   ├── test-videos.json
│   ├── search-queries.json
│   └── users.json
├── scripts/         # Helper scripts
│   ├── run-smoke.sh
│   ├── run-load.sh
│   ├── run-all.sh
│   ├── run-smoke.ps1
│   ├── run-load.ps1
│   └── run-all.ps1
└── reports/         # Test results (generated)
```

## Configuration

### Environment Variables

- `BASE_URL`: API base URL (default: `http://localhost:5000`)
- `ENVIRONMENT`: Test environment (default: `local`)
- `EXPORT_JSON`: Path to JSON report output
- `EXPORT_HTML`: Path to HTML report output

### Example with Custom URL

```bash
BASE_URL=https://api.youtuberag.com bash scripts/run-smoke.sh
```

## Understanding Results

### Key Metrics

- **http_req_duration**: Response time (aim for p95 < 2s)
- **http_req_failed**: Error rate (aim for < 1%)
- **http_reqs**: Request throughput
- **iteration_duration**: Full iteration time

### Thresholds

Tests define pass/fail thresholds:
- p95 response time < 2000ms
- p99 response time < 3000ms
- Error rate < 1%
- Minimum throughput > 10 req/s

### Sample Output

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

## Reports

Test reports are saved in `reports/` directory:
- **JSON reports**: Machine-readable results for CI/CD
- **HTML reports**: Human-readable dashboard (when available)

## Docker Usage

Run tests using Docker:

```bash
docker run --rm \
  -v $(pwd):/tests \
  -e BASE_URL=http://host.docker.internal:5000 \
  grafana/k6:latest run /tests/tests/smoke.js
```

## CI/CD Integration

Tests run automatically in GitHub Actions:
- **Smoke tests**: On every PR
- **Load tests**: Nightly schedule
- **Manual trigger**: Via workflow_dispatch

See `.github/workflows/performance-tests.yml` for details.

## Grafana Dashboard

Import the k6 dashboard to visualize performance metrics in Grafana:

```bash
# Dashboard location
monitoring/grafana/dashboards/k6-performance-dashboard.json
```

## Troubleshooting

### Application Not Running

```
Error: Connection refused at http://localhost:5000
```

**Solution**: Start the application first:
```bash
cd ../../YoutubeRag.Api
dotnet run
```

### Authentication Failures

```
Error: Login failed with status 401
```

**Solution**: Ensure test user exists. Run database migrations or use mock authentication mode.

### Thresholds Failing

```
Test Result: ❌ FAILED
Failure: HTTP error rate exceeds threshold
```

**Solution**:
1. Check application logs for errors
2. Verify database and Redis are running
3. Review system resources (CPU, memory)
4. Adjust VU count or thresholds if testing limits

## Best Practices

1. **Always run smoke tests first** before running longer tests
2. **Monitor system resources** during stress testing
3. **Run tests in isolation** to avoid interference
4. **Baseline before changes** to detect regressions
5. **Document performance SLOs** based on test results

## Additional Resources

- [k6 Documentation](https://k6.io/docs/)
- [k6 Best Practices](https://k6.io/docs/testing-guides/test-types/)
- [Performance Testing Guide](../../docs/PERFORMANCE_TESTING.md)

## Support

For issues or questions:
- Check application logs: `YoutubeRag.Api/logs/`
- Review test output in `reports/`
- Consult main documentation: `docs/PERFORMANCE_TESTING.md`
