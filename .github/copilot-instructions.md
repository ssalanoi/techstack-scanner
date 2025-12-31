# TechStack Scanner – AI Assistant Guide

## Overview

This is a monorepo application for scanning software projects to detect technologies, dependencies, and generate AI-powered insights. The project consists of an ASP.NET Core 10 backend, React 18 frontend, and shared TypeScript types.

**Tech Stack:**
- **Backend:** ASP.NET Core 10, C# 13, Entity Framework Core 9, SQLite, JWT Auth, Serilog
- **Frontend:** React 18, TypeScript 5.6, Vite 6, React Router 7, Mantine UI 7, TanStack Query v5, Axios
- **Testing:** xUnit (backend), Vitest (frontend)
- **LLM Integration:** Ollama (llama3.2)

## Architecture

**Monorepo Structure:**
```
techstack-scanner/
├── apps/
│   ├── api/              # ASP.NET Core backend
│   ├── api.Tests/        # Backend unit tests (38 tests)
│   └── web/              # React + Vite frontend
├── packages/
│   └── shared/           # Shared TypeScript types
└── .github/
    ├── copilot-instructions.md          # This file (general)
    └── instructions/
        ├── frontend.instructions.md     # Frontend-specific
        └── backend.instructions.md      # Backend-specific
```

## Quick Reference

### Development Commands
```bash
# Frontend (from repo root)
pnpm dev:web              # Start dev server on :5173
pnpm build:web            # Production build
pnpm test:web             # Run Vitest tests
pnpm lint:web             # ESLint

# Backend (from repo root)
dotnet run --project apps/api/api.csproj --urls http://localhost:5000
dotnet test apps/api.Tests/api.Tests.csproj
dotnet build apps/api/api.csproj  # Or use VS Code task: "build-api"

# Environment Setup (Backend)
# Development defaults exist; for production set:
JWT_SECRET=your-secret-min-32-chars
ADMIN_EMAIL=admin@techstack.local
ADMIN_PASSWORD=ChangeMe123!
OLLAMA_HOST=http://localhost:11434  # Optional
OLLAMA_MODEL=llama3.2               # Optional
```

### Key Endpoints
- **Frontend:** http://localhost:5173
- **Backend API:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger (dev only)
- **Health Check:** http://localhost:5000/health

## Core Data Flow

```
User → Frontend (React) → API (ASP.NET Core) → Database (SQLite)
                              ↓
                        ScanService → QueueService → ScanWorkerService
                                                         ↓
                                               OutdatedDependencyService
                                                         ↓
                                                   LlmService (Ollama)
```

**Scanning Pipeline:**
1. User triggers scan via frontend
2. API validates request, creates `Scan` entity
3. `ScanService` parses project files (package.json, requirements.txt, etc.)
4. Findings saved as `TechnologyFinding` entities
5. `QueueService` enqueues scan for background processing
6. `ScanWorkerService` processes queue, checks outdated dependencies
7. `LlmService` generates AI summary (optional)
8. Frontend polls scan status until complete

## Database Schema

**Entities (with cascade deletes):**
- `Project` (Id, Name, Description, CreatedAt, UpdatedAt)
  - `Scans` (navigation property)
- `Scan` (Id, ProjectId, Status, RootPath, LlmSummary, CreatedAt)
  - `TechnologyFindings` (navigation property)
- `TechnologyFinding` (Id, ScanId, Name, Version, Category, IsOutdated, LatestVersion)

**Supported Categories:** npm, nuget, pip, gem, go, maven, gradle, docker

## Authentication Flow

1. Frontend: User enters credentials → POST `/api/auth/login`
2. Backend: Validates against `ADMIN_EMAIL`/`ADMIN_PASSWORD` env vars
3. Backend: Issues JWT via `JwtService` (60min expiry, configurable)
4. Frontend: Stores token in `localStorage` under key `tss-token`
5. Frontend: Axios interceptor injects `Authorization: Bearer <token>` on all requests
6. Backend: `[Authorize]` attribute on controllers validates JWT
7. Frontend: 401 responses trigger logout + redirect to `/login?from=<current-path>`

## Frontend Patterns

