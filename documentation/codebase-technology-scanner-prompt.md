# Codebase Technology Scanner - Development Prompt

Act as an experienced senior full‑stack developer. Your role is to design and implement a complete application from scratch, generating all code and configurations. I will act mainly as a reviewer, making minimal manual edits. Work incrementally, file‑by‑file, and provide clear run instructions for each step. Document the process with a prompt/workflow log that I can copy into the README. The project must be fully built by you using GitHub Copilot.

## ROLE & WORKING MODE

- You are a senior full‑stack AI assistant. Propose architecture first, then implement in small slices.
- I will only answer YES/NO to choices. Ask me such questions only when a decision is strictly required.
- Generate all code/configs/instructions; I will review and minimally edit.

## PROJECT IDEA

"Codebase Technology Scanner": scans a local project folder and extracts technologies/frameworks (with versions) from files like package.json, *.csproj, Dockerfile, global.json, pyproject.toml, etc. The UI shows a dashboard with summaries, filters, and details. Results are persisted and there is a minimal admin area guarded by login. Additionally, a local LLM analyzes the technology stack and provides insights such as outdated dependencies, security concerns, compatibility issues, and upgrade recommendations.

## STACK & TOOLS (fixed unless a YES/NO choice is required)

- UI: React 18+ + TypeScript + Vite 6, React Router 7, Mantine UI 7, ESLint 9 (flat config) + Prettier + EditorConfig, Vitest + React Testing Library
- API: ASP.NET Core 9 (Web API), EF Core 9 (SQLite), Serilog, xUnit
- Local LLM: Ollama for local model hosting (llama3.2 or mistral); LangChain or Semantic Kernel for orchestration
- Package manager: pnpm (v9+)
- Optional (if helpful): TanStack Query (React Query v5) for fetching; Playwright for E2E
- Containerization: Dockerfiles + docker‑compose (run UI + API + Ollama together)
- GitHub: local git repo created and pushed via `gh` CLI

## ARCHITECTURE (monorepo)

Create a monorepo:
- /apps/web …… React + TS + Vite app
- /apps/api …… ASP.NET Core 9 Web API
- /packages/shared …… shared TypeScript types (API contracts for UI)
- /config …… baseline configs (eslint, prettier, tsconfig), optional
- README.md …… overview + setup/run + prompt/workflow log + insights

### UI requirements

- Pages: **Dashboard**, **Projects**, **Project Details**, **Admin**, **Login**
- Routing with layouts: **MainLayout** (public) and **AdminLayout** (protected)
- Guard admin routes by JWT (redirect to /login if unauthenticated)
- Dashboard displays aggregated technology statistics, version warnings, and AI-generated insights
- Project Details page shows technology findings with AI-powered recommendations
- Loading states for LLM analysis generation
- Basic components and styling with Mantine v7

### API requirements

- Endpoints:
  - **POST /auth/login** → returns JWT for admin user (env‑driven credentials)
  - **POST /scan** → enqueues a scan request; returns 202 + operationId
  - **GET /scan/{id}/status** → scan progress/status
  - **GET /projects** → list of scanned projects
  - **GET /projects/{id}** → project details (findings)
  - **GET /projects/{id}/insights** → AI-generated insights about the project's technology stack
  - **GET /technologies** → aggregated technology findings with version statistics

