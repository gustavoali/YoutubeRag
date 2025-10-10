# YoutubeRag Local Environment Setup Script
# This script sets up the complete local development environment
# IMPORTANT: Uses Docker in WSL, NOT Docker Desktop

param(
    [switch]$SkipDocker,
    [switch]$SkipBuild,
    [switch]$WithDevTools,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host " YoutubeRag LOCAL Environment Setup" -ForegroundColor Cyan
Write-Host "   Using Docker in WSL (NOT Desktop)" -ForegroundColor Yellow
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if a command exists
function Test-CommandExists {
    param($Command)
    $null = Get-Command $Command -ErrorAction SilentlyContinue
    return $?
}

# Function to check if WSL Docker is available
function Test-WslDockerExists {
    try {
        $result = wsl docker --version 2>&1
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

# Function to wait for a service to be ready
function Wait-ForService {
    param(
        [string]$ServiceName,
        [string]$TestCommand,
        [int]$MaxAttempts = 30,
        [int]$DelaySeconds = 2
    )

    Write-Host "Waiting for $ServiceName to be ready..." -NoNewline
    $attempts = 0

    while ($attempts -lt $MaxAttempts) {
        try {
            Invoke-Expression $TestCommand 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host " Ready!" -ForegroundColor Green
                return $true
            }
        }
        catch {}

        Write-Host "." -NoNewline
        Start-Sleep -Seconds $DelaySeconds
        $attempts++
    }

    Write-Host " Timeout!" -ForegroundColor Red
    return $false
}

# Step 1: Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

$missingPrereqs = @()

# Check for WSL
if (!(Test-CommandExists "wsl")) {
    $missingPrereqs += "WSL2 (Run 'wsl --install' in admin PowerShell)"
}

if (!(Test-CommandExists "dotnet")) {
    $missingPrereqs += ".NET SDK (https://dotnet.microsoft.com/download)"
}

# Check for Docker in WSL (NOT Docker Desktop)
if (!$SkipDocker) {
    if (!(Test-WslDockerExists)) {
        Write-Host "  Docker not found in WSL. Checking if Docker service needs to be started..." -ForegroundColor Yellow

        # Try to start Docker service in WSL
        $startResult = wsl sudo service docker start 2>&1
        Start-Sleep -Seconds 2

        if (!(Test-WslDockerExists)) {
            $missingPrereqs += "Docker in WSL (See REQUERIMIENTOS_SISTEMA.md for installation)"
            Write-Host "  Docker is NOT installed in WSL. Please install Docker inside WSL." -ForegroundColor Red
        } else {
            Write-Host "  Docker service started successfully in WSL!" -ForegroundColor Green
        }
    } else {
        Write-Host "  Docker found in WSL!" -ForegroundColor Green
    }
}

if (!(Test-CommandExists "python") -and !(Test-CommandExists "python3")) {
    $missingPrereqs += "Python 3.x (https://www.python.org/downloads/)"
}

if (!(Test-CommandExists "ffmpeg")) {
    Write-Host "  Warning: FFmpeg not found (optional but recommended)" -ForegroundColor Yellow
    Write-Host "  Install with: choco install ffmpeg" -ForegroundColor Gray
}

if ($missingPrereqs.Count -gt 0) {
    Write-Host "Missing prerequisites:" -ForegroundColor Red
    $missingPrereqs | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "All required prerequisites found!" -ForegroundColor Green
Write-Host ""

# Step 2: Check Python packages
Write-Host "Checking Python packages..." -ForegroundColor Yellow

$pythonCmd = if (Test-CommandExists "python") { "python" } else { "python3" }
$pipCmd = if (Test-CommandExists "pip") { "pip" } else { "pip3" }

$whisperInstalled = & $pipCmd show openai-whisper 2>&1 | Select-String "Version:"
if (!$whisperInstalled) {
    Write-Host "Installing OpenAI Whisper..." -ForegroundColor Yellow
    & $pipCmd install openai-whisper
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install Whisper. Please install manually: pip install openai-whisper" -ForegroundColor Red
        exit 1
    }
}
Write-Host "Whisper is installed!" -ForegroundColor Green
Write-Host ""

# Step 3: Clean up if requested
if ($Clean) {
    Write-Host "Cleaning up existing containers and volumes..." -ForegroundColor Yellow
    wsl docker-compose down -v 2>&1 | Out-Null
    Write-Host "Cleanup complete!" -ForegroundColor Green
    Write-Host ""
}

# Step 4: Start Docker services
if (!$SkipDocker) {
    Write-Host "Starting Docker services in WSL..." -ForegroundColor Yellow

    # Ensure Docker is running in WSL
    $dockerStatus = wsl sudo service docker status 2>&1
    if ($dockerStatus -notmatch "Docker is running" -and $dockerStatus -notmatch "docker is running") {
        Write-Host "Starting Docker service in WSL..." -ForegroundColor Yellow
        wsl sudo service docker start
        Start-Sleep -Seconds 3
    }

    # Verify Docker is now running
    wsl docker info 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Docker is not running in WSL. Please check your WSL Docker installation." -ForegroundColor Red
        Write-Host "Run: wsl sudo service docker start" -ForegroundColor Yellow
        exit 1
    }

    # Start services
    if ($WithDevTools) {
        Write-Host "Starting services with dev tools (Adminer, Redis Commander)..." -ForegroundColor Cyan
        wsl docker-compose --profile dev-tools up -d
    } else {
        wsl docker-compose up -d
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to start Docker services" -ForegroundColor Red
        Write-Host "Trying with explicit docker-compose path..." -ForegroundColor Yellow

        # Try alternative docker-compose command
        if ($WithDevTools) {
            wsl docker compose --profile dev-tools up -d
        } else {
            wsl docker compose up -d
        }

        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to start Docker services. Check that docker-compose is installed in WSL." -ForegroundColor Red
            exit 1
        }
    }

    Write-Host "Docker services started!" -ForegroundColor Green
    Write-Host ""

    # Wait for services
    Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow

    $mysqlReady = Wait-ForService -ServiceName "MySQL" -TestCommand "wsl docker exec youtube-rag-mysql mysqladmin ping -h localhost -u root -prootpassword"
    $redisReady = Wait-ForService -ServiceName "Redis" -TestCommand "wsl docker exec youtube-rag-redis redis-cli ping"

    if (!$mysqlReady -or !$redisReady) {
        Write-Host "Some services failed to start. Check Docker logs with: wsl docker-compose logs" -ForegroundColor Red
        exit 1
    }

    Write-Host "All services are healthy!" -ForegroundColor Green
    Write-Host ""
}

