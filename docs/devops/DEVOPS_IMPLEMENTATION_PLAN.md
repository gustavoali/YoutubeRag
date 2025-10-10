# DevOps Implementation Plan - YoutubeRag Project

**Document Version:** 1.0
**Date:** 2025-10-10
**Status:** Ready for Implementation
**Priority:** HIGH - Environment Inconsistencies Causing Test Failures

---

## Executive Summary

### Current State Assessment

The YoutubeRag project has a solid foundation with:
- ASP.NET Core 8.0 application with Clean Architecture
- MySQL 8.0 database with Entity Framework Core
- Redis for caching and session management
- Hangfire for background job processing
- GitHub Actions CI/CD with comprehensive workflows
- Docker and Docker Compose configurations
- 74 integration tests (71 passing, 3 failing due to environment differences)

### Problem Statement

**Environment inconsistencies** between local (Windows), CI (Linux), and production environments are causing:
- 3 integration tests failing in CI but passing locally
- Different behavior in path handling (Windows vs Linux)
- Configuration mismatches between environments
- Database migration inconsistencies
- Difficult onboarding for new developers

### Solution Overview

Implement a **consistent, containerized development workflow** that:
1. Uses Docker Compose for local development (matches CI/production)
2. Standardizes environment variables across all environments
3. Provides automated setup scripts for Windows and Linux
4. Enhances CI/CD with better environment parity
5. Implements monitoring and observability for all environments

### Expected Outcomes

- **Zero environment-specific test failures**
- **5-minute developer onboarding** (from clone to running)
- **Consistent behavior** across local/CI/production
- **Improved debugging** with standardized logging
- **Faster feedback loops** in CI/CD

---

## Phase 1: Quick Wins (Week 1 - 2-3 days)

**Goal:** Immediate improvements to reduce environment friction
**Effort:** 16-20 hours
**Risk:** LOW
**Dependencies:** None

### 1.1 Environment Configuration Standardization

**Problem:** Different appsettings.json between Windows and Linux

**Solution:**

```yaml
# .env.local (for local development)
# .env.ci (for GitHub Actions)
# .env.production (for production)

# Database
DB_HOST=localhost
DB_PORT=3306
DB_NAME=youtube_rag_db
DB_USER=youtube_rag_user
DB_PASSWORD=youtube_rag_password

# Redis
REDIS_HOST=localhost
REDIS_PORT=6379
REDIS_PASSWORD=

# Application
ASPNETCORE_ENVIRONMENT=Development
PROCESSING_TEMP_PATH=/tmp/youtuberag
WHISPER_MODELS_PATH=/tmp/whisper-models
FFMPEG_PATH=ffmpeg

# JWT
JWT_SECRET=YourSecretKeyMinimum256BitsForDevelopment123!
JWT_EXPIRATION_MINUTES=60
```

**Implementation Tasks:**

1. Create `.env.example` with all required variables (DONE - exists)
2. Create `.env.local`, `.env.ci`, `.env.production` templates
3. Update `appsettings.json` to use environment variables as fallbacks
4. Document all environment variables in README
5. Add validation on startup for required environment variables

**Deliverables:**
- Environment files in repository
- Updated Program.cs with environment variable validation
- Documentation in README.md

**Testing:**
- Verify application starts with each .env file
- Confirm all tests pass with standardized config

---

### 1.2 Path Normalization

**Problem:** Windows uses `C:\Temp\YoutubeRag`, Linux uses `/tmp/youtuberag`

**Solution:**

Create a cross-platform path resolver service:

```csharp
public class CrossPlatformPathService : IPathService
{
    private readonly IConfiguration _configuration;

    public string GetTempPath()
    {
        var configPath = _configuration["Processing:TempFilePath"];

        // If path is platform-specific, convert it
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return configPath?.Replace("/tmp/", @"C:\Temp\")
                   ?? @"C:\Temp\YoutubeRag";
        }
        else
        {
            return configPath?.Replace(@"C:\Temp\", "/tmp/")
                   ?? "/tmp/youtuberag";
        }
    }

    public string GetWhisperModelsPath()
    {
        // Similar normalization
    }
}
```

