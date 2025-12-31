# 🐳 Docker for Ollama

## Overview

Docker is used only for running Ollama with the llama3.2 model. The project (API and Web) runs locally and connects to Ollama in the container.

## Quick Start

### Prerequisites
- Docker Desktop installed and running
- Docker Compose v2.0+
- Minimum 4GB RAM for Ollama
- 5GB free disk space for llama3.2 model

### Starting Ollama

1. **Start Ollama container**
   ```powershell
   cd docker
   docker compose up -d
   ```

2. **Pull llama3.2 model (first time only)**
   ```powershell
   # Option 1: Using initialization script
   docker compose exec ollama sh /scripts/init-ollama.sh
   
   # Option 2: Directly
   docker compose exec ollama ollama pull llama3.2
   ```

3. **Check status**
   ```powershell
   # Check container is running
   docker compose ps
   
   # Check logs
   docker compose logs -f
   
   # Check downloaded models
   docker compose exec ollama ollama list
   ```

4. **Configure local project**
   
   Ensure the API environment variables point to the correct Ollama host:
   ```
   OLLAMA_HOST=http://localhost:11434
   OLLAMA_MODEL=llama3.2
   ```

## Service Information

### Ollama
- **Port**: 11434 (accessible on localhost)
- **Model**: llama3.2
- **Volume**: `ollama-data` (persists models across restarts)
- **Memory**: Requires ~4GB RAM
- **API**: http://localhost:11434

## Common Commands

### Container Management
```powershell
# Start Ollama
docker compose up -d

# Stop Ollama
docker compose down

# View logs
docker compose logs -f ollama

# Restart
docker compose restart ollama
```

### Model Management
```powershell
# List models
docker compose exec ollama ollama list

# Pull model
docker compose exec ollama ollama pull llama3.2

# Remove model
docker compose exec ollama ollama rm llama3.2

# Test model
docker compose exec ollama ollama run llama3.2 "Hello"
```

### Cleanup
```powershell
# Stop and remove container
docker compose down

# Remove container and volumes (will delete downloaded models!)
docker compose down -v
```

## Troubleshooting

### Ollama Model Not Loading
```powershell
# Check if model exists
docker compose exec ollama ollama list

# Pull model manually
docker compose exec ollama ollama pull llama3.2

# Check Ollama service health
curl http://localhost:11434/api/tags
```

### Container Won't Start
```powershell
# Check logs for errors
docker compose logs ollama

# Check disk space
docker system df

# Clean up unused resources
docker system prune -a
```

## Using Different Ollama Models

```powershell
# Pull a different model
docker compose exec ollama ollama pull mistral

# List available models
docker compose exec ollama ollama list

# Test the model
docker compose exec ollama ollama run mistral "Hello"
```

**Note:** Update `OLLAMA_MODEL` environment variable in your API configuration (`appsettings.json`) to match the model you want to use.

## Resource Limits (Optional)

To limit Ollama resource usage, add to [docker-compose.yml](docker/docker-compose.yml):

```yaml
services:
  ollama:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 4G
```

## Clean Up

```powershell
# Stop and remove container
docker compose down

# Remove container and volumes (deletes models!)
docker compose down -v

# Complete cleanup
docker system prune -a --volumes
```

## Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Ollama Models Library](https://ollama.ai/library)
- [Ollama Documentation](https://github.com/ollama/ollama/blob/main/docs/api.md)
- [Ollama GitHub](https://github.com/ollama/ollama)

---

**Note**: This Docker setup is for **Ollama only**. The API and Web applications run locally for development. See [QUICK_START.md](QUICK_START.md) for complete setup instructions.
