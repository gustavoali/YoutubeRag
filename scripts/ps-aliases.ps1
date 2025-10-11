# PowerShell Aliases for YoutubeRag.NET
# DEVOPS-012: Windows PowerShell aliases mirroring Makefile commands
#
# Installation:
#   1. Run: .\scripts\install-aliases.ps1
#   2. Or manually: . .\scripts\ps-aliases.ps1
#
# Usage:
#   dev              # Start local development
#   test             # Run tests
#   build            # Build solution
#   migrate          # Run migrations
#   ...and 40+ more commands

Write-Host "Loading YoutubeRag.NET PowerShell Aliases..." -ForegroundColor Cyan

# Helper function for colored output
function Write-ColorOutput($Message, $Color = "White") {
    Write-Host $Message -ForegroundColor $Color
}

##############################################################################
# Development Commands
##############################################################################

function dev {
    <#
    .SYNOPSIS
    Start local development (API + infrastructure)
    #>
    Write-ColorOutput "🚀 Starting local development..." "Cyan"
    Write-ColorOutput "📦 Starting infrastructure services..." "Yellow"
    docker-compose up -d mysql redis
    Write-ColorOutput "⏳ Waiting for services to be ready..." "Yellow"
    Start-Sleep -Seconds 15
    Write-ColorOutput "🔄 Running database migrations..." "Yellow"
    dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
    Write-ColorOutput "✅ Starting API..." "Green"
    dotnet run --project YoutubeRag.Api
}

function dev-docker {
    <#
    .SYNOPSIS
    Start development in Docker with hot reload
    #>
    Write-ColorOutput "🐳 Starting development environment in Docker..." "Cyan"
    docker-compose up --build api
    Write-Host ""
    Write-ColorOutput "✅ Development environment ready!" "Green"
    Write-Host "   API:         http://localhost:5000"
    Write-Host "   Swagger:     http://localhost:5000/swagger"
    Write-Host "   Adminer:     http://localhost:8080"
    Write-Host "   Redis UI:    http://localhost:8081"
}

function watch {
    <#
    .SYNOPSIS
    Start API with hot reload (dotnet watch)
    #>
    Write-ColorOutput "👀 Starting API with hot reload..." "Cyan"
    dotnet watch --project YoutubeRag.Api
}

##############################################################################
# Testing Commands
##############################################################################

function test {
    <#
    .SYNOPSIS
    Run all tests locally
    #>
    Write-ColorOutput "🧪 Running all tests..." "Cyan"
    dotnet test --verbosity normal
}

function test-ci {
    <#
    .SYNOPSIS
    Run tests with CI configuration
    #>
    Write-ColorOutput "🧪 Running tests (CI mode)..." "Cyan"
    dotnet test --configuration Release --verbosity normal --logger "trx"
}

function test-docker {
    <#
    .SYNOPSIS
    Run tests in Docker
    #>
    Write-ColorOutput "🐳 Running tests in Docker..." "Cyan"
    docker-compose --profile test up --abort-on-container-exit test-runner
    Write-Host ""
    Write-ColorOutput "✅ Tests complete. Results in .\TestResults" "Green"
}

function test-coverage {
    <#
    .SYNOPSIS
    Run tests with code coverage
    #>
    Write-ColorOutput "📊 Running tests with coverage..." "Cyan"
    dotnet test --collect:"XPlat Code Coverage" --verbosity normal
    Write-Host ""
    Write-ColorOutput "✅ Coverage report generated in TestResults" "Green"
}

function ci {
    <#
    .SYNOPSIS
    Simulate CI pipeline locally
    #>
    build
    test-ci
    Write-ColorOutput "✅ CI pipeline simulation complete!" "Green"
}

##############################################################################
# Building Commands
##############################################################################

function build {
    <#
    .SYNOPSIS
    Build the solution
    #>
    Write-ColorOutput "🔨 Building solution..." "Cyan"
    dotnet build --configuration Release
}

function build-docker {
    <#
    .SYNOPSIS
    Build Docker image
    #>
    Write-ColorOutput "🐳 Building Docker image..." "Cyan"
    docker-compose build api
}

