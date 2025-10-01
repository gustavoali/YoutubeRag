# Multi-stage build for YoutubeRag.Api
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["YoutubeRag.Api/YoutubeRag.Api.csproj", "YoutubeRag.Api/"]
COPY ["YoutubeRag.Domain/YoutubeRag.Domain.csproj", "YoutubeRag.Domain/"]
COPY ["YoutubeRag.Application/YoutubeRag.Application.csproj", "YoutubeRag.Application/"]
COPY ["YoutubeRag.Infrastructure/YoutubeRag.Infrastructure.csproj", "YoutubeRag.Infrastructure/"]
RUN dotnet restore "YoutubeRag.Api/YoutubeRag.Api.csproj"

# Copy all files and build
COPY . .
WORKDIR "/src/YoutubeRag.Api"
RUN dotnet build "YoutubeRag.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "YoutubeRag.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install Python and dependencies for local Whisper support
RUN apt-get update && \
    apt-get install -y \
    python3 \
    python3-pip \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# Install Whisper
RUN pip3 install --no-cache-dir openai-whisper

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published files
COPY --from=publish /app/publish .

# Set proper permissions
RUN chown -R appuser:appuser /app

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Entry point
ENTRYPOINT ["dotnet", "YoutubeRag.Api.dll"]