# CI/CD Quick Start Guide

**Last Updated:** 2025-10-10
**For:** YoutubeRag Development Team

---

## Before You Commit

### Quick Checklist

```bash
# 1. Build locally
dotnet build YoutubeRag.sln --configuration Release

# 2. Run tests
dotnet test YoutubeRag.sln --configuration Release

# 3. Format code
dotnet format YoutubeRag.sln

# 4. Verify no secrets
git diff | grep -E "(password|secret|key|token)" -i

# 5. Commit and push
git add .
git commit -m "Your message"
git push
```

---

## CI/CD Workflows

### 1. PR Checks (Fast - ~10 minutes)

**Triggers:** On every PR to develop/master

**What it does:**
- Quick build validation
- Code formatting check
- Unit tests only
- Security quick scan
- Docker build test

**How to monitor:**
```bash
gh pr checks
# or visit: GitHub > Pull Requests > Your PR > Checks
```

---

### 2. Full CI Pipeline (~18 minutes)

**Triggers:** On push to develop/master, or PR merge

**What it does:**
- Full build
- Database migrations
- All integration tests (74 tests)
- Code coverage (80% threshold)
- Security scans
- Artifact generation

**How to monitor:**
```bash
gh run list --workflow=ci.yml
gh run watch
```

---

### 3. Security Scanning (~15 minutes)

**Triggers:** Daily at 2 AM UTC, or on push to develop/master

**What it does:**
- CodeQL analysis
- Dependency vulnerabilities
- Container security
- Secret scanning
- License compliance

**How to view:**
```bash
# View security alerts
gh api repos/:owner/:repo/code-scanning/alerts
```

---

## Common Tasks

### Running Tests Locally

```bash
# All tests
dotnet test YoutubeRag.sln --configuration Release

# Integration tests only
dotnet test --filter "Category=Integration"

# Specific test
dotnet test --filter "FullyQualifiedName~YoutubeRag.Tests.Integration.VideoTests.ShouldDownloadVideo"

# With coverage
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# Generate coverage report
reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"CoverageReport" \
  -reporttypes:"HtmlInline"
```

---

### Testing with Docker Compose

```bash
# Start test environment (MySQL + Redis)
docker-compose -f docker-compose.test.yml up -d

# Wait for services
sleep 10

# Run tests
dotnet test YoutubeRag.sln

# View logs
docker-compose -f docker-compose.test.yml logs

# Clean up
docker-compose -f docker-compose.test.yml down -v
```

---

### Database Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api

# Apply migrations locally
dotnet ef database update \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api

# Generate SQL script (for review)
dotnet ef migrations script \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api \
  --output migration.sql

# Remove last migration (if not applied)
dotnet ef migrations remove \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api
```

---

### Code Formatting

```bash
# Check formatting
dotnet format YoutubeRag.sln --verify-no-changes

# Auto-fix formatting
dotnet format YoutubeRag.sln

# Format specific project
dotnet format YoutubeRag.Api/YoutubeRag.Api.csproj
```

---

### Docker Operations

```bash
# Build image
docker build -t youtuberag:dev .

# Build specific stage
docker build --target test -t youtuberag:test .
docker build --target debug -t youtuberag:debug .

# Run container
docker run -d -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;..." \
  -e ConnectionStrings__Redis="host.docker.internal:6379" \
  youtuberag:dev

# View logs
docker logs <container-id> -f

# Execute command in container
docker exec -it <container-id> /bin/bash
```

---

## Troubleshooting Quick Reference

### Build Fails

```bash
# Clear caches
dotnet clean
dotnet nuget locals all --clear
rm -rf bin/ obj/

# Restore and rebuild
dotnet restore YoutubeRag.sln
dotnet build YoutubeRag.sln --configuration Release
```

### Tests Fail

```bash
# Check services are running
docker ps
mysql -h localhost -P 3306 -u root -ptest_password -e "SELECT 1"
redis-cli ping

# View test output
dotnet test --logger "console;verbosity=detailed"

# Run single test with diagnostics
dotnet test --filter "TestName" --logger "console;verbosity=detailed"
```

### Migration Fails

```bash
# Check connection
mysql -h localhost -P 3306 -u root -ptest_password -e "SHOW DATABASES;"

# List migrations
dotnet ef migrations list \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api

# Reset database (DANGER: Deletes all data)
dotnet ef database drop --force \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api

dotnet ef database update \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api
```

### CI Fails But Local Passes

```bash
# Run with same environment as CI
export ASPNETCORE_ENVIRONMENT=Testing
export ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=test_db;User=root;Password=test_password;AllowPublicKeyRetrieval=True;"

# Run tests
dotnet test YoutubeRag.sln

# Compare .NET versions
dotnet --version
# Should be 8.0.x

