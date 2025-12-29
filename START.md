# 🚀 Starting Project with Ollama in Docker

## Steps to Launch

### 1. Start Ollama in Docker
```powershell
cd docker
docker compose up -d
```

### 2. Pull llama3.2 model (first time only)
```powershell
docker compose exec ollama ollama pull llama3.2
```

### 3. Start API (locally)
```powershell
# From project root
cd apps/api
dotnet run --urls http://localhost:5000
```

### 4. Start Frontend (locally)
```powershell
# From project root
cd apps/web
pnpm dev
```

### 5. Open application
Open http://localhost:5173 in your browser

## Check Ollama Status

```powershell
# Check container is running
docker ps

# Check models
docker compose exec ollama ollama list

# Check Ollama API
curl http://localhost:11434/api/tags
```

## Default Login
- Email: `admin@techstack.local`
- Password: `ChangeMe123!`

## Stopping

```powershell
# Stop Ollama
cd docker
docker compose down

# Stop API and Frontend - just close terminals or press Ctrl+C
```

## Project Structure

- **Docker (port 11434)**: Ollama with llama3.2 model
- **API (port 5000)**: ASP.NET Core API - runs locally
- **Frontend (port 5173)**: React application - runs locally

API automatically connects to Ollama at http://localhost:11434 according to settings in `apps/api/appsettings.json`.
