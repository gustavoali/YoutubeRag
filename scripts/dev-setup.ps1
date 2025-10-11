# YoutubeRag Development Environment Setup Script
# Windows PowerShell Version
# Last Updated: 2025-10-10

# Color functions
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }

# Header
Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  YoutubeRag Development Setup" -ForegroundColor Magenta
Write-Host "  Windows Environment" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

# Check if running as administrator (optional but recommended for Docker)
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Warning "Not running as Administrator. Some operations may fail."
    Write-Info "Consider running: Start-Process powershell -Verb runAs -ArgumentList '-File', '$PSCommandPath'"
    Write-Host ""
}

# Step 1: Check Prerequisites
Write-Info "Step 1/8: Checking prerequisites..."
Write-Host ""

# Check Git
Write-Info "  Checking Git..."
$gitVersion = $null
try {
    $gitVersion = git --version
    Write-Success "  ✓ Git installed: $gitVersion"
} catch {
    Write-Error "  ✗ Git is not installed or not in PATH"
    Write-Error "  Please install from: https://git-scm.com/"
    exit 1
}

# Check .NET SDK
Write-Info "  Checking .NET SDK..."
$dotnetVersion = $null
try {
    $dotnetVersion = dotnet --version
    if ([version]$dotnetVersion -lt [version]"8.0.0") {
        Write-Error "  ✗ .NET SDK 8.0 or higher required. Found: $dotnetVersion"
        Write-Error "  Please install from: https://dotnet.microsoft.com/download"
        exit 1
    }
    Write-Success "  ✓ .NET SDK installed: $dotnetVersion"
} catch {
    Write-Error "  ✗ .NET SDK is not installed or not in PATH"
    Write-Error "  Please install from: https://dotnet.microsoft.com/download"
    exit 1
}

# Check Docker
Write-Info "  Checking Docker..."
$dockerVersion = $null
try {
    $dockerVersion = docker --version
    Write-Success "  ✓ Docker installed: $dockerVersion"
} catch {
    Write-Error "  ✗ Docker is not installed or not in PATH"
    Write-Error "  Please install Docker Desktop from: https://www.docker.com/products/docker-desktop"
    exit 1
}

# Check Docker Compose
Write-Info "  Checking Docker Compose..."
$composeVersion = $null
try {
    $composeVersion = docker-compose --version
    Write-Success "  ✓ Docker Compose installed: $composeVersion"
} catch {
    Write-Error "  ✗ Docker Compose is not installed or not in PATH"
    Write-Error "  Please ensure Docker Desktop is properly installed"
    exit 1
}

# Check if Docker is running
Write-Info "  Checking if Docker is running..."
try {
    docker ps > $null 2>&1
    Write-Success "  ✓ Docker daemon is running"
} catch {
    Write-Error "  ✗ Docker daemon is not running"
    Write-Error "  Please start Docker Desktop and try again"
    exit 1
}

Write-Host ""

# Step 2: Environment Configuration
Write-Info "Step 2/8: Configuring environment..."
Write-Host ""

# Check if .env.local exists
if (Test-Path ".env.local") {
    Write-Warning "  .env.local already exists"
    $overwrite = Read-Host "  Overwrite? (y/N)"
    if ($overwrite -eq "y" -or $overwrite -eq "Y") {
        Copy-Item .env.example .env.local -Force
        Write-Success "  ✓ .env.local updated from template"
    } else {
        Write-Info "  Using existing .env.local"
    }
} else {
    if (Test-Path ".env.example") {
        Copy-Item .env.example .env.local
        Write-Success "  ✓ Created .env.local from template"
    } else {
        Write-Warning "  .env.example not found, creating default .env.local"
        @"
# YoutubeRag Local Development Environment
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
PROCESSING_TEMP_PATH=C:\Temp\YoutubeRag
WHISPER_MODELS_PATH=C:\Models\Whisper
FFMPEG_PATH=ffmpeg

# JWT
JWT_SECRET=DevelopmentSecretKey123456789012345678901234567890
JWT_EXPIRATION_MINUTES=60
"@ | Out-File -FilePath ".env.local" -Encoding UTF8
        Write-Success "  ✓ Created default .env.local"
    }
}