**See [.github/instructions/frontend.instructions.md](.github/instructions/frontend.instructions.md) for comprehensive frontend guidelines.**

Quick highlights:
- **Routing:** React Router v7 with `ProtectedRoute` wrapper for auth
- **Auth:** `AuthContext` + `useAuth()` hook for token management
- **API Client:** Centralized Axios instance in `services/api.ts` (never create new instances)
- **Data Fetching:** TanStack Query hooks from `hooks/queries.ts` (queries auto-disabled when no token)
- **UI:** Mantine components exclusively; use `Stack`, `Group` for layout
- **Types:** Import from `@shared/types`, never redefine
- **Testing:** Vitest + Testing Library; wrap in `QueryClientProvider`

**Key Files:**
- [apps/web/src/App.tsx](apps/web/src/App.tsx) – Routing config
- [apps/web/src/contexts/AuthContext.tsx](apps/web/src/contexts/AuthContext.tsx) – Auth state
- [apps/web/src/services/api.ts](apps/web/src/services/api.ts) – Axios client
- [apps/web/src/hooks/queries.ts](apps/web/src/hooks/queries.ts) – TanStack Query hooks
- [packages/shared/src/types.ts](packages/shared/src/types.ts) – Shared types

## Backend Patterns

**See [.github/instructions/backend.instructions.md](.github/instructions/backend.instructions.md) for comprehensive backend guidelines.**

Quick highlights:
- **Startup:** [apps/api/Program.cs](apps/api/Program.cs) – Serilog, DbContext, JWT, CORS, migrations auto-apply
- **Auth:** [apps/api/Controllers/AuthController.cs](apps/api/Controllers/AuthController.cs) – JWT generation via `JwtService`
- **Controllers:** Use `[Authorize]` attribute, inject `AppDbContext` + `ILogger<T>`, async/await, proper HTTP status codes
- **Services:** `ScanService` (parsing), `QueueService` (in-memory queue), `ScanWorkerService` (background worker), `LlmService` (Ollama)
- **Database:** EF Core with SQLite; cascade deletes configured; UTC normalization in `SaveChanges` override
- **Configuration:** Options pattern (`LlmOptions`, `QueueOptions`, `ScanOptions`) bound from `appsettings.json`
- **Logging:** Structured logging with Serilog (console + file sinks)
- **Testing:** xUnit + Moq + FluentAssertions (38 tests in `apps/api.Tests`)

**Key Files:**
- [apps/api/Program.cs](apps/api/Program.cs) – Application bootstrap
- [apps/api/Data/AppDbContext.cs](apps/api/Data/AppDbContext.cs) – EF Core context
- [apps/api/Services/ScanService.cs](apps/api/Services/ScanService.cs) – File parsing logic
- [apps/api/Services/ScanWorkerService.cs](apps/api/Services/ScanWorkerService.cs) – Background worker
- [apps/api/Services/LlmService.cs](apps/api/Services/LlmService.cs) – Ollama integration

## Shared Types

Located in [packages/shared/src/types.ts](packages/shared/src/types.ts). Used by both frontend and backend.

**Important:** `ScanStatusResponse.status` can be `string` OR numeric enum:
- `0` = Unknown
- `1` = Pending
- `2` = Running
- `3` = Completed
- `4` = Failed

Frontend must handle both types and map to display labels.

## Configuration

