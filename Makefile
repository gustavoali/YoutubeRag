# Makefile for YoutubeRag.NET
# DEVOPS-011: Comprehensive Makefile for common development commands
#
# Usage: make <target>
# For help: make help

.PHONY: help dev dev-docker test test-docker ci migrate seed clean logs build restore format check status up down restart ps

# Default target
.DEFAULT_GOAL := help

##@ General

help: ## Display this help message
	@echo "YoutubeRag.NET - Development Commands"
	@echo "======================================"
	@echo ""
	@awk 'BEGIN {FS = ":.*##"; printf "Usage:\n  make \033[36m<target>\033[0m\n"} /^[a-zA-Z_-]+:.*?##/ { printf "  \033[36m%-20s\033[0m %s\n", $$1, $$2 } /^##@/ { printf "\n\033[1m%s\033[0m\n", substr($$0, 5) } ' $(MAKEFILE_LIST)
	@echo ""
	@echo "Examples:"
	@echo "  make dev          # Start local development (no Docker)"
	@echo "  make dev-docker   # Start development in Docker with hot reload"
	@echo "  make test         # Run all tests locally"
	@echo "  make ci           # Simulate CI pipeline locally"
	@echo ""

##@ Development

dev: ## Start local development (API + infrastructure)
	@echo "🚀 Starting local development..."
	@echo "📦 Starting infrastructure services..."
	@docker-compose up -d mysql redis
	@echo "⏳ Waiting for services to be ready..."
	@sleep 15
	@echo "🔄 Running database migrations..."
	@dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api || true
	@echo "✅ Starting API..."
	@dotnet run --project YoutubeRag.Api

dev-docker: ## Start development in Docker with hot reload
	@echo "🐳 Starting development environment in Docker..."
	@docker-compose up --build api
	@echo ""
	@echo "✅ Development environment ready!"
	@echo "   API:         http://localhost:5000"
	@echo "   Swagger:     http://localhost:5000/swagger"
	@echo "   Adminer:     http://localhost:8080"
	@echo "   Redis UI:    http://localhost:8081"

watch: ## Start API with hot reload (dotnet watch)
	@echo "👀 Starting API with hot reload..."
	@dotnet watch --project YoutubeRag.Api

##@ Testing

test: ## Run all tests locally
	@echo "🧪 Running all tests..."
	@dotnet test --verbosity normal

test-ci: ## Run tests with CI configuration
	@echo "🧪 Running tests (CI mode)..."
	@dotnet test --configuration Release --verbosity normal --logger "trx"

test-docker: ## Run tests in Docker
	@echo "🐳 Running tests in Docker..."
	@docker-compose --profile test up --abort-on-container-exit test-runner
	@echo ""
	@echo "✅ Tests complete. Results in ./TestResults"

test-coverage: ## Run tests with code coverage
	@echo "📊 Running tests with coverage..."
	@dotnet test --collect:"XPlat Code Coverage" --verbosity normal
	@echo ""
	@echo "✅ Coverage report generated in TestResults"

ci: build test-ci ## Simulate CI pipeline locally
	@echo "✅ CI pipeline simulation complete!"

##@ Building

build: ## Build the solution
	@echo "🔨 Building solution..."
	@dotnet build --configuration Release

build-docker: ## Build Docker image
	@echo "🐳 Building Docker image..."
	@docker-compose build api

restore: ## Restore NuGet packages
	@echo "📦 Restoring NuGet packages..."
	@dotnet restore

clean: ## Clean build artifacts
	@echo "🧹 Cleaning build artifacts..."
	@dotnet clean
	@rm -rf **/bin **/obj
	@echo "✅ Clean complete"

rebuild: clean restore build ## Clean, restore, and rebuild
	@echo "✅ Rebuild complete"

##@ Database

migrate: ## Run database migrations
	@echo "🔄 Running database migrations..."
	@docker-compose up -d mysql
	@sleep 10
	@dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
	@echo "✅ Migrations applied"

migrate-docker: ## Run migrations in Docker
	@echo "🐳 Running migrations in Docker..."
	@docker-compose --profile migration up migration
	@echo "✅ Migrations complete"

seed: ## Seed database with test data
	@echo "🌱 Seeding database..."
	@if [ -f scripts/seed-database.sh ]; then \
		chmod +x scripts/seed-database.sh && ./scripts/seed-database.sh; \
	else \
		echo "❌ seed-database.sh not found"; \
	fi

migration-add: ## Create new migration (usage: make migration-add NAME=MigrationName)
	@if [ -z "$(NAME)" ]; then \
		echo "❌ Error: Please specify migration name"; \
		echo "   Usage: make migration-add NAME=MigrationName"; \
		exit 1; \
	fi
	@echo "📝 Creating migration: $(NAME)"
	@dotnet ef migrations add $(NAME) --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
	@echo "✅ Migration created"

migration-remove: ## Remove last migration
	@echo "🗑️  Removing last migration..."
	@dotnet ef migrations remove --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
	@echo "✅ Migration removed"

migration-list: ## List all migrations
	@echo "📋 Listing migrations..."
	@dotnet ef migrations list --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

db-reset: ## Reset database (WARNING: deletes all data)
	@echo "⚠️  WARNING: This will delete ALL data!"
	@read -p "Are you sure? [y/N] " -n 1 -r; \
	echo; \
	if [[ $$REPLY =~ ^[Yy]$$ ]]; then \
		echo "🗑️  Dropping database..."; \
		docker-compose down -v; \
		docker-compose up -d mysql; \
		sleep 15; \
		make migrate; \
		echo "✅ Database reset complete"; \
	else \
		echo "❌ Cancelled"; \
	fi

