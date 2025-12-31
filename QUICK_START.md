# Quick Start Guide - TechStack Scanner

## 🚀 Instant Development Setup

### Prerequisites
- **.NET SDK 10.0+** - [Download](https://dotnet.microsoft.com/download)
- **Node.js 18+** - [Download](https://nodejs.org/)
- **pnpm 9+** - Install: `npm install -g pnpm`
- **Git** - [Download](https://git-scm.com/)
- **Docker Desktop** (optional, for Ollama) - [Download](https://www.docker.com/products/docker-desktop/)

---

## 🎯 Quick Start (3 Steps)

### 1️⃣ Start Backend API
```powershell
cd apps/api
dotnet run --urls http://localhost:5000
```

✅ **No environment variables needed!** The console will show:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔐 DEVELOPMENT MODE - Default Credentials Loaded
   Email: admin@techstack.local
   Password: ChangeMe123!
   ⚠️  Change these in production!
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 2️⃣ Start Frontend
```powershell
# In a new terminal
cd apps/web
pnpm install  # First time only
pnpm dev
```

### 3️⃣ Open & Login
- Open **http://localhost:5173** in your browser
- Email: `admin@techstack.local`
- Password: `ChangeMe123!`

🎉 **You're ready to scan projects!**

---

## 📦 Optional: Ollama for AI Insights

### Option 1: Ollama in Docker (Recommended)

**Start Ollama container:**
```powershell
cd docker
docker compose up -d
```

**Pull llama3.2 model (first time only):**
```powershell
docker compose exec ollama ollama pull llama3.2
```

**Check status:**
```powershell
# Verify container is running
docker compose ps

# Check models
docker compose exec ollama ollama list

# Test Ollama API
curl http://localhost:11434/api/tags
```

### Option 2: Local Ollama

**Start Ollama:**
```powershell
ollama serve
```

**Pull model:**
```powershell
ollama pull llama3.2
```

**Verify it's running:**
```powershell
curl http://localhost:11434/api/tags
```

> **Note:** After Ollama is running, API will automatically connect to it at `http://localhost:11434` (configured in `appsettings.json`)

See [DOCKER.md](DOCKER.md) for detailed Ollama documentation.

---

## 🎯 Common Tasks

### Scan a Project
1. Login at http://localhost:5173/login
2. Go to **Admin** page
3. Click **"New Scan"** button
4. Enter:
   - **Project Name** - Display name
   - **Root Path** - Absolute path to project directory (e.g., `C:\Projects\my-project`)
   - **Generate LLM Summary** - Enable for AI insights (requires Ollama)
5. Click **"Start Scan"**
6. Wait for completion (status auto-refreshes)

### View Results
- **Dashboard** (`/`) - Technology overview with charts
- **Projects** (`/projects`) - List of all scanned projects  
- **Project Details** (`/projects/:id`) - Click any project to see:
  - Technology findings (name, version, category)
  - Outdated dependencies (highlighted in orange)
  - AI summary (if generated)
  - Scan metadata

### Check Outdated Dependencies
- Automatically checked during scans
- Results show `IsOutdated` flag and `LatestVersion`
- Re-check: Click **"Check for Outdated Dependencies"** button on project details

---

## 🧪 Run Tests
```powershell
# Backend tests (38 tests)
dotnet test apps/api.Tests/api.Tests.csproj

# Frontend tests
cd apps/web
pnpm test

# With coverage
dotnet test apps/api.Tests/api.Tests.csproj --collect:"XPlat Code Coverage"
```

Expected: **38 tests, all passing** ✅

---

## 🔧 Configuration

### Configure Git (First Time Only)
If you see a Git warning:
```powershell
git config --global user.name "Your Name"
git config --global user.email "your@email.com"
```

### Override Defaults (Optional)

**Environment variables:**
```powershell
$env:JWT_SECRET = 'your-custom-secret-32-characters-min'
$env:ADMIN_EMAIL = 'custom@example.com'
$env:ADMIN_PASSWORD = 'CustomPassword123!'
$env:OLLAMA_HOST = 'http://localhost:11434'
$env:OLLAMA_MODEL = 'llama3.2'
dotnet run --urls http://localhost:5000
```

**User secrets (persists across sessions):**
```powershell
cd apps/api
dotnet user-secrets set "JWT_SECRET" "your-secret-here"
dotnet user-secrets set "ADMIN_EMAIL" "your@email.com"
dotnet user-secrets set "ADMIN_PASSWORD" "YourPassword123!"
```

---

## 🛑 Stopping the Application

### Stop API
Press `Ctrl+C` in the terminal running `dotnet run`

### Stop Frontend
Press `Ctrl+C` in the terminal running `pnpm dev`

### Stop Ollama (Docker)
```powershell
cd docker
docker compose down

# Remove data (including downloaded models)
docker compose down -v
```

### Stop Ollama (Local)
Press `Ctrl+C` in the terminal running `ollama serve`

---

## 🆘 Troubleshooting

### Port Already in Use
```powershell
# Check what's using the port
netstat -ano | findstr :5000

# Kill the process
taskkill /F /PID <PID>
```

### Can't Login
- Check console output for displayed credentials
- Use default password: `ChangeMe123!`
- Clear browser cache and localStorage (F12 → Application → Local Storage)
- Check browser console (F12) for errors

### Scans Failing
- Check logs: `apps/api/Logs/log-YYYYMMDD.txt`
- Verify project path exists and is accessible
- Check console output for errors
- Ensure sufficient permissions to read directory

### Ollama Not Working
- Verify Ollama is running: `curl http://localhost:11434/api/tags`
- Check Docker container status: `docker compose ps`
- View logs: `docker compose logs ollama`
- Ensure model is pulled: `docker compose exec ollama ollama list`

### Database Issues
```powershell
# Delete database and restart (migrations will auto-apply)
Remove-Item apps\api\App_Data\scan.db
cd apps\api
dotnet run --urls http://localhost:5000
```

---

## 📚 Project Structure

```
techstack-scanner/
├── apps/
│   ├── api/              # ASP.NET Core 10 Backend (port 5000)
│   ├── web/              # React + Vite Frontend (port 5173)
│   └── api.Tests/        # xUnit test suite
├── packages/
│   └── shared/           # TypeScript shared types
└── docker/               # Docker configuration for Ollama (port 11434)
```

**Service Ports:**
- **Frontend:** http://localhost:5173
- **API:** http://localhost:5000
- **Ollama:** http://localhost:11434 (Docker container)

---

## 📖 Additional Documentation

- [README.md](README.md) - Complete project documentation
- [DOCKER.md](DOCKER.md) - Detailed Docker guide for Ollama
- [E2E_VERIFICATION.md](E2E_VERIFICATION.md) - Testing and verification guide
- [AI_DEVELOPMENT.md](AI_DEVELOPMENT.md) - AI-assisted development insights
- [.github/copilot-instructions.md](.github/copilot-instructions.md) - AI assistant guide

---

## 🎓 Tips

### Development Workflow
1. Keep API and Frontend running in separate terminals
2. Enable Ollama for AI-powered insights (optional but recommended)
3. Check logs in `apps/api/Logs/` if something goes wrong
4. Use browser DevTools (F12) to debug frontend issues

### Best Practices
- Scan smaller projects first to understand the results
- Use absolute paths when specifying project root
- Review generated AI summaries for accuracy
- Regularly check for outdated dependencies

### Performance
- Scans run in background (non-blocking)
- Multiple scans can be queued
- Outdated dependency checks are cached
- LLM generation has 120-second timeout

---

**Ready to scan!** 🚀

For detailed architecture, API documentation, and advanced features, see [README.md](README.md)
