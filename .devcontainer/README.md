# VS Code Dev Container for YoutubeRag.NET

This directory contains the configuration for developing YoutubeRag.NET in a Docker container using Visual Studio Code Dev Containers.

## What is a Dev Container?

A development container (or dev container for short) allows you to use a Docker container as a full-featured development environment. It provides:

- **Consistent environment** across all developers
- **Pre-configured tools** and dependencies
- **Isolated development** from your local machine
- **Easy onboarding** for new team members

## Prerequisites

1. **Docker Desktop** (Windows/Mac) or **Docker Engine** (Linux)
   - [Install Docker Desktop](https://www.docker.com/products/docker-desktop)

2. **Visual Studio Code**
   - [Download VS Code](https://code.visualstudio.com/)

3. **Remote - Containers Extension**
   - Install from VS Code: `ms-vscode-remote.remote-containers`
   - Or [Install from Marketplace](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)

## Quick Start

### Method 1: Automatic (Recommended)

1. Open the project folder in VS Code
2. VS Code will detect the `.devcontainer` folder
3. Click "Reopen in Container" when prompted
4. Wait for the container to build (first time takes 5-10 minutes)
5. VS Code will reload inside the container

### Method 2: Manual

1. Open VS Code
2. Press `F1` or `Ctrl+Shift+P` (Cmd+Shift+P on Mac)
3. Type: `Dev Containers: Reopen in Container`
4. Select it and wait for the build to complete

### Method 3: Command Line

```bash
# Clone the repository
git clone https://github.com/gustavoali/YoutubeRag.git
cd YoutubeRag

# Open in VS Code with dev container
code .

# Then use Command Palette (F1) -> "Dev Containers: Reopen in Container"
```

## What's Included?

### Base Image
- **.NET 8 SDK** - Latest stable version
- **Ubuntu** base image

### Tools & Utilities
- **Git** with Git LFS
- **FFmpeg** for audio/video processing
- **MySQL Client** for database access
- **Redis CLI** for cache access
- **Node.js & npm** for tooling
- **Python 3** for scripts
- **curl, wget, jq** and other CLI tools

### .NET Global Tools
- `dotnet-ef` - Entity Framework Core CLI
- `dotnet-format` - Code formatter
- `dotnet-reportgenerator-globaltool` - Coverage reports
- `dotnet-counters`, `dotnet-trace`, `dotnet-dump` - Diagnostics

### VS Code Extensions (Auto-installed)

**C# / .NET:**
- C# Dev Kit
- C# Extensions
- .NET Test Explorer

**Database:**
- MySQL Client
- Redis Client

**Development:**
- Docker
- GitLens
- Git Graph
- EditorConfig
- Error Lens
- REST Client

**Productivity:**
- Better Comments
- Code Spell Checker
- Material Icon Theme

### Infrastructure Services

All services run automatically when you open the dev container:

| Service | Port | Purpose | Web UI |
|---------|------|---------|--------|
| **API** | 5000 | ASP.NET Core API | http://localhost:5000/swagger |
| **MySQL** | 3306 | Primary database | - |
| **Redis** | 6379 | Cache & sessions | - |
| **Adminer** | 8080 | MySQL Web UI | http://localhost:8080 |
| **Redis Commander** | 8081 | Redis Web UI | http://localhost:8081 |

### Environment Variables

Pre-configured in the container:

```bash
# Database
ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=youtube_rag_db;...

# Redis
Redis__Configuration=localhost:6379

# JWT (Development only - DO NOT use in production!)
Jwt__Secret=development-secret-key-min-32-chars-long-for-jwt-token-signing

# Storage paths
Storage__TempPath=/tmp/youtube-rag
Storage__VideosPath=/workspace/data/videos
Storage__AudioPath=/workspace/data/audio
Storage__ModelsPath=/workspace/data/models

# Whisper Configuration
Whisper__Provider=Local
Whisper__DefaultModel=base
```

## Post-Create Setup

The `post-create.sh` script automatically runs after the container is created:

1. ✅ Restores NuGet packages
2. ✅ Installs .NET local tools (Husky)
3. ✅ Sets up Git hooks
4. ✅ Waits for MySQL to be ready
5. ✅ Runs database migrations
6. ✅ Waits for Redis to be ready
7. ✅ Creates necessary directories
8. ✅ Displays environment info

## Usage

### Building the Solution

```bash
dotnet build
# or
make build
```

### Running the API

```bash
dotnet run --project YoutubeRag.Api
# or
make dev
```

The API will be available at:
- **API**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger
- **Health**: http://localhost:5000/health

### Running Tests

```bash
# All tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Using make
make test
```

### Database Management

```bash
# Create migration
dotnet ef migrations add MigrationName \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api

# Apply migrations
dotnet ef database update \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api

# Or use make
make migrate
```

### Accessing Services

#### MySQL (via Adminer)
1. Open http://localhost:8080
2. Login with:
   - **System**: MySQL
   - **Server**: localhost
   - **Username**: youtube_rag_user
   - **Password**: youtube_rag_password
   - **Database**: youtube_rag_db

#### Redis (via Redis Commander)
1. Open http://localhost:8081
2. No login required
3. Connection is pre-configured

#### MySQL CLI
```bash
mysql -h localhost -u youtube_rag_user -pyoutube_rag_password youtube_rag_db
```

#### Redis CLI
```bash
redis-cli -h localhost
```

## Customization

### Adding VS Code Extensions

Edit `.devcontainer/devcontainer.json`:

```json
"extensions": [
  "existing.extensions",
  "your.new.extension"
]
```

### Changing VS Code Settings

Edit `.devcontainer/devcontainer.json` under `settings`:

```json
"settings": {
  "editor.fontSize": 14,
  "terminal.integrated.fontSize": 13
}
```

### Adding Packages to Container

Edit `.devcontainer/Dockerfile`:

```dockerfile
RUN apt-get update \
    && apt-get -y install --no-install-recommends \
    your-package-name
```

### Modifying Post-Create Script

Edit `.devcontainer/post-create.sh` to add custom setup steps.

## Troubleshooting

### Container won't start

```bash
# Rebuild container without cache
# In VS Code: F1 -> "Dev Containers: Rebuild Container"

# Or from command line:
docker-compose -f .devcontainer/docker-compose.yml build --no-cache
```

### Database connection fails

```bash
# Check if MySQL is running
docker ps | grep mysql

# Check MySQL logs
docker logs youtube-rag-mysql

# Manually run migrations
dotnet ef database update \
  --project YoutubeRag.Infrastructure \
  --startup-project YoutubeRag.Api
```

### Redis connection fails

```bash
# Check if Redis is running
docker ps | grep redis

# Test connection
redis-cli -h localhost ping
# Should return: PONG
```

### Port already in use

If ports 5000, 3306, or 6379 are already in use:

1. Stop conflicting services on your host
2. Or modify ports in `.devcontainer/docker-compose.yml`

### Out of disk space

```bash
# Clean up Docker
docker system prune -a --volumes

# Remove unused images
docker image prune -a
```

### Extensions not installing

1. Reload window: `F1` -> `Developer: Reload Window`
2. Rebuild container: `F1` -> `Dev Containers: Rebuild Container`

### Git hooks not working

```bash
# Reinstall hooks
dotnet tool restore
dotnet husky install
```

## Performance Tips

### Windows Users

For better performance on Windows:

1. Store the project on the **WSL2 filesystem** (not Windows filesystem)
2. Clone inside WSL: `\\wsl$\Ubuntu\home\user\projects`
3. Enable WSL2 backend in Docker Desktop settings

### Mac Users

1. Use **VirtioFS** file sharing (Docker Desktop preferences)
2. Ensure you have **enough RAM** allocated to Docker (8GB+ recommended)

### All Platforms

1. **Exclude** unnecessary files from volume mounts in `.dockerignore`
2. Use **cached** consistency for volume mounts (already configured)
3. Close unused services when not needed

## File Structure

```
.devcontainer/
├── devcontainer.json      # Main configuration
├── docker-compose.yml     # Services definition
├── Dockerfile             # Dev container image
├── post-create.sh         # Post-creation setup script
└── README.md              # This file
```

## Differences from Local Development

| Aspect | Local | Dev Container |
|--------|-------|---------------|
| **Setup Time** | Manual (30+ min) | Automatic (10 min) |
| **Consistency** | Varies by OS | Always identical |
| **Dependencies** | Manual install | Pre-installed |
| **Database** | External | Integrated |
| **Isolation** | System-wide | Containerized |
| **Portability** | OS-dependent | Works anywhere |

## Best Practices

1. **Commit `.devcontainer/` to Git** - Share configuration with team
2. **Don't store secrets** in devcontainer.json - Use environment variables
3. **Pin versions** in Dockerfile for reproducibility
4. **Use `.dockerignore`** to exclude unnecessary files
5. **Rebuild regularly** to get latest updates
6. **Test in CI** with the same Docker image

## Support

- **Documentation**: [VS Code Dev Containers Docs](https://code.visualstudio.com/docs/remote/containers)
- **Project Issues**: [GitHub Issues](https://github.com/gustavoali/YoutubeRag/issues)
- **Docker Help**: [Docker Documentation](https://docs.docker.com/)

## Further Reading

- [VS Code Remote Development](https://code.visualstudio.com/docs/remote/remote-overview)
- [Dev Containers Specification](https://containers.dev/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)

---

**Last Updated**: 2025-10-10
**Maintained By**: DevOps Team
