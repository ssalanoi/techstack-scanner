# TechStack Scanner - Comprehensive Implementation Guide

**Purpose:** Complete guide for building a production-ready Codebase Technology Scanner using GitHub Copilot

---

## 📋 PROJECT OVERVIEW

### What We're Building

A full-stack application that scans local project folders, identifies all technologies/frameworks with versions, and provides AI-powered insights about the technology stack. The system detects outdated dependencies, security concerns, compatibility issues, and suggests upgrade recommendations using a local LLM.

### Key Features

- 🔍 **Multi-ecosystem scanning** - Detects technologies from: Node.js, .NET, Python, Ruby, Go, Java, Docker
- 🤖 **AI-powered insights** - Local LLM analysis for recommendations (no cloud dependencies)
- 📊 **Visual dashboard** - Aggregated statistics, version warnings, technology trends
- 📝 **Scan history** - Persistent storage with SQLite
- 🔐 **Admin area** - JWT-protected settings and management
- 🐳 **Fully containerized** - Docker Compose for entire stack including LLM

### Learning Goals

- Generate up to 100% of code using GitHub Copilot
- Document the AI-assisted development process
- Learn effective prompting patterns
- Build a production-ready monorepo application
- Integrate local LLM (Ollama) for intelligent features

---

## 🎯 TECHNOLOGY STACK

### Frontend (React Ecosystem)
- **Framework:** React 18+ with TypeScript
- **Build Tool:** Vite 6
- **Routing:** React Router 7
- **UI Library:** Mantine UI 7 (modern component library)
- **State Management:** TanStack Query v5 (React Query)
- **Testing:** Vitest + React Testing Library
- **Linting:** ESLint 9 (flat config) + Prettier + EditorConfig
- **Package Manager:** pnpm v9+

### Backend (ASP.NET Ecosystem)
- **Framework:** ASP.NET Core 9 Web API
- **Database:** Entity Framework Core 9 + SQLite
- **Logging:** Serilog (Console + File sinks)
- **Auth:** JWT Bearer tokens
- **Background Jobs:** System.Threading.Channels
- **Testing:** xUnit + FluentAssertions

### AI & LLM
- **Model Host:** Ollama (local model hosting)
- **Models:** llama3.2 or mistral (7B parameters)
- **Orchestration:** Direct HTTP API or LangChain/Semantic Kernel
- **Features:** Technology stack analysis, upgrade recommendations, security insights

### DevOps & Tooling
- **Containerization:** Docker + Docker Compose
- **Version Control:** Git + GitHub CLI (`gh`)
- **E2E Testing:** Playwright (optional)
- **Monorepo:** Manual structure (no Nx/Turborepo complexity)

---

## 📁 ARCHITECTURE (Monorepo Structure)

```
techstack-scanner/
├── apps/
│   ├── web/                    # React + TypeScript + Vite
│   │   ├── src/
│   │   │   ├── components/
│   │   │   │   ├── Scanner/         # Folder selection, scan controls
│   │   │   │   ├── Dashboard/       # Aggregated statistics, charts
│   │   │   │   ├── Projects/        # Project list, details
│   │   │   │   ├── Insights/        # AI-generated recommendations
│   │   │   │   └── Shared/          # Reusable components
│   │   │   ├── layouts/
│   │   │   │   ├── MainLayout.tsx   # Public pages layout
│   │   │   │   └── AdminLayout.tsx  # Protected admin layout
│   │   │   ├── pages/
│   │   │   │   ├── Dashboard.tsx    # Home page with stats
│   │   │   │   ├── Projects.tsx     # All scanned projects
│   │   │   │   ├── ProjectDetails.tsx # Single project details
│   │   │   │   ├── Login.tsx        # Authentication
│   │   │   │   └── Admin.tsx        # Admin settings
│   │   │   ├── services/
│   │   │   │   └── api.ts           # Axios wrapper, auth interceptors
│   │   │   ├── hooks/
│   │   │   │   └── queries.ts       # TanStack Query hooks
│   │   │   ├── types/               # TypeScript interfaces
│   │   │   └── App.tsx
│   │   ├── vite.config.ts
│   │   ├── vitest.config.ts
│   │   └── package.json
│   │
│   └── api/                    # ASP.NET Core Web API
│       ├── Controllers/
│       │   ├── AuthController.cs     # POST /auth/login
│       │   ├── ScanController.cs     # POST /scan, GET /scan/{id}/status
│       │   └── ProjectsController.cs # GET /projects, /projects/{id}, /technologies
│       ├── Services/
│       │   ├── ScanService.cs        # File parsing logic
│       │   ├── LlmService.cs         # Ollama integration
│       │   ├── QueueService.cs       # Background processing
│       │   └── JwtService.cs         # Token generation/validation
│       ├── Data/
│       │   ├── AppDbContext.cs       # EF Core context
│       │   └── Entities/             # Project, Scan, TechnologyFinding
│       ├── Models/
│       │   └── DTOs/                 # API contracts
│       ├── Migrations/
│       ├── App_Data/
│       │   └── scan.db              # SQLite database file
│       ├── Tests/
│       │   └── ScanServiceTests.cs  # xUnit tests
│       ├── Program.cs
│       ├── appsettings.json
│       └── api.csproj
│
├── packages/
│   └── shared/                 # Shared TypeScript types
│       ├── src/
│       │   └── types.ts        # API contract interfaces
│       ├── tsconfig.json
│       └── package.json
│
├── config/                     # Baseline configs
│   ├── eslint.config.js       # ESLint 9 flat config
│   ├── prettier.config.js
│   ├── tsconfig.base.json
│   └── .editorconfig
│
├── docker/
│   ├── Dockerfile.api
│   ├── Dockerfile.web
│   └── docker-compose.yml     # API + WEB + Ollama
│
├── .github/
│   └── workflows/
│       └── ci.yml             # Optional CI/CD
│
├── README.md                   # Complete documentation
├── PROMPTS_LOG.md             # Development process log
└── package.json               # Root workspace config
```

