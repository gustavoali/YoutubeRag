# Environment Consistency - Implementation Task Breakdown

**Project:** YoutubeRag .NET
**Date:** 2025-10-10
**Status:** Ready for Development

This document provides the complete task breakdown for implementing environment consistency across Dev/CI/Prod environments.

---

## Epic 1: Docker Development Environment

**Priority:** P0 - Foundation
**Estimated Duration:** 3-5 days
**Assignee:** [Backend Lead Developer]

### Tasks

#### Task 1.1: Create docker-compose.dev.yml
**Effort:** 4 hours
**Files:**
- `C:\agents\youtube_rag_net\docker-compose.dev.yml` (new)

**Requirements:**
```yaml
# Services to include:
1. MySQL 8.0 (development database)
   - Environment: dev credentials
   - Volume: persistent dev data
   - Health check

2. Redis 7 (development cache)
   - No password for dev
   - Health check

3. API (development mode with hot reload)
   - Build from Dockerfile with development target
   - Volume mount source for hot reload
   - Environment variables from .env.development
   - Depends on MySQL and Redis health

4. Adminer (optional - database UI)
   - Profile: tools

5. Redis Commander (optional - Redis UI)
   - Profile: tools
```

**Validation:**
- [ ] `docker-compose -f docker-compose.dev.yml up -d` starts all services
- [ ] All services show healthy status
- [ ] API accessible at http://localhost:5000
- [ ] MySQL accessible at localhost:3306
- [ ] Code changes trigger hot reload

**Related Files:**
- Reference existing: `docker-compose.yml` (production)
- Reference existing: `docker-compose.test.yml` (testing)

---

#### Task 1.2: Update Dockerfile with Development Target
**Effort:** 3 hours
**Files:**
- `C:\agents\youtube_rag_net\Dockerfile` (modify)

**Requirements:**
Add new build stage BEFORE existing stages:

```dockerfile
# ========================================
# Development Stage (NEW - insert at top)
# ========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development

# Install system dependencies (same as runtime-base)
RUN apt-get update && apt-get install -y \
    curl ca-certificates python3 python3-pip python3-venv ffmpeg \
    vim git && rm -rf /var/lib/apt/lists/*

# Python + Whisper
RUN python3 -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"
RUN pip install --no-cache-dir --upgrade pip openai-whisper

# Workspace setup
WORKDIR /workspace

# Install dotnet tools
RUN dotnet tool install --global dotnet-ef dotnet-watch
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy and restore (cached layer)
COPY *.sln ./
COPY */*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*} && mv $file ${file%.*}/; done
RUN dotnet restore

# Copy all source
COPY . .

# Hot reload entry point
ENTRYPOINT ["dotnet", "watch", "run", "--project", "YoutubeRag.Api", "--no-restore"]

# ========================================
# Keep all existing stages below
# ========================================
```

**Validation:**
- [ ] `docker build --target development -t youtuberag:dev .` succeeds
- [ ] Container starts with hot reload active
- [ ] Changing .cs file triggers rebuild
- [ ] All dependencies (FFmpeg, Python, Whisper) available

---

#### Task 1.3: Create Dev Container Configuration
**Effort:** 2 hours
**Files:**
- `.devcontainer/devcontainer.json` (new)
- `.devcontainer/docker-compose.yml` (new - references main docker-compose.dev.yml)

**Requirements:**
```json
// .devcontainer/devcontainer.json
{
  "name": "YoutubeRag Development",
  "dockerComposeFile": ["../docker-compose.dev.yml"],
  "service": "api",
  "workspaceFolder": "/workspace",
  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "ms-azuretools.vscode-docker",
        "editorconfig.editorconfig",
        "ms-dotnettools.vscode-dotnet-runtime",
        "patcx.vscode-nuget-gallery"
      ],
      "settings": {
        "omnisharp.enableRoslynAnalyzers": true,
        "omnisharp.enableEditorConfigSupport": true
      }
    }
  },
  "postCreateCommand": "dotnet restore && dotnet build",
  "remoteUser": "root",
  "forwardPorts": [8080, 3306, 6379],
  "portsAttributes": {
    "8080": {"label": "API"},
    "3306": {"label": "MySQL"},
    "6379": {"label": "Redis"}
  }
}
```

**Validation:**
- [ ] VS Code detects devcontainer configuration
- [ ] "Reopen in Container" works
- [ ] Extensions installed automatically
- [ ] IntelliSense works
- [ ] Ports forwarded correctly

---

#### Task 1.4: Create Environment File Templates
**Effort:** 2 hours
**Files:**
- `.env.example` (update/create)
- `.env.development` (new - gitignored)
- `.gitignore` (update)

**Requirements:**

**`.env.example` (template - committed):**
```bash
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080

# Database
MYSQL_ROOT_PASSWORD=your-root-password-here
MYSQL_DATABASE=youtube_rag_dev
MYSQL_USER=dev_user
MYSQL_PASSWORD=your-password-here
ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=youtube_rag_dev;User=dev_user;Password=your-password-here;

# Redis
ConnectionStrings__Redis=redis:6379

# JWT
JwtSettings__SecretKey=your-jwt-secret-minimum-32-characters-required

# Processing Paths (Container Paths)
Processing__TempFilePath=/app/temp
Processing__FFmpegPath=ffmpeg
Whisper__ModelsPath=/app/models

# OpenAI (optional)
OPENAI_API_KEY=sk-your-key-here

# Feature Flags
AppSettings__EnableAuth=true
AppSettings__EnableBackgroundJobs=true
AppSettings__EnableWebSockets=true
```

