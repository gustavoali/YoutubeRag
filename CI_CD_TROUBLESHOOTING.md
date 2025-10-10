# CI/CD Troubleshooting Guide

**Project:** YoutubeRag - Video Transcription and RAG Platform
**Last Updated:** 2025-10-10
**Version:** 1.0

---

## Table of Contents

1. [Quick Diagnostics](#quick-diagnostics)
2. [Common Build Failures](#common-build-failures)
3. [Database and Migration Issues](#database-and-migration-issues)
4. [Test Failures](#test-failures)
5. [Docker Build Issues](#docker-build-issues)
6. [Deployment Failures](#deployment-failures)
7. [Security Scan Issues](#security-scan-issues)
8. [Performance Issues](#performance-issues)
9. [GitHub Actions Specific Issues](#github-actions-specific-issues)
10. [Emergency Procedures](#emergency-procedures)

---

## Quick Diagnostics

### Check CI Status

```bash
# View recent workflow runs
gh run list --limit 10

# View specific workflow run
gh run view <run-id>

# Download logs for failed run
gh run download <run-id>
```

### Common Quick Fixes

| Problem | Quick Fix | Time |
|---------|-----------|------|
| Build fails | Clear NuGet cache | 1 min |
| Tests timeout | Increase timeout value | 2 min |
| Docker build fails | Clear Docker cache | 5 min |
| Migration fails | Check connection string | 2 min |
| Coverage fails | Verify coverage files generated | 3 min |

---

## Common Build Failures

### Issue 1: .NET SDK Version Mismatch

**Symptoms:**
```
error NETSDK1045: The current .NET SDK does not support targeting .NET 9.0
```

**Cause:**
- Test project targets .NET 9.0 but CI uses .NET 8.0 SDK
- Mismatch between `global.json` and workflow SDK version

**Fix:**
```bash
# Option A: Change test project to .NET 8.0
# Edit YoutubeRag.Tests.Integration/YoutubeRag.Tests.Integration.csproj
<TargetFramework>net8.0</TargetFramework>

# Option B: Update CI workflow to use .NET 9.0
# Edit .github/workflows/ci.yml
env:
  DOTNET_VERSION: '9.0.x'
```

**Prevention:**
- Keep all projects on same .NET version
- Lock SDK version in `global.json`
- Add pre-commit hook to check target frameworks

---

### Issue 2: NuGet Package Restore Failure

**Symptoms:**
```
error NU1301: Unable to load the service index for source
error NU1101: Package 'PackageName' not found
```

**Diagnosis:**
```bash
# Test NuGet source locally
dotnet nuget list source

# Verify package exists
dotnet search PackageName --take 1
```

**Fix:**
```yaml
# Add to workflow before restore step
- name: Clear NuGet Cache
  run: dotnet nuget locals all --clear

- name: Restore with Retry
  run: |
    for i in {1..3}; do
      dotnet restore YoutubeRag.sln && break
      echo "Restore attempt $i failed, retrying..."
      sleep 5
    done
```

**Prevention:**
- Pin package versions in .csproj files
- Use NuGet package locks
- Configure private feed credentials

---

### Issue 3: Build Warnings Treated as Errors

**Symptoms:**
```
Build FAILED.
    92 Warning(s)
error MSB3073: TreatWarningsAsErrors is true
```

**Diagnosis:**
```bash
# Build locally to see warnings
dotnet build YoutubeRag.sln --configuration Release /warnaserror

# List specific warnings
dotnet build | grep "warning CS"
```

**Temporary Fix:**
```yaml
# Disable warning enforcement temporarily
- name: Build Solution
  run: dotnet build YoutubeRag.sln --configuration Release /p:TreatWarningsAsErrors=false
```

**Permanent Fix:**
```bash
# Fix warnings in code
# Common nullable reference warnings:
# CS8600, CS8602, CS8603, CS8604, CS8625

# Or suppress specific warnings
<PropertyGroup>
  <NoWarn>CS8600;CS8602;CS8603</NoWarn>
</PropertyGroup>
```

---

### Issue 4: Circular Dependency Error

**Symptoms:**
```
error MSB4006: There is a circular dependency in the project references
```

**Diagnosis:**
```bash
# Visualize project dependencies
dotnet list YoutubeRag.sln reference
```

**Fix:**
- Review project references
- Remove circular references
- Use interfaces to break dependencies
- Move shared code to separate project

---

## Database and Migration Issues

### Issue 1: Migration Not Applied

**Symptoms:**
```
MySqlException: Table 'test_db.Videos' doesn't exist
```

**Diagnosis:**
```bash
# Check if migrations are in code
ls YoutubeRag.Infrastructure/Migrations/

# Test migration locally
dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
```

**Fix in CI:**
```yaml
- name: Apply Database Migrations
  env:
    ConnectionStrings__DefaultConnection: "Server=localhost;Port=3306;Database=test_db;User=root;Password=test_password;AllowPublicKeyRetrieval=True;"
  run: |
    dotnet tool install --global dotnet-ef
    export PATH="$PATH:$HOME/.dotnet/tools"
    dotnet ef database update \
      --project YoutubeRag.Infrastructure \
      --startup-project YoutubeRag.Api \
      --verbose
```

**Prevention:**
- Always apply migrations in CI before tests
- Test migrations on clean database
- Use migration bundle for production

---

### Issue 2: MySQL Connection Refused

**Symptoms:**
```
MySqlException: Unable to connect to any of the specified MySQL hosts
Connection refused at localhost:3306
```

**Diagnosis:**
```yaml
# Add diagnostic step in workflow
- name: Check MySQL Status
  run: |
    mysql --version
    nc -zv localhost 3306
    mysql -h localhost -P 3306 -u root -ptest_password -e "SELECT 1"
```

**Fix:**
```yaml
# Ensure MySQL service is healthy
services:
  mysql:
    image: mysql:8.0
    options: >-
      --health-cmd="mysqladmin ping"
      --health-interval=10s
      --health-timeout=5s
      --health-retries=5
```

**Common Causes:**
- MySQL service not started
- Port conflict
- Authentication plugin mismatch
- Health check not passing

---

### Issue 3: Migration Timeout

**Symptoms:**
```
Execution Timeout Expired. The timeout period elapsed prior to completion
```

**Fix:**
```yaml
# Increase command timeout in connection string
ConnectionStrings__DefaultConnection: "Server=localhost;...;CommandTimeout=300"
```

Or in migrations:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("SET SESSION max_execution_time = 300000;");
    // your migration code
}
```

---

### Issue 4: Duplicate Migration Applied

**Symptoms:**
```
MySqlException: Duplicate entry 'xxx' for key 'PRIMARY'
Table already exists
```

**Fix:**
```bash
# Check migration history
dotnet ef migrations list --project YoutubeRag.Infrastructure

# Remove last migration if not applied to production
dotnet ef migrations remove --project YoutubeRag.Infrastructure

# Reapply clean migration
dotnet ef database update --project YoutubeRag.Infrastructure
```

---

## Test Failures

### Issue 1: Tests Pass Locally But Fail in CI

**Common Causes:**
1. **Environment differences:** File paths (Windows vs Linux)
2. **Timing issues:** Race conditions, timeouts
3. **Service dependencies:** MySQL, Redis not ready
4. **Configuration missing:** Environment variables

**Diagnosis:**
```yaml
# Add diagnostic output in CI
- name: Debug Test Environment
  run: |
    echo "OS: $(uname -a)"
    echo "User: $(whoami)"
    echo "Working Dir: $(pwd)"
    echo "Temp Dir: $TMPDIR"
    env | grep -E 'Connection|JWT|Processing|Whisper' | sort
    mysql -h localhost -P 3306 -u root -ptest_password -e "SHOW DATABASES;"
    redis-cli ping
```

**Fix:**
```yaml
# Ensure services are ready
- name: Wait for Services
  run: |
    timeout 60 bash -c 'until mysql -h localhost -P 3306 -u root -ptest_password -e "SELECT 1"; do sleep 2; done'
    timeout 60 bash -c 'until redis-cli ping; do sleep 2; done'
```

---

### Issue 2: Flaky Integration Tests

**Symptoms:**
- Tests pass sometimes, fail other times
- Random timeouts or race conditions

**Fix:**
```csharp
// Increase timeouts for CI
public class IntegrationTest
{
    private int GetTimeout() =>
        Environment.GetEnvironmentVariable("CI") == "true"
            ? 30000  // 30 seconds in CI
            : 5000;  // 5 seconds locally
}

// Add retry logic
[Fact]
[Retry(3)]
public async Task FlakeyTest()
{
    // Test code
}
```

---

### Issue 3: Test Isolation Issues

**Symptoms:**
```
Tests fail when run together but pass individually
```

**Fix:**
```csharp
// Ensure proper cleanup
public class TestClass : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
    }
}
```

Or use unique databases per test:
```yaml
env:
  ConnectionStrings__DefaultConnection: "Server=localhost;Database=test_db_${{ github.run_id }};..."
```

---

### Issue 4: Code Coverage Too Low

**Symptoms:**
```
Code coverage 65% is below the required threshold of 80%
```

**Diagnosis:**
```bash
# Generate local coverage report
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:HtmlInline

# Open report
open CoverageReport/index.html
```

**Fix:**
1. Add more tests for uncovered code
2. Exclude generated code from coverage
3. Remove dead code
4. Adjust threshold temporarily

```xml
<!-- coverlet.runsettings -->
<Exclude>
  [*]*.Migrations.*,
  [*]*Migration,
  [*]*.Designer
</Exclude>
```

---

## Docker Build Issues

### Issue 1: Docker Build Fails on COPY Command

**Symptoms:**
```
ERROR: failed to solve: failed to compute cache key: failed to stat /src
COPY failed: file not found
```

**Fix:**
1. Check `.dockerignore` is not excluding required files
2. Verify paths in Dockerfile are correct
3. Use `COPY --from=build` with correct stage

```dockerfile
# Correct
COPY --from=build /src/YoutubeRag.Api/bin/Release/net8.0/publish .

# Incorrect (missing publish)
COPY --from=build /src/YoutubeRag.Api/bin/Release/net8.0 .
```

---

### Issue 2: Docker Build Times Out

**Symptoms:**
```
Error: buildx call failed with exit code 1: failed to solve: process timeout
```

**Fix:**
```yaml
# Increase timeout
- name: Build Docker Image
  timeout-minutes: 30
  uses: docker/build-push-action@v5
```

Or optimize Dockerfile:
```dockerfile
# Use build cache
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore first (cached)
COPY *.sln .
COPY */*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet restore

# Then copy source (invalidates cache only when code changes)
COPY . .
RUN dotnet build
```

---

### Issue 3: Docker Multi-Platform Build Fails

**Symptoms:**
```
ERROR: failed to solve: failed to load metadata for docker.io/library/image
```

**Fix:**
```yaml
- name: Set up QEMU
  uses: docker/setup-qemu-action@v3

- name: Set up Docker Buildx
  uses: docker/setup-buildx-action@v3

- name: Build Multi-Platform
  uses: docker/build-push-action@v5
  with:
    platforms: linux/amd64,linux/arm64
    # Remove arm64 temporarily if causing issues
    # platforms: linux/amd64
```

---

### Issue 4: Container Health Check Fails

**Symptoms:**
```
Container is unhealthy after 30s
health check failed
```

**Diagnosis:**
```bash
# Check health endpoint locally
docker build -t youtuberag:test .
docker run -d -p 8080:8080 youtuberag:test
sleep 10
curl http://localhost:8080/health

# View container logs
docker logs <container-id>
```

**Fix:**
```dockerfile
# Simplify health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Or use wget if curl not available
HEALTHCHECK CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1
```

---

## Deployment Failures

### Issue 1: Helm Chart Not Found

**Symptoms:**
```
Error: INSTALLATION FAILED: failed to download "youtuberag"
Error: path "./helm-chart" not found
```

**Current Status:** Helm charts not yet implemented

**Temporary Fix:**
```yaml
# In cd.yml
continue-on-error: true  # Already set
```

**Permanent Fix (Sprint 3):**
1. Create Helm chart structure
2. Define values.yaml
3. Create templates for deployment, service, ingress

---

### Issue 2: Secrets Not Found

**Symptoms:**
```
Error: Secret STAGING_DB_PASSWORD not found
```

**Fix:**
```bash
# Add secret via GitHub CLI
gh secret set STAGING_DB_PASSWORD --body "your-password"

# Or via UI
Settings > Secrets and variables > Actions > New repository secret
```

**Required Secrets:**
```bash
# Staging
STAGING_DB_HOST
STAGING_DB_PASSWORD
STAGING_REDIS_HOST
STAGING_JWT_SECRET

# Production
PROD_DB_HOST
PROD_DB_PASSWORD
PROD_REDIS_HOST
PROD_JWT_SECRET

# Optional
SLACK_WEBHOOK
SNYK_TOKEN
NVD_API_KEY
```

---

### Issue 3: Smoke Tests Fail After Deployment

**Symptoms:**
```
curl: (7) Failed to connect to staging.youtuberag.example.com port 443
```

**Diagnosis:**
```bash
# Check if service is running
kubectl get pods -n staging
kubectl get svc -n staging
kubectl logs -n staging deployment/youtuberag-staging

# Test from within cluster
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- \
  curl http://youtuberag-staging:8080/health
```

**Fix:**
1. Verify deployment succeeded
2. Check service selector matches pod labels
3. Verify ingress configuration
4. Check DNS resolution

---

## Security Scan Issues

### Issue 1: OWASP Dependency Check Fails

**Symptoms:**
```
Error: NVD API rate limit exceeded
Error: Unable to download vulnerability database
```

**Fix:**
```yaml
# Add NVD API key (free from nvd.nist.gov)
- name: Run OWASP Dependency Check
  with:
    args: >
      --nvdApiKey ${{ secrets.NVD_API_KEY }}
      --nvdApiDelay 2000
```

Or use continue-on-error:
```yaml
- name: Run OWASP Dependency Check
  continue-on-error: true
```

---

### Issue 2: CodeQL Analysis Timeout

**Symptoms:**
```
Error: Analysis timed out after 120 minutes
```

**Fix:**
```yaml
- name: Perform CodeQL Analysis
  timeout-minutes: 180  # Increase timeout
  uses: github/codeql-action/analyze@v2
```

---

### Issue 3: Container Scan Finds Vulnerabilities

**Symptoms:**
```
HIGH severity vulnerabilities found in base image
```

**Fix:**
1. Update base image to latest:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
   ```

2. Add security updates:
   ```dockerfile
   RUN apk upgrade --no-cache
   ```

3. Suppress known false positives in `.dependency-check-suppressions.xml`

---

## Performance Issues

### Issue 1: CI Pipeline Too Slow

**Symptoms:**
- Build takes >20 minutes
- Tests timeout frequently

**Optimizations:**

1. **Cache NuGet packages:**
   ```yaml
   - uses: actions/cache@v3
     with:
       path: ~/.nuget/packages
       key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
   ```

2. **Parallel test execution:**
   ```yaml
   run: dotnet test --parallel --max-cpus 4
   ```

3. **Use matrix builds:**
   ```yaml
   strategy:
     matrix:
       project: [Domain, Application, Infrastructure, Api]
   ```

4. **Skip unnecessary jobs:**
   ```yaml
   if: github.event_name != 'pull_request'  # Skip on PRs
   ```

---

### Issue 2: Docker Build Too Slow

**Optimizations:**

1. **Use BuildKit:**
   ```yaml
   env:
     DOCKER_BUILDKIT: 1
   ```

2. **Enable caching:**
   ```yaml
   - uses: docker/build-push-action@v5
     with:
       cache-from: type=gha
       cache-to: type=gha,mode=max
   ```

3. **Multi-stage build optimization:**
   ```dockerfile
   # Cache dependencies separately
   FROM sdk AS restore
   COPY *.csproj .
   RUN dotnet restore

   FROM restore AS build
   COPY . .
   RUN dotnet build
   ```

---

## GitHub Actions Specific Issues

### Issue 1: Workflow Not Triggering

**Diagnosis:**
```yaml
# Check workflow syntax
name: workflow-name.yml

# Verify triggers
on:
  push:
    branches: [ develop, master ]
```

**Common Causes:**
- YAML syntax error
- Incorrect branch name
- Workflow disabled
- Path filters excluding changes

**Fix:**
```bash
# Validate workflow syntax
gh workflow view ci.yml

# Enable workflow
gh workflow enable ci.yml

# Manual trigger
gh workflow run ci.yml
```

---

### Issue 2: Actions Token Permissions

**Symptoms:**
```
Error: Resource not accessible by integration
```

**Fix:**
```yaml
# Add permissions at workflow level
permissions:
  contents: read
  security-events: write
  actions: read
  packages: write
```

---

### Issue 3: Artifact Upload Fails

**Symptoms:**
```
Error: Unable to upload artifact
```

**Fix:**
```yaml
- uses: actions/upload-artifact@v3
  if: always()  # Upload even on failure
  with:
    name: test-results
    path: TestResults/
    retention-days: 30
    if-no-files-found: warn  # Don't fail if no files
```

---

## Emergency Procedures

### Bypass CI Checks (Emergency Only)

```bash
# Push without CI
git push origin main --no-verify

# Or merge PR with admin override
# Settings > Branches > Edit protection rule > Allow administrators to bypass
```

**WARNING:** Only use in emergencies. Document why in commit message.

---

### Rollback Deployment

```bash
# Helm rollback
helm rollback youtuberag -n production

# Kubernetes rollback
kubectl rollout undo deployment/youtuberag -n production

# Docker tag rollback
docker tag youtuberag:previous youtuberag:latest
docker push youtuberag:latest
```

---

### Disable Failing Workflow

```bash
# Temporarily disable workflow
gh workflow disable security.yml

# Re-enable later
gh workflow enable security.yml
```

---

## Getting Help

### Log Collection

```bash
# Download workflow logs
gh run download <run-id>

# View specific job logs
gh run view <run-id> --log --job <job-id>

# Export as artifact
zip -r logs.zip .github-workflow-logs/
```

### Report Issue Template

```markdown
## Issue Description
[Describe the problem]

## Workflow
- Name: [ci.yml / cd.yml / security.yml]
- Run ID: [link to workflow run]
- Branch: [branch name]

## Error Message
```
[paste error]
```

## Steps Taken
1. [what you tried]
2. [results]

## Environment
- Runner: [ubuntu-latest / windows-latest]
- .NET Version: [8.0.x]
- Docker Version: [if applicable]

## Logs
[attach workflow logs]
```

---

## Useful Commands Reference

### Local Testing

```bash
# Test build locally
dotnet build YoutubeRag.sln --configuration Release

# Test with same environment as CI
docker-compose -f docker-compose.test.yml up --build

# Run specific test
dotnet test --filter "FullyQualifiedName~YoutubeRag.Tests.Integration.VideoTests"

# Generate coverage locally
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

### GitHub CLI

```bash
# List workflows
gh workflow list

# View workflow runs
gh run list --workflow=ci.yml --limit 10

# Watch workflow run
gh run watch

# Cancel running workflow
gh run cancel <run-id>

# Re-run failed workflow
gh run rerun <run-id>
```

### Docker

```bash
# Build locally matching CI
docker build -t youtuberag:test --target runtime .

# Run and test
docker run -d -p 8080:8080 youtuberag:test
curl http://localhost:8080/health

# Debug container
docker run -it --entrypoint /bin/bash youtuberag:test

# Check logs
docker logs <container-id>
```

---

## Prevention Checklist

Before committing changes that affect CI/CD:

- [ ] Build succeeds locally
- [ ] All tests pass locally
- [ ] Docker image builds successfully
- [ ] Code formatted (`dotnet format`)
- [ ] No secrets in code
- [ ] Update CI/CD docs if needed
- [ ] Test in feature branch first
- [ ] Monitor first CI run after merge

---

## Monitoring and Alerts

### Set Up Workflow Status Badges

Add to README.md:
```markdown
![CI](https://github.com/user/repo/workflows/CI%20Pipeline/badge.svg)
![CD](https://github.com/user/repo/workflows/CD%20Pipeline/badge.svg)
![Security](https://github.com/user/repo/workflows/Security%20Scanning/badge.svg)
```

### Configure Slack Notifications

```yaml
- name: Notify on Failure
  if: failure()
  uses: 8398a7/action-slack@v3
  with:
    status: ${{ job.status }}
    webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

---

## Document History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2025-10-10 | Initial creation | DevOps Team |

---

**End of Troubleshooting Guide**
