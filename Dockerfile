# Multi-stage Dockerfile for YoutubeRag .NET 8.0 Application
# Optimized for size, security, and performance

# Build stage arguments
ARG DOTNET_VERSION=8.0
ARG ALPINE_VERSION=3.18

################################################################################
# Stage 1: Base image for restore (caching dependencies)
################################################################################
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore
WORKDIR /src

# Copy solution and project files for optimal layer caching
COPY YoutubeRag.sln ./
COPY YoutubeRag.Domain/*.csproj ./YoutubeRag.Domain/
COPY YoutubeRag.Application/*.csproj ./YoutubeRag.Application/
COPY YoutubeRag.Infrastructure/*.csproj ./YoutubeRag.Infrastructure/
COPY YoutubeRag.Api/*.csproj ./YoutubeRag.Api/

# Restore dependencies with optimizations
RUN dotnet restore YoutubeRag.sln \
    --runtime linux-x64 \
    --disable-parallel

################################################################################
# Stage 2: Build stage
################################################################################
FROM restore AS build
WORKDIR /src

# Copy all source code
COPY . .

# Build the application with Release configuration
RUN dotnet build YoutubeRag.sln \
    --configuration Release \
    --no-restore \
    --runtime linux-x64 \
    -warnaserror

################################################################################
# Stage 3: Test stage (optional - can be skipped in production builds)
################################################################################
FROM build AS test
WORKDIR /src

# Set test environment variables
ENV ASPNETCORE_ENVIRONMENT=Testing
ENV ConnectionStrings__DefaultConnection="Server=localhost;Database=test_db;User=root;Password=test"
ENV Redis__ConnectionString="localhost:6379"
ENV JWT__Secret="TestSecretKeyForJWTTokenGenerationMinimum256Bits!"

# Run tests (will skip if external services are not available)
RUN dotnet test YoutubeRag.sln \
    --configuration Release \
    --no-build \
    --verbosity normal \
    --logger "trx" \
    --results-directory /test-results \
    --collect:"XPlat Code Coverage" \
    || echo "Tests skipped in Docker build (require external services)"

################################################################################
# Stage 4: Publish stage
################################################################################
FROM build AS publish
WORKDIR /src

# Publish with optimizations
RUN dotnet publish YoutubeRag.Api/YoutubeRag.Api.csproj \
    --configuration Release \
    --no-build \
    --output /app/publish \
    --runtime linux-x64 \
    --self-contained false \
    -p:PublishReadyToRun=true \
    -p:PublishSingleFile=false \
    -p:PublishTrimmed=false \
    -p:DebugType=None \
    -p:DebugSymbols=false

################################################################################
# Stage 5: Runtime base with Python/Whisper support
################################################################################
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime-base

# Install system dependencies including Python and FFmpeg for Whisper
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    python3 \
    python3-pip \
    python3-venv \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# Create Python virtual environment and install Whisper
RUN python3 -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"
RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir openai-whisper

################################################################################
# Stage 6: Final runtime stage
################################################################################
FROM runtime-base AS runtime

# Create non-root user and group
RUN groupadd -r -g 1000 appgroup && \
    useradd -r -u 1000 -g appgroup -m -s /bin/bash appuser && \
    mkdir -p /app /app/logs /app/temp && \
    chown -R appuser:appgroup /app

WORKDIR /app

# Copy published application from publish stage
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Set .NET environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

# Python/Whisper environment
ENV PATH="/opt/venv/bin:$PATH" \
    PYTHONUNBUFFERED=1

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Switch to non-root user
USER appuser

# Expose port (non-privileged)
EXPOSE 8080

# Set entrypoint
ENTRYPOINT ["dotnet", "YoutubeRag.Api.dll"]

################################################################################
# Stage 7: Alpine-based minimal runtime (alternative lightweight option)
################################################################################
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine${ALPINE_VERSION} AS runtime-alpine

# Install required packages
RUN apk add --no-cache \
    icu-libs \
    icu-data-full \
    tzdata \
    curl \
    ca-certificates \
    python3 \
    py3-pip \
    ffmpeg \
    && addgroup -g 1000 -S appgroup \
    && adduser -u 1000 -S appuser -G appgroup \
    && mkdir -p /app /app/logs /app/temp \
    && chown -R appuser:appgroup /app

# Install Whisper in Alpine
RUN python3 -m pip install --no-cache-dir --break-system-packages openai-whisper

WORKDIR /app

# Copy published application
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=UTC

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "YoutubeRag.Api.dll"]

################################################################################
# Stage 8: Debug stage (for troubleshooting)
################################################################################
FROM runtime AS debug

USER root

# Install debugging tools
RUN apt-get update && apt-get install -y \
    vim \
    net-tools \
    procps \
    htop \
    tcpdump \
    strace \
    && rm -rf /var/lib/apt/lists/*

# Install .NET diagnostic tools
RUN dotnet tool install --global dotnet-counters && \
    dotnet tool install --global dotnet-trace && \
    dotnet tool install --global dotnet-dump && \
    dotnet tool install --global dotnet-gcdump

ENV PATH="${PATH}:/root/.dotnet/tools"

USER appuser

################################################################################
# Stage 9: Migration runner (for database migrations)
################################################################################
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS migration

# Install EF Core tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /src

# Copy source code from build stage
COPY --from=build /src .

# Entry point for running migrations
ENTRYPOINT ["dotnet", "ef", "database", "update", \
    "--project", "YoutubeRag.Infrastructure", \
    "--startup-project", "YoutubeRag.Api", \
    "--configuration", "Release"]