**`.env.development` (actual values - gitignored):**
```bash
# Copy from .env.example and fill in actual dev values
MYSQL_ROOT_PASSWORD=dev_root_password
MYSQL_PASSWORD=dev_password
JwtSettings__SecretKey=dev-secret-key-minimum-32-characters-long-12345
# ... etc
```

**`.gitignore` (add):**
```
.env
.env.development
.env.local
.env.*.local
```

**Validation:**
- [ ] `.env.example` has all required variables documented
- [ ] `.env.development` not committed (in .gitignore)
- [ ] Variables load into containers correctly

---

#### Task 1.5: Create Quick Start Script
**Effort:** 1 hour
**Files:**
- `scripts/docker-dev-start.sh` (new)
- `scripts/docker-dev-start.ps1` (new - Windows)

**Requirements:**
```bash
#!/bin/bash
# docker-dev-start.sh

echo "🚀 Starting YoutubeRag Development Environment..."

# Check Docker is running
if ! docker info > /dev/null 2>&1; then
  echo "❌ Docker is not running. Please start Docker Desktop and try again."
  exit 1
fi

# Check for .env.development
if [ ! -f .env.development ]; then
  echo "⚠️  .env.development not found. Copying from .env.example..."
  cp .env.example .env.development
  echo "✏️  Please edit .env.development with your configuration."
  echo "   Then run this script again."
  exit 0
fi

# Start services
echo "📦 Starting services..."
docker-compose -f docker-compose.dev.yml up -d

# Wait for health
echo "⏳ Waiting for services to be healthy..."
timeout 60 bash -c 'until docker-compose -f docker-compose.dev.yml ps | grep -q "healthy"; do sleep 2; done' || {
  echo "⚠️  Services took too long to start. Check logs:"
  echo "   docker-compose -f docker-compose.dev.yml logs"
  exit 1
}

echo "✅ Development environment ready!"
echo ""
echo "📍 Services:"
echo "   API:    http://localhost:5000"
echo "   Swagger: http://localhost:5000/swagger"
echo "   MySQL:  localhost:3306 (user: dev_user, password: see .env.development)"
echo "   Redis:  localhost:6379"
echo ""
echo "📝 Useful commands:"
echo "   View logs:  docker-compose -f docker-compose.dev.yml logs -f"
echo "   Stop:       docker-compose -f docker-compose.dev.yml down"
echo "   Restart:    docker-compose -f docker-compose.dev.yml restart api"
```

**PowerShell version** for Windows users.

**Validation:**
- [ ] Script executable: `chmod +x scripts/docker-dev-start.sh`
- [ ] Script starts environment successfully
- [ ] Error messages helpful
- [ ] Works on both Linux/Mac and Windows

---

#### Task 1.6: Documentation - Docker Development Guide
**Effort:** 4 hours
**Files:**
- `docs/DOCKER_DEVELOPMENT_GUIDE.md` (new)

**Requirements:**
Comprehensive guide covering:
1. Prerequisites (Docker Desktop installation)
2. Quick Start (3 methods: VS Code, script, manual)
3. Daily Workflow (start, stop, logs, rebuild)
4. Hot Reload explanation and usage
5. Database access (Adminer, command line)
6. Redis access (Redis Commander, CLI)
7. Running tests in containers
8. Debugging in containers
9. Troubleshooting common issues
10. Performance tuning (Windows WSL2)

**Validation:**
- [ ] New developer can follow guide and setup environment
- [ ] All common scenarios covered
- [ ] Troubleshooting section comprehensive

---

#### Task 1.7: Update README.md
**Effort:** 1 hour
**Files:**
- `README.md` (modify)

**Requirements:**
Add prominent Docker Quick Start section at top:
```markdown
## Quick Start (Docker - Recommended)

### Prerequisites
- Docker Desktop 24.x+ ([Download](https://www.docker.com/products/docker-desktop))
- VS Code (recommended) or JetBrains Rider

### Option 1: VS Code Dev Containers (Easiest)
1. Install VS Code Remote - Containers extension
2. Clone repository and open in VS Code
3. Click "Reopen in Container" when prompted
4. Wait 5-10 minutes for initial setup
5. Start coding!

### Option 2: Quick Start Script
```bash
./scripts/docker-dev-start.sh
```

### Option 3: Manual
```bash
cp .env.example .env.development
# Edit .env.development with your settings
docker-compose -f docker-compose.dev.yml up -d
```

See [Docker Development Guide](docs/DOCKER_DEVELOPMENT_GUIDE.md) for details.
```

**Validation:**
- [ ] Quick Start section prominent
- [ ] Links work
- [ ] Instructions accurate

---

**Epic 1 Acceptance Criteria:**
- [ ] Complete docker-compose.dev.yml
- [ ] Dockerfile with development target
- [ ] VS Code Dev Containers working
- [ ] Environment templates created
- [ ] Quick start script functional
- [ ] Comprehensive documentation
- [ ] Setup time < 15 minutes for new developer
- [ ] Hot reload functional

---

## Epic 2: Configuration Management Standardization

