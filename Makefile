# Makefile for YoutubeRag.NET Project
# Cross-platform compatible commands with WSL Docker support

.PHONY: help setup build run test clean docker-up docker-down docker-logs migrate

# Detect OS and set Docker command accordingly
ifeq ($(OS),Windows_NT)
    DOCKER := wsl docker
    DOCKER_COMPOSE := wsl docker-compose
else
    DOCKER := docker
    DOCKER_COMPOSE := docker-compose
endif

# Default target
help:
	@echo "Available commands:"
	@echo "  make setup        - Set up local development environment"
	@echo "  make build        - Build the project"
	@echo "  make run          - Run the application in LOCAL mode"
	@echo "  make run-dev      - Run the application in Development mode"
	@echo "  make test         - Run tests"
	@echo "  make clean        - Clean build artifacts"
	@echo "  make docker-up    - Start Docker services"
	@echo "  make docker-down  - Stop Docker services"
	@echo "  make docker-logs  - View Docker logs"
	@echo "  make migrate      - Run database migrations"
	@echo "  make format       - Format code"

# Setup local environment
setup:
ifeq ($(OS),Windows_NT)
	@powershell -ExecutionPolicy Bypass -File setup-local.ps1
else
	@chmod +x setup-local.sh
	@./setup-local.sh
endif

# Build the project
build:
	dotnet restore
	dotnet build --configuration Debug

# Run in LOCAL mode
run:
	dotnet run --project YoutubeRag.Api --environment Local

# Run in Development mode
run-dev:
	dotnet run --project YoutubeRag.Api --environment Development

# Run tests
test:
	dotnet test --configuration Debug --verbosity normal

# Clean build artifacts
clean:
	dotnet clean
	find . -type d -name bin -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name obj -exec rm -rf {} + 2>/dev/null || true

# Docker commands
docker-up:
ifeq ($(OS),Windows_NT)
	@echo "Starting Docker services in WSL..."
	@wsl sudo service docker start 2>/dev/null || true
endif
	$(DOCKER_COMPOSE) up -d

docker-down:
	$(DOCKER_COMPOSE) down

docker-logs:
	$(DOCKER_COMPOSE) logs -f

docker-clean:
	$(DOCKER_COMPOSE) down -v

# Check Docker status (helpful for debugging)
docker-status:
ifeq ($(OS),Windows_NT)
	@echo "Checking Docker in WSL..."
	@wsl docker --version || echo "Docker not found in WSL"
	@wsl sudo service docker status || echo "Docker service not running"
else
	@docker --version
	@docker ps
endif

# Database migrations
migrate:
	@echo "Installing EF tools if not present..."
	@dotnet tool install --global dotnet-ef 2>/dev/null || true
	@echo "Running migrations..."
	cd YoutubeRag.Api && dotnet ef database update --project ../YoutubeRag.Infrastructure

# Code formatting
format:
	dotnet format

# Install dependencies
install-deps:
	@echo "Installing Python dependencies..."
ifeq ($(OS),Windows_NT)
	@pip install openai-whisper
else
	@pip3 install openai-whisper
endif
	@echo "Installing .NET tools..."
	@dotnet tool install --global dotnet-ef
	@dotnet tool install --global dotnet-format

# Quick start (setup + run)
quickstart: setup build run

# Production build
build-prod:
	dotnet publish YoutubeRag.Api/YoutubeRag.Api.csproj -c Release -o ./publish

# Docker production
docker-prod:
	$(DOCKER_COMPOSE) -f docker-compose.prod.yml up -d

# Health check
health:
	@curl -f http://localhost:62788/health || echo "Service is not healthy"

# API test
api-test:
	@echo "Testing API endpoint..."
	@curl http://localhost:62788/ || echo "API is not responding"