function restore {
    <#
    .SYNOPSIS
    Restore NuGet packages
    #>
    Write-ColorOutput "📦 Restoring NuGet packages..." "Cyan"
    dotnet restore
}

function clean {
    <#
    .SYNOPSIS
    Clean build artifacts
    #>
    Write-ColorOutput "🧹 Cleaning build artifacts..." "Cyan"
    dotnet clean
    Get-ChildItem -Path . -Include bin,obj -Recurse -Force | Remove-Item -Recurse -Force
    Write-ColorOutput "✅ Clean complete" "Green"
}

function rebuild {
    <#
    .SYNOPSIS
    Clean, restore, and rebuild
    #>
    clean
    restore
    build
    Write-ColorOutput "✅ Rebuild complete" "Green"
}

##############################################################################
# Database Commands
##############################################################################

function migrate {
    <#
    .SYNOPSIS
    Run database migrations
    #>
    Write-ColorOutput "🔄 Running database migrations..." "Cyan"
    docker-compose up -d mysql
    Start-Sleep -Seconds 10
    dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
    Write-ColorOutput "✅ Migrations applied" "Green"
}

function migrate-docker {
    <#
    .SYNOPSIS
    Run migrations in Docker
    #>
    Write-ColorOutput "🐳 Running migrations in Docker..." "Cyan"
    docker-compose --profile migration up migration
    Write-ColorOutput "✅ Migrations complete" "Green"
}

function seed {
    <#
    .SYNOPSIS
    Seed database with test data
    #>
    Write-ColorOutput "🌱 Seeding database..." "Cyan"
    if (Test-Path ".\scripts\seed-database.ps1") {
        .\scripts\seed-database.ps1
    } else {
        Write-ColorOutput "❌ seed-database.ps1 not found" "Red"
    }
}

function migration-add {
    <#
    .SYNOPSIS
    Create new migration
    .PARAMETER Name
    Migration name
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$Name
    )
    Write-ColorOutput "📝 Creating migration: $Name" "Cyan"
    dotnet ef migrations add $Name --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
    Write-ColorOutput "✅ Migration created" "Green"
}

function migration-remove {
    <#
    .SYNOPSIS
    Remove last migration
    #>
    Write-ColorOutput "🗑️  Removing last migration..." "Cyan"
    dotnet ef migrations remove --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
    Write-ColorOutput "✅ Migration removed" "Green"
}

function migration-list {
    <#
    .SYNOPSIS
    List all migrations
    #>
    Write-ColorOutput "📋 Listing migrations..." "Cyan"
    dotnet ef migrations list --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
}

function db-reset {
    <#
    .SYNOPSIS
    Reset database (WARNING: deletes all data)
    #>
    Write-ColorOutput "⚠️  WARNING: This will delete ALL data!" "Yellow"
    $confirmation = Read-Host "Are you sure? [y/N]"
    if ($confirmation -eq 'y' -or $confirmation -eq 'Y') {
        Write-ColorOutput "🗑️  Dropping database..." "Yellow"
        docker-compose down -v
        docker-compose up -d mysql
        Start-Sleep -Seconds 15
        migrate
        Write-ColorOutput "✅ Database reset complete" "Green"
    } else {
        Write-ColorOutput "❌ Cancelled" "Red"
    }
}

##############################################################################
# Docker Commands
##############################################################################

function up {
    <#
    .SYNOPSIS
    Start all services
    #>
    Write-ColorOutput "🚀 Starting all services..." "Cyan"
    docker-compose up -d
    Write-ColorOutput "✅ Services started" "Green"
    ps
}

function down {
    <#
    .SYNOPSIS
    Stop all services
    #>
    Write-ColorOutput "🛑 Stopping all services..." "Cyan"
    docker-compose down
    Write-ColorOutput "✅ Services stopped" "Green"
}

function down-v {
    <#
    .SYNOPSIS
    Stop all services and remove volumes
    #>
    Write-ColorOutput "⚠️  Stopping services and removing volumes..." "Yellow"
    docker-compose down -v
    Write-ColorOutput "✅ Services stopped and volumes removed" "Green"
}