# Step 5: Build the project
if (!$SkipBuild) {
    Write-Host "Building the project..." -ForegroundColor Yellow
    dotnet build --configuration Debug

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }

    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host ""
}

# Step 6: Run database migrations (if EF Core tools are installed)
if (Test-CommandExists "dotnet-ef") {
    Write-Host "Running database migrations..." -ForegroundColor Yellow
    Push-Location "YoutubeRag.Api"
    dotnet ef database update --project ../YoutubeRag.Infrastructure
    Pop-Location

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Migrations applied successfully!" -ForegroundColor Green
    } else {
        Write-Host "Migrations failed (this is okay if EF tools not installed)" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Step 7: Display status and next steps
Write-Host "======================================" -ForegroundColor Green
Write-Host " Setup Complete!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "Services Status:" -ForegroundColor Cyan
Write-Host "  MySQL:    http://localhost:3306" -ForegroundColor White
Write-Host "  Redis:    http://localhost:6379" -ForegroundColor White

if ($WithDevTools) {
    Write-Host "  Adminer:  http://localhost:8080 (MySQL UI)" -ForegroundColor White
    Write-Host "  Redis UI: http://localhost:8081" -ForegroundColor White
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Run the application:" -ForegroundColor White
Write-Host "     dotnet run --project YoutubeRag.Api --environment Local" -ForegroundColor Yellow
Write-Host ""
Write-Host "  2. Access the API:" -ForegroundColor White
Write-Host "     http://localhost:62788/" -ForegroundColor Yellow
Write-Host "     http://localhost:62788/docs (Swagger UI)" -ForegroundColor Yellow
Write-Host ""
Write-Host "  3. Test video processing:" -ForegroundColor White
Write-Host '     curl -X POST http://localhost:62788/api/v1/videos/from-url \' -ForegroundColor Gray
Write-Host '       -H "Content-Type: application/json" \' -ForegroundColor Gray
Write-Host '       -d ''{"url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ", "title": "Test"}''' -ForegroundColor Gray
Write-Host ""
Write-Host "Troubleshooting:" -ForegroundColor Cyan
Write-Host "  - View logs: wsl docker-compose logs -f" -ForegroundColor White
Write-Host "  - Stop services: wsl docker-compose down" -ForegroundColor White
Write-Host "  - Clean everything: .\setup-local.ps1 -Clean" -ForegroundColor White
Write-Host "  - Start Docker in WSL: wsl sudo service docker start" -ForegroundColor White
Write-Host "  - Check Docker status: wsl docker ps" -ForegroundColor White
Write-Host ""
Write-Host "WSL Docker Notes:" -ForegroundColor Yellow
Write-Host "  - Docker is running INSIDE WSL, not Docker Desktop" -ForegroundColor White
Write-Host "  - All docker commands use 'wsl' prefix from Windows" -ForegroundColor White
Write-Host "  - Services are accessible on localhost from Windows" -ForegroundColor White
Write-Host ""