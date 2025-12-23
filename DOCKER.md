# 🐳 Docker Deployment Guide

## Quick Start

### Prerequisites
- Docker Desktop installed and running
- Docker Compose v2.0+
- At least 8GB RAM available for containers
- 10GB free disk space (for Ollama models)

### Setup Steps

1. **Create environment file**
   ```powershell
   # Copy example to .env
   Copy-Item .env.example .env
   
   # Edit .env and set secure values
   # IMPORTANT: Change JWT_SECRET and ADMIN_PASSWORD!
   notepad .env
   ```

2. **Build and start all services**
   ```powershell
   cd docker
   docker compose up --build -d
   ```

3. **Initialize Ollama (first time only)**
   ```powershell
   # Pull the LLM model
   docker compose exec ollama ollama pull llama3.2
   
   # Or use the init script
   docker compose exec ollama sh /scripts/init-ollama.sh
   ```

4. **Verify services are running**
   ```powershell
   # Check status
   docker compose ps
   
   # View logs
   docker compose logs -f
   
   # Check individual services
   docker compose logs api
   docker compose logs web
   docker compose logs ollama
   ```

5. **Access the application**
   - **Web UI**: http://localhost:3000
   - **API**: http://localhost:5000
   - **Ollama API**: http://localhost:11434

## Service Details

### Ollama (LLM Service)
- **Port**: 11434
- **Volume**: `ollama-data` (persists models)
- **Model**: llama3.2 (configurable via OLLAMA_MODEL env var)
- **Memory**: Requires ~4GB RAM for 7B models

### API (ASP.NET Core)
- **Port**: 5000
- **Database**: SQLite in `/app/data` (mounted volume)
- **Dependencies**: Ollama service
- **Health Check**: http://localhost:5000/health

### Web (React + Nginx)
- **Port**: 3000 (nginx on 8080 internally)
- **Dependencies**: API service
- **Health Check**: http://localhost:3000/health

## Common Commands

### Starting Services
```powershell
# Start all services
docker compose up -d

# Start specific service
docker compose up -d api

# Start with logs
docker compose up
```

### Stopping Services
```powershell
# Stop all services
docker compose down

# Stop and remove volumes (clean slate)
docker compose down -v

# Stop specific service
docker compose stop api
```

### Viewing Logs
```powershell
# All services
docker compose logs -f

# Specific service
docker compose logs -f api

# Last 100 lines
docker compose logs --tail=100 api
```

### Rebuilding
```powershell
# Rebuild all services
docker compose build --no-cache

# Rebuild specific service
docker compose build --no-cache api

# Rebuild and restart
docker compose up --build -d
```

### Database Management
```powershell
# Backup database
Copy-Item apps\api\App_Data\scan.db backups\scan_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').db

# Restore database (stop services first)
docker compose down
Copy-Item backups\scan_backup.db apps\api\App_Data\scan.db
docker compose up -d
```

### Ollama Model Management
```powershell
# List installed models
docker compose exec ollama ollama list

# Pull a different model
docker compose exec ollama ollama pull mistral

# Remove a model
docker compose exec ollama ollama rm llama3.2

# Test generation
docker compose exec ollama ollama run llama3.2 "Hello, test message"
```

## Troubleshooting

### Service Won't Start
```powershell
# Check logs for errors
docker compose logs api

# Verify environment variables
docker compose config

# Check disk space
docker system df

# Clean up unused resources
docker system prune -a
```

### Ollama Model Not Loading
```powershell
# Check if model exists
docker compose exec ollama ollama list

# Pull model manually
docker compose exec ollama ollama pull llama3.2

# Check Ollama service health
curl http://localhost:11434/api/tags
```

### API Can't Connect to Database
```powershell
# Check volume mount
docker compose exec api ls -la /app/data

# Verify permissions
docker compose exec api stat /app/data/scan.db

# Restart API service
docker compose restart api
```

### Port Already in Use
```powershell
# Check what's using the port
netstat -ano | findstr :5000

# Kill the process or change port in docker-compose.yml
# Edit ports section: "5001:5000" instead of "5000:5000"
```

## Production Considerations

### Security
1. **Change default credentials** in `.env`
2. **Use strong JWT_SECRET** (minimum 32 characters)
3. **Enable HTTPS** with reverse proxy (nginx/Caddy)
4. **Restrict network access** to necessary ports only
5. **Regular updates** of base images

### Performance
1. **Resource limits** - Add to docker-compose.yml:
   ```yaml
   deploy:
     resources:
       limits:
         cpus: '2'
         memory: 4G
   ```

2. **Database optimization** - Consider PostgreSQL for production
3. **Caching** - Add Redis for API response caching
4. **Load balancing** - Use multiple API replicas

### Monitoring
```powershell
# Resource usage
docker stats

# Container health
docker compose ps

# Disk usage
docker system df -v
```

### Backup Strategy
```powershell
# Automated backup script (run daily)
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupDir = "backups\$timestamp"
New-Item -ItemType Directory -Path $backupDir -Force

# Backup database
Copy-Item apps\api\App_Data\scan.db "$backupDir\scan.db"

# Backup environment
Copy-Item .env "$backupDir\.env"

# Compress
Compress-Archive -Path $backupDir -DestinationPath "backups\backup_$timestamp.zip"
Remove-Item -Recurse $backupDir
```

## Development vs Production

### Development Mode
- Use `docker-compose.override.yml` for dev settings
- Mount source code for hot reload
- Detailed logging enabled
- Debug mode enabled

### Production Mode
- Remove override file
- Use optimized builds
- Minimal logging
- Health checks enabled
- Resource limits set

## Scaling

### Horizontal Scaling (Multiple API Instances)
```yaml
services:
  api:
    deploy:
      replicas: 3
    # Add load balancer in front
```

### Using Different Ollama Models
```powershell
# Edit .env
OLLAMA_MODEL=mistral  # or codellama, llama2, etc.

# Restart services
docker compose down && docker compose up -d

# Pull new model
docker compose exec ollama ollama pull mistral
```

## Clean Up

```powershell
# Stop and remove everything
docker compose down -v

# Remove images
docker compose down --rmi all

# Complete cleanup (careful!)
docker system prune -a --volumes
```

## Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Ollama Models](https://ollama.ai/library)
- [ASP.NET Core Docker](https://docs.microsoft.com/aspnet/core/host-and-deploy/docker/)
- [Nginx Docker](https://hub.docker.com/_/nginx)

---

**Note**: Always test thoroughly in a staging environment before deploying to production!