---

## 🔧 PREREQUISITES & SETUP

### Required Installations

#### Core Tools
- **VS Code** (latest) - [Download](https://code.visualstudio.com/)
- **Node.js** v20+ - [Download](https://nodejs.org/)
- **pnpm** v9+ - `npm install -g pnpm`
- **.NET SDK 9.0** - [Download](https://dotnet.microsoft.com/download)
- **Git** - [Download](https://git-scm.com/)
- **GitHub CLI** (`gh`) - [Download](https://cli.github.com/)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **Ollama** - [Download](https://ollama.ai/)

#### VS Code Extensions (Required)
- **GitHub Copilot** (`GitHub.copilot`)
- **GitHub Copilot Chat** (`GitHub.copilot-chat`)
- **C# Dev Kit** (`ms-dotnettools.csdevkit`)
- **ESLint** (`dbaeumer.vscode-eslint`)
- **Prettier** (`esbenp.prettier-vscode`)

#### VS Code Extensions (Recommended)
- **Thunder Client** (`rangav.vscode-thunder-client`) - API testing
- **GitLens** (`eamodio.gitlens`) - Git visualization
- **Docker** (`ms-azuretools.vscode-docker`)
- **SQLite Viewer** (`qwtel.sqlite-viewer`)

### Verification Commands

```powershell
# Verify installations
node --version          # Should be v20+
pnpm --version          # Should be v9+
dotnet --version        # Should be 9.0.x
git --version
gh --version
docker --version
ollama --version

# Setup Ollama
ollama pull llama3.2    # or: ollama pull mistral
ollama list             # Verify model downloaded
curl http://localhost:11434/api/tags  # Test API

# Authenticate GitHub CLI
gh auth login
gh auth status
```

### Pre-Flight Checklist

- [ ] All tools installed and verified
- [ ] VS Code extensions installed
- [ ] Ollama running with model downloaded
- [ ] GitHub CLI authenticated
- [ ] Docker Desktop running
- [ ] Created workspace folder: `C:\Work\Course\techstack-scanner`
- [ ] Decided on LLM model (llama3.2 recommended for balance)
- [ ] Ready to document each step in PROMPTS_LOG.md

---

## 🚀 IMPLEMENTATION ROADMAP

### Phase 1: Foundation (Day 1)

**Goal:** Set up monorepo structure, tooling, and initial configuration

#### Step 1.1 - Monorepo Scaffolding

**Prompt for Copilot:**
```
Create a monorepo structure for a TechStack Scanner application with:
- /apps/web: React 18+ TypeScript project using Vite 6
- /apps/api: ASP.NET Core 9 Web API project
- /packages/shared: Shared TypeScript types package
- /config: ESLint 9 flat config, Prettier, tsconfig.base.json, .editorconfig
- Root package.json with pnpm workspace configuration

Include:
- Vite 6 with React plugin
- React Router 7
- Mantine UI 7
- TanStack Query v5
- Vitest + React Testing Library
- ESLint 9 flat config with TypeScript support
- All necessary TypeScript configurations

Generate exact pnpm commands to install dependencies.
```

**Manual Commands:**
```powershell
cd C:\Work\Course
mkdir techstack-scanner
cd techstack-scanner

# Create basic folder structure
mkdir -p apps/web/src, apps/api, packages/shared/src, config, docker

# Initialize git
git init
git add .
git commit -m "chore: initial monorepo structure"

# Create GitHub repository
gh repo create codebase-technology-scanner --public --source=. --remote=origin
git push -u origin main
```

**Expected Files:**
- `package.json` (root workspace)
- `pnpm-workspace.yaml`
- `config/eslint.config.js` (ESLint 9 flat config)
- `config/prettier.config.js`
- `config/tsconfig.base.json`
- `config/.editorconfig`
- `apps/web/package.json` + `vite.config.ts` + `vitest.config.ts`
- `apps/api/api.csproj` + `Program.cs`
- `packages/shared/package.json` + `tsconfig.json`

**Acceptance:**
```powershell
pnpm install         # Should install all workspaces
dotnet build         # Should compile API project
pnpm -C apps/web dev # Should start Vite dev server
```

---

#### Step 1.2 - API Database & Entities

**Prompt for Copilot:**
```
Create Entity Framework Core 9 setup for ASP.NET Core 9 API with SQLite:

Entities:
1. Project (Id: Guid, Name: string, Path: string, LastScannedAt: DateTime, AiInsights: string)
2. Scan (Id: Guid, ProjectId: Guid, StartedAt: DateTime, FinishedAt: DateTime?, Status: enum[Pending,Running,Completed,Failed])
3. TechnologyFinding (Id: Guid, ScanId: Guid, Name: string, Version: string, SourceFile: string, Detector: string, IsOutdated: bool, LatestVersion: string?)

Configure:
- AppDbContext with DbSets
- SQLite connection string: "Data Source=App_Data/scan.db"
- Relationships: Project → Scans (1:many), Scan → TechnologyFindings (1:many)
- Add migrations and database creation on startup

Include appsettings.json with connection string and Serilog configuration.
```

**Manual Commands:**
```powershell
cd apps/api
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File

# Create migration
dotnet ef migrations add InitialCreate

# Apply migration (will create db on first run)
dotnet run
```

---

#### Step 1.3 - JWT Authentication Setup

**Prompt for Copilot:**
```
Add JWT authentication to ASP.NET Core 9 API:

Requirements:
- Environment variables: ADMIN_EMAIL, ADMIN_PASSWORD, JWT_SECRET
- POST /api/auth/login endpoint accepting {email, password}
- Validate credentials against env vars
- Generate JWT token with role "Admin", TTL 1 hour
- Configure JWT authentication middleware
- Add [Authorize(Roles = "Admin")] attribute support

Create:
- JwtService for token generation/validation
- AuthController with login endpoint
- LoginRequest/LoginResponse DTOs
- Configure authentication in Program.cs

Security:
- Hash password comparison
- Secure JWT settings (issuer, audience, signing key)
```

**Manual Commands:**
```powershell
cd apps/api
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt

# Add to appsettings.json or use dotnet user-secrets
dotnet user-secrets init
dotnet user-secrets set "ADMIN_EMAIL" "admin@techstack.local"
dotnet user-secrets set "ADMIN_PASSWORD" "Change@Me123!"
dotnet user-secrets set "JWT_SECRET" "your-256-bit-secret-key-here-min-32-chars"
```

---

### Phase 2: Core Scanning (Day 2)

**Goal:** Implement file scanning and technology detection

#### Step 2.1 - ScanService Implementation

**Prompt for Copilot:**
```
Create a comprehensive ScanService in C# that scans project folders and detects technologies:

Supported files:
- package.json → Node.js dependencies (npm/yarn/pnpm)
- *.csproj → .NET packages
- global.json → .NET SDK version
- requirements.txt, pyproject.toml → Python packages
- Gemfile, Gemfile.lock → Ruby gems
- go.mod, go.sum → Go modules
- pom.xml, build.gradle → Java dependencies
- Dockerfile → Docker base images
- docker-compose.yml → Services

For each technology finding, extract:
- Name, Version, SourceFile, Detector type

Features:
- Recursive directory scanning (configurable max depth)
- Path validation (prevent directory traversal)
- Regex/XML/JSON parsing as appropriate
- Error handling for corrupt files
- Return structured TechnologyFinding objects

Include xUnit tests for each parser method with sample file content.
```

**Test Data Setup:**
```powershell
# Create test fixtures folder
mkdir apps/api/Tests/Fixtures
# Copilot can generate sample package.json, .csproj, etc. for testing
```

---

#### Step 2.2 - Background Queue Service

**Prompt for Copilot:**
```
Implement background processing for scan operations using System.Threading.Channels:

Requirements:
- QueueService using Channel<ScanRequest> for queue
- Background worker (IHostedService) that processes queue
- Scan workflow:
  1. Receive scan request (projectId, path)
  2. Update Scan.Status = Running
  3. Call ScanService to detect technologies
  4. Save TechnologyFindings to database
  5. Call LlmService to generate insights
  6. Update Scan.Status = Completed
  7. Store AI insights in Project.AiInsights
- Concurrent scan limit (configurable, default 2)
- Graceful cancellation on shutdown
- Error handling with retry logic (max 3 retries)
- Logging for each step

Create:
- QueueService.cs
- ScanWorkerService.cs (BackgroundService)
- ScanRequest model
- Configure in Program.cs
```

---

#### Step 2.3 - Scan Controller & Endpoints

**Prompt for Copilot:**
```
Create REST API endpoints for scanning:

Endpoints:
1. POST /api/scan
   - Body: { projectName: string, path: string }
   - Validates path exists and is safe
   - Creates Project and Scan records
   - Enqueues scan operation
   - Returns 202 Accepted with { scanId: guid, status: "Pending" }

2. GET /api/scan/{scanId}/status
   - Returns { scanId, status, startedAt, finishedAt, progress, error }
   - Poll-able for UI progress tracking

3. GET /api/projects
   - Returns list of all projects with last scan date
   - Pagination support (skip, take)
   - Filter by name (optional query param)

4. GET /api/projects/{projectId}
   - Returns project details with latest scan findings
   - Include technology counts

5. GET /api/projects/{projectId}/insights
   - Returns AI-generated insights
   - 404 if not yet generated

6. GET /api/technologies
   - Aggregated technology statistics across all projects
   - Group by technology name
   - Show version distribution

All protected endpoints require [Authorize(Roles = "Admin")].
Add CORS for http://localhost:5173.
```

---

### Phase 3: LLM Integration (Day 3)

**Goal:** Integrate Ollama for AI-powered insights

#### Step 3.1 - Ollama Setup & Verification

**Manual Steps:**
```powershell
# Install Ollama (Windows)
winget install Ollama.Ollama

# Start Ollama service (runs automatically on Windows)
# Verify it's running
curl http://localhost:11434/api/tags

# Pull recommended model
ollama pull llama3.2
# Alternative: ollama pull mistral

# Test generation
ollama run llama3.2 "Hello, are you working?"
```

**Configuration:**
```json
// Add to appsettings.json
{
  "Ollama": {
    "Host": "http://localhost:11434",
    "Model": "llama3.2",
    "TimeoutSeconds": 60,
    "MaxTokens": 1000
  }
}
```

---

#### Step 3.2 - LlmService Implementation

**Prompt for Copilot:**
```
Create an LlmService in C# that interfaces with Ollama API for technology stack analysis:

Requirements:
- HTTP client to POST http://localhost:11434/api/generate
- Request format: { model: "llama3.2", prompt: "...", stream: false }
- Build prompt with technology findings data (JSON format)
- Prompt template requesting:
  * Outdated dependencies identification
  * Security vulnerability warnings
  * Compatibility issues between technologies
  * Recommended upgrade paths
  * Technology stack health score (1-10)
- Parse response and extract insights
- Timeout handling (60 seconds)
- Retry with exponential backoff (max 3 attempts)
- Fallback message if LLM unavailable
- Structured output (markdown formatted)

Configuration:
- OLLAMA_HOST env var (default: http://localhost:11434)
- OLLAMA_MODEL env var (default: llama3.2)
- Timeout configurable

Create:
- LlmService.cs with AnalyzeTechnologyStack method
- OllamaRequest/OllamaResponse models
- Unit tests with mocked HTTP responses
```

**Test Prompt Example:**
```
Given this technology stack:
- React 18.2.0
- TypeScript 5.3.0
- ASP.NET Core 9.0.0
- Entity Framework Core 9.0.0
- SQLite 3.45.0

Analyze and provide:
1. Outdated dependencies
2. Security concerns
3. Compatibility issues
4. Upgrade recommendations
5. Overall health score (1-10)
```

---

### Phase 4: Frontend Development (Day 4)

**Goal:** Build React UI with routing, layouts, and components

#### Step 4.1 - Routing & Layouts

**Prompt for Copilot:**
```
Set up React Router 7 with layouts and protected routes:

Routes:
- / → MainLayout → Dashboard page
- /projects → MainLayout → Projects list page
- /projects/:id → MainLayout → Project details page
- /login → Minimal layout → Login page
- /admin → AdminLayout → Admin settings page (protected)

Layouts:
1. MainLayout.tsx
   - Mantine AppShell with header, navbar, main
   - Navigation links (Home, Projects)
   - User menu with logout
   - Responsive design

2. AdminLayout.tsx
   - AppShell with sidebar
   - Admin navigation (Settings, Users, Logs)
   - Protected by JWT check
   - Redirect to /login if not authenticated

Features:
- Auth context/hook for JWT storage
- Protected route wrapper component
- Axios interceptor for Bearer token
- Token refresh logic (check expiry)
- Redirect after login to intended route

Use Mantine v7 components (AppShell, Header, Navbar, Container).
```

---

#### Step 4.2 - Dashboard Page

**Prompt for Copilot:**
```
Create a Dashboard page component with aggregated statistics:

Features:
- Statistics cards showing:
  * Total projects scanned
  * Total technologies detected
  * Outdated dependencies count
  * Last scan timestamp
- Technology distribution chart (pie/bar chart using Recharts)
- Recent scans table (last 5 scans with status)
- Quick scan button (opens modal)
- AI insights summary (if available)

Data fetching:
- Use TanStack Query v5 hooks
- GET /api/projects endpoint
- GET /api/technologies endpoint
- Auto-refresh every 30 seconds for scan status

UI:
- Mantine Grid for layout
- Cards, Table, Badge components
- Loading skeletons
- Error states with retry button
- Responsive design (mobile-friendly)

Create:
- Dashboard.tsx page component
- StatisticsCard.tsx component
- RecentScansTable.tsx component
- useDashboardData.ts query hook
```

---

#### Step 4.3 - Projects List & Details

**Prompt for Copilot:**
```
Create Projects list and detail pages:

1. Projects.tsx (List page)
   - Table with columns: Name, Path, Last Scanned, Technologies Count, Status
   - Search/filter by name
   - Sort by last scanned date
   - Pagination (20 per page)
   - "New Scan" button (opens modal)
   - Click row to navigate to details

2. ProjectDetails.tsx (Detail page)
   - Project header with name, path, last scanned date
   - Tabs:
     a) Technologies - Table with Name, Version, Source File, Status badge
     b) AI Insights - Markdown rendered insights with loading spinner
     c) Scan History - Timeline of all scans
   - "Rescan" button
   - "Delete Project" button (confirmation modal)

3. ScanModal.tsx
   - Input for project name
   - Input for folder path (text + browse button if possible)
   - Validation (required fields, path format)
   - Submit → POST /api/scan
   - Show scan ID and polling status
   - Close button

Data fetching:
- TanStack Query for projects list
- Query for single project details
- Mutation for triggering new scan
- Polling query for scan status (refetch interval based on status)

UI:
- Mantine Table, Tabs, Modal, TextInput, Button
- Badges for technology status (outdated = red, current = green)
- Syntax highlighting for AI insights markdown
- Loading states and error handling
```

---

#### Step 4.4 - Login & Admin Pages

**Prompt for Copilot:**
```
Create authentication and admin pages:

1. Login.tsx
   - Email and password inputs
   - "Login" button
   - Form validation (required, email format)
   - POST /api/auth/login
   - Store JWT in localStorage
   - Redirect to /admin or intended route after success
   - Error message display for invalid credentials
   - Mantine form components

2. Admin.tsx (Settings page)
   - Protected by JWT check
   - Tabs:
     a) Scan Settings - Max depth, concurrent scans, file patterns
     b) LLM Settings - Ollama host, model selection, timeout
     c) Database - Export/import scan data, clear history
   - Forms with save buttons
   - Confirmation modals for destructive actions

API client setup:
- Create src/services/api.ts with axios instance
- Base URL: http://localhost:5000 (configurable via env)
- Request interceptor to add Bearer token
- Response interceptor for 401 handling (redirect to login)
- Error handling utilities
```

---

### Phase 5: Integration & Testing (Day 5)

**Goal:** Connect frontend to backend, add tests, and verify everything works

#### Step 5.1 - API Integration

**Prompt for Copilot:**
```
Create API service layer and TanStack Query hooks:

1. api.ts service with methods:
   - auth.login(email, password)
   - scans.trigger(projectName, path)
   - scans.getStatus(scanId)
   - projects.getAll(skip, take, search)
   - projects.getById(id)
   - projects.getInsights(id)
   - technologies.getAll()

2. Query hooks (queries.ts):
   - useDashboardData() - combines projects + technologies
   - useProjects(filters) - with pagination
   - useProject(id) - single project with findings
   - useProjectInsights(id) - AI insights with polling
   - useScanStatus(scanId) - poll until completed
   - useTriggerScan() - mutation hook

3. Auth utilities:
   - useAuth() hook - provides login, logout, isAuthenticated, user
   - ProtectedRoute wrapper component
   - Token storage and retrieval
   - Auto-logout on token expiry

Error handling:
- Typed error responses
- Toast notifications for errors (Mantine notifications)
- Retry logic for network failures
- Loading states for all queries
```

---

#### Step 5.2 - Testing Implementation

**Prompt for Copilot:**
```
Add comprehensive tests for frontend and backend:

Frontend (Vitest + React Testing Library):
1. Component tests:
   - Dashboard renders statistics correctly
   - Projects table displays and filters
   - Login form validates and submits
   - Protected routes redirect when unauthenticated

2. Hook tests:
   - useAuth handles login/logout
   - useTriggerScan mutation works
   - Query hooks fetch and cache data

3. Integration tests:
   - Full scan workflow (trigger → poll → view results)
   - Login → navigate to admin → logout flow

Backend (xUnit + FluentAssertions):
1. Unit tests:
   - ScanService parses each file type correctly
   - LlmService formats prompts properly (mock HTTP)
   - JwtService generates valid tokens

2. Integration tests:
   - API endpoints return correct data
   - Scan workflow from POST to completion
   - Auth middleware blocks unauthorized requests

3. Test data fixtures:
   - Sample package.json, .csproj files
   - Mock Ollama responses
   - Seed database for tests

Run commands:
- pnpm -C apps/web test
- dotnet test apps/api
```

---

### Phase 6: Dockerization (Day 5-6)

**Goal:** Containerize the application with Docker Compose

#### Step 6.1 - Dockerfiles

**Prompt for Copilot:**
```
Create production Dockerfiles:

1. docker/Dockerfile.api (ASP.NET Core 9)
   - Multi-stage build (sdk + runtime)
   - Copy .csproj and restore first (layer caching)
   - Copy source and build Release
   - Runtime image with minimal size
   - Expose port 5000
   - Set environment variables for DB path, JWT, Ollama
   - Create App_Data volume mount point

2. docker/Dockerfile.web (React + Vite)
   - Multi-stage build (node build + nginx serve)
   - Copy package.json and pnpm-lock.yaml, install deps
   - Copy source and build production bundle
   - Nginx image to serve static files
   - Copy custom nginx.conf for SPA routing
   - Expose port 80
   - Environment variables at runtime (API URL)

Optimization:
- Use .dockerignore files
- Layer caching strategy
- Minimal final image size
- Security: non-root user, no unnecessary tools
```

---

#### Step 6.2 - Docker Compose

**Prompt for Copilot:**
```
Create docker-compose.yml with three services:

Services:
1. ollama
   - Image: ollama/ollama:latest
   - Volumes: ollama-data:/root/.ollama (persist models)
   - Ports: 11434:11434
   - Environment: OLLAMA_HOST=0.0.0.0
   - Healthcheck: curl localhost:11434/api/tags
   - Command to pull model on startup (init script)

2. api
   - Build: ./docker/Dockerfile.api
   - Depends on: ollama
   - Ports: 5000:5000
   - Environment:
     * ConnectionStrings__DefaultConnection=Data Source=/app/data/scan.db
     * ADMIN_EMAIL, ADMIN_PASSWORD, JWT_SECRET (from .env)
     * OLLAMA_HOST=http://ollama:11434
     * OLLAMA_MODEL=llama3.2
   - Volumes:
     * ./apps/api/App_Data:/app/data (persist database)
   - Healthcheck: curl localhost:5000/health

3. web
   - Build: ./docker/Dockerfile.web
   - Depends on: api
   - Ports: 3000:80
   - Environment:
     * VITE_API_URL=http://localhost:5000

Networks:
- Custom bridge network for service communication

Volumes:
- ollama-data (named volume for models)
- api-data (bind mount for SQLite)

Include:
- .env.example file with all required variables
- init-ollama.sh script to pull model on first run
- docker-compose.override.yml for development
```

**Manual Commands:**
```powershell
# Create .env file
cp .env.example .env
# Edit .env with your values

# Build and start all services
docker compose up --build -d

# View logs
docker compose logs -f

# Initialize Ollama (first time)
docker compose exec ollama ollama pull llama3.2

# Stop all services
docker compose down

# Remove volumes (clean slate)
docker compose down -v
```

---

### Phase 7: Documentation & Deployment (Day 6)

**Goal:** Complete documentation and prepare for deployment

#### Step 7.1 - README.md

**Prompt for Copilot:**
```
Generate comprehensive README.md with sections:

1. Project Overview
   - What it does
   - Key features with emojis
   - Screenshot (placeholder for now)
   - Tech stack badges

2. Prerequisites
   - Required installations with versions
   - Ollama setup instructions
   - Links to downloads

3. Quick Start
   - Clone repository
   - Local development setup (step-by-step)
   - Docker setup (step-by-step)
   - Environment variables table

4. Architecture
   - Diagram (mermaid or text)
   - Folder structure explanation
   - Data flow (scan → LLM → insights)

5. API Documentation
   - All endpoints with method, path, auth required
   - Request/response examples
   - Status codes

6. Development
   - Running tests
   - Adding new technology detectors
   - Debugging tips
   - VS Code launch configurations

7. Deployment
   - Docker production considerations
   - Environment variables for production
   - Database backup/restore
   - Scaling considerations

8. Ollama Integration
   - Supported models
   - Customizing prompts
   - Performance tuning
   - Fallback behavior

9. Contributing
   - Code style guidelines
   - Commit message conventions
   - Pull request process

10. License
    - MIT License

11. Acknowledgments
    - GitHub Copilot
    - Open source dependencies
```

---

#### Step 7.2 - PROMPTS_LOG.md

**Template Structure:**
```markdown
# Development Process Log

## Overview
- **Project:** TechStack Scanner
- **Duration:** [Start Date] - [End Date]
- **Total Time:** X hours
- **Total Prompts:** X
- **Code Generation Rate:** Y%

---

## Prompt 1: [Title]
**Date:** YYYY-MM-DD HH:MM  
**Phase:** Phase 1 - Foundation  
**Tool:** GitHub Copilot Chat in VS Code  
**Model:** Claude Sonnet 4.5 (via Copilot)

**Context Before:**
- No code existed
- Prerequisites installed

**Prompt:**
```
[Exact prompt text]
```

**Result:**
- Files created: X
- Lines of code: ~Y
- Build status: ✅ Success / ⚠️ Warnings / ❌ Failed

**What Was Accepted:**
- List specific files/functions that worked perfectly
- Percentage of generated code accepted: Z%

**Manual Changes Required:**
- List what needed manual editing
- Why changes were needed

**Git Diff Stats:**
```
[Output of: git show --stat --oneline -1]
```

**Time Spent:**
- Prompting: X min
- Review: Y min
- Fixes: Z min
- Total: W min

**Insights:**
- What worked well in the prompt
- What could be improved
- Copilot's strengths/weaknesses observed
- Patterns learned

---

## Statistics Summary

### By Phase
| Phase | Prompts | Files Created | LOC Generated | Acceptance % | Time (hrs) |
|-------|---------|---------------|---------------|--------------|------------|
| 1     | X       | Y             | Z             | W%           | H          |
| ...   | ...     | ...           | ...           | ...          | ...        |

### By File Type
| Type       | Files | LOC  | Acceptance % |
|------------|-------|------|--------------|
| TypeScript | X     | Y    | Z%           |
| C#         | X     | Y    | Z%           |
| Config     | X     | Y    | Z%           |

### Technology Breakdown
- **Frontend:** X% generated, Y% manual
- **Backend:** X% generated, Y% manual
- **Config/DevOps:** X% generated, Y% manual

### Prompt Effectiveness
- **Perfect first try:** X prompts (Y%)
- **Minor edits needed:** X prompts (Y%)
- **Major rework needed:** X prompts (Y%)
- **Failed/abandoned:** X prompts (Y%)

### Time Analysis
- **Total development time:** X hours
- **Time saved (estimated):** Y hours (Z% faster than manual)
- **Most time-consuming phase:** Phase X
- **Fastest phase:** Phase Y

---

## Key Learnings

### What Worked Best
1. Specific, detailed prompts with technology versions
2. Providing context about existing code structure
3. Breaking complex tasks into smaller prompts
4. Reviewing generated code immediately
5. Using Copilot for boilerplate/config files

### What Didn't Work
1. Vague prompts without context
2. Requesting too much in one prompt
3. Assuming Copilot knows project structure
4. Not specifying versions (led to deprecated APIs)
5. Complex business logic (needed manual refinement)

### Prompt Patterns That Worked
```
Pattern: "Create [component] that [specific behavior] using [technology version]"
Success Rate: 90%

Pattern: "Add [feature] to existing [file] following [pattern/convention]"
Success Rate: 85%

Pattern: "Refactor [code] to use [better approach] while maintaining [constraint]"
Success Rate: 75%
```

### Recommendations for Future Projects
1. Start with architecture prompt (get structure right first)
2. Use Copilot heavily for: configs, DTOs, CRUD, routing, tests
3. Write complex business logic manually or provide detailed pseudocode
4. Always specify technology versions in prompts
5. Keep prompts focused (one feature/file at a time)
6. Provide examples when requesting specific patterns
7. Review and test generated code immediately
8. Document prompts in real-time (don't wait until end)
9. Use git commits after each successful generation
10. Iterate: refine prompts based on initial results

### ROI Analysis
- **Manual development estimate:** X hours
- **AI-assisted development actual:** Y hours
- **Time saved:** Z hours (W% reduction)
- **Code quality:** Similar/Better than manual (subjective)
- **Learning curve:** Worth it? YES/NO
- **Would use Copilot again:** Definitely/Maybe/No

---

## Conclusion

[Summary of the project experience, Copilot's impact, and final thoughts]

Generated with ❤️ and 🤖 GitHub Copilot
```

---

## 🎓 WORKING WITH COPILOT - BEST PRACTICES

### Prompt Engineering Tips

#### ✅ DO's

1. **Be Specific**
   ```
   ❌ "Create a component"
   ✅ "Create a React functional component called ProjectCard that displays project name, last scan date, and technology count using Mantine Card component"
   ```

2. **Specify Versions**
   ```
   ❌ "Use React Router"
   ✅ "Use React Router v7 with createBrowserRouter API"
   ```

3. **Provide Context**
   ```
   ✅ "Add a new endpoint to existing ScanController.cs that..."
   ✅ "Following the pattern in Dashboard.tsx, create Settings.tsx..."
   ```

4. **Include Constraints**
   ```
   ✅ "Create a form that validates email format and password length (min 8 chars)"
   ✅ "Implement caching with 5-minute TTL"
   ```

5. **Request Tests**
   ```
   ✅ "Generate the component with Vitest tests covering happy path and error states"
   ```

#### ❌ DON'Ts

1. **Vague Requirements**
   ```
   ❌ "Make it better"
   ❌ "Fix the bug"
   ❌ "Add some features"
   ```

2. **Too Much at Once**
   ```
   ❌ "Create the entire frontend with all pages, routing, state management, API integration, and tests"
   ```

3. **Assuming Context**
   ```
   ❌ "Update the function" (which function? which file?)
   ```

### Copilot Strengths (Use Heavily)

- ✅ Boilerplate code (configs, DTOs, models)
- ✅ CRUD operations (standard Create, Read, Update, Delete)
- ✅ Routing setup (React Router, API routes)
- ✅ Form validation logic
- ✅ Database entities and migrations
- ✅ API client wrappers (axios, fetch)
- ✅ Unit test scaffolding
- ✅ TypeScript interfaces and types
- ✅ CSS styling (Tailwind, CSS-in-JS)
- ✅ Configuration files (eslint, prettier, tsconfig)

### Copilot Limitations (Manual or Guide Carefully)

- ⚠️ Complex business logic (requires domain knowledge)
- ⚠️ Performance-critical algorithms
- ⚠️ Security-sensitive code (review carefully!)
- ⚠️ Architectural decisions (patterns, structure)
- ⚠️ Novel/cutting-edge libraries (may hallucinate)
- ⚠️ Integration of multiple systems (provide examples)

### Iterative Refinement Process

1. **Initial Prompt** → Generate foundation
2. **Review** → Test and identify issues
3. **Refined Prompt** → "Fix the validation logic to handle edge case X"
4. **Review Again** → Verify fix works
5. **Commit** → Save working version

### Example Workflow

```
You: "Create ScanService with method to parse package.json"
Copilot: [generates code]
You: [tests it, finds it doesn't handle missing version field]
You: "Update ScanService.ParsePackageJson to handle missing version field, return 'unknown' as fallback"
Copilot: [fixes the code]
You: [tests again, works!]
You: git add . && git commit -m "feat: add ScanService with package.json parsing"
```

---

## 📊 SUCCESS METRICS

### Completion Checklist

#### Functionality
- [ ] Can scan multiple project types (Node, .NET, Python, etc.)
- [ ] Detects technology versions accurately
- [ ] Stores scan results in SQLite database
- [ ] Generates AI insights using Ollama
- [ ] Dashboard displays aggregated statistics
- [ ] Project details page shows findings
- [ ] AI insights rendered as formatted markdown
- [ ] Login/logout with JWT authentication
- [ ] Admin area is protected
- [ ] Scan history tracked and viewable
- [ ] Background queue processes scans asynchronously

#### Technical Requirements
- [ ] React 18+ with TypeScript
- [ ] Vite 6 build tool
- [ ] React Router 7 with multiple layouts
- [ ] Mantine UI 7 components
- [ ] ASP.NET Core 9 API
- [ ] Entity Framework Core 9 + SQLite
- [ ] Ollama integration (llama3.2 or mistral)
- [ ] JWT authentication
- [ ] CORS configured correctly
- [ ] Serilog logging
- [ ] Tests: xUnit for backend, Vitest for frontend

#### DevOps
- [ ] Git repository initialized
- [ ] GitHub repository created and pushed
- [ ] Docker Compose setup (API + WEB + Ollama)
- [ ] Environment variables documented
- [ ] Database persisted in volume
- [ ] Ollama models persisted in volume

#### Documentation
- [ ] README.md with setup instructions
- [ ] PROMPTS_LOG.md with all prompts used
- [ ] API endpoints documented
- [ ] Environment variables listed
- [ ] Ollama setup guide
- [ ] Local and Docker run instructions
- [ ] Architecture diagram or description

#### Quality Metrics
- [ ] Code generation acceptance rate > 70%
- [ ] All tests passing
- [ ] No critical security issues
- [ ] Application runs locally without errors
- [ ] Application runs in Docker without errors
- [ ] LLM generates reasonable insights (manual verification)
- [ ] UI is responsive (mobile-friendly)

### Final Deliverables

1. **Working Application**
   - Runnable locally: `pnpm dev` + `dotnet run`
   - Runnable via Docker: `docker compose up`

2. **Source Code**
   - GitHub repository with commits
   - Clean code structure
   - Comments where necessary

3. **Documentation**
   - Complete README.md
   - PROMPTS_LOG.md with statistics
   - Insights section with learnings

4. **Demonstration**
   - Screenshots of key pages
   - Sample scan results
   - AI insights example

---

## 🚨 TROUBLESHOOTING GUIDE

### Common Issues

#### Ollama Not Running
```powershell
# Windows
# Check if service is running
Get-Service Ollama

# Start service
Start-Service Ollama

# Verify API
curl http://localhost:11434/api/tags
```

#### CORS Errors
```csharp
// In Program.cs, ensure CORS is before MapControllers()
app.UseCors("AllowVite");
```

#### JWT Not Working
```
- Check JWT_SECRET is at least 32 characters
- Verify token is being sent in Authorization: Bearer {token}
- Check token expiry with jwt.io
```

#### Database Migration Errors
```powershell
cd apps/api
dotnet ef database drop --force
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

#### Vite Build Errors
```powershell
cd apps/web
rm -rf node_modules
pnpm install
pnpm build
```

#### Docker Build Failures
```powershell
# Clean Docker cache
docker system prune -a

# Rebuild without cache
docker compose build --no-cache

# Check logs
docker compose logs api
docker compose logs ollama
```

---

## 🎯 NEXT STEPS AFTER COMPLETION

### Enhancements (Optional)

1. **Advanced Scanning**
   - Detect security vulnerabilities using NVD API
   - Check for dependency conflicts
   - License compliance checking

2. **Better Insights**
   - Fine-tune LLM prompts for your domain
   - Add RAG (Retrieval Augmented Generation) with documentation
   - Multi-model comparison (llama3.2 vs mistral)

3. **UI Improvements**
   - Dark mode toggle
   - Export scan results (PDF, CSV)
   - Visualization charts (dependency graphs)
   - Notifications (email, Slack) for outdated dependencies

4. **Performance**
   - Add Redis for caching
   - Parallelize file parsing
   - Incremental scanning (only changed files)

5. **Deployment**
   - CI/CD pipeline (GitHub Actions)
   - Deploy to Azure Container Apps / AWS ECS
   - Production database (PostgreSQL)
   - Monitoring (Application Insights, Sentry)

### Learning Objectives Achieved

- ✅ Used GitHub Copilot extensively for code generation
- ✅ Built full-stack application (React + .NET)
- ✅ Integrated local LLM (Ollama)
- ✅ Implemented authentication (JWT)
- ✅ Used modern frameworks (React Router 7, Mantine 7, EF Core 9)
- ✅ Containerized with Docker
- ✅ Documented development process
- ✅ Learned effective AI prompting techniques

---

## 📚 RESOURCES & REFERENCES

### Official Documentation
- [React](https://react.dev)
- [React Router v7](https://reactrouter.com)
- [Mantine UI v7](https://mantine.dev)
- [TanStack Query v5](https://tanstack.com/query/latest)
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Ollama](https://ollama.ai/docs)

### Tools
- [GitHub Copilot](https://github.com/features/copilot)
- [VS Code](https://code.visualstudio.com)
- [Docker](https://www.docker.com)

### Learning Resources
- [Copilot Best Practices](https://github.blog/2023-06-20-how-to-write-better-prompts-for-github-copilot/)
- [React TypeScript Cheatsheet](https://react-typescript-cheatsheet.netlify.app/)
- [Clean Architecture in .NET](https://docs.microsoft.com/dotnet/architecture/modern-web-apps-azure/)

---

## 📝 LICENSE

MIT License - Feel free to use this guide and generated code for learning purposes.

---

## 🙏 ACKNOWLEDGMENTS

- **GitHub Copilot** for AI-assisted development
- **Ollama** for local LLM hosting
- **Open Source Community** for amazing tools and libraries

---

**Ready to Start?**

1. ✅ Complete the Prerequisites checklist
2. ✅ Navigate to `C:\Work\Course\techstack-scanner`
3. ✅ Open VS Code: `code .`
4. ✅ Start with Phase 1, Step 1.1
5. ✅ Document everything in PROMPTS_LOG.md as you go

**Good luck building with Copilot! 🚀🤖**

---