**Implementation Tasks:**

1. Create `IPathService` interface
2. Implement `CrossPlatformPathService`
3. Register in DI container
4. Update all services using hardcoded paths
5. Add unit tests for path normalization

**Deliverables:**
- PathService implementation
- Updated services to use PathService
- Unit tests for path handling

**Testing:**
- Run tests on both Windows and Linux
- Verify temp files created in correct locations

---

### 1.3 Database Seeding Script

**Problem:** Inconsistent test data between environments

**Solution:**

Create a database seeding script for test data:

```sql
-- scripts/seed-test-data.sql
USE test_db;

-- Create test user
INSERT INTO Users (Id, Email, PasswordHash, CreatedAt, UpdatedAt)
VALUES (UUID(), 'test@example.com', '$2a$11$...', NOW(), NOW());

-- Create test videos
INSERT INTO Videos (Id, YouTubeId, Title, Status, CreatedAt, UpdatedAt)
VALUES (UUID(), 'test-video-1', 'Test Video 1', 1, NOW(), NOW());

-- More seed data...
```

**Implementation Tasks:**

1. Create `scripts/seed-test-data.sql`
2. Update CI workflow to run seed script after migrations
3. Add `make seed-test-data` command to Makefile
4. Document seeding process

**Deliverables:**
- SQL seed script
- Updated CI workflow
- Makefile target

**Testing:**
- Run seed script in CI
- Verify tests use seeded data
- Check data consistency

---

### 1.4 Developer Quick Start Script

**Problem:** Manual setup is error-prone and time-consuming

**Solution:**

Create automated setup scripts:

**Windows (PowerShell):**
```powershell
# scripts/dev-setup.ps1
Write-Host "Setting up YoutubeRag development environment..."

# Check prerequisites
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker is not installed. Please install Docker Desktop."
    exit 1
}

# Copy environment file
Copy-Item .env.example .env.local

# Start services
docker-compose up -d mysql redis

# Wait for services
Write-Host "Waiting for services to be ready..."
Start-Sleep -Seconds 20

# Run migrations
dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

# Seed test data
docker exec youtube-rag-mysql mysql -u root -prootpassword < scripts/seed-test-data.sql

Write-Host "Setup complete! Run 'dotnet run --project YoutubeRag.Api' to start."
```

**Linux/Mac (Bash):**
```bash
#!/bin/bash
# scripts/dev-setup.sh
echo "Setting up YoutubeRag development environment..."

# Check prerequisites
command -v docker >/dev/null 2>&1 || { echo "Docker not installed"; exit 1; }

# Copy environment file
cp .env.example .env.local

# Start services
docker-compose up -d mysql redis

# Wait for services
echo "Waiting for services..."
sleep 20

# Run migrations
dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

# Seed test data
docker exec youtube-rag-mysql mysql -u root -prootpassword < scripts/seed-test-data.sql

echo "Setup complete! Run 'dotnet run --project YoutubeRag.Api' to start."
```

**Implementation Tasks:**

1. Create `scripts/dev-setup.ps1` (Windows)
2. Create `scripts/dev-setup.sh` (Linux/Mac)
3. Add prerequisite checks (Docker, .NET, etc.)
4. Test on both platforms
5. Document in README

**Deliverables:**
- Setup scripts for both platforms
- Updated README with setup instructions
- Troubleshooting guide

**Testing:**
- Test on clean Windows machine
- Test on clean Linux machine
- Verify all services start correctly

---

## Phase 2: Core Infrastructure (Week 2-3 - 5-7 days)