**Priority:** P0 - Foundation
**Estimated Duration:** 3-5 days
**Assignee:** [Backend Developer]

### Tasks

#### Task 2.1: Configuration Audit
**Effort:** 2 hours
**Files:**
- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Local.json`
- `appsettings.Testing.json`
- `appsettings.Production.json`
- `appsettings.Real.json`

**Requirements:**
1. Create spreadsheet listing all settings across all files
2. Identify duplicates
3. Identify conflicts (same key, different values)
4. Identify unused settings
5. Identify missing documentation
6. Categorize by:
   - Common (all environments)
   - Environment-specific (varies)
   - Deprecated (should remove)

**Deliverable:** Excel/Google Sheet with audit results

**Validation:**
- [ ] All appsettings files analyzed
- [ ] Duplicates identified
- [ ] Conflicts documented
- [ ] Categorization complete

---

#### Task 2.2: Consolidate Base Configuration
**Effort:** 3 hours
**Files:**
- `appsettings.json` (modify)

**Requirements:**
Create canonical base configuration with:
1. Only common settings (true for ALL environments)
2. Safe defaults where possible
3. Documentation comments for each section
4. No secrets or environment-specific values

Example structure:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "",  // Override in environment-specific config
    "Redis": ""               // Override in environment-specific config
  },
  "JwtSettings": {
    "SecretKey": "",          // REQUIRED: Override in env-specific config
    "ExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  },
  "AppSettings": {
    "ProcessingMode": "Mock",
    "StorageMode": "Database"
    // Common defaults
  },
  "YouTube": {
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "MaxVideoDurationSeconds": 14400
  },
  // ... etc
}
```

**Validation:**
- [ ] Only common settings in base
- [ ] No secrets
- [ ] Documentation clear
- [ ] Safe defaults

---

#### Task 2.3: Create Environment-Specific Overrides
**Effort:** 3 hours
**Files:**
- `appsettings.Development.json` (modify)
- `appsettings.Testing.json` (modify)
- `appsettings.Production.json` (modify)

**Requirements:**

**appsettings.Development.json:**
```json
{
  "AppSettings": {
    "Environment": "Development",
    "EnableAuth": true,
    "EnableBackgroundJobs": true,
    "EnableWebSockets": true,
    "EnableDocs": true
  },
  "Processing": {
    "TempFilePath": "/app/temp",  // Container path
    "FFmpegPath": "ffmpeg"
  },
  "Whisper": {
    "ModelsPath": "/app/models"   // Container path
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

**appsettings.Testing.json:**
```json
{
  "AppSettings": {
    "Environment": "Testing",
    "ProcessingMode": "Mock",
    "StorageMode": "Database",  // Now uses real DB not in-memory
    "EnableAuth": false,
    "EnableBackgroundJobs": false,
    "EnableWebSockets": false,
    "EnableDocs": false
  },
  "Processing": {
    "TempFilePath": "/tmp/youtuberag",
    "CleanupAfterHours": 1
  },
  "Cleanup": {
    "JobRetentionDays": 1,
    "NotificationRetentionDays": 1
  }
}
```

**Validation:**
- [ ] Only environment-specific overrides
- [ ] No duplication of base settings
- [ ] Testing config suitable for CI
- [ ] Development config for local containers

---

#### Task 2.4: Implement Configuration Validator
**Effort:** 4 hours
**Files:**
- `YoutubeRag.Api/Configuration/ConfigurationValidator.cs` (new)
- `YoutubeRag.Api/Program.cs` (modify)

**Requirements:**
```csharp
namespace YoutubeRag.Api.Configuration;

public static class ConfigurationValidator
{
    public static void ValidateConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var errors = new List<string>();

        // Validate required connection strings
        var dbConnection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(dbConnection))
        {
            errors.Add("ConnectionStrings:DefaultConnection is required");
        }

        var redisConnection = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            errors.Add("ConnectionStrings:Redis is required");
        }

        // Validate JWT settings
        var jwtSecret = configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            errors.Add("JwtSettings:SecretKey is required");
        }
        else if (jwtSecret.Length < 32)
        {
            errors.Add($"JwtSettings:SecretKey must be at least 32 characters (current: {jwtSecret.Length})");
        }

        // Validate paths exist or can be created
        var tempPath = configuration["Processing:TempFilePath"];
        if (!string.IsNullOrWhiteSpace(tempPath))
        {
            try
            {
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Cannot create Processing:TempFilePath '{tempPath}': {ex.Message}");
            }
        }

        // Production-specific validations
        if (environment.IsProduction())
        {
            if (jwtSecret?.Contains("dev") == true || jwtSecret?.Contains("test") == true)
            {
                errors.Add("Production JWT secret appears to be a development/test value");
            }

            var enableAuth = configuration.GetValue<bool>("AppSettings:EnableAuth");
            if (!enableAuth)
            {
                errors.Add("AppSettings:EnableAuth must be true in production");
            }
        }

        if (errors.Any())
        {
            var errorMessage = "Configuration validation failed:\n" + string.Join("\n", errors.Select(e => $"  - {e}"));
            throw new InvalidOperationException(errorMessage);
        }
    }
}
```

**Update Program.cs:**
```csharp
// Add after builder is created, before services are built
var builder = WebApplication.CreateBuilder(args);

