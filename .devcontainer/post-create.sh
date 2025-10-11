#!/bin/bash
# Post-create script for YoutubeRag.NET Dev Container
# This script runs after the container is created

set -e

echo "🚀 Running post-create setup..."
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${CYAN}ℹ️  $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

# 1. Restore .NET packages
log_info "Restoring NuGet packages..."
if dotnet restore YoutubeRag.sln --verbosity quiet; then
    log_success "NuGet packages restored"
else
    log_warning "Failed to restore packages (non-critical)"
fi

echo ""

# 2. Install .NET local tools (Husky)
log_info "Restoring .NET local tools..."
if dotnet tool restore --verbosity quiet; then
    log_success ".NET tools restored"
else
    log_warning "Failed to restore tools (non-critical)"
fi

echo ""

# 3. Install Git hooks
log_info "Installing Git hooks (Husky)..."
if dotnet husky install 2>/dev/null; then
    log_success "Git hooks installed"
else
    log_warning "Git hooks not installed (will retry on first commit)"
fi

echo ""

# 4. Wait for database to be ready
log_info "Waiting for MySQL to be ready..."
max_attempts=30
attempt=0

while ! mysqladmin ping -h localhost --silent 2>/dev/null; do
    attempt=$((attempt + 1))
    if [ $attempt -ge $max_attempts ]; then
        log_warning "MySQL not responding after $max_attempts attempts"
        log_warning "You may need to run migrations manually: dotnet ef database update"
        break
    fi
    echo -n "."
    sleep 2
done

if [ $attempt -lt $max_attempts ]; then
    echo ""
    log_success "MySQL is ready"

    # 5. Run database migrations
    log_info "Running database migrations..."
    if dotnet ef database update \
        --project YoutubeRag.Infrastructure \
        --startup-project YoutubeRag.Api \
        --no-build \
        --verbosity quiet; then
        log_success "Database migrations applied"
    else
        log_warning "Migrations failed (you can run manually later)"
    fi
fi

echo ""

# 6. Wait for Redis to be ready
log_info "Waiting for Redis to be ready..."
max_attempts=10
attempt=0

while ! redis-cli -h localhost ping 2>/dev/null | grep -q "PONG"; do
    attempt=$((attempt + 1))
    if [ $attempt -ge $max_attempts ]; then
        log_warning "Redis not responding after $max_attempts attempts"
        break
    fi
    echo -n "."
    sleep 1
done

if [ $attempt -lt $max_attempts ]; then
    echo ""
    log_success "Redis is ready"
fi

echo ""

# 7. Create necessary directories
log_info "Creating data directories..."
mkdir -p data/videos
mkdir -p data/audio
mkdir -p data/models
mkdir -p /tmp/youtube-rag
log_success "Directories created"

echo ""

# 8. Display environment information
log_info "Environment Information:"
echo "  .NET SDK:        $(dotnet --version)"
echo "  Git:             $(git --version | head -n1)"
echo "  FFmpeg:          $(ffmpeg -version 2>/dev/null | head -n1 | awk '{print $3}' || echo 'Not found')"
echo "  MySQL Client:    $(mysql --version | awk '{print $3}' | sed 's/,//')"
echo "  Redis CLI:       $(redis-cli --version | awk '{print $2}')"

echo ""

# 9. Display next steps
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
log_success "Dev Container Setup Complete!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📝 Next Steps:"
echo ""
echo "  1. Build the solution:"
echo "     ${GREEN}dotnet build${NC}"
echo ""
echo "  2. Run the API:"
echo "     ${GREEN}dotnet run --project YoutubeRag.Api${NC}"
echo ""
echo "  3. Run tests:"
echo "     ${GREEN}dotnet test${NC}"
echo ""
echo "  4. Access services:"
echo "     • API:              http://localhost:5000"
echo "     • Swagger:          http://localhost:5000/swagger"
echo "     • Adminer (MySQL):  http://localhost:8080"
echo "     • Redis Commander:  http://localhost:8081"
echo ""
echo "  5. Useful commands:"
echo "     • ${GREEN}make help${NC}        - Show all available make commands"
echo "     • ${GREEN}make dev${NC}         - Start development environment"
echo "     • ${GREEN}make test${NC}        - Run all tests"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
log_success "Happy coding! 🎉"
echo ""