**Goal:** Establish consistent Docker-based development workflow
**Effort:** 40-50 hours
**Risk:** MEDIUM
**Dependencies:** Phase 1 complete

### 2.1 Enhanced Docker Compose for Local Development

**Problem:** Current docker-compose.yml doesn't fully replicate CI environment

**Solution:**

Create environment-specific compose files:

```yaml
# docker-compose.override.yml (local development)
version: '3.8'

services:
  api:
    build:
      target: debug  # Use debug stage for local dev
    volumes:
      - .:/src:ro  # Live reload
      - ~/.nuget/packages:/root/.nuget/packages:ro  # Cache NuGet
    environment:
      ASPNETCORE_ENVIRONMENT: Development
    ports:
      - "5000:8080"
      - "5001:8081"  # HTTPS
    command: ["dotnet", "watch", "run", "--project", "YoutubeRag.Api"]

  # Debugging tools (only in local)
  redis-commander:
    profiles: ["dev-tools"]

  adminer:
    profiles: ["dev-tools"]
```

```yaml
# docker-compose.test.yml (matches CI exactly)
version: '3.8'

services:
  mysql-test:
    environment:
      MYSQL_ROOT_PASSWORD: test_password
      MYSQL_DATABASE: test_db

  api-test:
    build:
      target: test
    environment:
      ASPNETCORE_ENVIRONMENT: Testing
      ConnectionStrings__DefaultConnection: "Server=mysql-test;Port=3306;Database=test_db;User=root;Password=test_password;"
```

**Implementation Tasks:**

1. Create `docker-compose.override.yml` for local development
2. Enhance `docker-compose.test.yml` to match CI exactly
3. Add hot-reload support for local development
4. Configure volume mounts for logs and temp files
5. Add health checks to all services
6. Test on both Windows and Linux

**Deliverables:**
- Environment-specific compose files
- Updated Dockerfile with debug stage
- Documentation for each compose file

**Testing:**
- Start local environment: `docker-compose up`
- Run tests: `docker-compose -f docker-compose.test.yml up --abort-on-container-exit`
- Verify behavior matches CI

---

### 2.2 Dockerfile Enhancements

**Problem:** Current Dockerfile is good but can be optimized for development

**Solution:**

Add additional stages to Dockerfile:

```dockerfile
# Stage: Development (with debugging tools)
FROM runtime AS development

USER root

# Install development tools
RUN apt-get update && apt-get install -y \
    vim \
    git \
    curl \
    iputils-ping \
    && rm -rf /var/lib/apt/lists/*

# Install .NET debugging tools
RUN dotnet tool install --global dotnet-watch
ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /src
USER appuser

# Watch mode for live reload
ENTRYPOINT ["dotnet", "watch", "run", "--project", "YoutubeRag.Api"]

# Stage: Test with coverage
FROM test AS test-with-coverage

RUN dotnet tool install --global dotnet-coverage
ENV PATH="${PATH}:/root/.dotnet/tools"

# Run tests with coverage
CMD ["dotnet-coverage", "collect", "-f", "cobertura", "-o", "/test-results/coverage.xml", \
     "dotnet", "test", "--no-build", "--logger", "trx"]
```

**Implementation Tasks:**

1. Add development stage to Dockerfile
2. Add test-with-coverage stage
3. Optimize layer caching
4. Add .dockerignore optimizations
5. Test build times and image sizes

**Deliverables:**
- Enhanced Dockerfile with new stages
- Build performance comparison
- Documentation for each stage

**Testing:**
- Build all stages: `docker build --target <stage>`
- Verify image sizes
- Test live reload in development stage

---

### 2.3 Makefile for Common Tasks

**Problem:** Complex Docker commands are hard to remember

**Solution:**

Enhance existing Makefile:

```makefile
# Makefile

.PHONY: help dev test ci clean

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'

dev: ## Start development environment
	docker-compose up -d mysql redis
	@echo "Waiting for services..."
	@sleep 10
	dotnet run --project YoutubeRag.Api

dev-docker: ## Start full development environment in Docker
	docker-compose up

dev-tools: ## Start with dev tools (Adminer, Redis Commander)
	docker-compose --profile dev-tools up

test-local: ## Run tests locally
	dotnet test --configuration Release

test-docker: ## Run tests in Docker (matches CI)
	docker-compose -f docker-compose.test.yml up --build --abort-on-container-exit

test-watch: ## Run tests in watch mode
	dotnet watch test --project YoutubeRag.Tests.Integration

ci: ## Simulate CI pipeline locally
	@echo "Simulating CI pipeline..."
	docker-compose -f docker-compose.test.yml build
	docker-compose -f docker-compose.test.yml up --abort-on-container-exit
	docker-compose -f docker-compose.test.yml down -v

migrate: ## Run database migrations
	dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

migrate-add: ## Add new migration (use NAME=MigrationName)
	dotnet ef migrations add $(NAME) --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

seed: ## Seed test data
	docker exec youtube-rag-mysql mysql -u root -prootpassword < scripts/seed-test-data.sql

clean: ## Clean containers and volumes
	docker-compose down -v
	docker-compose -f docker-compose.test.yml down -v

clean-all: ## Clean everything including images
	docker-compose down -v --rmi all
	docker-compose -f docker-compose.test.yml down -v --rmi all

logs: ## Show logs for all services
	docker-compose logs -f

logs-api: ## Show API logs only
	docker-compose logs -f api

build: ## Build solution
	dotnet build --configuration Release

format: ## Format code
	dotnet format

lint: ## Run code analysis
	dotnet build --configuration Release /p:EnableNETAnalyzers=true

security: ## Check for security vulnerabilities
	dotnet list package --vulnerable --include-transitive

restore: ## Restore NuGet packages
	dotnet restore

publish: ## Publish application
	dotnet publish YoutubeRag.Api -c Release -o ./publish
```

**Implementation Tasks:**

1. Enhance existing Makefile with new targets
2. Add Windows equivalent (PowerShell aliases)
3. Test all targets on both platforms
4. Document common workflows

**Deliverables:**
- Enhanced Makefile
- Windows PowerShell profile with aliases
- Documentation

**Testing:**
- Run each make target
- Verify behavior on Windows and Linux

---

### 2.4 CI/CD Environment Parity

**Problem:** CI uses service containers, local uses docker-compose

**Solution:**

Update CI workflow to use docker-compose for consistency:

```yaml
# .github/workflows/ci-enhanced.yml
name: CI Pipeline (Enhanced)

on:
  push:
    branches: [develop, master]
  pull_request:
    branches: [develop, master]

jobs:
  test-with-docker-compose:
    name: Test with Docker Compose
    runs-on: ubuntu-latest

    steps:
      - name: Checkout Code
        uses: actions/checkout@v4

      - name: Create .env.ci
        run: |
          cat > .env.ci << EOF
          DB_HOST=mysql-test
          DB_PORT=3306
          DB_NAME=test_db
          DB_USER=root
          DB_PASSWORD=test_password
          REDIS_HOST=redis-test
          REDIS_PORT=6379
          ASPNETCORE_ENVIRONMENT=Testing
          EOF

      - name: Start Services
        run: docker-compose -f docker-compose.test.yml up -d

      - name: Wait for Services
        run: |
          timeout 60 bash -c 'until docker-compose -f docker-compose.test.yml exec -T mysql-test mysqladmin ping -h localhost -u root -ptest_password; do sleep 2; done'

      - name: Run Migrations
        run: |
          docker-compose -f docker-compose.test.yml exec -T api-test \
            dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

      - name: Run Tests
        run: |
          docker-compose -f docker-compose.test.yml run --rm test-runner

      - name: Collect Results
        if: always()
        run: |
          docker-compose -f docker-compose.test.yml cp test-runner:/test-results ./TestResults

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: TestResults/

      - name: Cleanup
        if: always()
        run: docker-compose -f docker-compose.test.yml down -v
```