// Validate configuration early
ConfigurationValidator.ValidateConfiguration(builder.Configuration, builder.Environment);

// ... rest of Program.cs
```

**Validation:**
- [ ] Invalid configuration throws clear error on startup
- [ ] All required settings validated
- [ ] Path validations work
- [ ] Production-specific validations active
- [ ] Error messages helpful

---

#### Task 2.5: Configuration Documentation
**Effort:** 3 hours
**Files:**
- `docs/CONFIGURATION_GUIDE.md` (new)

**Requirements:**
Document ALL configuration options:
1. Connection Strings (format, examples)
2. JWT Settings (security considerations)
3. App Settings (all flags explained)
4. Processing Settings (paths, limits)
5. Whisper Settings (models, cleanup)
6. Logging Configuration (Serilog)
7. Environment Variable Mapping (hierarchy)
8. Secrets Management (where to put secrets)
9. Configuration Override Precedence

**Format:**
```markdown
### JwtSettings:SecretKey

**Type:** string
**Required:** Yes
**Minimum Length:** 32 characters
**Environment Variable:** `JwtSettings__SecretKey`

**Description:**
Secret key used for signing JWT tokens. Must be at least 256 bits (32 characters).

**Examples:**
- Development: `dev-secret-key-minimum-32-characters-long`
- Production: Use cryptographically random value from secret manager

**Security:**
- NEVER commit to version control
- Store in:
  - Dev: `.env.development` (gitignored)
  - CI: GitHub Secrets
  - Prod: Azure Key Vault / AWS Secrets Manager
```

**Validation:**
- [ ] All settings documented
- [ ] Examples provided
- [ ] Security notes included
- [ ] Environment variable names correct

---

#### Task 2.6: Secrets Management Setup
**Effort:** 4 hours
**Files:**
- `.gitignore` (update)
- `docs/SECRETS_MANAGEMENT.md` (new)
- `.github/workflows/ci.yml` (update if needed)

**Requirements:**

**For Development:**
1. Ensure `.env*` files in .gitignore
2. Create `.env.example` with placeholders
3. Document secret generation (JWT keys, passwords)

**For CI:**
1. Document GitHub Secrets needed
2. Update CI workflow to use secrets
3. Create setup checklist

**For Production:**
Choose and document ONE of:
- Azure Key Vault (if Azure deployment)
- AWS Secrets Manager (if AWS deployment)
- Docker Secrets (if Docker Swarm)
- Kubernetes Secrets (if K8s deployment)

**Pre-commit Hook (optional but recommended):**
```bash
#!/bin/bash
# .git/hooks/pre-commit

# Check for potential secrets
if git diff --cached | grep -qE "(password|secret|api_?key|token).*=.*(sk-|[A-Za-z0-9]{32})"; then
  echo "⚠️  WARNING: Potential secret detected in commit!"
  echo "Please review your changes and ensure no secrets are committed."
  exit 1
fi
```

**Validation:**
- [ ] All secret types documented
- [ ] Generation procedures documented
- [ ] CI secrets configured
- [ ] Pre-commit hook optional but documented

---

**Epic 2 Acceptance Criteria:**
- [ ] Configuration audit complete
- [ ] Base appsettings.json consolidated
- [ ] Environment-specific files minimal
- [ ] ConfigurationValidator implemented
- [ ] Startup validation active
- [ ] All settings documented
- [ ] Secrets never committed
- [ ] Clear configuration precedence

---

## Epic 3: Cross-Platform Path Handling

**Priority:** P1 - Critical for parity
**Estimated Duration:** 2-3 days
**Assignee:** [Backend Developer]

### Tasks

#### Task 3.1: Create IPathProvider Interface
**Effort:** 1 hour
**Files:**
- `YoutubeRag.Application/Interfaces/IPathProvider.cs` (new)
- `YoutubeRag.Infrastructure/Services/ContainerPathProvider.cs` (new)

**Requirements:**
```csharp
// YoutubeRag.Application/Interfaces/IPathProvider.cs
namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Provides cross-platform path operations
/// </summary>
public interface IPathProvider
{
    /// <summary>
    /// Gets the temporary files directory path
    /// </summary>
    string GetTempPath();

    /// <summary>
    /// Gets the Whisper models directory path
    /// </summary>
    string GetModelsPath();

    /// <summary>
    /// Gets the logs directory path
    /// </summary>
    string GetLogsPath();

    /// <summary>
    /// Combines path segments using OS-appropriate separator
    /// </summary>
    string CombinePaths(params string[] paths);

    /// <summary>
    /// Ensures a directory exists, creating if necessary
    /// </summary>
    void EnsureDirectoryExists(string path);

    /// <summary>
    /// Gets a unique temporary file path
    /// </summary>
    string GetTempFilePath(string extension = "");
}
```

```csharp
// YoutubeRag.Infrastructure/Services/ContainerPathProvider.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace YoutubeRag.Infrastructure.Services;