# Check target frameworks
grep -r "TargetFramework" **/*.csproj
# All should be net8.0
```

---

## Viewing CI Results

### GitHub Web UI

1. Go to: `https://github.com/[org]/[repo]/actions`
2. Click on workflow run
3. Click on specific job
4. View logs and artifacts

### GitHub CLI

```bash
# List recent runs
gh run list --limit 10

# View specific run
gh run view <run-id>

# Download artifacts
gh run download <run-id>

# View logs
gh run view <run-id> --log

# Cancel running workflow
gh run cancel <run-id>

# Re-run failed workflow
gh run rerun <run-id>
```

---

## Workflow Badges

Add to README.md:

```markdown
![CI Pipeline](https://github.com/[org]/[repo]/workflows/CI%20Pipeline/badge.svg)
![CD Pipeline](https://github.com/[org]/[repo]/workflows/CD%20Pipeline/badge.svg)
![Security Scanning](https://github.com/[org]/[repo]/workflows/Security%20Scanning/badge.svg)
![PR Checks](https://github.com/[org]/[repo]/workflows/PR%20Checks/badge.svg)
```

---

## Configuration Files

### Environment Variables (CI)

Located in: `.github/workflows/ci.yml`

Key variables:
- `ConnectionStrings__DefaultConnection`
- `ConnectionStrings__Redis`
- `JwtSettings__SecretKey`
- `Processing__TempFilePath`
- `Whisper__ModelsPath`

### Code Coverage

Located in: `coverlet.runsettings`

Excludes:
- Migrations
- Generated code
- Test projects

### Dependency Check

Located in: `.dependency-check-suppressions.xml`

Suppresses:
- Development dependencies
- Known false positives

---

## Git Workflow with CI/CD

### Feature Branch Flow

```bash
# 1. Create feature branch from develop
git checkout develop
git pull origin develop
git checkout -b feature/my-feature

# 2. Make changes
# ... edit files ...

# 3. Format and test
dotnet format YoutubeRag.sln
dotnet test YoutubeRag.sln

# 4. Commit and push
git add .
git commit -m "feat: add new feature"
git push origin feature/my-feature

# 5. Create PR
gh pr create --base develop --title "Add new feature"

# 6. Monitor PR checks
gh pr checks

# 7. After approval, merge
gh pr merge --squash
```

### Hotfix Flow

```bash
# 1. Create hotfix from master
git checkout master
git pull origin master
git checkout -b hotfix/critical-fix

# 2. Fix issue
# ... edit files ...

# 3. Test thoroughly
dotnet test YoutubeRag.sln

# 4. Commit and push
git add .
git commit -m "fix: critical bug in production"
git push origin hotfix/critical-fix

# 5. Create PR to master
gh pr create --base master --title "HOTFIX: Critical bug"

# 6. After merge to master, merge to develop
git checkout develop
git merge master
git push origin develop
```

---

## Performance Tips

### Faster Local Builds

```bash
# Skip restore if packages haven't changed
dotnet build --no-restore

# Skip tests during development
dotnet build --no-test

# Build specific project
dotnet build YoutubeRag.Api/YoutubeRag.Api.csproj
```

### Faster Tests

```bash
# Run tests in parallel
dotnet test --parallel

# Run only fast tests
dotnet test --filter "Category=Unit"

# Skip integration tests
dotnet test --filter "Category!=Integration"
```

### Faster Docker Builds

```bash
# Use BuildKit
export DOCKER_BUILDKIT=1
docker build -t youtuberag:dev .

# Use cache
docker build --cache-from youtuberag:latest -t youtuberag:dev .
```

---

## Getting Help

### Documentation

1. **CI_CD_ANALYSIS_REPORT.md** - Complete analysis of CI/CD issues
2. **CI_CD_FIXES.md** - Step-by-step fix instructions
3. **CI_CD_TROUBLESHOOTING.md** - Comprehensive troubleshooting guide
4. **CI_CD_IMPLEMENTATION_SUMMARY.md** - Implementation summary

### Support Channels

- GitHub Issues: For bugs and feature requests
- Team Chat: For quick questions
- DevOps Team: For pipeline issues

### Useful Links

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET CLI Documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [Docker Documentation](https://docs.docker.com/)
- [EF Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

---

## Best Practices

### Before Committing

- [ ] Code builds locally
- [ ] Tests pass locally
- [ ] Code formatted
- [ ] No secrets in code
- [ ] Meaningful commit message
- [ ] Updated documentation if needed

### Creating PRs

- [ ] Descriptive title
- [ ] Clear description of changes
- [ ] Link to user story/issue
- [ ] Screenshots (if UI changes)
- [ ] Tests added/updated
- [ ] Documentation updated

### Reviewing PRs

- [ ] Code quality
- [ ] Test coverage
- [ ] No secrets exposed
- [ ] Breaking changes documented
- [ ] CI checks passed

---

## Emergency Procedures

### CI Completely Broken

```bash
# Temporarily disable CI
gh workflow disable ci.yml

# Fix issues locally
# ... make fixes ...

# Test locally
dotnet build YoutubeRag.sln
dotnet test YoutubeRag.sln

# Push fix
git commit -m "fix: CI pipeline"
git push

# Re-enable CI
gh workflow enable ci.yml

# Manually trigger
gh workflow run ci.yml
```

### Need to Bypass CI (EMERGENCY ONLY)

```bash
# Admin can bypass
git push --no-verify

# Or merge PR with admin override
# Settings > Branches > Edit protection rule > Allow administrators to bypass
```

**WARNING:** Document why in commit message!

### Rollback Production Deployment

```bash
# If using Helm
helm rollback youtuberag -n production

# If using Kubernetes
kubectl rollout undo deployment/youtuberag -n production

# If using Docker
docker tag youtuberag:previous youtuberag:latest
docker push youtuberag:latest
```

---

## Cheat Sheet

```bash
# Quick commands

# Build
dotnet build YoutubeRag.sln --configuration Release

# Test
dotnet test YoutubeRag.sln

# Format
dotnet format YoutubeRag.sln

# Coverage
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# Migrations
dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

# Docker
docker build -t youtuberag:dev .
docker-compose -f docker-compose.test.yml up -d

# GitHub CLI
gh pr checks
gh run watch
gh workflow list
```

---

**Quick Start Guide End**

For detailed information, see:
- CI_CD_ANALYSIS_REPORT.md
- CI_CD_FIXES.md
- CI_CD_TROUBLESHOOTING.md