**Implementation Tasks:**

1. Create enhanced CI workflow using docker-compose
2. Ensure exact environment variable parity
3. Add caching for Docker layers
4. Test workflow on actual PR

**Deliverables:**
- Enhanced CI workflow
- Documentation of changes
- Comparison of old vs new approach

**Testing:**
- Run workflow on test PR
- Compare results with current workflow
- Verify all tests pass

---

## Phase 3: Full Automation (Week 4-5 - 5-7 days)

**Goal:** Complete automation and monitoring
**Effort:** 40-50 hours
**Risk:** MEDIUM
**Dependencies:** Phase 2 complete

### 3.1 Development Container (devcontainer.json)

**Problem:** Developers have different IDE configurations

**Solution:**

Create VS Code devcontainer for consistent environment:

```json
// .devcontainer/devcontainer.json
{
  "name": "YoutubeRag Development",
  "dockerComposeFile": ["../docker-compose.yml", "docker-compose.devcontainer.yml"],
  "service": "api",
  "workspaceFolder": "/src",

  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "ms-dotnettools.csdevkit",
        "ms-azuretools.vscode-docker",
        "ms-vscode.makefile-tools",
        "editorconfig.editorconfig",
        "humao.rest-client"
      ],
      "settings": {
        "dotnet.defaultSolution": "YoutubeRag.sln",
        "omnisharp.enableRoslynAnalyzers": true,
        "omnisharp.enableEditorConfigSupport": true
      }
    }
  },

  "forwardPorts": [5000, 3306, 6379],
  "postCreateCommand": "dotnet restore",
  "remoteUser": "appuser"
}
```

**Implementation Tasks:**

1. Create `.devcontainer/devcontainer.json`
2. Create `.devcontainer/docker-compose.devcontainer.yml`
3. Test with VS Code
4. Document setup process

**Deliverables:**
- Devcontainer configuration
- Documentation for VS Code users
- Troubleshooting guide

**Testing:**
- Open project in VS Code with devcontainer
- Verify all extensions load
- Test debugging

---

### 3.2 Monitoring and Observability

**Problem:** Limited visibility into application behavior across environments

**Solution:**

Implement structured logging and metrics:

```csharp
// Enhanced Program.cs logging
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .Enrich.WithProperty("ProcessId", Environment.ProcessId)
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: "logs/app-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
);
```

**Prometheus Metrics:**

```csharp
// Add Prometheus metrics
builder.Services.AddOpenTelemetryMetrics(options =>
{
    options
        .AddPrometheusExporter()
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation();
});

app.MapPrometheusScrapingEndpoint("/metrics");
```

**Implementation Tasks:**

1. Add structured logging with Serilog
2. Implement Prometheus metrics
3. Create Grafana dashboards
4. Add distributed tracing with OpenTelemetry
5. Configure alerts for critical metrics

**Deliverables:**
- Structured logging implementation
- Prometheus metrics
- Grafana dashboards
- Alert configurations

**Testing:**
- Verify logs in JSON format
- Check metrics endpoint
- View dashboards in Grafana

---

### 3.3 Pre-commit Hooks and Code Quality

**Problem:** Code quality issues discovered late in CI

**Solution:**

Implement pre-commit hooks with Husky.NET:

```bash
# .husky/pre-commit
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

echo "Running pre-commit checks..."

# Format check
dotnet format --verify-no-changes --verbosity quiet || {
    echo "Code formatting issues found. Run 'dotnet format' to fix."
    exit 1
}

# Build check
dotnet build --configuration Release --no-restore --verbosity quiet || {
    echo "Build failed. Fix compilation errors."
    exit 1
}

# Quick unit tests
dotnet test --filter "Category=Unit" --no-build --verbosity quiet || {
    echo "Unit tests failed."
    exit 1
}

echo "Pre-commit checks passed!"
```

