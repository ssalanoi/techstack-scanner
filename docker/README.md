# Docker Configuration for Ollama

## What's Here

This folder contains Docker configuration **only for Ollama**. API and Web applications run locally.

## Files

- `docker-compose.yml` - Docker Compose configuration for Ollama
- `init-ollama.sh` - script for automatic llama3.2 model loading
- `.env` - environment variables (optional)

## Quick Start

### 1. Start Ollama
```powershell
docker compose up -d
```

### 2. Pull llama3.2 model
```powershell
docker compose exec ollama ollama pull llama3.2
```

### 3. Check status
```powershell
# List containers
docker compose ps

# List models
docker compose exec ollama ollama list
```

## Using with Local Project

After starting Ollama in Docker, ensure the API environment variables are set to:

```
OLLAMA_HOST=http://localhost:11434
OLLAMA_MODEL=llama3.2
```

By default, these values are already configured in `appsettings.json`.

## Useful Commands

```powershell
# Stop Ollama
docker compose down

# View logs
docker compose logs -f

# Restart
docker compose restart

# Remove container and data
docker compose down -v
```

## Detailed Documentation

See [../DOCKER.md](../DOCKER.md) for full documentation.
