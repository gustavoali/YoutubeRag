# YoutubeRag.NET - Intelligent YouTube Video Search & Analysis

A powerful RAG (Retrieval-Augmented Generation) system for YouTube video transcriptions with semantic search capabilities. Built with .NET 8 and designed to run completely locally without external API costs.

## Features

- **YouTube Video Processing**: Automatic download and processing of YouTube videos
- **AI Transcription**: Convert video audio to text using Whisper (local or cloud)
- **Semantic Search**: Find relevant content across all processed videos
- **Local Mode**: Run everything locally without OpenAI API keys
- **Clean Architecture**: Domain-driven design with clear separation of concerns
- **RESTful API**: Well-documented API with Swagger/OpenAPI support

## Quick Start

### Prerequisites

- .NET 8 SDK
- WSL2 with Docker installed inside (NOT Docker Desktop)
- Python 3.x with pip
- FFmpeg (recommended)
- Windows 10/11, macOS, or Linux

### Docker in WSL Setup (Required)

**IMPORTANT**: This project uses Docker running inside WSL2, not Docker Desktop.

**First-time WSL Docker installation:**
```bash
# Install WSL2 (Windows only)
wsl --install
wsl --set-default-version 2

# Inside WSL, install Docker
sudo apt update && sudo apt upgrade -y
sudo apt install -y docker.io docker-compose
sudo usermod -aG docker $USER
sudo service docker start
```

### One-Command Setup

**Windows (PowerShell):**
```powershell
# Ensure Docker is running in WSL first
wsl sudo service docker start

# Run setup script
.\setup-local.ps1
```

**Linux/macOS/WSL:**
```bash
chmod +x setup-local.sh
./setup-local.sh
```

This will:
1. Check all prerequisites (including WSL Docker)
2. Start Docker service in WSL if needed
3. Install Python packages (Whisper)
4. Start MySQL and Redis in Docker/WSL
5. Build the project
6. Apply database migrations

### Manual Setup

1. **Install Dependencies:**
```bash
# Install Whisper for local transcription
pip install openai-whisper

# Start infrastructure services (from Windows)
wsl docker-compose up -d

# Or from within WSL/Linux
docker-compose up -d
```

2. **Build and Run:**
```bash
# Build the project
dotnet build

# Run in LOCAL mode (no API keys needed)
dotnet run --project YoutubeRag.Api --environment Local
```

3. **Access the Application:**
- API: http://localhost:62788
- Swagger UI: http://localhost:62788/docs
- Health Check: http://localhost:62788/health

## Usage Examples

### Process a YouTube Video
```bash
curl -X POST http://localhost:62788/api/v1/videos/from-url \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://www.youtube.com/watch?v=VIDEO_ID",
    "title": "Video Title",
    "description": "Optional description"
  }'
```

### Search Across Videos
```bash
curl -X POST http://localhost:62788/api/v1/search/semantic \
  -H "Content-Type: application/json" \
  -d '{
    "query": "machine learning tutorial",
    "maxResults": 5,
    "minRelevanceScore": 0.7
  }'
```

### Check Processing Status
```bash
curl http://localhost:62788/api/v1/videos/{videoId}/progress
```

## Operating Modes

### 1. Local Mode (No API Keys Required)
- Uses local Whisper for transcription
- Local embeddings for semantic search
- Completely offline operation
- No costs, slower processing

### 2. Mock Mode (Development)
- Simulated services for rapid development
- No real processing
- Instant responses with fake data

### 3. Cloud Mode (Production)
- OpenAI Whisper API for transcription
- OpenAI embeddings for better search
- Faster processing, requires API key
- ~$0.06 per 10-minute video

## Project Structure

```
YoutubeRag.NET/
├── YoutubeRag.Domain/        # Domain entities and interfaces
├── YoutubeRag.Application/   # Business logic and use cases
├── YoutubeRag.Infrastructure/# External services and data access
├── YoutubeRag.Api/          # REST API and configuration
├── docker-compose.yml       # Infrastructure services
└── setup-local.ps1/sh      # Setup scripts
```

## Configuration

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Set to `Local`, `Development`, or `Production`
- `OPENAI__APIKEY`: OpenAI API key (optional, for cloud mode)

### Connection Strings
Edit `appsettings.Local.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=youtube_rag_local;...",
    "Redis": "localhost:6379"
  }
}
```

## Development Tools

### Optional UI Tools
Start with dev tools for database and cache inspection:
```bash
# Windows
.\setup-local.ps1 -WithDevTools

# Linux/macOS
./setup-local.sh --with-dev-tools
```

Access:
- Adminer (MySQL UI): http://localhost:8080
- Redis Commander: http://localhost:8081

### Database Migrations
```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Create new migration
dotnet ef migrations add MigrationName --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

# Apply migrations
dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
```

## Troubleshooting

### Docker in WSL Issues

**From Windows PowerShell:**
```powershell
# Start Docker service in WSL
wsl sudo service docker start

# Check Docker status
wsl docker ps

# View service logs
wsl docker-compose logs -f

# Restart services
wsl docker-compose restart

# Clean everything
wsl docker-compose down -v
```

**From within WSL/Linux:**
```bash
# Start Docker service
sudo service docker start

# View service logs
docker-compose logs -f

# Restart services
docker-compose restart

# Clean everything
docker-compose down -v
```

### Connection Issues
- Ensure Docker is running INSIDE WSL (NOT Docker Desktop)
- Check if ports 3306 (MySQL) and 6379 (Redis) are available
- Verify WSL2 is properly configured
- Run `wsl --status` to check WSL version

### Build Issues
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

## Performance Considerations

| Mode | Video (10 min) | Processing Time | Cost |
|------|---------------|-----------------|------|
| Local | Whisper Base | 30-50 minutes | Free |
| Local | Whisper Small | 15-25 minutes | Free |
| Cloud | OpenAI API | 3-5 minutes | ~$0.06 |

## Documentation

- [System Requirements](REQUERIMIENTOS_SISTEMA.md)
- [Local Mode Guide](MODO_LOCAL_SIN_OPENAI.md)
- [Production Mode Guide](MODO_REAL_GUIA.md)
- [Infrastructure Assessment](INFRASTRUCTURE_ASSESSMENT.md)

## API Documentation

When running, access the Swagger UI at http://localhost:62788/docs for interactive API documentation.

### Key Endpoints
- `POST /api/v1/videos/from-url` - Process YouTube video
- `GET /api/v1/videos/{id}` - Get video details
- `GET /api/v1/videos/{id}/progress` - Check processing progress
- `POST /api/v1/search/semantic` - Semantic search
- `GET /api/v1/search/videos` - List all videos
- `GET /health` - Health check

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests (when available)
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or suggestions:
1. Check the [troubleshooting section](#troubleshooting)
2. Review existing documentation
3. Open an issue on GitHub

## Roadmap

- [ ] Add comprehensive test coverage
- [ ] Implement CI/CD with GitHub Actions
- [ ] Add support for batch video processing
- [ ] Implement video playlist support
- [ ] Add export functionality (PDF, JSON)
- [ ] Create web UI frontend
- [ ] Add support for other video platforms
- [ ] Implement user authentication and multi-tenancy

---

Built with .NET 8 | Clean Architecture | Domain-Driven Design