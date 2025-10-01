# Requerimientos Innegociables del Sistema YouTube RAG .NET

## 🔧 Infraestructura Base Requerida

### Docker en WSL (NO Docker Desktop)
- **CRÍTICO**: Docker debe estar instalado y corriendo DENTRO de WSL2
- **NO usar Docker Desktop para Windows**
- **Razón**: Containerización independiente del SO, portabilidad completa
- **Versión mínima**: Docker 20.10+ en WSL2

### MySQL Database
- **OBLIGATORIO**: MySQL corriendo en Docker dentro de WSL
- **Puerto**: 3306
- **Acceso**: Debe ser accesible desde Windows host
- **Base de datos**: `youtube_rag_local`
- **Usuario**: `youtube_rag_user`
- **Configuración**: Persistencia de datos habilitada

### Redis Cache
- **OBLIGATORIO**: Redis corriendo en Docker dentro de WSL
- **Puerto**: 6379
- **Acceso**: Debe ser accesible desde Windows host
- **Configuración**: Para cache y sesiones

### Local Whisper
- **OBLIGATORIO**: Whisper instalado localmente para transcripción
- **Versión**: Compatible con comando `whisper`
- **Modelos**: Base model como mínimo

## 🐳 Configuración Docker en WSL

### Instalación de Docker en WSL2

#### 1. Instalar WSL2 (si no está instalado)
```powershell
# En PowerShell como Administrador
wsl --install
wsl --set-default-version 2

# Instalar Ubuntu (o tu distro preferida)
wsl --install -d Ubuntu
```

#### 2. Instalar Docker dentro de WSL
```bash
# Dentro de WSL Ubuntu
# Actualizar paquetes
sudo apt update && sudo apt upgrade -y

# Instalar dependencias
sudo apt install -y ca-certificates curl gnupg lsb-release

# Agregar repositorio de Docker
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Instalar Docker Engine
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Agregar usuario al grupo docker
sudo usermod -aG docker $USER

# Iniciar servicio Docker
sudo service docker start

# Verificar instalación
docker --version
docker-compose --version
```

### Comandos de Verificación y Configuración

#### 1. Verificar Docker en WSL desde Windows
```powershell
# Desde PowerShell en Windows
# Verificar que WSL tiene Docker
wsl docker --version
wsl docker-compose --version

# Verificar servicio Docker
wsl sudo service docker status

# Iniciar Docker si no está corriendo
wsl sudo service docker start

# Verificar contenedores corriendo
wsl docker ps

# Verificar MySQL específicamente
wsl docker ps | grep mysql

# Verificar Redis específicamente
wsl docker ps | grep redis
```

#### 2. Iniciar servicios si no están corriendo (desde Windows)
```powershell
# Desde PowerShell en Windows
# MySQL
wsl docker run -d `
  --name mysql-local `
  -e MYSQL_ROOT_PASSWORD=rootpassword `
  -e MYSQL_DATABASE=youtube_rag_local `
  -e MYSQL_USER=youtube_rag_user `
  -e MYSQL_PASSWORD=youtube_rag_password `
  -p 3306:3306 `
  mysql:8.0

# Redis
wsl docker run -d `
  --name redis-local `
  -p 6379:6379 `
  redis:alpine

# O usando docker-compose
wsl docker-compose up -d
```

#### 3. Gestión del servicio Docker en WSL
```powershell
# Iniciar Docker automáticamente en WSL
wsl sudo service docker start

# Para hacer que Docker inicie automáticamente
# Agregar esto a ~/.bashrc en WSL:
wsl -e bash -c "echo 'sudo service docker start' >> ~/.bashrc"
```

#### 4. Verificar conectividad desde Windows
```powershell
# MySQL
Test-NetConnection -ComputerName localhost -Port 3306

# Redis
Test-NetConnection -ComputerName localhost -Port 6379
```

### Cadenas de Conexión
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=youtube_rag_local;Uid=youtube_rag_user;Pwd=youtube_rag_password;",
    "Redis": "localhost:6379"
  }
}
```

## 📋 Checklist de Instalación

- [ ] WSL2 instalado y configurado
- [ ] Docker instalado DENTRO de WSL (NO Docker Desktop)
- [ ] Servicio Docker iniciado en WSL (`wsl sudo service docker start`)
- [ ] Docker-compose instalado en WSL
- [ ] MySQL container corriendo en Docker/WSL
- [ ] Redis container corriendo en Docker/WSL
- [ ] Puertos 3306 y 6379 accesibles desde Windows host
- [ ] Base de datos y usuario MySQL creados
- [ ] Whisper instalado localmente
- [ ] .NET 8 SDK instalado en Windows

## ⚠️ Notas Importantes

- **NO usar Docker Desktop**: Docker debe estar instalado dentro de WSL2
- **NO usar servicios en la nube**: Todo debe correr localmente
- **NO usar OpenAI API**: Usar Whisper local y embeddings locales
- **NO usar bases de datos en memoria**: MySQL es obligatorio para persistencia
- **Configuración Local**: El sistema debe funcionar completamente offline
- **Comandos Docker**: Todos los comandos docker desde Windows deben usar prefijo `wsl`

## 🔄 Proceso de Desarrollo

1. Verificar que MySQL y Redis estén corriendo en Docker/WSL
2. Configurar cadenas de conexión apuntando a localhost
3. Ejecutar aplicación en modo Local con StorageMode="Database"
4. Validar funcionamiento completo del pipeline de procesamiento