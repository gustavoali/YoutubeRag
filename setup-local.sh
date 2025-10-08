#!/bin/bash

# YoutubeRag Local Environment Setup Script (Linux/macOS/WSL)
# This script sets up the complete local development environment
# Detects if running in WSL and handles accordingly

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Detect if running in WSL
IS_WSL=false
if grep -qi microsoft /proc/version 2>/dev/null || [ -f /proc/sys/fs/binfmt_misc/WSLInterop ]; then
    IS_WSL=true
fi

# Parse command line arguments
SKIP_DOCKER=false
SKIP_BUILD=false
WITH_DEV_TOOLS=false
CLEAN=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-docker)
            SKIP_DOCKER=true
            shift
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --with-dev-tools)
            WITH_DEV_TOOLS=true
            shift
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--skip-docker] [--skip-build] [--with-dev-tools] [--clean]"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}======================================"
echo " YoutubeRag LOCAL Environment Setup"
if [ "$IS_WSL" = true ]; then
    echo -e "   ${YELLOW}Running in WSL Environment${NC}"
fi
echo -e "======================================${NC}"
echo ""

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to wait for a service to be ready
wait_for_service() {
    local service_name=$1
    local test_command=$2
    local max_attempts=${3:-30}
    local delay=${4:-2}

    echo -n "Waiting for $service_name to be ready..."
    attempts=0

    while [ $attempts -lt $max_attempts ]; do
        if eval "$test_command" >/dev/null 2>&1; then
            echo -e " ${GREEN}Ready!${NC}"
            return 0
        fi

        echo -n "."
        sleep $delay
        attempts=$((attempts + 1))
    done

    echo -e " ${RED}Timeout!${NC}"
    return 1
}

# Step 1: Check prerequisites
echo -e "${YELLOW}Checking prerequisites...${NC}"

missing_prereqs=()

if ! command_exists dotnet; then
    missing_prereqs+=(".NET SDK (https://dotnet.microsoft.com/download)")
fi

if ! command_exists docker && [ "$SKIP_DOCKER" = false ]; then
    if [ "$IS_WSL" = true ]; then
        missing_prereqs+=("Docker in WSL (See REQUERIMIENTOS_SISTEMA.md for WSL Docker installation)")
    else
        missing_prereqs+=("Docker (https://docs.docker.com/get-docker/)")
    fi
fi

if ! command_exists python && ! command_exists python3; then
    missing_prereqs+=("Python 3.x (https://www.python.org/downloads/)")
fi

if ! command_exists ffmpeg; then
    echo -e "  ${YELLOW}Warning: FFmpeg not found (optional but recommended)${NC}"
    echo "  Install with:"
    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "    brew install ffmpeg"
    else
        echo "    sudo apt-get install ffmpeg  # Debian/Ubuntu"
        echo "    sudo yum install ffmpeg      # RHEL/CentOS"
    fi
fi