**Implementation Tasks:**

1. Install Husky.NET
2. Create pre-commit hook
3. Add pre-push hook for integration tests
4. Configure EditorConfig
5. Test on both platforms

**Deliverables:**
- Husky.NET configuration
- Pre-commit and pre-push hooks
- EditorConfig file
- Documentation

**Testing:**
- Make a commit with formatting issues
- Verify hook prevents commit
- Test on Windows and Linux

---

### 3.4 Automated Database Migrations in CI/CD

**Problem:** Manual migration management is error-prone

**Solution:**

Implement automated migration checks:

```yaml
# .github/workflows/migration-check.yml
name: Migration Check

on:
  pull_request:
    paths:
      - 'YoutubeRag.Infrastructure/Migrations/**'

jobs:
  check-migrations:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout PR
        uses: actions/checkout@v4

      - name: Checkout Base
        run: |
          git fetch origin ${{ github.base_ref }}
          git checkout origin/${{ github.base_ref }} -- YoutubeRag.Infrastructure/Migrations/

      - name: Check for conflicts
        run: |
          # Detect if migrations conflict with base branch
          dotnet ef migrations list --project YoutubeRag.Infrastructure || exit 1

      - name: Generate SQL Script
        run: |
          dotnet ef migrations script --idempotent --output migration.sql \
            --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

      - name: Upload Migration Script
        uses: actions/upload-artifact@v4
        with:
          name: migration-script
          path: migration.sql
```

**Implementation Tasks:**

1. Create migration check workflow
2. Add idempotent migration script generation
3. Implement rollback scripts
4. Test migration conflicts

**Deliverables:**
- Migration check workflow
- Idempotent migration scripts
- Rollback procedures
- Documentation

**Testing:**
- Create conflicting migrations
- Verify workflow detects conflicts
- Test rollback procedures

---

## Phase 4: Production Readiness (Week 6 - 3-4 days)

**Goal:** Prepare for production deployment
**Effort:** 24-32 hours
**Risk:** HIGH
**Dependencies:** Phase 3 complete

### 4.1 Production Docker Compose

```yaml
# docker-compose.prod.yml
version: '3.8'

services:
  api:
    image: youtuberag:${VERSION:-latest}
    restart: always
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
    secrets:
      - db_password
      - jwt_secret
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    deploy:
      replicas: 2
      resources:
        limits:
          cpus: '1.0'
          memory: 2G
        reservations:
          cpus: '0.5'
          memory: 1G

secrets:
  db_password:
    external: true
  jwt_secret:
    external: true
```

### 4.2 Kubernetes Manifests (Optional)

For future Kubernetes deployment:

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: youtuberag-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: youtuberag-api
  template:
    metadata:
      labels:
        app: youtuberag-api
    spec:
      containers:
      - name: api
        image: youtuberag:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        livenessProbe:
          httpGet:
            path: /live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
```

### 4.3 Deployment Pipeline

```yaml
# .github/workflows/cd.yml
name: Continuous Deployment

on:
  push:
    tags:
      - 'v*'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Build and Push Docker Image
        run: |
          docker build -t youtuberag:${{ github.ref_name }} .
          docker push youtuberag:${{ github.ref_name }}

      - name: Deploy to Production
        run: |
          # Deploy using docker-compose or kubectl
          docker-compose -f docker-compose.prod.yml up -d
