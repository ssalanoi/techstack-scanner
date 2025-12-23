# TechStack Scanner – AI Assistant Guide

## Big Picture
- Monorepo: ASP.NET Core 9 API (`apps/api`) + React/Vite frontend (`apps/web`) + shared TypeScript types (`packages/shared`).
- Data: SQLite via EF Core; migrations run automatically on startup (`Program.cs`). Entities: `Project`, `Scan`, `TechnologyFinding` with cascade deletes.
- Scanning pipeline: `ScanService` parses repo files (npm, pip, Gem, Go, Maven/Gradle, Docker) → findings → `QueueService` + `ScanWorkerService` process scans → optional LLM summary via Ollama (`LlmService`).
- Auth: JWT-based admin-only, credentials from env vars; tokens stored in `localStorage` under `tss-token` and injected by Axios interceptor. Frontend routes `/projects`, `/projects/:id`, `/admin` are behind `ProtectedRoute`.

## Local Dev / Commands
- Frontend: from repo root `pnpm dev:web` (Vite at :5173). Build/test: `pnpm build:web`, `pnpm test:web`, `pnpm lint:web`.
- API: from repo root `dotnet run --project apps/api/api.csproj --urls http://localhost:5000`. Requires env vars `JWT_SECRET` (≥32 chars), `ADMIN_EMAIL`, `ADMIN_PASSWORD`; optional `OLLAMA_HOST`, `OLLAMA_MODEL`, `Jwt:Issuer/Audience/ExpiryMinutes`.
- Preconfigured VS Code tasks: "build-api" builds `apps/api/api.csproj`.

## Frontend Patterns
- Router v7 in [apps/web/src/App.tsx](apps/web/src/App.tsx): public `/` (dashboard) and `/login`; protected groups wrap `MainLayout` (`/projects`, `/projects/:id`) and `AdminLayout` (`/admin`).
- Auth context in [apps/web/src/contexts/AuthContext.tsx](apps/web/src/contexts/AuthContext.tsx): `token`, `logout()`, `isAuthenticated`; keep state synced across tabs. Protected guard in [apps/web/src/components/ProtectedRoute.tsx](apps/web/src/components/ProtectedRoute.tsx).
- API client in [apps/web/src/services/api.ts](apps/web/src/services/api.ts): Axios with `VITE_API_URL` (default `http://localhost:5000`); 401 responses clear token and redirect to `/login?from=...`.
- Data fetching via TanStack Query hooks in [apps/web/src/hooks/queries.ts](apps/web/src/hooks/queries.ts); queries disabled when no token; scan status refetches while Pending/Running.
- UI: Mantine components; breadcrumbs component [apps/web/src/components/AppBreadcrumbs.tsx](apps/web/src/components/AppBreadcrumbs.tsx). Markdown rendering with tables in [apps/web/src/pages/ProjectDetails.tsx](apps/web/src/pages/ProjectDetails.tsx) (also reused on dashboard).
- Shared types from [packages/shared/src/types.ts](packages/shared/src/types.ts); note `ScanStatusResponse.status` can be string or numeric enum—UI maps numbers to labels.

## Backend Patterns (API)
- Startup in [apps/api/Program.cs](apps/api/Program.cs): Serilog, SQLite `AppDbContext`, JWT bearer auth, CORS for Vite, Swagger (dev only), health `/health`; EF migrations apply on startup.
- Auth in [apps/api/Controllers/AuthController.cs](apps/api/Controllers/AuthController.cs): validates env `ADMIN_EMAIL`/`ADMIN_PASSWORD`, issues JWT via `JwtService` using `JwtSettings` (env-driven, `JWT_SECRET` ≥32 chars).
- Scanning pipeline: `ScanService` parses manifests (`package.json`, `global.json`, `.csproj`, `requirements.txt`, `pyproject.toml`, `Gemfile`, `Gemfile.lock`, `go.mod/sum`, `pom.xml`, `build.gradle`, Dockerfiles/compose) → findings → enqueued via `QueueService` → processed by `ScanWorkerService` (background hosted service) → LLM summary via `LlmService` (Ollama `/api/generate`, 3 attempts with backoff).
- Entities with cascade delete: `Project` → `Scan` → `TechnologyFinding`; date fields normalized to UTC in [apps/api/Data/AppDbContext.cs](apps/api/Data/AppDbContext.cs).
- Extending parsers: add format-specific parser inside `ScanService` and map to `CreateFinding`; keep regex culture-invariant and guarded against malformed input.
- Configuration: `LlmOptions`, `QueueOptions`, `ScanOptions` bound from config; CORS allows only `http://localhost:5173` by default.

## Frontend Patterns (Web)
- Routing in [apps/web/src/App.tsx](apps/web/src/App.tsx): public `/` (dashboard) and `/login`; protected group wraps `MainLayout` for `/projects` and `/projects/:id`, and `AdminLayout` for `/admin` via `ProtectedRoute` (redirects to login with `from` state).
- Auth context in [apps/web/src/contexts/AuthContext.tsx](apps/web/src/contexts/AuthContext.tsx): token stored under `tss-token`, kept in sync across tabs; `logout()` clears token. Layouts call `logout` + navigate to `/login` + `window.location.reload()` to flush React Query cache.
- Axios client in [apps/web/src/services/api.ts](apps/web/src/services/api.ts): injects bearer token; on 401 clears token and redirects to login with `from` query. Use this client for all API calls; base URL `VITE_API_URL` (default `http://localhost:5000`).
- Data fetching via TanStack Query hooks in [apps/web/src/hooks/queries.ts](apps/web/src/hooks/queries.ts): all queries `enabled` when token present; scan status refetches while Pending/Running; mutations invalidate relevant queries.
- Shared types from [packages/shared/src/types.ts](packages/shared/src/types.ts): `ScanStatusResponse.status` may be string or numeric enum—map to labels in UI when displaying statuses.
- UI components: Mantine across layouts; breadcrumbs in [apps/web/src/components/AppBreadcrumbs.tsx](apps/web/src/components/AppBreadcrumbs.tsx); markdown with tables rendered in [apps/web/src/pages/ProjectDetails.tsx](apps/web/src/pages/ProjectDetails.tsx) (also used on dashboard).
- Guards/visibility: nav links for Projects/Admin hidden when logged out; refresh/scan actions disabled without auth; protected routes prevent `/projects` access when not authenticated.

## Conventions / Gotchas
- Backend: `JWT_SECRET` must be set and ≥32 chars or startup fails; CORS default is `http://localhost:5173`; migrations auto-run on boot (SQLite file under `App_Data`).
- Frontend: rely on provided Axios interceptors for auth flow; avoid bypassing `useAuth`/React Query `enabled` flags or you’ll hit 401 loops.
- Scans cascade-delete with projects—be careful when deleting projects. LLM errors are tolerated with fallback text.

## Testing / Debugging
- Frontend tests with Vitest (`pnpm test:web`); uses jsdom and Testing Library. Lint with `pnpm lint:web`.
- API: no explicit test suite; rely on `dotnet run` with env vars. Swagger available in Development.

If any section is unclear or missing, tell me which part to refine (frontend patterns, API/scan flow, env/setup, or conventions).