public class ContainerPathProvider : IPathProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContainerPathProvider> _logger;

    public ContainerPathProvider(IConfiguration configuration, ILogger<ContainerPathProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GetTempPath()
    {
        var path = _configuration["Processing:TempFilePath"] ?? "/app/temp";
        EnsureDirectoryExists(path);
        return path;
    }

    public string GetModelsPath()
    {
        var path = _configuration["Whisper:ModelsPath"] ?? "/app/models";
        EnsureDirectoryExists(path);
        return path;
    }

    public string GetLogsPath()
    {
        var path = _configuration["Logging:LogPath"] ?? "/app/logs";
        EnsureDirectoryExists(path);
        return path;
    }

    public string CombinePaths(params string[] paths)
    {
        if (paths == null || paths.Length == 0)
            return string.Empty;

        return Path.Combine(paths);
    }

    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation("Created directory: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directory: {Path}", path);
                throw;
            }
        }
    }

    public string GetTempFilePath(string extension = "")
    {
        var tempDir = GetTempPath();
        var fileName = $"{Guid.NewGuid()}{extension}";
        return CombinePaths(tempDir, fileName);
    }
}
```

**Register in DI (Program.cs):**
```csharp
builder.Services.AddSingleton<IPathProvider, ContainerPathProvider>();
```

**Validation:**
- [ ] Interface defined in Application layer
- [ ] Implementation in Infrastructure layer
- [ ] Registered in DI container
- [ ] Logging included

---

#### Task 3.2: Update TempFileManagementService
**Effort:** 2 hours
**Files:**
- `YoutubeRag.Infrastructure/Services/TempFileManagementService.cs` (modify)

**Current Issues:**
- May have hard-coded paths
- May use Windows-specific path operations

**Requirements:**
1. Inject IPathProvider in constructor
2. Replace all path operations with IPathProvider methods
3. Use Path.Combine() for any remaining path operations
4. Remove hard-coded paths

**Example Changes:**
```csharp
// BEFORE
private readonly string _tempPath = "C:\\Temp\\YoutubeRag";

public string CreateTempFile(string extension)
{
    var fileName = $"{Guid.NewGuid()}{extension}";
    return $"{_tempPath}\\{fileName}";  // ❌ Windows-specific
}

// AFTER
private readonly IPathProvider _pathProvider;

public TempFileManagementService(IPathProvider pathProvider, ILogger<TempFileManagementService> logger)
{
    _pathProvider = pathProvider;
    _logger = logger;
}

public string CreateTempFile(string extension)
{
    return _pathProvider.GetTempFilePath(extension);  // ✅ Cross-platform
}
```

**Validation:**
- [ ] IPathProvider injected
- [ ] No hard-coded paths
- [ ] All path operations use Path.Combine() or IPathProvider
- [ ] Tests updated

---

#### Task 3.3: Update WhisperModelDownloadService
**Effort:** 2 hours
**Files:**
- `YoutubeRag.Infrastructure/Services/WhisperModelDownloadService.cs` (modify)

**Requirements:**
Same as Task 3.2:
1. Inject IPathProvider
2. Use GetModelsPath() instead of hard-coded path
3. Use CombinePaths() for model file paths

**Example:**
```csharp
// BEFORE
private readonly string _modelsPath = "C:\\Models\\Whisper";

// AFTER
private readonly IPathProvider _pathProvider;

public async Task<string> DownloadModelAsync(string modelName)
{
    var modelsDir = _pathProvider.GetModelsPath();
    var modelPath = _pathProvider.CombinePaths(modelsDir, $"{modelName}.pt");
    // ...
}
```

**Validation:**
- [ ] No hard-coded paths
- [ ] Uses IPathProvider
- [ ] Cross-platform compatible

---

#### Task 3.4: Update VideoDownloadService
**Effort:** 1 hour
**Files:**
- `YoutubeRag.Infrastructure/Services/VideoDownloadService.cs` (modify)

**Requirements:**
1. Inject IPathProvider
2. Use GetTempPath() for video downloads
3. Use GetTempFilePath() for unique file names

**Validation:**
- [ ] IPathProvider used
- [ ] No hard-coded paths
- [ ] File path generation cross-platform

---

#### Task 3.5: Update Configuration Files
**Effort:** 1 hour
**Files:**
- `appsettings.json` (modify)
- `appsettings.Development.json` (modify)
- `appsettings.Testing.json` (modify)
- `.env.example` (modify)

**Requirements:**
Replace all Windows paths with container paths:

```json
// BEFORE
{
  "Processing": {
    "TempFilePath": "C:\\Temp\\YoutubeRag",
    "FFmpegPath": "C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe"
  },
  "Whisper": {
    "ModelsPath": "C:\\Models\\Whisper"
  }
}

// AFTER
{
  "Processing": {
    "TempFilePath": "/app/temp",
    "FFmpegPath": "ffmpeg"  // Rely on PATH
  },
  "Whisper": {
    "ModelsPath": "/app/models"
  }
}
```

**Validation:**
- [ ] No Windows paths (C:\)
- [ ] All paths use forward slashes (/)
- [ ] Paths appropriate for containers
- [ ] FFmpeg relies on PATH not absolute path

---

#### Task 3.6: Add Cross-Platform Path Tests
**Effort:** 3 hours
**Files:**
- `YoutubeRag.Tests.Integration/Services/PathProviderTests.cs` (new)

**Requirements:**
Test that path operations work on both Windows and Linux:

```csharp
[Fact]
public void CombinePaths_WithMultipleSegments_UsesCorrectSeparator()
{
    // Arrange
    var pathProvider = new ContainerPathProvider(config, logger);

    // Act
    var result = pathProvider.CombinePaths("app", "temp", "file.txt");

    // Assert
    Assert.Equal(Path.Combine("app", "temp", "file.txt"), result);
    Assert.Contains("file.txt", result);
}

