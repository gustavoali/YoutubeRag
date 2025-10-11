# YoutubeRag.NET - Quick Reference Card

**DEVOPS-008** | Last Updated: 2025-10-10 | Version: 1.0

Quick reference for common commands and workflows. Keep this handy for daily development!

---

## 🚀 Setup & Installation

### First-Time Setup (5 Minutes)

**Windows:**
```powershell
.\scripts\dev-setup.ps1
```

**Linux/macOS:**
```bash
chmod +x scripts/dev-setup.sh
./scripts/dev-setup.sh
```

### Manual Setup

```bash
# 1. Copy environment template
cp .env.template .env

# 2. Start infrastructure
docker-compose up -d

# 3. Restore packages
dotnet restore

# 4. Build solution
dotnet build

# 5. Run migrations
dotnet ef database update \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api
```

---

## 🐳 Docker Commands

### Start/Stop Services

```bash
# Start all services
docker-compose up -d

# Start specific service
docker-compose up -d mysql
docker-compose up -d redis

# Stop all services
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v

# View running containers
docker-compose ps
```

### Logs & Debugging

```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f mysql
docker-compose logs -f redis

# View last 100 lines
docker-compose logs --tail=100 mysql

# Restart service
docker-compose restart mysql
```

### Database Access

```bash
# Connect to MySQL in Docker
docker-compose exec mysql mysql -u root -p
# Password: root

# Run SQL file
docker-compose exec -T mysql mysql -u root -proot youtube_rag_db < script.sql
```

---

## 🔨 Build & Run

### Development

```bash
# Build solution
dotnet build

# Build specific project
dotnet build YoutubeRag.Api

# Clean build
dotnet clean
dotnet build

# Run API (Development mode)
dotnet run --project YoutubeRag.Api

# Run with specific environment
dotnet run --project YoutubeRag.Api --environment Local
dotnet run --project YoutubeRag.Api --environment Production

# Watch mode (auto-reload on changes)
dotnet watch --project YoutubeRag.Api
```

### Production Build

```bash
# Build for release
dotnet build --configuration Release

# Publish for deployment
dotnet publish --configuration Release --output ./publish
```

---

## 🧪 Testing

### Run Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test project
dotnet test YoutubeRag.Tests.Integration

# Run tests matching filter
dotnet test --filter "Category=Integration"
dotnet test --filter "FullyQualifiedName~WhisperModel"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Build then test (Release)
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

### Test Results

Current metrics: **422/425 tests passing (99.3%)**

```bash
# View test summary
dotnet test --logger:"console;verbosity=detailed"

# Generate HTML coverage report (requires ReportGenerator)
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:./coverage-report
```

---

## 💾 Database

### Migrations

```bash
# Create new migration
dotnet ef migrations add MigrationName \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api

# Apply migrations
dotnet ef database update \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api

# Revert to specific migration
dotnet ef database update PreviousMigrationName \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api

# Remove last migration
dotnet ef migrations remove \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api

# List migrations
dotnet ef migrations list \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api
```

### Database Reset

```bash
# Drop and recreate database
docker-compose down -v
docker-compose up -d mysql
sleep 30  # Wait for MySQL to start
dotnet ef database update
```

### Seed Test Data

```powershell
# Windows
.\scripts\seed-database.ps1

# Linux/macOS
./scripts/seed-database.sh
```

Creates:
- 4 test users
- 5 sample videos
- 5 background jobs
- Test transcript segments

---

## 📦 Package Management

### NuGet Packages

```bash
# Restore all packages
dotnet restore

# Add package to project
dotnet add YoutubeRag.Api package PackageName

# Update package
dotnet add YoutubeRag.Api package PackageName --version X.Y.Z

# Remove package
dotnet remove YoutubeRag.Api package PackageName

# List outdated packages
dotnet list package --outdated

# Update all packages (CAUTION)
dotnet list package --outdated | grep ">" | awk '{print $2}' | xargs -I {} dotnet add package {}
```

---

## 🔍 Code Quality

### Formatting

```bash
# Format code
dotnet format

# Check formatting (CI mode)
dotnet format --verify-no-changes

# Format specific project
dotnet format YoutubeRag.Api
```

### Analysis

```bash
# Run analyzers
dotnet build /p:RunAnalyzers=true

# Treat warnings as errors
dotnet build /p:TreatWarningsAsErrors=true
```

---

## 🎯 Common Workflows

### Start Development Session

```bash
# 1. Update code
git pull origin master

# 2. Start infrastructure
docker-compose up -d

# 3. Apply new migrations
dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

# 4. Run tests
dotnet test

# 5. Start API
dotnet run --project YoutubeRag.Api
```

### Before Committing

```bash
# 1. Format code
dotnet format

# 2. Build
dotnet build

# 3. Run tests
dotnet test

# 4. Check for warnings
dotnet build /p:TreatWarningsAsErrors=true
```

### Create Feature Branch

