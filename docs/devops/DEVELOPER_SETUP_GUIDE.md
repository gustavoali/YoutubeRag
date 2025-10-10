# Developer Setup Guide - YoutubeRag Project

**Last Updated:** 2025-10-10
**Version:** 1.0
**Target Audience:** New developers joining the project

---

## Welcome!

This guide will help you set up the YoutubeRag development environment from scratch. By following these instructions, you'll have a fully functional development environment in approximately **5-10 minutes**.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start (Recommended)](#quick-start-recommended)
3. [Manual Setup](#manual-setup)
4. [Verify Installation](#verify-installation)
5. [Development Workflows](#development-workflows)
6. [Troubleshooting](#troubleshooting)
7. [IDE Setup](#ide-setup)
8. [Next Steps](#next-steps)

---

## Prerequisites

Before starting, ensure you have the following installed:

### Required Software

| Software | Version | Download Link | Purpose |
|----------|---------|---------------|---------|
| Git | Latest | [git-scm.com](https://git-scm.com/) | Version control |
| .NET SDK | 8.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) | Build and run application |
| Docker Desktop | Latest | [docker.com](https://www.docker.com/products/docker-desktop) | Container runtime |

### Optional but Recommended

| Software | Purpose |
|----------|---------|
| Visual Studio Code | Lightweight code editor with excellent .NET support |
| Visual Studio 2022 | Full-featured IDE for .NET development |
| Windows Terminal | Modern terminal with tabs and better PowerShell support |
| WSL2 (Windows only) | Better Docker performance on Windows |

### System Requirements

**Minimum:**
- CPU: 4 cores
- RAM: 8 GB
- Disk: 20 GB free space
- OS: Windows 10/11, macOS, or Linux

**Recommended:**
- CPU: 8 cores
- RAM: 16 GB
- Disk: 50 GB free space (SSD preferred)
- OS: Windows 11 with WSL2 enabled

---

## Quick Start (Recommended)

### For Windows Users

1. **Clone the repository**
   ```powershell
   git clone https://github.com/gustavoali/YoutubeRag.git
   cd YoutubeRag
   ```

2. **Run the setup script**
   ```powershell
   .\scripts\dev-setup.ps1
   ```

3. **Wait for setup to complete** (~5 minutes)
   - Downloads Docker images
   - Starts MySQL and Redis
   - Runs database migrations
   - Seeds test data

4. **Start the application**
   ```powershell
   dotnet run --project YoutubeRag.Api
   ```

5. **Open your browser**
   - API: http://localhost:5000
   - Swagger: http://localhost:5000/swagger
   - Health: http://localhost:5000/health

### For Linux/Mac Users

1. **Clone the repository**
   ```bash
   git clone https://github.com/gustavoali/YoutubeRag.git
   cd YoutubeRag
   ```

2. **Run the setup script**
   ```bash
   chmod +x scripts/dev-setup.sh
   ./scripts/dev-setup.sh
   ```

3. **Wait for setup to complete** (~5 minutes)

4. **Start the application**
   ```bash
   dotnet run --project YoutubeRag.Api
   ```

5. **Open your browser**
   - API: http://localhost:5000
   - Swagger: http://localhost:5000/swagger
   - Health: http://localhost:5000/health

**That's it!** You're ready to start developing.

---

## Manual Setup

If you prefer to understand each step or the automated script fails, follow these manual instructions.

### Step 1: Verify Prerequisites

**Check Git:**
```bash
git --version
# Expected: git version 2.x.x or higher
```

**Check .NET SDK:**
```bash
dotnet --version
# Expected: 8.0.x or higher
```

**Check Docker:**
```bash
docker --version
docker-compose --version
# Expected: Docker version 20.x or higher
# Expected: Docker Compose version 2.x or higher
```

### Step 2: Clone Repository

```bash
git clone https://github.com/gustavoali/YoutubeRag.git
cd YoutubeRag
```

### Step 3: Configure Environment

**Copy environment template:**
```bash
# Windows PowerShell
Copy-Item .env.example .env.local

# Linux/Mac
cp .env.example .env.local
```

**Edit `.env.local` if needed** (optional - defaults work for local dev):
```bash
# Database
DB_HOST=localhost
DB_PORT=3306
DB_NAME=youtube_rag_db
DB_USER=youtube_rag_user
DB_PASSWORD=youtube_rag_password

# Redis
REDIS_HOST=localhost
REDIS_PORT=6379

# Application
ASPNETCORE_ENVIRONMENT=Development
PROCESSING_TEMP_PATH=/tmp/youtuberag    # Windows: C:\Temp\YoutubeRag
WHISPER_MODELS_PATH=/tmp/whisper-models # Windows: C:\Models\Whisper
```

### Step 4: Start Infrastructure Services

**Using Docker Compose (recommended):**
```bash
docker-compose up -d mysql redis
```

**Wait for services to be ready:**
```bash
# Windows PowerShell
Start-Sleep -Seconds 20

# Linux/Mac
sleep 20
```

**Verify services are running:**
```bash
docker-compose ps

# Expected output:
# NAME                    STATUS    PORTS
# youtube-rag-mysql       Up        0.0.0.0:3306->3306/tcp
# youtube-rag-redis       Up        0.0.0.0:6379->6379/tcp
```

### Step 5: Restore Dependencies

```bash
dotnet restore YoutubeRag.sln
```

### Step 6: Build Solution

```bash
dotnet build YoutubeRag.sln --configuration Release
```

### Step 7: Run Database Migrations

**Install EF Core tools (if not already installed):**
```bash
dotnet tool install --global dotnet-ef
```

**Run migrations:**
```bash
dotnet ef database update \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api
```

**Expected output:**
```
Applying migration '20241001000000_InitialCreate'.
Applying migration '20241002000000_AddVideoMetadata'.
...
Done.
```

### Step 8: Seed Test Data (Optional)

```bash
# Windows PowerShell
docker exec youtube-rag-mysql mysql -u root -prootpassword < scripts\seed-test-data.sql

# Linux/Mac
docker exec youtube-rag-mysql mysql -u root -prootpassword < scripts/seed-test-data.sql
```

### Step 9: Run the Application

```bash
dotnet run --project YoutubeRag.Api
```

**Expected output:**
```
[12:34:56 INF] Starting YouTube RAG API
[12:34:57 INF] Database connection successful - Storage Mode: Database
[12:34:57 INF] Configured Hangfire recurring jobs
[12:34:58 INF] Now listening on: http://localhost:5000
[12:34:58 INF] Application started. Press Ctrl+C to shut down.
```

### Step 10: Verify Installation

Open your browser and visit:
- **API Root:** http://localhost:5000
- **Swagger UI:** http://localhost:5000/swagger
- **Health Check:** http://localhost:5000/health

---

## Verify Installation

### Health Check

Visit http://localhost:5000/health and verify all checks are green:

```json
{
  "status": "Healthy",
  "totalDuration": 123.45,
  "checks": {
    "database": {
      "status": "Healthy",
      "description": "MySQL connection is healthy"
    },
    "redis": {
      "status": "Healthy",
      "description": "Redis connection is healthy"
    },
    "ffmpeg": {
      "status": "Healthy",
      "description": "FFmpeg is available"
    },
    "disk_space": {
      "status": "Healthy",
      "description": "Sufficient disk space available"
    }
  },
  "timestamp": "2025-10-10T12:34:56Z"
}
```

### Run Tests

**Run all tests:**
```bash
dotnet test YoutubeRag.sln --configuration Release
```

**Expected output:**
```
Passed!  - Failed:     0, Passed:    74, Skipped:     0, Total:    74
```

### Check Database Connection

```bash
docker exec -it youtube-rag-mysql mysql -u youtube_rag_user -pyoutube_rag_password youtube_rag_db -e "SHOW TABLES;"
```

**Expected output:**
```
+---------------------------+
| Tables_in_youtube_rag_db  |
+---------------------------+
| Users                     |
| Videos                    |
| TranscriptSegments        |
| Jobs                      |
| RefreshTokens             |
| ...                       |
+---------------------------+
```

### Check Redis Connection

```bash
docker exec -it youtube-rag-redis redis-cli ping
```

**Expected output:**
```
PONG
```

---

## Development Workflows

### Daily Development

**Start your day:**
```bash
# Start infrastructure services
docker-compose up -d mysql redis

# Start the API (with hot reload)
dotnet watch run --project YoutubeRag.Api
```

**End your day:**
```bash
# Stop the API: Ctrl+C

# Stop infrastructure (optional - can leave running)
docker-compose stop
```

### Running Tests

**Run all tests:**
```bash
dotnet test
```

**Run tests in watch mode:**
```bash
dotnet watch test --project YoutubeRag.Tests.Integration
```

**Run specific test:**
```bash
dotnet test --filter "FullyQualifiedName~VideoService"
```

**Run tests with coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Database Migrations

**Create a new migration:**
```bash
dotnet ef migrations add MigrationName \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api
```

**Apply migrations:**
```bash
dotnet ef database update \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api
```

**Rollback to previous migration:**
```bash
dotnet ef database update PreviousMigrationName \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api
```

**Generate SQL script:**
```bash
dotnet ef migrations script --idempotent \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api \
  --output migration.sql
```

### Code Quality

**Format code:**
```bash
dotnet format YoutubeRag.sln
```

**Check format without changes:**
```bash
dotnet format YoutubeRag.sln --verify-no-changes
```

**Run code analysis:**
```bash
dotnet build --configuration Release \
  /p:EnableNETAnalyzers=true \
  /p:AnalysisLevel=latest
```

**Check for vulnerabilities:**
```bash
dotnet list package --vulnerable --include-transitive
```

### Using Make Commands (Optional)

If you have `make` installed (Linux/Mac) or use WSL on Windows:

```bash
make help                    # Show all available commands
make dev                     # Start local development
make test-local              # Run tests locally
make test-docker             # Run tests in Docker (matches CI)
make migrate                 # Run database migrations
make format                  # Format code
make lint                    # Run code analysis
make security                # Check vulnerabilities
make clean                   # Clean containers and volumes
```

---

## Troubleshooting

### Common Issues and Solutions

#### Issue: Port 3306 already in use

**Symptoms:**
```
Error: Bind for 0.0.0.0:3306 failed: port is already allocated
```

**Solutions:**

**Option 1: Stop existing MySQL**
```bash
# Windows
net stop MySQL80

# Linux/Mac
sudo systemctl stop mysql
```

**Option 2: Use different port**
Edit `.env.local`:
```bash
DB_PORT=3307
```

Update `docker-compose.yml`:
```yaml
mysql:
  ports:
    - "3307:3306"
```

#### Issue: Docker containers won't start

**Symptoms:**
```
ERROR: Cannot start service mysql: ...
```

**Solutions:**

1. **Check Docker is running:**
   ```bash
   docker ps
   ```

2. **Clean up old containers:**
   ```bash
   docker-compose down -v
   docker system prune -a
   ```

3. **Restart Docker Desktop**

4. **Check logs:**
   ```bash
   docker-compose logs mysql
   ```

#### Issue: EF Core migrations fail

**Symptoms:**
```
Unable to create an object of type 'ApplicationDbContext'
```

**Solutions:**

1. **Ensure database is running:**
   ```bash
   docker ps | grep mysql
   ```

2. **Check connection string:**
   Verify in `.env.local` or `appsettings.json`

3. **Install EF Core tools:**
   ```bash
   dotnet tool install --global dotnet-ef
   ```

4. **Add to PATH (if needed):**
   ```bash
   # Windows PowerShell
   $env:PATH += ";$HOME\.dotnet\tools"

   # Linux/Mac
   export PATH="$PATH:$HOME/.dotnet/tools"
   ```

#### Issue: Application won't start - DI error

**Symptoms:**
```
Cannot consume scoped service from singleton
```

**Solution:**
This has been fixed in recent commits. Pull latest changes:
```bash
git pull origin develop
dotnet restore
dotnet build
```

#### Issue: Tests fail locally but pass in CI

**Symptoms:**
Different test results between environments

**Solutions:**

1. **Run tests in Docker (matches CI exactly):**
   ```bash
   docker-compose -f docker-compose.test.yml up --build --abort-on-container-exit
   ```

2. **Check environment variables:**
   Compare local `.env.local` with CI environment in `.github/workflows/ci.yml`

3. **Clean database:**
   ```bash
   docker-compose down -v
   docker-compose up -d mysql redis
   # Wait for services
   dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
   ```

#### Issue: Slow Docker performance on Windows

**Symptoms:**
- Long build times
- Slow file access
- High CPU usage

**Solutions:**

1. **Use WSL2 backend in Docker Desktop:**
   - Open Docker Desktop settings
   - Go to "General"
   - Enable "Use the WSL 2 based engine"

2. **Move project to WSL2 filesystem:**
   ```bash
   # In WSL terminal
   cd /home/yourusername
   git clone https://github.com/gustavoali/YoutubeRag.git
   ```

3. **Disable antivirus for Docker directories:**
   - Exclude `C:\ProgramData\Docker`
   - Exclude your project directory

4. **Increase Docker resource limits:**
   - Open Docker Desktop settings
   - Go to "Resources"
   - Increase Memory to 8GB
   - Increase CPUs to 4

#### Issue: NuGet restore fails

**Symptoms:**
```
error NU1301: The local source doesn't exist
```

**Solution:**
Remove Windows-specific path from `nuget.config`:
```bash
# Edit nuget.config and remove:
# <add key="Microsoft Visual Studio Offline Packages" value="C:\Program Files (x86)\..." />
```

#### Issue: FFmpeg not found

**Symptoms:**
```
FFmpeg health check failed
```

**Solutions:**

**Windows:**
1. Download FFmpeg from [ffmpeg.org](https://ffmpeg.org/download.html)
2. Extract to `C:\ffmpeg`
3. Add to PATH: `C:\ffmpeg\bin`
4. Restart terminal

**Linux/Mac:**
```bash
# Ubuntu/Debian
sudo apt-get install ffmpeg

# macOS
brew install ffmpeg
```

**Verify:**
```bash
ffmpeg -version
```

---

## IDE Setup

### Visual Studio Code

#### Recommended Extensions

Install these extensions for the best development experience:

1. **C# Dev Kit** (ms-dotnettools.csdevkit)
   - IntelliSense, debugging, testing

2. **Docker** (ms-azuretools.vscode-docker)
   - Manage containers from VS Code

3. **EditorConfig** (editorconfig.editorconfig)
   - Consistent code formatting

4. **REST Client** (humao.rest-client)
   - Test API endpoints

5. **GitLens** (eamodio.gitlens)
   - Enhanced Git integration

6. **TODO Tree** (gruntfuggly.todo-tree)
   - Track TODOs in code

#### Setup

1. **Install extensions:**
   ```bash
   code --install-extension ms-dotnettools.csdevkit
   code --install-extension ms-azuretools.vscode-docker
   code --install-extension editorconfig.editorconfig
   ```

2. **Open workspace:**
   ```bash
   code .
   ```

3. **Select SDK:**
   - Press `Ctrl+Shift+P` (Windows/Linux) or `Cmd+Shift+P` (Mac)
   - Type "SDK"
   - Select ".NET: Use .NET SDK: 8.0.x"

4. **Configure debugger:**
   - Press `F5`
   - Select ".NET Core"
   - VS Code will create `.vscode/launch.json`

5. **Start debugging:**
   - Press `F5`
   - Application starts with debugger attached
   - Set breakpoints by clicking left of line numbers

#### Useful Keyboard Shortcuts

| Action | Windows/Linux | Mac |
|--------|---------------|-----|
| Start Debugging | `F5` | `F5` |
| Run Without Debugging | `Ctrl+F5` | `Cmd+F5` |
| Stop Debugging | `Shift+F5` | `Shift+F5` |
| Toggle Breakpoint | `F9` | `F9` |
| Command Palette | `Ctrl+Shift+P` | `Cmd+Shift+P` |
| Quick Open | `Ctrl+P` | `Cmd+P` |
| Integrated Terminal | `` Ctrl+` `` | `` Cmd+` `` |

### Visual Studio 2022

#### Setup

1. **Open solution:**
   - File > Open > Project/Solution
   - Select `YoutubeRag.sln`

2. **Set startup project:**
   - Right-click `YoutubeRag.Api` in Solution Explorer
   - Select "Set as Startup Project"

3. **Configure launch settings:**
   - Right-click `YoutubeRag.Api`
   - Properties > Debug > Launch Profiles
   - Verify URL: `http://localhost:5000`

4. **Start debugging:**
   - Press `F5` or click "Start Debugging"

#### Useful Features

- **Package Manager Console:** Tools > NuGet Package Manager > Package Manager Console
  - Run EF Core commands directly

- **SQL Server Object Explorer:** View > SQL Server Object Explorer
  - Connect to MySQL using MySQL Connector

- **Test Explorer:** View > Test Explorer
  - Run and debug tests

### Development Containers (Advanced)

For the most consistent development experience, use VS Code devcontainers:

1. **Install "Dev Containers" extension**
   ```bash
   code --install-extension ms-vscode-remote.remote-containers
   ```

2. **Open in container:**
   - Press `Ctrl+Shift+P`
   - Select "Dev Containers: Reopen in Container"
   - Wait for container to build (~5 minutes first time)

3. **Develop inside container:**
   - All tools pre-configured
   - Same environment as CI
   - Extensions auto-installed

---

## Next Steps

### Learning Resources

1. **Project Documentation:**
   - [Architecture Guide](../../ARCHITECTURE_VIDEO_PIPELINE.md)
   - [API Usage Guide](../../API_USAGE_GUIDE.md)
   - [Development Guidelines](../../DEVELOPMENT_GUIDELINES_NET.md)

2. **Sprint Planning:**
   - [Sprint 2 Plan](../../SPRINT_2_PLAN.md)
   - [User Stories](../../SPRINT_2_USER_STORIES.md)
   - [Product Backlog](../../PRODUCT_BACKLOG.md)

3. **External Resources:**
   - [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
   - [Entity Framework Core](https://docs.microsoft.com/ef/core)
   - [Hangfire Documentation](https://docs.hangfire.io)

### Your First Contribution

Ready to contribute? Here's a suggested path:

1. **Familiarize yourself with the codebase:**
   ```bash
   # Explore the project structure
   tree -L 2
   ```

2. **Run existing tests:**
   ```bash
   dotnet test --configuration Release
   ```

3. **Pick a task:**
   - Check [BACKLOG_ITEMS.md](../../BACKLOG_ITEMS.md)
   - Look for issues labeled "good first issue"

4. **Create a feature branch:**
   ```bash
   git checkout -b feature/your-feature-name
   ```

5. **Make changes and test:**
   ```bash
   # Make your changes
   dotnet build
   dotnet test
   ```

6. **Commit and push:**
   ```bash
   git add .
   git commit -m "feat: your feature description"
   git push origin feature/your-feature-name
   ```

7. **Create a Pull Request:**
   - Go to GitHub
   - Click "New Pull Request"
   - Fill in the PR template
   - Wait for code review

### Get Help

If you encounter issues:

1. **Check documentation:**
   - This guide
   - [Troubleshooting Guide](CI_CD_TROUBLESHOOTING.md)
   - [GitHub CI Lessons Learned](../../GITHUB_CI_LESSONS_LEARNED.md)

2. **Search existing issues:**
   - [GitHub Issues](https://github.com/gustavoali/YoutubeRag/issues)

3. **Ask the team:**
   - Create a new GitHub issue
   - Tag with "question" label
   - Provide detailed error messages and logs

4. **DevOps support:**
   - For infrastructure issues
   - Environment setup problems
   - CI/CD pipeline failures

---

## Quick Reference Card

### Essential Commands

```bash
# Start development
docker-compose up -d mysql redis
dotnet run --project YoutubeRag.Api

# Run tests
dotnet test

# Database migrations
dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

# Format code
dotnet format

# Check for issues
dotnet list package --vulnerable --include-transitive

# View logs
docker-compose logs -f mysql
docker-compose logs -f redis

# Stop everything
docker-compose down
```

### Important URLs

- **API:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger
- **Health Check:** http://localhost:5000/health
- **Hangfire Dashboard:** http://localhost:5000/hangfire (when enabled)

### Default Credentials

**MySQL:**
- Host: localhost:3306
- Database: youtube_rag_db
- User: youtube_rag_user
- Password: youtube_rag_password
- Root Password: rootpassword

**Redis:**
- Host: localhost:6379
- Password: (none)

---

## Feedback

This guide is continuously improved based on developer feedback.

**Found an issue?** [Create an issue](https://github.com/gustavoali/YoutubeRag/issues/new)

**Have a suggestion?** [Submit a PR](https://github.com/gustavoali/YoutubeRag/compare)

---

**Welcome to the team! Happy coding!** 🚀