[Fact]
public void GetTempFilePath_GeneratesUniqueFile_WithCorrectExtension()
{
    // Arrange
    var pathProvider = new ContainerPathProvider(config, logger);

    // Act
    var path1 = pathProvider.GetTempFilePath(".mp3");
    var path2 = pathProvider.GetTempFilePath(".mp3");

    // Assert
    Assert.NotEqual(path1, path2);  // Unique
    Assert.EndsWith(".mp3", path1);
    Assert.EndsWith(".mp3", path2);
}

[Fact]
public void GetTempPath_FromConfiguration_ReturnsConfiguredPath()
{
    // Test that configuration overrides work
}
```

**Validation:**
- [ ] Tests pass on Windows (local)
- [ ] Tests pass on Linux (CI)
- [ ] Path separators correct for OS
- [ ] Configuration overrides tested

---

#### Task 3.7: Search and Replace Hard-Coded Paths
**Effort:** 2 hours
**Files:**
- All .cs files in solution

**Requirements:**
1. Search for `"C:\\` in all C# files
2. Search for `@"C:\` in all C# files
3. Search for string concatenation with `\\` or `/`
4. Replace with Path.Combine() or IPathProvider

**Grep commands:**
```bash
# Find Windows paths
grep -r "C:\\\\" --include="*.cs" .

# Find path concatenation
grep -r 'path.*\\+\|path.*\"+' --include="*.cs" .

# Find forward slash concatenation
grep -r 'path.*/+' --include="*.cs" .
```

**Validation:**
- [ ] No C:\ paths in code
- [ ] All path operations use Path.Combine()
- [ ] Solution builds successfully
- [ ] All tests pass

---

**Epic 3 Acceptance Criteria:**
- [ ] IPathProvider interface created and implemented
- [ ] All services updated to use IPathProvider
- [ ] Configuration uses container paths
- [ ] No hard-coded Windows paths anywhere
- [ ] Cross-platform tests pass
- [ ] Solution builds on Windows and Linux

---

## Epic 4: Testing Infrastructure Parity

**Priority:** P1 - Critical for reliability
**Estimated Duration:** 4-5 days
**Assignee:** [Backend Lead Developer + QA]

### Tasks

#### Task 4.1: Update CustomWebApplicationFactory
**Effort:** 3 hours
**Files:**
- `YoutubeRag.Tests.Integration/Infrastructure/CustomWebApplicationFactory.cs` (modify)

**Current Issue:**
Uses in-memory database which behaves differently than real MySQL.

**Requirements:**
Change from in-memory to real MySQL:

```csharp
// BEFORE
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseInMemoryDatabase(_databaseName);
});

// AFTER
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.UseEnvironment("Testing");

    builder.ConfigureTestServices(services =>
    {
        // Remove existing DbContext
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
        if (descriptor != null)
            services.Remove(descriptor);

        // Get connection string from configuration (docker-compose or env var)
        var connectionString = builder.Configuration.GetConnectionString("TestConnection")
            ?? "Server=localhost;Port=3306;Database=test_db;User=root;Password=test_password;AllowPublicKeyRetrieval=True;";

        // Add MySQL DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString,
                new MySqlServerVersion(new Version(8, 0, 23)),
                mySqlOptions => {
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                }));
    });
}
```

**Validation:**
- [ ] Uses real MySQL connection
- [ ] Connection string from configuration
- [ ] Retry logic for transient failures
- [ ] Tests still pass

---

#### Task 4.2: Create TestDatabaseManager
**Effort:** 4 hours
**Files:**
- `YoutubeRag.Tests.Integration/Infrastructure/TestDatabaseManager.cs` (new)

**Requirements:**
Manage test database lifecycle:

```csharp
public class TestDatabaseManager
{
    private readonly string _baseConnectionString;
    private readonly ILogger _logger;

    public TestDatabaseManager(string baseConnectionString, ILogger logger)
    {
        _baseConnectionString = baseConnectionString;
        _logger = logger;
    }

    /// <summary>
    /// Creates a unique test database for isolation
    /// </summary>
    public async Task<string> CreateTestDatabaseAsync()
    {
        var dbName = $"test_db_{Guid.NewGuid():N}";
        var connectionStringBuilder = new MySqlConnectionStringBuilder(_baseConnectionString)
        {
            Database = dbName
        };

        try
        {
            // Connect to MySQL without database
            using var connection = new MySqlConnection(_baseConnectionString);
            await connection.OpenAsync();

            // Create database
            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = $"CREATE DATABASE {dbName} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
            await createCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Created test database: {DatabaseName}", dbName);

            // Run migrations
            await RunMigrationsAsync(connectionStringBuilder.ConnectionString);

            return connectionStringBuilder.ConnectionString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test database: {DatabaseName}", dbName);
            throw;
        }
    }