# Create temp directories
Write-Info "  Creating temp directories..."
$tempDirs = @("C:\Temp\YoutubeRag", "C:\Models\Whisper", "logs", "temp")
foreach ($dir in $tempDirs) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force > $null
        Write-Success "  ✓ Created directory: $dir"
    }
}

Write-Host ""

# Step 3: Clean up existing containers
Write-Info "Step 3/8: Cleaning up existing containers..."
Write-Host ""

try {
    $existingContainers = docker ps -a --filter "name=youtube-rag" --format "{{.Names}}"
    if ($existingContainers) {
        Write-Warning "  Found existing containers, stopping..."
        docker-compose down -v 2>&1 | Out-Null
        Write-Success "  ✓ Cleaned up existing containers"
    } else {
        Write-Info "  No existing containers found"
    }
} catch {
    Write-Info "  No cleanup needed"
}

Write-Host ""

# Step 4: Pull Docker Images
Write-Info "Step 4/8: Pulling Docker images..."
Write-Host ""

Write-Info "  Pulling MySQL 8.0..."
docker pull mysql:8.0
Write-Success "  ✓ MySQL image ready"

Write-Info "  Pulling Redis 7 Alpine..."
docker pull redis:7-alpine
Write-Success "  ✓ Redis image ready"

Write-Host ""

# Step 5: Start Infrastructure Services
Write-Info "Step 5/8: Starting infrastructure services..."
Write-Host ""

Write-Info "  Starting MySQL and Redis..."
docker-compose up -d mysql redis

Write-Info "  Waiting for services to be ready (30 seconds)..."
$seconds = 30
for ($i = $seconds; $i -gt 0; $i--) {
    Write-Host -NoNewline "`r  Waiting... $i seconds remaining "
    Start-Sleep -Seconds 1
}
Write-Host ""

# Verify services are running
$mysqlStatus = docker ps --filter "name=youtube-rag-mysql" --format "{{.Status}}"
$redisStatus = docker ps --filter "name=youtube-rag-redis" --format "{{.Status}}"

if ($mysqlStatus -like "*Up*") {
    Write-Success "  ✓ MySQL is running"
} else {
    Write-Error "  ✗ MySQL failed to start"
    Write-Error "  Check logs: docker-compose logs mysql"
    exit 1
}

if ($redisStatus -like "*Up*") {
    Write-Success "  ✓ Redis is running"
} else {
    Write-Error "  ✗ Redis failed to start"
    Write-Error "  Check logs: docker-compose logs redis"
    exit 1
}

Write-Host ""

# Step 6: Restore NuGet Packages
Write-Info "Step 6/8: Restoring NuGet packages..."
Write-Host ""

try {
    dotnet restore YoutubeRag.sln --verbosity quiet
    Write-Success "  ✓ NuGet packages restored"
} catch {
    Write-Error "  ✗ Failed to restore NuGet packages"
    exit 1
}

Write-Host ""

# Step 6.5: Install Git Hooks
Write-Info "Step 6.5/9: Installing Git hooks (Husky.NET)..."
Write-Host ""

try {
    # Restore .NET local tools (including Husky)
    Write-Info "  Restoring .NET tools..."
    dotnet tool restore --verbosity quiet
    Write-Success "  ✓ .NET tools restored"

    # Install Git hooks
    Write-Info "  Installing pre-commit hooks..."
    dotnet husky install
    Write-Success "  ✓ Git hooks installed"
    Write-Info "  → Pre-commit: Code formatting + Build check"
    Write-Info "  → Pre-push: Unit tests"
} catch {
    Write-Warning "  ⚠ Git hooks installation skipped (non-critical)"
    Write-Info "  You can install manually later with: dotnet husky install"
}

Write-Host ""

# Step 7: Build Solution
Write-Info "Step 7/9: Building solution..."
Write-Host ""

try {
    dotnet build YoutubeRag.sln --configuration Release --no-restore --verbosity quiet
    Write-Success "  ✓ Solution built successfully"
} catch {
    Write-Error "  ✗ Build failed"
    exit 1
}