### Backend (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App_Data/scan.db"
  },
  "Jwt": {
    "Issuer": "TechStackScanner",
    "Audience": "TechStackScanner",
    "ExpiryMinutes": 60
  },
  "Ollama": {
    "Host": "http://localhost:11434",
    "Model": "llama3.2",
    "TimeoutSeconds": 120
  },
  "ScanOptions": {
    "MaxDepth": 5
  },
  "QueueOptions": {
    "MaxConcurrency": 2,
    "MaxRetries": 3
  }
}
```

### Frontend (.env)
```env
VITE_API_URL=http://localhost:5000
```

## Common Tasks

### Adding a New Package Manager Parser
1. Add parsing method in `ScanService` (e.g., `ParseCargoToml()`)
2. Detect manifest file in `ScanDirectory()` loop
3. Extract dependencies and call `CreateFinding(name, version, "cargo")`
4. Add unit test in `ScanServiceTests.cs`
5. Update shared types if needed

### Adding a New API Endpoint
1. Add controller action with `[Authorize]` attribute
2. Inject `AppDbContext` and `ILogger<T>`
3. Use async/await, return proper HTTP status codes
4. Add corresponding frontend hook in `hooks/queries.ts`
5. Update types in `packages/shared/src/types.ts`

### Extending LLM Functionality
1. Modify prompt in `LlmService.BuildPrompt()`
2. Adjust `Ollama:MaxTokens` in appsettings.json
3. Handle response parsing in `GenerateSummaryAsync()`
4. Ensure fallback text on failure (don't throw exceptions)

## Conventions

### Backend
- **Naming:** PascalCase classes/methods, camelCase params, `_camelCase` private fields
- **Async:** Suffix methods with `Async`, return `Task<T>`
- **DI:** Constructor injection only
- **Dates:** Always use `DateTime.UtcNow` (never `DateTime.Now`)
- **Exceptions:** Log with structured logging, return error responses (don't throw to client)
- **Entities vs DTOs:** Never expose entities directly; map to DTOs for API responses

### Frontend
- **Naming:** PascalCase components, camelCase hooks/handlers, kebab-case file names
- **Exports:** Named exports (not default)
- **Auth:** Use `useAuth()` hook, never bypass `ProtectedRoute`
- **API Calls:** Use `api` instance from `services/api.ts`, never create new Axios
- **Queries:** Use TanStack Query hooks from `hooks/queries.ts`, respect `enabled` flag
- **UI:** Mantine components only; use design system spacing/colors

## Gotchas & Pitfalls

**Backend:**
- JWT_SECRET must be ≥32 chars or startup fails
- CORS configured for `http://localhost:5173` only
- Migrations auto-run on startup (SQLite file: `App_Data/scan.db`)
- Scans cascade-delete with projects (data loss warning)
- LLM errors are tolerated (fallback text, no exceptions)

**Frontend:**
- Axios interceptors handle auth; don't bypass
- React Query `enabled` flag prevents 401 loops
- Logout must call `window.location.reload()` to flush cache
- `ScanStatusResponse.status` is union type (string | number)
- Token stored under key `tss-token` in localStorage

## Testing

### Backend (xUnit)
```bash
dotnet test apps/api.Tests/api.Tests.csproj --logger "console;verbosity=detailed"
```
- 38 tests covering `ScanService`, `OutdatedDependencyService`, `LlmService`
- Use Moq for mocking, FluentAssertions for assertions
- Test files: `{ServiceName}Tests.cs`

### Frontend (Vitest)
```bash
pnpm test:web           # Run once
pnpm test:web --watch   # Watch mode
```
- Uses jsdom + Testing Library
- Wrap components in `QueryClientProvider` for tests
- Mock API calls with `vi.mock()`

## Debugging

**Frontend issues:**
1. Check browser console for errors
2. Check Network tab for failed requests
3. Verify token in localStorage (`tss-token`)
4. Verify VITE_API_URL env var
5. Use React Query DevTools

**Backend issues:**
1. Check logs in `Logs/` folder
2. Verify env vars set (`JWT_SECRET`, etc.)
3. Check database exists: `App_Data/scan.db`
4. Test endpoints via Swagger (dev mode)
5. Verify Ollama running (if using LLM)

## Additional Resources

- **Frontend Details:** [.github/instructions/frontend.instructions.md](.github/instructions/frontend.instructions.md)
- **Backend Details:** [.github/instructions/backend.instructions.md](.github/instructions/backend.instructions.md)
- **Project README:** [README.md](README.md) – User-facing documentation
- **Quick Start:** [QUICK_START.md](QUICK_START.md) – 3-step setup guide
- **Docker Guide:** [DOCKER.md](DOCKER.md) – Ollama setup
- **AI Development:** [AI_DEVELOPMENT.md](AI_DEVELOPMENT.md) – AI-assisted workflow

---

**Note:** This is the main instruction file. For detailed technology-specific guidance, refer to the specialized instruction files in `.github/instructions/`.
