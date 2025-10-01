# WSL Docker Setup Guide for YoutubeRag.NET

## Why WSL Docker Instead of Docker Desktop?

### Key Benefits
1. **OS Independence**: True containerization that's platform-agnostic
2. **Full Portability**: Containers run identically across all environments
3. **No License Issues**: Docker Desktop has licensing restrictions for enterprises
4. **Better Performance**: Direct Linux kernel access without virtualization overhead
5. **Resource Efficiency**: Lower memory and CPU usage
6. **Native Linux Experience**: Full Linux Docker ecosystem

### Architecture Overview
```
Windows Host
    │
    ├── .NET Application (Windows)
    │   └── Connects to localhost:3306, localhost:6379
    │
    └── WSL2 (Ubuntu)
        └── Docker Engine (Native Linux)
            ├── MySQL Container (port 3306)
            └── Redis Container (port 6379)
```

## Complete WSL Docker Installation

### Step 1: Install WSL2

```powershell
# Run PowerShell as Administrator

# Install WSL2
wsl --install

# Set WSL2 as default
wsl --set-default-version 2

# Install Ubuntu (recommended)
wsl --install -d Ubuntu

# Verify installation
wsl --list --verbose
```

### Step 2: Configure WSL

Create or edit `%USERPROFILE%\.wslconfig`:

```ini
[wsl2]
memory=4GB
processors=2
localhostForwarding=true
nestedVirtualization=true

[experimental]
autoMemoryReclaim=gradual
sparseVhd=true
```

### Step 3: Install Docker Inside WSL

```bash
# Enter WSL
wsl

# Update packages
sudo apt update && sudo apt upgrade -y

# Install prerequisites
sudo apt install -y \
    ca-certificates \
    curl \
    gnupg \
    lsb-release

# Add Docker's official GPG key
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

# Set up stable repository
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker Engine
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Add user to docker group
sudo usermod -aG docker $USER

# Start Docker service
sudo service docker start

# Verify installation
docker --version
docker compose version
```

### Step 4: Configure Docker to Start Automatically

```bash
# Edit bashrc to start Docker on WSL launch
echo '# Start Docker daemon automatically when logging in if not running
if [ -z "$(pgrep dockerd)" ]; then
    sudo service docker start
fi' >> ~/.bashrc

# Alternative: Use systemd (if enabled in WSL)
sudo systemctl enable docker
```

### Step 5: Install docker-compose (Standalone)

```bash
# Download docker-compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose

# Make executable
sudo chmod +x /usr/local/bin/docker-compose

# Verify
docker-compose --version
```

## Port Forwarding Configuration

### Automatic Port Forwarding
WSL2 automatically forwards ports from WSL to Windows host. Verify with:

```powershell
# From Windows PowerShell
Test-NetConnection -ComputerName localhost -Port 3306
Test-NetConnection -ComputerName localhost -Port 6379
```

### Manual Port Forwarding (if needed)
```powershell
# Get WSL IP
$wslIp = (wsl hostname -I).Trim()

# Forward ports
netsh interface portproxy add v4tov4 listenport=3306 listenaddress=0.0.0.0 connectport=3306 connectaddress=$wslIp
netsh interface portproxy add v4tov4 listenport=6379 listenaddress=0.0.0.0 connectport=6379 connectaddress=$wslIp

# View forwarded ports
netsh interface portproxy show all
```

## Network Troubleshooting

### Common Issues and Solutions

#### Issue: Cannot connect to services from Windows

**Solution 1: Check Docker binding**
```bash
# In WSL, ensure containers bind to 0.0.0.0
docker run -p 0.0.0.0:3306:3306 mysql
```

**Solution 2: Windows Firewall**
```powershell
# Add firewall rules
New-NetFirewallRule -DisplayName "WSL MySQL" -Direction Inbound -LocalPort 3306 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "WSL Redis" -Direction Inbound -LocalPort 6379 -Protocol TCP -Action Allow
```

**Solution 3: WSL network reset**
```powershell
wsl --shutdown
netsh winsock reset
netsh int ip reset
```

#### Issue: Docker service not starting

**Solution:**
```bash
# Check Docker status
sudo service docker status

# View logs
sudo journalctl -xe | grep docker

# Reset Docker
sudo rm -rf /var/lib/docker
sudo service docker start
```