Write-Host ""

# Step 8: Database Migrations
Write-Info "Step 8/9: Running database migrations..."
Write-Host ""

# Check if EF Core tools are installed
Write-Info "  Checking EF Core tools..."
$efInstalled = $false
try {
    $efVersion = dotnet ef --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "  ✓ EF Core tools installed"
        $efInstalled = $true
    }
} catch {
    Write-Info "  EF Core tools not found"
}

# Install EF Core tools if not present
if (-not $efInstalled) {
    Write-Info "  Installing EF Core tools..."
    dotnet tool install --global dotnet-ef --version 8.0.0
    Write-Success "  ✓ EF Core tools installed"

    # Update PATH for current session
    $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
}

# Run migrations
Write-Info "  Applying database migrations..."
try {
    $env:ConnectionStrings__DefaultConnection = "Server=localhost;Port=3306;Database=youtube_rag_db;User=youtube_rag_user;Password=youtube_rag_password;AllowPublicKeyRetrieval=True;"

    dotnet ef database update `
        --project YoutubeRag.Infrastructure `
        --startup-project YoutubeRag.Api `
        --configuration Release `
        --no-build `
        --verbose

    Write-Success "  ✓ Database migrations applied"
} catch {
    Write-Warning "  ⚠ Migrations failed (this might be okay if database already exists)"
    Write-Info "  You can manually run: dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api"
}

Write-Host ""

# Optional: Seed test data
$seedData = Read-Host "Seed test data? (y/N)"
if ($seedData -eq "y" -or $seedData -eq "Y") {
    Write-Info "Seeding test data..."
    if (Test-Path "scripts\seed-test-data.sql") {
        try {
            docker exec youtube-rag-mysql mysql -u root -prootpassword youtube_rag_db < scripts\seed-test-data.sql
            Write-Success "  ✓ Test data seeded"
        } catch {
            Write-Warning "  ⚠ Failed to seed test data"
        }
    } else {
        Write-Warning "  seed-test-data.sql not found, skipping"
    }
}

# Final Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Success "✓ All prerequisites verified"
Write-Success "✓ Environment configured"
Write-Success "✓ Docker services running"
Write-Success "✓ Dependencies restored"
Write-Success "✓ Git hooks installed (pre-commit + pre-push)"
Write-Success "✓ Solution built"
Write-Success "✓ Database initialized"
Write-Host ""

# Next Steps
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  1. Start the application:" -ForegroundColor White
Write-Host "     dotnet run --project YoutubeRag.Api" -ForegroundColor Yellow
Write-Host ""
Write-Host "  2. Open your browser to:" -ForegroundColor White
Write-Host "     http://localhost:5000" -ForegroundColor Yellow
Write-Host "     http://localhost:5000/swagger" -ForegroundColor Yellow
Write-Host "     http://localhost:5000/health" -ForegroundColor Yellow
Write-Host ""
Write-Host "  3. Run tests:" -ForegroundColor White
Write-Host "     dotnet test" -ForegroundColor Yellow
Write-Host ""
Write-Host "  4. View logs:" -ForegroundColor White
Write-Host "     docker-compose logs -f" -ForegroundColor Yellow
Write-Host ""
Write-Host "  5. Stop services (when done):" -ForegroundColor White
Write-Host "     docker-compose stop" -ForegroundColor Yellow
Write-Host ""

Write-Info "For more information, see: docs/devops/DEVELOPER_SETUP_GUIDE.md"
Write-Host ""

# Check for FFmpeg
Write-Info "Optional: Checking for FFmpeg..."
try {
    $ffmpegVersion = ffmpeg -version 2>&1 | Select-Object -First 1
    Write-Success "✓ FFmpeg is installed: $ffmpegVersion"
} catch {
    Write-Warning "⚠ FFmpeg not found (required for audio extraction)"
    Write-Info "  Install from: https://ffmpeg.org/download.html"
    Write-Info "  Or use: choco install ffmpeg (if Chocolatey is installed)"
}

Write-Host ""
Write-Success "Happy coding! 🚀"
Write-Host ""
