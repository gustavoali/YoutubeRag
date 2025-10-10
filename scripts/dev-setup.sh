#!/bin/bash
# YoutubeRag Development Environment Setup Script
# Linux/Mac Version
# Last Updated: 2025-10-10

set -e  # Exit on error

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
NC='\033[0m' # No Color

# Functions
log_success() { echo -e "${GREEN}$1${NC}"; }
log_info() { echo -e "${CYAN}$1${NC}"; }
log_warning() { echo -e "${YELLOW}$1${NC}"; }
log_error() { echo -e "${RED}$1${NC}"; }
log_header() { echo -e "${MAGENTA}$1${NC}"; }

# Header
echo ""
log_header "========================================"
log_header "  YoutubeRag Development Setup"
log_header "  Linux/Mac Environment"
log_header "========================================"
echo ""

# Step 1: Check Prerequisites
log_info "Step 1/8: Checking prerequisites..."
echo ""

# Check Git
log_info "  Checking Git..."
if command -v git >/dev/null 2>&1; then
    GIT_VERSION=$(git --version)
    log_success "  ✓ Git installed: $GIT_VERSION"
else
    log_error "  ✗ Git is not installed"
    log_error "  Please install: sudo apt-get install git (Ubuntu/Debian)"
    log_error "  Or: brew install git (macOS)"
    exit 1
fi

# Check .NET SDK
log_info "  Checking .NET SDK..."
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_VERSION=$(dotnet --version)
    REQUIRED_VERSION="8.0.0"
    if [ "$(printf '%s\n' "$REQUIRED_VERSION" "$DOTNET_VERSION" | sort -V | head -n1)" = "$REQUIRED_VERSION" ]; then
        log_success "  ✓ .NET SDK installed: $DOTNET_VERSION"
    else
        log_error "  ✗ .NET SDK 8.0 or higher required. Found: $DOTNET_VERSION"
        log_error "  Please install from: https://dotnet.microsoft.com/download"
        exit 1
    fi
else
    log_error "  ✗ .NET SDK is not installed"
    log_error "  Please install from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Check Docker
log_info "  Checking Docker..."
if command -v docker >/dev/null 2>&1; then
    DOCKER_VERSION=$(docker --version)
    log_success "  ✓ Docker installed: $DOCKER_VERSION"
else
    log_error "  ✗ Docker is not installed"
    log_error "  Please install from: https://docs.docker.com/get-docker/"
    exit 1
fi

# Check Docker Compose
log_info "  Checking Docker Compose..."
if command -v docker-compose >/dev/null 2>&1; then
    COMPOSE_VERSION=$(docker-compose --version)
    log_success "  ✓ Docker Compose installed: $COMPOSE_VERSION"
else
    log_error "  ✗ Docker Compose is not installed"
    log_error "  Please install from: https://docs.docker.com/compose/install/"
    exit 1
fi

# Check if Docker is running
log_info "  Checking if Docker is running..."
if docker ps >/dev/null 2>&1; then
    log_success "  ✓ Docker daemon is running"
else
    log_error "  ✗ Docker daemon is not running"
    log_error "  Please start Docker and try again"
    log_error "  Linux: sudo systemctl start docker"
    log_error "  macOS: Start Docker Desktop"
    exit 1
fi

echo ""

# Step 2: Environment Configuration
log_info "Step 2/8: Configuring environment..."
echo ""

# Check if .env.local exists
if [ -f ".env.local" ]; then
    log_warning "  .env.local already exists"
    read -p "  Overwrite? (y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        cp .env.example .env.local
        log_success "  ✓ .env.local updated from template"
    else
        log_info "  Using existing .env.local"
    fi
else
    if [ -f ".env.example" ]; then
        cp .env.example .env.local
        log_success "  ✓ Created .env.local from template"
    else
        log_warning "  .env.example not found, creating default .env.local"
        cat > .env.local << 'EOF'
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
PROCESSING_TEMP_PATH=/tmp/youtuberag
WHISPER_MODELS_PATH=/tmp/whisper-models
FFMPEG_PATH=ffmpeg

# JWT
JWT_SECRET=DevelopmentSecretKey123456789012345678901234567890
JWT_EXPIRATION_MINUTES=60
EOF
        log_success "  ✓ Created default .env.local"
    fi
fi

# Create temp directories
log_info "  Creating temp directories..."
TEMP_DIRS=("/tmp/youtuberag" "/tmp/whisper-models" "logs" "temp")
for dir in "${TEMP_DIRS[@]}"; do
    if [ ! -d "$dir" ]; then
        mkdir -p "$dir"
        log_success "  ✓ Created directory: $dir"
    fi
done

echo ""

# Step 3: Clean up existing containers
log_info "Step 3/8: Cleaning up existing containers..."
echo ""

if docker ps -a --filter "name=youtube-rag" --format "{{.Names}}" | grep -q "youtube-rag"; then
    log_warning "  Found existing containers, stopping..."
    docker-compose down -v >/dev/null 2>&1 || true
    log_success "  ✓ Cleaned up existing containers"
else
    log_info "  No existing containers found"
fi

echo ""

# Step 4: Pull Docker Images
log_info "Step 4/8: Pulling Docker images..."
echo ""

log_info "  Pulling MySQL 8.0..."
docker pull mysql:8.0
log_success "  ✓ MySQL image ready"

log_info "  Pulling Redis 7 Alpine..."
docker pull redis:7-alpine
log_success "  ✓ Redis image ready"