```

---

## Risk Assessment

### Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Docker performance on Windows | MEDIUM | MEDIUM | Use WSL2, document performance tuning |
| Database migration conflicts | MEDIUM | HIGH | Implement migration checks, use idempotent scripts |
| Container size too large | LOW | LOW | Multi-stage builds already implemented |
| Network issues in containers | LOW | MEDIUM | Use health checks, implement retry logic |
| Local environment resource constraints | MEDIUM | MEDIUM | Document minimum requirements, optimize containers |

### Process Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Developer resistance to Docker | MEDIUM | HIGH | Provide excellent documentation, setup scripts |
| Learning curve for new tools | HIGH | MEDIUM | Create comprehensive guides, provide training |
| Breaking existing workflows | LOW | HIGH | Implement gradually, maintain backward compatibility |
| Time overrun | MEDIUM | MEDIUM | Phased approach allows stopping at any phase |

---

## Success Metrics

### Quantitative Metrics

1. **Environment Consistency**
   - Target: 0 environment-specific test failures
   - Current: 3 failures
   - Measurement: CI test results

2. **Developer Onboarding Time**
   - Target: < 5 minutes from clone to running
   - Current: 30-60 minutes
   - Measurement: Time tracking for new developers

3. **CI Pipeline Success Rate**
   - Target: > 95%
   - Current: ~85% (with recent fixes)
   - Measurement: GitHub Actions history

4. **Build Time**
   - Target: < 3 minutes for cached builds
   - Current: ~4-5 minutes
   - Measurement: CI pipeline duration

5. **Developer Satisfaction**
   - Target: 4.5/5 average rating
   - Current: 3.5/5 (estimated)
   - Measurement: Team survey

### Qualitative Metrics

1. **Documentation Quality**
   - All commands documented
   - Troubleshooting guides complete
   - Examples for common workflows

2. **Debugging Capability**
   - Easy to reproduce CI failures locally
   - Structured logging available
   - Metrics for performance issues

3. **Maintainability**
   - Clear separation of environments
   - Easy to update dependencies
   - Automated security scanning

---

## Implementation Timeline

### Week 1: Quick Wins (Phase 1)
- Days 1-2: Environment configuration standardization
- Day 3: Path normalization and database seeding
- Days 4-5: Developer setup scripts and testing

### Week 2-3: Core Infrastructure (Phase 2)
- Days 1-2: Enhanced Docker Compose
- Days 3-4: Dockerfile enhancements and Makefile
- Days 5-7: CI/CD environment parity and testing

### Week 4-5: Full Automation (Phase 3)
- Days 1-2: Development container setup
- Days 3-4: Monitoring and observability
- Days 5-6: Pre-commit hooks and code quality
- Day 7: Automated migration checks

### Week 6: Production Readiness (Phase 4)
- Days 1-2: Production Docker Compose
- Day 3: Kubernetes manifests (optional)
- Day 4: Final testing and documentation

**Total Duration:** 6 weeks (can be compressed with dedicated resources)

---

## Resource Requirements

### Personnel

- **DevOps Engineer:** 100% allocation for 6 weeks
- **Backend Developer:** 25% allocation (reviews, testing)
- **QA Engineer:** 25% allocation (testing new workflows)

### Infrastructure

- **Development:** Docker Desktop, 16GB RAM minimum
- **CI/CD:** GitHub Actions (current usage acceptable)
- **Testing:** Docker Compose test environment

### Tools

- Docker Desktop (free)
- VS Code with devcontainers (free)
- Makefile/PowerShell (built-in)
- Optional: Kubernetes (for Phase 4)

---

## Maintenance and Support

### Ongoing Maintenance

1. **Weekly Tasks:**
   - Monitor CI/CD success rates
   - Review and update dependencies
   - Check for security vulnerabilities

2. **Monthly Tasks:**
   - Review and update documentation
   - Evaluate new tools and practices
   - Team feedback sessions

3. **Quarterly Tasks:**
   - Major dependency updates
   - Performance optimization
   - Disaster recovery testing

### Support Structure

- **Documentation:** Comprehensive guides in `/docs/devops/`
- **Troubleshooting:** Common issues and solutions documented
- **Escalation:** DevOps engineer available for complex issues

---

## Appendix A: Technology Stack

### Core Technologies
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- MySQL 8.0
- Redis 7.x
- Hangfire (background jobs)

### DevOps Tools
- Docker & Docker Compose
- GitHub Actions
- Makefile (Linux) / PowerShell (Windows)
- Serilog (structured logging)
- Prometheus & Grafana (monitoring)
- OpenTelemetry (tracing)

### Development Tools
- VS Code with devcontainers
- .NET SDK 8.0
- EF Core tools
- dotnet format
- Husky.NET (pre-commit hooks)

---

## Appendix B: Useful Commands Reference

```bash
# Development
make dev                    # Start local development
make dev-docker             # Start in Docker
make test-local             # Run tests locally
make test-docker            # Run tests in Docker (matches CI)