#### Issue: Permission denied errors

**Solution:**
```bash
# Ensure user is in docker group
sudo usermod -aG docker $USER

# Logout and login again
exit
wsl

# Verify
groups
```

## Performance Optimization

### WSL Settings
Edit `%USERPROFILE%\.wslconfig`:

```ini
[wsl2]
memory=8GB              # Adjust based on available RAM
processors=4            # Adjust based on CPU cores
swap=2GB
swapFile=C:\\temp\\wsl-swap.vhd
localhostForwarding=true
```

### Docker Settings
Create `/etc/docker/daemon.json` in WSL:

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  },
  "storage-driver": "overlay2",
  "features": {
    "buildkit": true
  }
}
```

### Disk Space Management
```bash
# Check disk usage
docker system df

# Clean up unused resources
docker system prune -a --volumes

# Compact WSL virtual disk (from Windows)
wsl --shutdown
diskpart
select vdisk file="C:\Users\[username]\AppData\Local\Packages\[distro]\LocalState\ext4.vhdx"
compact vdisk
```

## Common Commands Reference

### From Windows PowerShell
```powershell
# Start Docker in WSL
wsl sudo service docker start

# Run Docker commands
wsl docker ps
wsl docker-compose up -d
wsl docker logs container_name

# Execute in container
wsl docker exec -it mysql-local mysql -u root -p

# Stop services
wsl docker-compose down

# View logs
wsl docker-compose logs -f
```

### From Within WSL
```bash
# Start Docker service
sudo service docker start

# Docker commands
docker ps
docker-compose up -d
docker logs container_name

# Execute in container
docker exec -it mysql-local mysql -u root -p

# Stop services
docker-compose down

# View logs
docker-compose logs -f
```

## Verification Checklist

After installation, verify everything works:

- [ ] WSL2 is installed: `wsl --status`
- [ ] Ubuntu is default distro: `wsl --list`
- [ ] Docker is installed in WSL: `wsl docker --version`
- [ ] Docker service starts: `wsl sudo service docker start`
- [ ] Can run containers: `wsl docker run hello-world`
- [ ] Ports are accessible from Windows: `Test-NetConnection localhost -Port 3306`
- [ ] docker-compose works: `wsl docker-compose --version`
- [ ] User is in docker group: `wsl groups`

## Troubleshooting Guide

### Reset Everything
```powershell
# Complete reset (WARNING: Deletes all WSL data)
wsl --unregister Ubuntu
wsl --install -d Ubuntu
# Then follow installation steps again
```

### Check WSL Version
```powershell
wsl --status
wsl --list --verbose
```

### Update WSL
```powershell
wsl --update
wsl --shutdown
```

### Docker Daemon Not Starting
```bash
# In WSL
sudo dockerd --debug

# Check for errors and fix accordingly
```

### Network Issues
```powershell
# Reset network stack
netsh winsock reset
netsh int ip reset
ipconfig /flushdns
```

## Security Considerations

1. **No Root Passwords**: Never run containers with hardcoded root passwords in production
2. **Firewall Rules**: Only open necessary ports
3. **Resource Limits**: Set memory and CPU limits for containers
4. **Regular Updates**: Keep WSL and Docker updated
5. **Secrets Management**: Use Docker secrets or environment files

## Best Practices

1. **Always use WSL2**: Better performance than WSL1
2. **Regular Cleanup**: Run `docker system prune` weekly
3. **Monitor Resources**: Check `docker stats` regularly
4. **Backup Important Data**: WSL VHD can be backed up
5. **Document Custom Configurations**: Keep track of any custom settings

## Additional Resources

- [WSL Documentation](https://docs.microsoft.com/en-us/windows/wsl/)
- [Docker in WSL2](https://docs.docker.com/desktop/windows/wsl/)
- [WSL2 Networking](https://docs.microsoft.com/en-us/windows/wsl/networking)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)

## Support

If you encounter issues:

1. Check this guide's troubleshooting section
2. Review Docker logs: `wsl docker logs [container]`
3. Check WSL logs: `wsl dmesg`
4. Verify network connectivity
5. Ensure all prerequisites are met

Remember: **We're using Docker INSIDE WSL, not Docker Desktop!**