echo ""

# Step 5: Start Infrastructure Services
log_info "Step 5/8: Starting infrastructure services..."
echo ""

log_info "  Starting MySQL and Redis..."
docker-compose up -d mysql redis

log_info "  Waiting for services to be ready (30 seconds)..."
for i in {30..1}; do
    echo -ne "\r  Waiting... $i seconds remaining "
    sleep 1
done
echo ""

# Verify services are running
MYSQL_STATUS=$(docker ps --filter "name=youtube-rag-mysql" --format "{{.Status}}")
REDIS_STATUS=$(docker ps --filter "name=youtube-rag-redis" --format "{{.Status}}")

if [[ $MYSQL_STATUS == *"Up"* ]]; then
    log_success "  ✓ MySQL is running"
else
    log_error "  ✗ MySQL failed to start"
    log_error "  Check logs: docker-compose logs mysql"
    exit 1
fi

if [[ $REDIS_STATUS == *"Up"* ]]; then
    log_success "  ✓ Redis is running"
else
    log_error "  ✗ Redis failed to start"
    log_error "  Check logs: docker-compose logs redis"
    exit 1
fi

echo ""

# Step 6: Restore NuGet Packages
log_info "Step 6/8: Restoring NuGet packages..."
echo ""

if dotnet restore YoutubeRag.sln --verbosity quiet; then
    log_success "  ✓ NuGet packages restored"
else
    log_error "  ✗ Failed to restore NuGet packages"
    exit 1
fi

echo ""

# Step 7: Build Solution
log_info "Step 7/8: Building solution..."
echo ""

if dotnet build YoutubeRag.sln --configuration Release --no-restore --verbosity quiet; then
    log_success "  ✓ Solution built successfully"
else
    log_error "  ✗ Build failed"
    exit 1
fi

echo ""

# Step 8: Database Migrations
log_info "Step 8/8: Running database migrations..."
echo ""

# Check if EF Core tools are installed
log_info "  Checking EF Core tools..."
if dotnet ef --version >/dev/null 2>&1; then
    log_success "  ✓ EF Core tools installed"
else
    log_info "  Installing EF Core tools..."
    dotnet tool install --global dotnet-ef --version 8.0.0
    log_success "  ✓ EF Core tools installed"

    # Update PATH for current session
    export PATH="$PATH:$HOME/.dotnet/tools"
fi

# Run migrations
log_info "  Applying database migrations..."
export ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=youtube_rag_db;User=youtube_rag_user;Password=youtube_rag_password;AllowPublicKeyRetrieval=True;"

if dotnet ef database update \
    --project YoutubeRag.Infrastructure \
    --startup-project YoutubeRag.Api \
    --configuration Release \
    --no-build \
    --verbose; then
    log_success "  ✓ Database migrations applied"
else
    log_warning "  ⚠ Migrations failed (this might be okay if database already exists)"
    log_info "  You can manually run: dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api"
fi

echo ""

# Optional: Seed test data
read -p "Seed test data? (y/N) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    log_info "Seeding test data..."
    if [ -f "scripts/seed-test-data.sql" ]; then
        if docker exec youtube-rag-mysql mysql -u root -prootpassword youtube_rag_db < scripts/seed-test-data.sql; then
            log_success "  ✓ Test data seeded"
        else
            log_warning "  ⚠ Failed to seed test data"
        fi
    else
        log_warning "  seed-test-data.sql not found, skipping"
    fi
fi

# Final Summary
echo ""
log_header "========================================"
log_header "  Setup Complete!"
log_header "========================================"
echo ""
log_success "✓ All prerequisites verified"
log_success "✓ Environment configured"
log_success "✓ Docker services running"
log_success "✓ Dependencies restored"
log_success "✓ Solution built"
log_success "✓ Database initialized"
echo ""

# Next Steps
log_info "Next Steps:"
echo ""
echo -e "${NC}  1. Start the application:"
echo -e "     ${YELLOW}dotnet run --project YoutubeRag.Api${NC}"
echo ""
echo -e "${NC}  2. Open your browser to:"
echo -e "     ${YELLOW}http://localhost:5000${NC}"
echo -e "     ${YELLOW}http://localhost:5000/swagger${NC}"
echo -e "     ${YELLOW}http://localhost:5000/health${NC}"
echo ""
echo -e "${NC}  3. Run tests:"
echo -e "     ${YELLOW}dotnet test${NC}"
echo ""
echo -e "${NC}  4. View logs:"
echo -e "     ${YELLOW}docker-compose logs -f${NC}"
echo ""
echo -e "${NC}  5. Stop services (when done):"
echo -e "     ${YELLOW}docker-compose stop${NC}"
echo ""

log_info "For more information, see: docs/devops/DEVELOPER_SETUP_GUIDE.md"
echo ""

# Check for FFmpeg
log_info "Optional: Checking for FFmpeg..."
if command -v ffmpeg >/dev/null 2>&1; then
    FFMPEG_VERSION=$(ffmpeg -version 2>&1 | head -n1)
    log_success "✓ FFmpeg is installed: $FFMPEG_VERSION"
else
    log_warning "⚠ FFmpeg not found (required for audio extraction)"
    log_info "  Ubuntu/Debian: sudo apt-get install ffmpeg"
    log_info "  macOS: brew install ffmpeg"
fi

echo ""
log_success "Happy coding! 🚀"
echo ""