function restart {
    <#
    .SYNOPSIS
    Restart all services
    #>
    Write-ColorOutput "🔄 Restarting services..." "Cyan"
    docker-compose restart
    Write-ColorOutput "✅ Services restarted" "Green"
}

function ps {
    <#
    .SYNOPSIS
    List running containers
    #>
    docker-compose ps
}

function logs {
    <#
    .SYNOPSIS
    Show logs for all services
    .PARAMETER Service
    Specific service name (optional)
    #>
    param([string]$Service)
    if ($Service) {
        docker-compose logs -f $Service
    } else {
        docker-compose logs -f
    }
}

function logs-api { docker-compose logs -f api }
function logs-mysql { docker-compose logs -f mysql }
function logs-redis { docker-compose logs -f redis }

##############################################################################
# Code Quality Commands
##############################################################################

function format {
    <#
    .SYNOPSIS
    Format code
    #>
    Write-ColorOutput "✨ Formatting code..." "Cyan"
    dotnet format
    Write-ColorOutput "✅ Code formatted" "Green"
}

function format-check {
    <#
    .SYNOPSIS
    Check code formatting (CI mode)
    #>
    Write-ColorOutput "🔍 Checking code formatting..." "Cyan"
    dotnet format --verify-no-changes
}

function lint {
    <#
    .SYNOPSIS
    Run code analyzers
    #>
    Write-ColorOutput "🔍 Running code analyzers..." "Cyan"
    dotnet build /p:RunAnalyzers=true
    Write-ColorOutput "✅ Analysis complete" "Green"
}

function check {
    <#
    .SYNOPSIS
    Run all quality checks
    #>
    format-check
    lint
    test-ci
    Write-ColorOutput "✅ All quality checks passed" "Green"
}

##############################################################################
# Utility Commands
##############################################################################

function status {
    <#
    .SYNOPSIS
    Show project status
    #>
    Write-ColorOutput "📊 Project Status" "Cyan"
    Write-ColorOutput "=================" "Cyan"
    Write-Host ""
    Write-Host "Docker Containers:"
    docker-compose ps
    Write-Host ""
    Write-Host "Git Status:"
    git status --short
    Write-Host ""
    Write-Host "Latest Commits:"
    git log --oneline -5
    Write-Host ""
}

function env {
    <#
    .SYNOPSIS
    Show environment info
    #>
    Write-ColorOutput "🌍 Environment Information" "Cyan"
    Write-ColorOutput "==========================" "Cyan"
    Write-Host "Docker version:   $(docker --version)"
    Write-Host ".NET version:     $(dotnet --version)"
    Write-Host "Git version:      $(git --version)"
    Write-Host "PowerShell:       $($PSVersionTable.PSVersion)"
    Write-Host "Working dir:      $(Get-Location)"
    Write-Host "Branch:           $(git branch --show-current)"
    Write-Host ""
}

function install-tools {
    <#
    .SYNOPSIS
    Install required .NET tools
    #>
    Write-ColorOutput "🔧 Installing .NET tools..." "Cyan"
    dotnet tool install --global dotnet-ef
    dotnet tool install --global dotnet-format
    dotnet tool install --global dotnet-reportgenerator-globaltool
    Write-ColorOutput "✅ Tools installed" "Green"
}

function setup {
    <#
    .SYNOPSIS
    Complete first-time setup
    #>
    install-tools
    restore
    build
    Write-ColorOutput "🎉 Setup complete!" "Green"
    Write-Host ""
    Write-Host "Next steps:"
    Write-Host "  1. Copy .env.template to .env and configure"
    Write-Host "  2. Run 'dev' to start development"
    Write-Host ""
}

function health {
    <#
    .SYNOPSIS
    Check service health
    #>
    Write-ColorOutput "🏥 Checking service health..." "Cyan"
    Write-Host ""
    Write-Host "MySQL:"
    docker-compose exec mysql mysqladmin ping -h localhost
    Write-Host ""
    Write-Host "Redis:"
    docker-compose exec redis redis-cli ping
    Write-Host ""
    Write-Host "API:"
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing
        Write-ColorOutput "✅ API is healthy" "Green"
    } catch {
        Write-ColorOutput "❌ API not responding" "Red"
    }
    Write-Host ""
}