- Services:
  - **ScanService**: parse package.json, *.csproj, Dockerfile, global.json, pyproject.toml, requirements.txt, Gemfile, go.mod, pom.xml (and similar), return structured findings
  - **LlmService**: interface with Ollama API (http://localhost:11434) to analyze technology stacks and generate insights (outdated packages, security concerns, compatibility issues, upgrade paths)
  - **QueueService**: background processing using System.Threading.Channels for scanning and LLM analysis

- Persistence:
  - EF Core 9 + SQLite (file: `apps/api/App_Data/scan.db`)
  - Entities: 
    - **Project**(Id, Name, Path, LastScannedAt, AiInsights)
    - **Scan**(Id, ProjectId, StartedAt, FinishedAt, Status)
    - **TechnologyFinding**(Id, ScanId, Name, Version, SourceFile, Detector, IsOutdated, LatestVersion)

- Logging: Serilog (Console + File sinks)

- CORS: allow http://localhost:5173 (Vite dev)

- Auth: minimal JWT using env vars **ADMIN_EMAIL**, **ADMIN_PASSWORD**, **JWT_SECRET**; role=Admin; token TTL 1 hour

- LLM Config: 
  - **OLLAMA_HOST** (default: http://localhost:11434)
  - **OLLAMA_MODEL** (default: llama3.2 or mistral)
  - Ensure Ollama is running before starting API; provide setup instructions

- Security basics:
  - Validate scan paths (prevent directory traversal)
  - Sanitize file paths and storage paths
  - Implement timeouts for LLM requests
  - Limit concurrent scans (configurable)

### Testing

- Backend: xUnit + FluentAssertions for ScanService parsing and LLM service (with mocked Ollama responses)
- Frontend: Vitest + React Testing Library for components/routing
- Optional: Playwright for E2E

### Docker

- Dockerfile for API and WEB
- docker-compose.yml to run API + WEB + Ollama; support env vars for auth, DB path, and LLM config
- Include Ollama service with model auto-pull on startup

## DELIVERABLES & ACCEPTANCE CRITERIA

### Deliverables

- A repository that builds and runs locally and via Docker
- UI with multiple pages, routing, and two distinct layouts (MainLayout, AdminLayout guard)
- API exposing the scan/auth endpoints and returning structured data with AI insights
- SQLite persistence for scan results and AI-generated insights
- Ollama integration for local LLM inference (technology stack analysis)
- Basic tests (xUnit for API with LLM mocks, Vitest for UI)
- README with Overview, Setup/Run (local & Docker), Ollama setup instructions, Prompt & Workflow Log, and Insights

### Acceptance criteria

- Clear, reproducible commands to set up and run (local & Docker)
- Ollama installation and model setup documented
- LLM insights are contextual and provide actionable recommendations about technology stacks
- Implementation is simple, reliable, and avoids over‑engineering
- Code is primarily generated by you; I only review and minimally edit
- Prompt & workflow log documents key steps (prompts, results, what changed, insights)

## WORKING STYLE & RETURN FORMAT

At each significant step:
1) Provide a short plan and the exact commands (pnpm/dotnet/gh/docker) to run.
2) Generate code in small slices (file‑by‑file or small feature), avoiding giant dumps.
3) Explain what was generated and how to run/verify (start commands, URLs, sample cURL).
4) Provide a Prompt & Workflow Log entry I can paste into README with these fields:
   - Prompt (short)
   - Tool/Model (e.g., GitHub Copilot Chat VS Code)
   - Context provided
   - Result (summary, not full code)
   - Accepted/Changed & Why
   - Time spent (minutes)
   - Diff stats (use: `git show --stat --oneline -1` or `git show -1 --numstat`)
   - Notes/Insights

Version‑sensitive rules:
- If a library/config version is uncertain, use reasonable defaults or ask a YES/NO question.
- Avoid hallucinations; briefly justify non‑obvious dependencies before adding them.

## INITIAL TASKS (start now)

### Task A — Monorepo scaffolding

- Propose the folder structure and generate exact commands to scaffold:
  - /apps/web (React 18+ + TS + Vite 6 + React Router 7 + Mantine 7 + ESLint 9 flat config + Prettier + EditorConfig + Vitest)
  - /apps/api (ASP.NET Core 9 Web API + Serilog + EF Core 9 SQLite + xUnit)
  - /packages/shared (TypeScript package for shared types)
  - /config (baseline config files)
- Return initial config files content (eslint.config.js flat config, prettier, tsconfig, .editorconfig) and the commands to install dependencies.

### Task B — Ollama setup & LLM integration prep

- Generate instructions for installing Ollama locally (Windows/Mac/Linux)
- Commands to pull recommended model (llama3.2 or mistral)
- Verify Ollama is running: `curl http://localhost:11434/api/tags`
- Document environment variables for LLM configuration

