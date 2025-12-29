# Quick Start Guide - TechStack Scanner

## 🚀 Instant Development Setup

### Prerequisites
- .NET 10 SDK
- Node.js 18+ with pnpm
- Git configured

### 1️⃣ Start Backend API
```powershell
cd apps/api
dotnet run --urls http://localhost:5000
```

**That's it!** No environment variables needed. The console will show:
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
cd apps/web
pnpm dev
```

Open http://localhost:5173

### 3️⃣ Login
- Email: `admin@techstack.local`
- Password: `ChangeMe123!`

---

## 🔧 Configure Git (First Time Only)

If you see a Git warning:
```powershell
git config --global user.name "Your Name"
git config --global user.email "your@email.com"
```

---

## 📦 Optional: Ollama for AI Insights

### Option 1: Local Ollama
Verify Ollama is running (usually starts automatically on Windows):
```powershell
curl http://localhost:11434/api/tags
```

If not running:
```powershell
ollama serve
```

Pull the model:
```powershell
ollama pull llama3.2
```

### Option 2: Ollama in Docker (recommended)
```powershell
# Start Ollama container
cd docker
docker compose up -d

# Pull llama3.2 model (first time only)
docker compose exec ollama ollama pull llama3.2
```

After this, API will automatically connect to Ollama at http://localhost:11434

See [DOCKER.md](DOCKER.md) for more details

---

## 🧪 Run Tests
```powershell
dotnet test apps/api.Tests/api.Tests.csproj
```

Expected: **38 tests, all passing**

---

## 📝 Override Defaults (Optional)

Set environment variables:
```powershell
$env:JWT_SECRET = 'your-custom-secret-32-characters-min'
$env:ADMIN_EMAIL = 'custom@example.com'
$env:ADMIN_PASSWORD = 'CustomPassword123!'
dotnet run --urls http://localhost:5000
```

Or use user secrets (persists):
```powershell
cd apps/api
dotnet user-secrets set "ADMIN_EMAIL" "your@email.com"
dotnet user-secrets set "ADMIN_PASSWORD" "YourPassword123!"
```

---

## 🎯 Common Tasks

### Scan a Project
1. Login at http://localhost:5173/login
2. Go to Admin page
3. Click "New Scan"
4. Enter project path and name
5. Click "Start Scan"

### View Results
- **Dashboard**: Technology overview with charts
- **Projects**: List of all scanned projects  
- **Project Details**: Click to see findings and AI insights

### Check Outdated Dependencies
Automatically checked during scans. Results show:
- `IsOutdated` flag on findings
- Latest version when available

---

## 🆘 Troubleshooting

### Port Already in Use
```powershell
netstat -ano | findstr :5000
taskkill /F /PID <PID>
```

### Can't Login
- Check console for credentials
- Use default: `ChangeMe123!`
- Check browser console (F12)

### Scans Failing
- Check logs: `apps/api/Logs/`
- Verify path exists
- Check console output

---

## 📚 Documentation

- [E2E Verification](E2E_VERIFICATION.md)
- [Docker Guide](docker/DOCKER.md)
- [AI Instructions](.github/copilot-instructions.md)

---

**Ready to scan!** 🎉