    /// <summary>
    /// Drops the test database
    /// </summary>
    public async Task DropTestDatabaseAsync(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var dbName = builder.Database;

        try
        {
            using var connection = new MySqlConnection(_baseConnectionString);
            await connection.OpenAsync();

            using var dropCmd = connection.CreateCommand();
            dropCmd.CommandText = $"DROP DATABASE IF EXISTS {dbName}";
            await dropCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Dropped test database: {DatabaseName}", dbName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to drop test database: {DatabaseName}", dbName);
            // Don't throw - cleanup is best effort
        }
    }

    private async Task RunMigrationsAsync(string connectionString)
    {
        // Use EF Core migrations programmatically
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(connectionString,
            new MySqlServerVersion(new Version(8, 0, 23)));

        using var context = new ApplicationDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        _logger.LogInformation("Applied migrations to test database");
    }
}
```

**Validation:**
- [ ] Creates unique database per test run
- [ ] Runs EF migrations correctly
- [ ] Cleans up databases
- [ ] Handles errors gracefully

---

#### Task 4.3: Create IntegrationTestFixture
**Effort:** 2 hours
**Files:**
- `YoutubeRag.Tests.Integration/Infrastructure/IntegrationTestFixture.cs` (new)

**Requirements:**
IAsyncLifetime implementation for xUnit:

```csharp
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly TestDatabaseManager _databaseManager;
    private string _connectionString;

    public IntegrationTestFixture()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__TestBase")
            ?? "Server=localhost;Port=3306;User=root;Password=test_password;";

        _databaseManager = new TestDatabaseManager(
            baseConnectionString,
            NullLogger<TestDatabaseManager>.Instance);
    }

    public async Task InitializeAsync()
    {
        // Create test database before tests run
        _connectionString = await _databaseManager.CreateTestDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        // Drop test database after tests complete
        if (!string.IsNullOrEmpty(_connectionString))
        {
            await _databaseManager.DropTestDatabaseAsync(_connectionString);
        }
    }

    public string GetConnectionString() => _connectionString;
}
```

**Usage in tests:**
```csharp
public class VideoServiceTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public VideoServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateVideo_WithValidData_SavesSuccessfully()
    {
        // Each test class gets its own database
        var connectionString = _fixture.GetConnectionString();
        // ... test implementation
    }
}
```

**Validation:**
- [ ] Fixture creates database on initialization
- [ ] Fixture drops database on disposal
- [ ] Tests can use fixture
- [ ] Parallel test execution safe

---

#### Task 4.4: Create docker-compose.test.yml
**Effort:** 3 hours
**Files:**
- `docker-compose.test.yml` (update/create)

**Requirements:**
Optimize for test execution:

```yaml
version: '3.8'

services:
  # MySQL Test Database with tmpfs for speed
  mysql-test:
    image: mysql:8.0
    container_name: youtuberag-mysql-test
    environment:
      MYSQL_ROOT_PASSWORD: test_password
      MYSQL_DATABASE: test_db
      MYSQL_USER: test_user
      MYSQL_PASSWORD: test_password
    ports:
      - "3306:3306"
    tmpfs:
      - /var/lib/mysql  # In-memory storage for speed
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost", "-u", "root", "-ptest_password"]
      interval: 5s
      timeout: 3s
      retries: 10
      start_period: 20s
    networks:
      - test-network
    command:
      - --default-authentication-plugin=mysql_native_password
      - --character-set-server=utf8mb4
      - --collation-server=utf8mb4_unicode_ci
      - --max-connections=200

  # Redis Test Cache
  redis-test:
    image: redis:7-alpine
    container_name: youtuberag-redis-test
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 10
    networks:
      - test-network
    command: redis-server --appendonly no --maxmemory 256mb

  # Test Runner (optional - for containerized testing)
  test-runner:
    build:
      context: .
      dockerfile: Dockerfile
      target: test  # You may need to add this stage to Dockerfile
    container_name: youtuberag-test-runner
    environment:
      ASPNETCORE_ENVIRONMENT: Testing
      ConnectionStrings__DefaultConnection: "Server=mysql-test;Port=3306;Database=test_db;User=root;Password=test_password;AllowPublicKeyRetrieval=True;"
      ConnectionStrings__Redis: "redis-test:6379"
      JwtSettings__SecretKey: "TestSecretKeyForJWTTokenGenerationMinimum256Bits!"
      Processing__TempFilePath: "/tmp/youtuberag"
      Whisper__ModelsPath: "/tmp/whisper-models"
    depends_on:
      mysql-test:
        condition: service_healthy
      redis-test:
        condition: service_healthy
    networks:
      - test-network
    volumes:
      - ./TestResults:/test-results
    profiles:
      - test-runner
    command: >
      dotnet test --configuration Release
        --logger "trx;LogFileName=test-results.trx"
        --logger "console;verbosity=detailed"
        --collect:"XPlat Code Coverage"
        --results-directory /test-results

networks:
  test-network:
    driver: bridge