```bash
# 1. Create branch
git checkout -b feature/my-feature

# 2. Make changes, commit
git add .
git commit -m "feat: add my feature"

# 3. Push branch
git push -u origin feature/my-feature

# 4. Create PR (if gh CLI is installed)
gh pr create --title "Add my feature" --body "Description"
```

---

## 🌐 API Access

### Endpoints

```bash
# API Base URL
http://localhost:5000

# Swagger UI
http://localhost:5000/swagger

# Health Check
curl http://localhost:5000/health

# Process YouTube Video
curl -X POST http://localhost:5000/api/videos/from-url \
  -H "Content-Type: application/json" \
  -d '{"url":"https://youtube.com/watch?v=VIDEO_ID"}'

# Semantic Search
curl -X POST http://localhost:5000/api/search/semantic \
  -H "Content-Type: application/json" \
  -d '{"query":"machine learning","maxResults":5}'
```

### Authentication

```bash
# Register user
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}'

# Use token in requests
curl -X GET http://localhost:5000/api/videos \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

---

## 🐛 Troubleshooting

### Common Issues

**Port already in use:**
```bash
# Windows
netstat -ano | findstr :5000
taskkill /PID <process_id> /F

# Linux/macOS
lsof -i :5000
kill -9 <process_id>
```

**Database connection fails:**
```bash
# Check MySQL is running
docker-compose ps mysql

# Restart MySQL
docker-compose restart mysql

# View MySQL logs
docker-compose logs mysql
```

**Tests failing:**
```bash
# Clean and rebuild
dotnet clean
dotnet build --configuration Release

# Reset test database
docker-compose restart mysql
sleep 30
dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
```

**NuGet restore fails:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore again
dotnet restore
```

---

## 📝 Environment Variables

### Required

```bash
# Database
DATABASE_HOST=localhost
DATABASE_PORT=3306
DATABASE_NAME=youtube_rag_db
DATABASE_USER=root
DATABASE_PASSWORD=root

# Redis
REDIS_HOST=localhost
REDIS_PORT=6379

# JWT (generate with: openssl rand -base64 32)
JWT_SECRET=your-super-secret-key-minimum-32-chars
JWT_ISSUER=YoutubeRag.Api
JWT_AUDIENCE=YoutubeRag.Client
```

### Optional

```bash
# Whisper
WHISPER_MODELS_PATH=/app/models  # or C:\Models\Whisper
WHISPER_MODEL_SIZE=base          # tiny, base, small

# Application Paths
TEMP_PATH=/app/temp
UPLOADS_PATH=/app/uploads

# Logging
SERILOG__MINIMUMLEVEL=Information
```

---

## 📚 Documentation

### Quick Links

- [README](../README.md) - Main documentation
- [Developer Setup Guide](devops/DEVELOPER_SETUP_GUIDE.md) - Detailed setup
- [DevOps Plan](devops/DEVOPS_IMPLEMENTATION_PLAN.md) - DevOps roadmap
- [CI/CD Troubleshooting](../GITHUB_CI_LESSONS_LEARNED.md) - Pipeline issues
- [Test Results](../TEST_RESULTS_REPORT.md) - Test metrics

### API Documentation

- **Swagger UI:** http://localhost:5000/swagger (when running)
- **OpenAPI JSON:** http://localhost:5000/swagger/v1/swagger.json

---

## 🔗 Useful Resources

### .NET Tools

```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Update EF Core tools
dotnet tool update --global dotnet-ef

# Install code coverage tool
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### Git Aliases

Add to `.gitconfig`:

```ini
[alias]
  st = status
  co = checkout
  br = branch
  ci = commit
  cm = commit -m
  cp = cherry-pick
  lg = log --oneline --graph --all --decorate
  last = log -1 HEAD
  undo = reset HEAD~1 --soft
```

---

## 💡 Tips & Tricks

### Speed Up Builds

```bash
# Build only changed projects
dotnet build --no-dependencies

# Skip restore if packages are up to date
dotnet build --no-restore

# Parallel builds (default, but can force)
dotnet build --no-incremental -m
```

### Watch for Changes

```bash
# Auto-reload API on code changes
dotnet watch --project YoutubeRag.Api

# Run tests on changes
dotnet watch test --project YoutubeRag.Tests.Integration
```

### Docker Optimization

```bash
# Remove unused images
docker system prune -a

# Remove volumes not in use
docker volume prune

# View disk usage
docker system df
```

---

## 📞 Getting Help

1. Check this quick reference
2. Review [README.md](../README.md)
3. Search [GitHub Issues](https://github.com/gustavoali/YoutubeRag/issues)
4. Check [Troubleshooting Guide](../README.md#troubleshooting)
5. Create new issue with:
   - Error message
   - Steps to reproduce
   - Environment info (OS, .NET version, Docker version)

---

**Last Updated:** 2025-10-10
**Version:** 1.0
**Maintainer:** YoutubeRag Development Team

**💡 Tip:** Print this reference card and keep it near your workspace!