##############################################################################
# Monitoring Commands
##############################################################################

function dev-tools {
    <#
    .SYNOPSIS
    Start development tools (Adminer, Redis Commander)
    #>
    Write-ColorOutput "🛠️  Starting development tools..." "Cyan"
    docker-compose --profile dev-tools up -d
    Write-Host ""
    Write-ColorOutput "✅ Dev tools started:" "Green"
    Write-Host "   Adminer (MySQL):  http://localhost:8080"
    Write-Host "   Redis Commander:  http://localhost:8081"
}

function monitoring {
    <#
    .SYNOPSIS
    Start monitoring stack (Prometheus, Grafana)
    #>
    Write-ColorOutput "📊 Starting monitoring stack..." "Cyan"
    docker-compose --profile monitoring up -d
    Write-Host ""
    Write-ColorOutput "✅ Monitoring started:" "Green"
    Write-Host "   Prometheus:  http://localhost:9090"
    Write-Host "   Grafana:     http://localhost:3001 (admin/admin)"
}

##############################################################################
# Cleanup Commands
##############################################################################

function prune {
    <#
    .SYNOPSIS
    Remove unused Docker resources
    #>
    Write-ColorOutput "🧹 Pruning unused Docker resources..." "Cyan"
    docker system prune -af --volumes
    Write-ColorOutput "✅ Cleanup complete" "Green"
}

function clean-all {
    <#
    .SYNOPSIS
    Complete cleanup (WARNING: removes all data)
    #>
    down-v
    prune
    clean
    Write-ColorOutput "✅ Complete cleanup finished" "Green"
}

##############################################################################
# Release Commands
##############################################################################

function version {
    <#
    .SYNOPSIS
    Show current version
    #>
    $ver = git describe --tags --always
    Write-ColorOutput "📌 Current version: $ver" "Cyan"
}

function tag {
    <#
    .SYNOPSIS
    Create new version tag
    .PARAMETER Version
    Version tag (e.g., v1.0.0)
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$Version
    )
    Write-ColorOutput "🏷️  Creating tag: $Version" "Cyan"
    git tag -a $Version -m "Release $Version"
    git push origin $Version
    Write-ColorOutput "✅ Tag created and pushed" "Green"
}

##############################################################################
# Help Command
##############################################################################

function help {
    <#
    .SYNOPSIS
    Display all available commands
    #>
    Write-ColorOutput "YoutubeRag.NET - PowerShell Commands" "Cyan"
    Write-ColorOutput "====================================" "Cyan"
    Write-Host ""
    Write-Host "Development:"
    Write-Host "  dev, dev-docker, watch"
    Write-Host ""
    Write-Host "Testing:"
    Write-Host "  test, test-ci, test-docker, test-coverage, ci"
    Write-Host ""
    Write-Host "Building:"
    Write-Host "  build, build-docker, restore, clean, rebuild"
    Write-Host ""
    Write-Host "Database:"
    Write-Host "  migrate, migrate-docker, seed"
    Write-Host "  migration-add -Name <name>, migration-remove, migration-list"
    Write-Host "  db-reset"
    Write-Host ""
    Write-Host "Docker:"
    Write-Host "  up, down, down-v, restart, ps"
    Write-Host "  logs [-Service <name>], logs-api, logs-mysql, logs-redis"
    Write-Host ""
    Write-Host "Code Quality:"
    Write-Host "  format, format-check, lint, check"
    Write-Host ""
    Write-Host "Utilities:"
    Write-Host "  status, env, install-tools, setup, health"
    Write-Host ""
    Write-Host "Monitoring:"
    Write-Host "  dev-tools, monitoring"
    Write-Host ""
    Write-Host "Cleanup:"
    Write-Host "  prune, clean-all"
    Write-Host ""
    Write-Host "Release:"
    Write-Host "  version, tag -Version <version>"
    Write-Host ""
    Write-Host "For detailed help on any command:"
    Write-Host "  Get-Help <command> -Detailed"
    Write-Host ""
}

# Display welcome message
Write-ColorOutput "✅ YoutubeRag.NET aliases loaded!" "Green"
Write-Host "   Type 'help' to see all available commands"
Write-Host ""