```

**Validation:**
- [ ] MySQL starts with tmpfs (fast)
- [ ] Redis configured for testing
- [ ] Health checks work
- [ ] Services start in < 30 seconds
- [ ] Tests can connect to services

---

#### Task 4.5: Update CI Workflow
**Effort:** 2 hours
**Files:**
- `.github/workflows/ci.yml` (modify)

**Requirements:**
Simplify CI to use docker-compose.test.yml:

```yaml
# .github/workflows/ci.yml
jobs:
  build-and-test:
    name: Build and Test
    runs-on: ubuntu-latest

    steps:
      - name: Checkout Code
        uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Start Test Services
        run: |
          docker-compose -f docker-compose.test.yml up -d mysql-test redis-test

      - name: Wait for Services
        run: |
          timeout 60 bash -c 'until docker-compose -f docker-compose.test.yml exec -T mysql-test mysqladmin ping -h localhost -u root -ptest_password; do sleep 2; done'
          echo "✅ MySQL ready"
          timeout 30 bash -c 'until docker-compose -f docker-compose.test.yml exec -T redis-test redis-cli ping; do sleep 2; done'
          echo "✅ Redis ready"

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore Dependencies
        run: dotnet restore YoutubeRag.sln

      - name: Build Solution
        run: dotnet build YoutubeRag.sln --configuration Release --no-restore

      - name: Run Tests
        env:
          ConnectionStrings__DefaultConnection: "Server=localhost;Port=3306;Database=test_db;User=root;Password=test_password;AllowPublicKeyRetrieval=True;"
          ConnectionStrings__Redis: "localhost:6379"
          JwtSettings__SecretKey: "TestSecretKeyForJWTTokenGenerationMinimum256Bits!"
          ASPNETCORE_ENVIRONMENT: Testing
        run: |
          dotnet test YoutubeRag.sln \
            --configuration Release \
            --no-build \
            --logger "trx;LogFileName=test-results.trx" \
            --logger "console;verbosity=detailed" \
            --collect:"XPlat Code Coverage" \
            --results-directory ./TestResults

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: TestResults/**/*.trx

      - name: Cleanup
        if: always()
        run: |
          docker-compose -f docker-compose.test.yml down -v
```

**Validation:**
- [ ] CI uses same docker-compose.test.yml as developers
- [ ] Environment variables match local setup
- [ ] Tests run successfully
- [ ] Cleanup always runs

---

#### Task 4.6: Test Execution Documentation
**Effort:** 2 hours
**Files:**
- `docs/TESTING_GUIDE.md` (new)

**Requirements:**
Document:
1. How to run tests locally (with Docker)
2. How to run tests in CI (automatic)
3. Test database strategy
4. Test isolation approach
5. Debugging failing tests
6. Performance considerations
7. Troubleshooting common test issues

**Validation:**
- [ ] Clear instructions for developers
- [ ] Examples included
- [ ] Troubleshooting section comprehensive

---

**Epic 4 Acceptance Criteria:**
- [ ] CustomWebApplicationFactory uses real MySQL
- [ ] TestDatabaseManager creates/drops databases
- [ ] IntegrationTestFixture working
- [ ] docker-compose.test.yml optimized for tests
- [ ] CI workflow simplified and using docker-compose
- [ ] Local test execution matches CI exactly
- [ ] Test execution time < 5 minutes
- [ ] 100% test parity (local = CI results)
- [ ] Zero environment-specific failures

---

## Summary Checklist

### Overall Project Success Criteria

#### Environment Consistency
- [ ] 100% test parity: local test results = CI test results
- [ ] Zero hard-coded Windows paths in codebase
- [ ] All services containerized
- [ ] Configuration consolidated and validated

#### Developer Experience
- [ ] New developer setup < 15 minutes
- [ ] One-command environment start
- [ ] Hot reload working in containers
- [ ] Comprehensive documentation

#### Configuration Management
- [ ] Single source of truth for each setting
- [ ] Clear override precedence
- [ ] All secrets externalized
- [ ] Configuration validated on startup

#### CI/CD
- [ ] CI uses same docker-compose.test.yml as developers
- [ ] No configuration drift
- [ ] Build time < 10 minutes
- [ ] Test execution < 5 minutes

### Documentation Deliverables
- [ ] DOCKER_DEVELOPMENT_GUIDE.md
- [ ] CONFIGURATION_GUIDE.md
- [ ] TESTING_GUIDE.md
- [ ] SECRETS_MANAGEMENT.md
- [ ] Updated README.md

### Code Deliverables
- [ ] docker-compose.dev.yml
- [ ] docker-compose.test.yml (updated)
- [ ] .devcontainer/devcontainer.json
- [ ] IPathProvider + implementation
- [ ] ConfigurationValidator
- [ ] TestDatabaseManager
- [ ] IntegrationTestFixture
- [ ] Updated services (TempFileManagement, WhisperModelDownload, VideoDownload)

---

## Post-Implementation

### Week 3-4: Adoption and Optimization

1. **Team Training (4 hours)**
   - Docker basics
   - VS Code Dev Containers
   - Configuration management
   - Testing workflow

2. **Monitoring (ongoing)**
   - Track setup time for new developers
   - Monitor test execution time
   - Collect developer feedback
   - Track environment-specific issues (target: 0)

3. **Optimization (as needed)**
   - Docker build time optimization
   - Test execution speed improvements
   - Documentation improvements based on feedback

4. **Retrospective (end of Week 4)**
   - What worked well?
   - What could be improved?
   - Action items for future
   - Lessons learned

---

## Contact and Support

**Questions during implementation:**
- Create GitHub issue tagged with `environment-consistency`
- Tag assignee in issue or PR
- Slack channel: #environment-consistency (if available)

**Blockers:**
- Escalate to Technical Lead
- Daily standup discussion
- Document blocker and workaround

---

**Document Version:** 1.0
**Last Updated:** 2025-10-10
**Status:** Ready for Development