##@ Docker

up: ## Start all services
	@echo "🚀 Starting all services..."
	@docker-compose up -d
	@echo "✅ Services started"
	@make ps

down: ## Stop all services
	@echo "🛑 Stopping all services..."
	@docker-compose down
	@echo "✅ Services stopped"

down-v: ## Stop all services and remove volumes
	@echo "⚠️  Stopping services and removing volumes..."
	@docker-compose down -v
	@echo "✅ Services stopped and volumes removed"

restart: ## Restart all services
	@echo "🔄 Restarting services..."
	@docker-compose restart
	@echo "✅ Services restarted"

ps: ## List running containers
	@docker-compose ps

logs: ## Show logs for all services
	@docker-compose logs -f

logs-api: ## Show API logs
	@docker-compose logs -f api

logs-mysql: ## Show MySQL logs
	@docker-compose logs -f mysql

logs-redis: ## Show Redis logs
	@docker-compose logs -f redis

##@ Code Quality

format: ## Format code
	@echo "✨ Formatting code..."
	@dotnet format
	@echo "✅ Code formatted"

format-check: ## Check code formatting (CI mode)
	@echo "🔍 Checking code formatting..."
	@dotnet format --verify-no-changes

lint: ## Run code analyzers
	@echo "🔍 Running code analyzers..."
	@dotnet build /p:RunAnalyzers=true
	@echo "✅ Analysis complete"

check: format-check lint test-ci ## Run all quality checks
	@echo "✅ All quality checks passed"

##@ Utilities

status: ## Show project status
	@echo "📊 Project Status"
	@echo "================="
	@echo ""
	@echo "Docker Containers:"
	@docker-compose ps
	@echo ""
	@echo "Git Status:"
	@git status --short
	@echo ""
	@echo "Latest Commits:"
	@git log --oneline -5
	@echo ""

env: ## Show environment info
	@echo "🌍 Environment Information"
	@echo "=========================="
	@echo "Docker version:   $$(docker --version)"
	@echo ".NET version:     $$(dotnet --version)"
	@echo "Git version:      $$(git --version)"
	@echo "Working dir:      $$(pwd)"
	@echo "Branch:           $$(git branch --show-current)"
	@echo ""

install-tools: ## Install required .NET tools
	@echo "🔧 Installing .NET tools..."
	@dotnet tool install --global dotnet-ef || dotnet tool update --global dotnet-ef
	@dotnet tool install --global dotnet-format || dotnet tool update --global dotnet-format
	@dotnet tool install --global dotnet-reportgenerator-globaltool || dotnet tool update --global dotnet-reportgenerator-globaltool
	@echo "✅ Tools installed"

setup: install-tools restore build ## Complete first-time setup
	@echo "🎉 Setup complete!"
	@echo ""
	@echo "Next steps:"
	@echo "  1. Copy .env.template to .env and configure"
	@echo "  2. Run 'make dev' to start development"
	@echo ""

health: ## Check service health
	@echo "🏥 Checking service health..."
	@echo ""
	@echo "MySQL:"
	@docker-compose exec mysql mysqladmin ping -h localhost || echo "❌ MySQL not responding"
	@echo ""
	@echo "Redis:"
	@docker-compose exec redis redis-cli ping || echo "❌ Redis not responding"
	@echo ""
	@echo "API:"
	@curl -sf http://localhost:5000/health > /dev/null && echo "✅ API is healthy" || echo "❌ API not responding"
	@echo ""

##@ Documentation

docs-serve: ## Serve documentation locally
	@echo "📚 Documentation server not yet configured"
	@echo "   View docs in docs/ directory"

##@ Monitoring

dev-tools: ## Start development tools (Adminer, Redis Commander)
	@echo "🛠️  Starting development tools..."
	@docker-compose --profile dev-tools up -d
	@echo ""
	@echo "✅ Dev tools started:"
	@echo "   Adminer (MySQL):  http://localhost:8080"
	@echo "   Redis Commander:  http://localhost:8081"

monitoring: ## Start monitoring stack (Prometheus, Grafana)
	@echo "📊 Starting monitoring stack..."
	@docker-compose --profile monitoring up -d
	@echo ""
	@echo "✅ Monitoring started:"
	@echo "   Prometheus:  http://localhost:9090"
	@echo "   Grafana:     http://localhost:3001 (admin/admin)"

##@ Cleanup

prune: ## Remove unused Docker resources
	@echo "🧹 Pruning unused Docker resources..."
	@docker system prune -af --volumes
	@echo "✅ Cleanup complete"

clean-all: down-v prune clean ## Complete cleanup (WARNING: removes all data)
	@echo "✅ Complete cleanup finished"

##@ Release

version: ## Show current version
	@echo "📌 Current version: $$(git describe --tags --always)"

tag: ## Create new version tag (usage: make tag VERSION=v1.0.0)
	@if [ -z "$(VERSION)" ]; then \
		echo "❌ Error: Please specify version"; \
		echo "   Usage: make tag VERSION=v1.0.0"; \
		exit 1; \
	fi
	@echo "🏷️  Creating tag: $(VERSION)"
	@git tag -a $(VERSION) -m "Release $(VERSION)"
	@git push origin $(VERSION)
	@echo "✅ Tag created and pushed"