### Task C — GitHub repository creation

- Generate exact commands to initialize git, create the first commit, create a public GitHub repository named `codebase-technology-scanner` via `gh`, set remote origin, and push to `main`.
- Include guidance for `gh auth login` if not authenticated.

### Task D — API minimal implementation

- Implement DbContext and entities for SQLite at `apps/api/App_Data/scan.db`.
- Add JWT auth:
  - POST /auth/login accepts email/password; validate vs ADMIN_EMAIL/ADMIN_PASSWORD; issue JWT with role=Admin; TTL 1 hour; secret from JWT_SECRET.
- Add endpoints: 
  - POST /scan (enqueue), GET /scan/{id}/status, 
  - GET /projects, GET /projects/{id}, GET /projects/{id}/insights (LLM-generated),
  - GET /technologies.
- Add QueueService (System.Threading.Channels) for background scanning and LLM analysis.
- Add ScanService (parsers for package.json, *.csproj, Dockerfile, global.json, pyproject.toml, requirements.txt, Gemfile, go.mod, pom.xml).
- Add LlmService (interface with Ollama API for technology stack analysis; handle timeouts and errors gracefully).
- Configure Serilog and CORS (http://localhost:5173).
- Provide commands to add EF Core 9 packages, create/apply migrations, and run (`dotnet run`).
- Include 2–3 xUnit tests (ScanService parsing, LLM service with mocked HTTP responses).

### Task E — UI minimal implementation

- Create React app with routes: / (Dashboard), /projects, /projects/:id, /admin, /login.
- Implement MainLayout and AdminLayout; guard /admin with JWT (redirect to /login).
- Build Dashboard (aggregated tech stats, version warnings, AI insights summary).
- Build Project Details page (findings table/cards with AI recommendations display).
- Add Mantine 7 components for layout/navigation.
- Add a small API client wrapper (fetch + Bearer token); optionally TanStack Query v5.
- Display LLM-generated insights with loading spinner during generation.
- Include Vitest + React Testing Library setup and 2–3 sample tests.
- Return run instructions: `pnpm dev` (Vite default port 5173).

### Task F — UI↔API wiring

- Connect Dashboard/Projects to API endpoints; handle loading/error states; display tables/cards.
- Implement real-time status polling for scan operations (while LLM generates insights).

### Task G — Dockerization

- Provide Dockerfile for API and WEB.
- Provide docker-compose.yml to run API + WEB + Ollama service.
- Configure Ollama service to auto-pull model on startup.
- Ensure env vars: ADMIN_EMAIL, ADMIN_PASSWORD, JWT_SECRET, DB path, OLLAMA_HOST, OLLAMA_MODEL.
- Provide exact `docker compose up --build` instructions and URLs.
- Document volume mounts for Ollama models and SQLite database.

### Task H — README & Logs

- Generate README.md including:
  - Overview (highlighting local LLM feature for technology stack analysis)
  - Tech stack (including Ollama + model)
  - Prerequisites (Ollama installation)
  - Setup & Run (local & Docker)
  - Ollama setup instructions (install, pull model, verify)
  - Prompt & Workflow Log template (fields above)
  - Insights template (what worked, what didn't, prompting patterns, LLM integration learnings, recommendations)

## KEY UPDATES FROM ORIGINAL

- Updated to .NET 9 (latest stable as of Dec 2025)
- Updated to React Router 7, Mantine 7, Vite 6
- ESLint 9 with flat config format
- TanStack Query v5 (rebranded from React Query)
- Added local LLM integration via Ollama (llama3.2/mistral)
- LlmService for AI-powered technology stack analysis and recommendations
- Enhanced scanning support for multiple ecosystems (Python, Ruby, Go, Java)
- Docker compose includes Ollama service
- Enhanced testing to include LLM mocking
- Updated README to include Ollama setup
- AiInsights field in Project entity
- IsOutdated and LatestVersion fields in TechnologyFinding entity

---