if [ ${#missing_prereqs[@]} -gt 0 ]; then
    echo -e "${RED}Missing prerequisites:${NC}"
    for prereq in "${missing_prereqs[@]}"; do
        echo -e "  ${RED}- $prereq${NC}"
    done
    exit 1
fi

echo -e "${GREEN}All required prerequisites found!${NC}"
echo ""

# Step 2: Check Python packages
echo -e "${YELLOW}Checking Python packages...${NC}"

# Determine Python and pip commands
if command_exists python3; then
    PYTHON_CMD="python3"
    PIP_CMD="pip3"
elif command_exists python; then
    PYTHON_CMD="python"
    PIP_CMD="pip"
fi

# Check if Whisper is installed
if ! $PIP_CMD show openai-whisper >/dev/null 2>&1; then
    echo -e "${YELLOW}Installing OpenAI Whisper...${NC}"
    $PIP_CMD install openai-whisper
    if [ $? -ne 0 ]; then
        echo -e "${RED}Failed to install Whisper. Please install manually: pip install openai-whisper${NC}"
        exit 1
    fi
fi
echo -e "${GREEN}Whisper is installed!${NC}"
echo ""

# Step 3: Clean up if requested
if [ "$CLEAN" = true ]; then
    echo -e "${YELLOW}Cleaning up existing containers and volumes...${NC}"
    docker-compose down -v 2>/dev/null || true
    echo -e "${GREEN}Cleanup complete!${NC}"
    echo ""
fi

# Step 4: Start Docker services
if [ "$SKIP_DOCKER" = false ]; then
    echo -e "${YELLOW}Starting Docker services...${NC}"

    # If in WSL, ensure Docker service is running
    if [ "$IS_WSL" = true ]; then
        if ! sudo service docker status >/dev/null 2>&1; then
            echo -e "${YELLOW}Starting Docker service in WSL...${NC}"
            sudo service docker start
            sleep 3
        fi
    fi

    # Check if Docker is running
    if ! docker info >/dev/null 2>&1; then
        if [ "$IS_WSL" = true ]; then
            echo -e "${RED}Docker is not running in WSL. Try: sudo service docker start${NC}"
        else
            echo -e "${RED}Docker is not running. Please start Docker and try again.${NC}"
        fi
        exit 1
    fi

    # Start services
    if [ "$WITH_DEV_TOOLS" = true ]; then
        echo -e "${CYAN}Starting services with dev tools (Adminer, Redis Commander)...${NC}"
        docker-compose --profile dev-tools up -d
    else
        docker-compose up -d
    fi

    if [ $? -ne 0 ]; then
        echo -e "${RED}Failed to start Docker services${NC}"
        exit 1
    fi

    echo -e "${GREEN}Docker services started!${NC}"
    echo ""

    # Wait for services
    echo -e "${YELLOW}Waiting for services to be healthy...${NC}"

    mysql_ready=$(wait_for_service "MySQL" "docker exec youtube-rag-mysql mysqladmin ping -h localhost -u root -prootpassword")
    redis_ready=$(wait_for_service "Redis" "docker exec youtube-rag-redis redis-cli ping")

    if [ "$mysql_ready" != 0 ] || [ "$redis_ready" != 0 ]; then
        echo -e "${RED}Some services failed to start. Check Docker logs with: docker-compose logs${NC}"
        exit 1
    fi

    echo -e "${GREEN}All services are healthy!${NC}"
    echo ""
fi

# Step 5: Build the project
if [ "$SKIP_BUILD" = false ]; then
    echo -e "${YELLOW}Building the project...${NC}"
    dotnet build --configuration Debug

    if [ $? -ne 0 ]; then
        echo -e "${RED}Build failed!${NC}"
        exit 1
    fi

    echo -e "${GREEN}Build successful!${NC}"
    echo ""
fi

# Step 6: Run database migrations (if EF Core tools are installed)
if command_exists dotnet-ef; then
    echo -e "${YELLOW}Running database migrations...${NC}"
    cd YoutubeRag.Api
    dotnet ef database update --project ../YoutubeRag.Infrastructure
    cd ..

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}Migrations applied successfully!${NC}"
    else
        echo -e "${YELLOW}Migrations failed (this is okay if EF tools not installed)${NC}"
    fi
    echo ""
fi

# Step 7: Display status and next steps
echo -e "${GREEN}======================================"
echo " Setup Complete!"
echo -e "======================================${NC}"
echo ""
echo -e "${CYAN}Services Status:${NC}"
echo "  MySQL:    http://localhost:3306"
echo "  Redis:    http://localhost:6379"

if [ "$WITH_DEV_TOOLS" = true ]; then
    echo "  Adminer:  http://localhost:8080 (MySQL UI)"
    echo "  Redis UI: http://localhost:8081"
fi

echo ""
echo -e "${CYAN}Next Steps:${NC}"
echo "  1. Run the application:"
echo -e "     ${YELLOW}dotnet run --project YoutubeRag.Api --environment Local${NC}"
echo ""
echo "  2. Access the API:"
echo -e "     ${YELLOW}http://localhost:62788/${NC}"
echo -e "     ${YELLOW}http://localhost:62788/docs${NC} (Swagger UI)"
echo ""
echo "  3. Test video processing:"
echo '     curl -X POST http://localhost:62788/api/v1/videos/from-url \'
echo '       -H "Content-Type: application/json" \'
echo '       -d '"'"'{"url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ", "title": "Test"}'"'"
echo ""
echo -e "${CYAN}Troubleshooting:${NC}"
echo "  - View logs: docker-compose logs -f"
echo "  - Stop services: docker-compose down"
echo "  - Clean everything: ./setup-local.sh --clean"
if [ "$IS_WSL" = true ]; then
    echo "  - Start Docker in WSL: sudo service docker start"
    echo "  - Check Docker status: docker ps"
    echo ""
    echo -e "${YELLOW}WSL Notes:${NC}"
    echo "  - Docker is running inside WSL, not Docker Desktop"
    echo "  - Services are accessible on localhost from Windows"
fi
echo ""