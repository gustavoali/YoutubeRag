# YoutubeRag.NET - Quick Start Guide

**Get up and running in 5 minutes!** 🚀

This guide will help you set up your development environment and start coding immediately.

---

## Prerequisites

Ensure you have these installed:

- ✅ **Git** - Version control
- ✅ **.NET SDK 8.0+** - Application runtime ([download](https://dotnet.microsoft.com/download))
- ✅ **Docker Desktop** - Container platform ([download](https://www.docker.com/products/docker-desktop))
- ✅ **Docker Compose** - Multi-container orchestration (included with Docker Desktop)

**Verify Installation:**
```bash
git --version           # Should be 2.x or higher
dotnet --version        # Should be 8.0.x or higher
docker --version        # Should be 20.x or higher
docker-compose --version # Should be 2.x or higher
```

---

## 5-Minute Setup

### Step 1: Clone Repository (30 seconds)

```bash
git clone <repository-url>
cd youtube_rag_net
```

### Step 2: Configure Environment (1 minute)

**Windows (PowerShell):**
```powershell
Copy-Item .env.template .env.local
# Edit .env.local if needed (defaults work fine)
```

**Linux/Mac:**
```bash
cp .env.template .env.local
# Edit .env.local if needed (defaults work fine)
```

> **Tip:** The `.env.template` file contains sensible defaults. You can use it as-is for local development!

### Step 3: Run Automated Setup (3 minutes)

**Windows (PowerShell):**
```powershell
.\scripts\dev-setup.ps1
```

**Linux/Mac (Bash):**
```bash
chmod +x scripts/dev-setup.sh
./scripts/dev-setup.sh
```

This script will:
- ✅ Verify all prerequisites are installed
- ✅ Pull Docker images (MySQL, Redis)
- ✅ Start database and cache services
- ✅ Restore NuGet packages
- ✅ Build the solution
- ✅ Run database migrations
- ✅ Optionally seed test data

### Step 4: Seed Test Data (30 seconds) - OPTIONAL but RECOMMENDED

**Windows:**
```powershell
.\scripts\seed-database.ps1
```

**Linux/Mac:**
```bash
./scripts/seed-database.sh
```

This creates:
- 👤 4 test users (including admin@youtuberag.com)
- 🎥 5 sample videos with different statuses
- ⚙️ 5 background jobs in various states
- 📝 5 transcript segments
- 🔔 3 user notifications

### Step 5: Start the Application (30 seconds)

```bash
dotnet run --project YoutubeRag.Api
```

**Your API is now running!** 🎉

- **Swagger UI:** http://localhost:5000/swagger
- **API Root:** http://localhost:5000
- **Health Check:** http://localhost:5000/health

---

## Test User Credentials

Use these accounts to test the application:

| Email | Password | Role | Status |
|-------|----------|------|--------|
| `admin@youtuberag.com` | `Admin123!` | Admin | Active |
| `user1@test.example.com` | `Test123!` | User | Active |
| `user2@test.example.com` | `Test123!` | User | Active |
| `inactive@test.example.com` | `Test123!` | User | Inactive |

---

## Common Commands

### Development

```bash
# Start API
dotnet run --project YoutubeRag.Api

# Run tests
dotnet test

# Watch mode (auto-restart on changes)
dotnet watch run --project YoutubeRag.Api

# Build solution
dotnet build --configuration Release
```

### Database

```bash
# View container logs
docker-compose logs mysql

# Access MySQL CLI
docker exec -it youtube-rag-mysql mysql -u root -prootpassword youtube_rag_db

# Re-seed database (clean and fresh)
.\scripts\seed-database.ps1 -CleanFirst        # Windows
./scripts/seed-database.sh --clean             # Linux/Mac
```

### Docker Services

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose stop

# View running containers
docker ps

# View logs
docker-compose logs -f

# Clean up everything
docker-compose down -v
```

### Database Migrations

```bash
# Apply migrations
dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

# Create new migration
dotnet ef migrations add MigrationName --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api

# Revert last migration
dotnet ef database update PreviousMigration --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api
```

---

## IDE Setup

### Visual Studio 2022

1. Open `YoutubeRag.sln`
2. Set `YoutubeRag.Api` as startup project
3. Press F5 to run

### Visual Studio Code

1. Open folder: `File > Open Folder > youtube_rag_net`
2. Install recommended extensions (C# DevKit, Docker)
3. Press F5 to run (or use terminal commands above)

### JetBrains Rider

1. Open `YoutubeRag.sln`
2. Rider will auto-detect configuration
3. Press F5 to run

---

## Troubleshooting

### Docker Not Running

**Error:** `Cannot connect to Docker daemon`

**Solution:**
```bash
# Windows/Mac: Start Docker Desktop
# Linux: sudo systemctl start docker
```

### Port Already in Use

**Error:** `Port 3306 is already allocated`

**Solution:**
```bash
# Stop conflicting service or change port in .env.local
docker ps  # Find conflicting container
docker stop <container-id>
```

### Database Connection Failed

**Error:** `Unable to connect to MySQL`

**Solution:**
```bash
# Check if MySQL is running
docker ps | grep mysql

# Restart MySQL
docker-compose restart mysql

# Wait 30 seconds and try again
```

### Build Errors

**Error:** Compilation errors

**Solution:**
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Migration Errors

**Error:** `No migrations to apply`

**Solution:**
```bash
# Check if database exists
docker exec -it youtube-rag-mysql mysql -u root -prootpassword -e "SHOW DATABASES;"

# Try manual migration
dotnet ef database update --project YoutubeRag.Infrastructure --startup-project YoutubeRag.Api --verbose
```

---

## What's Included

### Sample Data (after seeding)

- **Videos:**
  - ✅ Completed video with transcription
  - ⏳ Video currently processing (45% complete)
  - 📋 Video pending processing
  - ❌ Video that failed processing
  - 📺 Long video (2 hours) for testing

- **Jobs:**
  - ✅ Completed transcription job
  - ⏳ Running video processing job
  - 📋 Pending embedding job
  - ❌ Failed job (3 retries exhausted)
  - ⚡ High-priority pending job

- **Transcripts:**
  - 5 segments for the completed video
  - Realistic timestamps and text content

### Development Tools (optional)

Access web UIs for database and cache inspection:

```bash
# Start with dev tools
docker-compose --profile dev-tools up -d

# Access tools:
# Adminer (MySQL UI): http://localhost:8080
# Redis Commander: http://localhost:8081
```

---

## Next Steps

1. **Explore the API:**
   - Visit http://localhost:5000/swagger
   - Try the authentication endpoints with test users
   - Upload a YouTube URL for processing

2. **Run Tests:**
   ```bash
   dotnet test
   ```

3. **Read Documentation:**
   - Architecture: `docs/ARCHITECTURE.md`
   - DevOps Guide: `docs/devops/DEVOPS_IMPLEMENTATION_PLAN.md`
   - API Docs: Available in Swagger UI

4. **Start Developing:**
   - Create a new branch: `git checkout -b feature/your-feature`
   - Make changes and test locally
   - Commit and push: `git commit -am "Your message" && git push`

---

## Environment Variables Reference

See `.env.template` for all available configuration options.

**Key Variables:**

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Development | Environment name |
| `MYSQL_PASSWORD` | youtube_rag_password | Database password |
| `REDIS_PASSWORD` | (empty) | Redis password |
| `JWT_SECRET` | development-secret... | JWT signing key |
| `PROCESSING_TEMP_PATH` | /app/temp | Temp files location |
| `WHISPER_MODELS_PATH` | /app/models | Whisper models location |

**Cross-Platform Paths:**

The application uses `IPathProvider` for automatic path resolution:

- **Windows:** `C:\Temp\YoutubeRag`, `C:\Models\Whisper`
- **Linux/Mac:** `/tmp/youtuberag`, `/tmp/whisper-models`
- **Docker:** `/app/temp`, `/app/models`

You don't need to worry about paths - they're handled automatically! ✨

---

## Getting Help

- **Documentation:** Check `docs/` folder
- **Issues:** Create a GitHub issue
- **Questions:** Ask in team Slack/Discord
- **DevOps:** See `docs/devops/PHASE1_IMPLEMENTATION_SUMMARY.md`

---

## Success Checklist

After setup, verify everything works:

- [ ] API starts successfully: `dotnet run --project YoutubeRag.Api`
- [ ] Swagger UI accessible: http://localhost:5000/swagger
- [ ] Health check returns HTTP 200: http://localhost:5000/health
- [ ] MySQL container running: `docker ps | grep mysql`
- [ ] Redis container running: `docker ps | grep redis`
- [ ] Tests pass: `dotnet test`
- [ ] Sample data exists in database (if seeded)
- [ ] Can login with test users

**All checked?** You're ready to code! 🎉

---

**Updated:** 2025-10-10
**Version:** 1.0 (Phase 1 Complete)
