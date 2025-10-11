# YoutubeRag.NET Development Container
# Based on official .NET SDK with additional development tools

FROM mcr.microsoft.com/dotnet/sdk:8.0

# Avoid warnings by switching to noninteractive
ENV DEBIAN_FRONTEND=noninteractive

# Configure apt and install packages
RUN apt-get update \
    && apt-get -y install --no-install-recommends \
    # Common utilities
    apt-utils \
    dialog \
    openssh-client \
    less \
    iproute2 \
    procps \
    curl \
    wget \
    unzip \
    nano \
    vim \
    jq \
    # Build tools
    build-essential \
    # Git
    git \
    git-lfs \
    # FFmpeg for audio extraction
    ffmpeg \
    # MySQL client
    default-mysql-client \
    # Redis client
    redis-tools \
    # Python (for scripts)
    python3 \
    python3-pip \
    # Node.js and npm (for some tools)
    nodejs \
    npm \
    # Clean up
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

# Install .NET global tools
RUN dotnet tool install --global dotnet-ef \
    && dotnet tool install --global dotnet-format \
    && dotnet tool install --global dotnet-reportgenerator-globaltool \
    && dotnet tool install --global dotnet-counters \
    && dotnet tool install --global dotnet-trace \
    && dotnet tool install --global dotnet-dump

# Add .NET tools to PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Install Oh My Zsh for better terminal experience (optional)
RUN sh -c "$(wget -O- https://raw.githubusercontent.com/ohmyzsh/ohmyzsh/master/tools/install.sh)" "" --unattended

# Set up command history persistence
RUN SNIPPET="export PROMPT_COMMAND='history -a' && export HISTFILE=/commandhistory/.bash_history" \
    && mkdir /commandhistory \
    && touch /commandhistory/.bash_history \
    && chown -R vscode /commandhistory \
    && echo $SNIPPET >> "/root/.bashrc"

# Create non-root user
ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=$USER_UID

RUN groupadd --gid $USER_GID $USERNAME \
    && useradd --uid $USER_UID --gid $USER_GID -m $USERNAME -s /bin/bash \
    && apt-get update \
    && apt-get install -y sudo \
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME \
    && chmod 0440 /etc/sudoers.d/$USERNAME

# Add .NET tools to vscode user PATH
RUN mkdir -p /home/vscode/.dotnet/tools \
    && chown -R vscode:vscode /home/vscode/.dotnet

ENV PATH="${PATH}:/home/vscode/.dotnet/tools"

# Switch back to dialog for any ad-hoc use of apt-get
ENV DEBIAN_FRONTEND=dialog

# Create workspace directory
RUN mkdir -p /workspace && chown -R vscode:vscode /workspace

# Create data directories for videos, audio, models
RUN mkdir -p /workspace/data/videos \
    && mkdir -p /workspace/data/audio \
    && mkdir -p /workspace/data/models \
    && mkdir -p /tmp/youtube-rag \
    && chown -R vscode:vscode /workspace/data \
    && chown -R vscode:vscode /tmp/youtube-rag

# Set the default user
USER vscode

# Install user-level .NET tools
RUN dotnet tool install --global dotnet-ef \
    && dotnet tool install --global dotnet-format \
    && dotnet tool install --global dotnet-reportgenerator-globaltool

# Set working directory
WORKDIR /workspace

# Default command
CMD ["/bin/bash"]