# Database
make migrate                # Run migrations
make migrate-add NAME=X     # Create new migration
make seed                   # Seed test data

# Code Quality
make format                 # Format code
make lint                   # Run static analysis
make security               # Check vulnerabilities

# CI/CD Simulation
make ci                     # Run full CI pipeline locally

# Cleanup
make clean                  # Remove containers and volumes
make clean-all              # Remove everything including images

# Debugging
make logs                   # Show all logs
make logs-api               # Show API logs only
```

---

## Appendix C: Environment Variables Reference

### Required Variables

```bash
# Database
DB_HOST=localhost                    # Database host
DB_PORT=3306                        # Database port
DB_NAME=youtube_rag_db              # Database name
DB_USER=youtube_rag_user            # Database user
DB_PASSWORD=secure_password         # Database password

# Redis
REDIS_HOST=localhost                # Redis host
REDIS_PORT=6379                     # Redis port
REDIS_PASSWORD=                     # Redis password (optional)

# Application
ASPNETCORE_ENVIRONMENT=Development  # Environment (Development/Testing/Production)
PROCESSING_TEMP_PATH=/tmp/youtuberag # Temp file storage path
WHISPER_MODELS_PATH=/tmp/whisper    # Whisper models path
FFMPEG_PATH=ffmpeg                  # FFmpeg executable path

# Security
JWT_SECRET=your-secret-key-min-256-bits  # JWT signing key
JWT_EXPIRATION_MINUTES=60                # Token expiration

# Optional
CORS_ORIGINS=http://localhost:3000       # Allowed CORS origins
LOG_LEVEL=Information                    # Logging level
```

---

## Appendix D: Troubleshooting Guide

### Common Issues

#### Issue: Docker containers won't start
**Symptoms:** `docker-compose up` fails
**Solutions:**
1. Check Docker is running: `docker ps`
2. Check port conflicts: `netstat -ano | findstr :3306`
3. Clean up old containers: `make clean`
4. Check logs: `docker-compose logs`

#### Issue: Database migration fails
**Symptoms:** EF Core errors during migration
**Solutions:**
1. Ensure database is running: `docker ps | grep mysql`
2. Check connection string in environment
3. Verify EF Core tools installed: `dotnet ef --version`
4. Run with verbose: `dotnet ef database update --verbose`

#### Issue: Tests fail locally but pass in CI (or vice versa)
**Symptoms:** Inconsistent test results
**Solutions:**
1. Run tests with docker-compose: `make test-docker`
2. Check environment variables match CI
3. Verify database state: `make clean && make seed`
4. Check logs for differences in behavior

#### Issue: Slow Docker performance on Windows
**Symptoms:** Long build times, slow file access
**Solutions:**
1. Use WSL2 backend in Docker Desktop
2. Move project to WSL2 filesystem
3. Disable antivirus scanning for Docker directories
4. Increase Docker resource limits

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-10 | DevOps Engineer | Initial comprehensive plan |

---

## Approval

- [ ] Technical Lead
- [ ] Backend Team Lead
- [ ] QA Lead
- [ ] Product Owner

**Next Steps:** Review and approve this plan, then proceed with Phase 1 implementation.
