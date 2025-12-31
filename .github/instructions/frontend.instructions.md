# Frontend Development Instructions – TechStack Scanner

## Tech Stack
- **Framework:** React 18.3 with TypeScript 5.6
- **Build Tool:** Vite 6.0
- **Router:** React Router 7.0
- **UI Library:** Mantine 7.13
- **State Management:** TanStack Query v5.62
- **HTTP Client:** Axios 1.7
- **Testing:** Vitest 2.1 + Testing Library 16.1
- **Styling:** CSS Modules + Mantine components

## Project Structure

```
apps/web/
├── src/
│   ├── components/       # Reusable UI components
│   ├── contexts/         # React contexts (Auth)
│   ├── hooks/            # Custom hooks + TanStack Query
│   ├── layouts/          # MainLayout, AdminLayout
│   ├── pages/            # Route components
│   ├── services/         # API client (Axios)
│   └── tests/            # Unit tests
├── public/               # Static assets
└── vite.config.ts
```

## Core Patterns

### 1. Routing (React Router v7)

Use React Router v7 patterns with typed routes:

```typescript
// App.tsx structure
import { BrowserRouter, Routes, Route } from 'react-router-dom';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public routes */}
        <Route path="/" element={<Dashboard />} />
        <Route path="/login" element={<Login />} />
        
        {/* Protected routes */}
        <Route element={<ProtectedRoute><MainLayout /></ProtectedRoute>}>
          <Route path="/projects" element={<Projects />} />
          <Route path="/projects/:id" element={<ProjectDetails />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
```

**Requirements:**
- Use `<ProtectedRoute>` wrapper for authenticated routes
- Protected routes redirect to `/login?from=<current-path>`
- Use `useNavigate()` and `useParams()` hooks, never `window.location`

### 2. Authentication Context

All auth state managed via `AuthContext`:

```typescript
// contexts/AuthContext.tsx pattern
interface AuthContextType {
  token: string | null;
  isAuthenticated: boolean;
  logout: () => void;
}

// Usage in components
const { token, isAuthenticated, logout } = useAuth();
```

**Requirements:**
- Token stored in `localStorage` under `tss-token`
- Auth state synced across tabs via `storage` events
- Call `logout()` for logout; never manually clear token
- Logout triggers navigation to `/login` + `window.location.reload()` to flush cache

### 3. API Client (Axios)

Use centralized API client from `services/api.ts`:

```typescript
// services/api.ts exports configured Axios instance
import api from '@/services/api';

// All API calls use this instance
const response = await api.get('/projects');
const data = await api.post('/scan', scanRequest);
```

**Requirements:**
- Import `api` instance, never create new Axios instances
- Token automatically injected via interceptor
- 401 responses handled globally (clear token + redirect)
- Base URL from `VITE_API_URL` env var (default: `http://localhost:5000`)
- Never manually set `Authorization` header

### 4. Data Fetching (TanStack Query)

Use pre-defined hooks from `hooks/queries.ts`:

```typescript
// Queries
const { data: projects } = useProjects();
const { data: project } = useProject(id);
const { data: technologies } = useTechnologies();

// Mutations
const createProjectMutation = useCreateProject();
const deleteScanMutation = useDeleteScan();

createProjectMutation.mutate(projectData, {
  onSuccess: () => {
    // Query invalidation handled automatically
  }
});
```

**Requirements:**
- All queries have `enabled: !!token` to prevent unauthorized requests
- Mutations automatically invalidate related queries
- Scan status queries refetch every 2s while `Pending` or `Running`
- Never call `queryClient.invalidateQueries()` manually; mutations handle it
- Handle loading/error states: `isLoading`, `isError`, `error`

### 5. UI Components (Mantine)

Use Mantine components consistently:

```typescript
import { Button, Card, Text, Badge, Stack, Group } from '@mantine/core';

// Prefer Mantine components over HTML elements
<Button variant="filled" color="blue" onClick={handleClick}>
  Click Me
</Button>

// Use Mantine's layout components
<Stack gap="md">
  <Card shadow="sm" padding="lg">
    <Group justify="space-between">
      <Text size="lg" fw={500}>Title</Text>
      <Badge color="green">Active</Badge>
    </Group>
  </Card>
</Stack>
```

**Requirements:**
- Import components from `@mantine/core`
- Use Mantine's spacing scale: `xs`, `sm`, `md`, `lg`, `xl`
- Use Mantine colors: `blue`, `red`, `green`, `orange`, etc.
- Prefer `fw` (fontWeight), `c` (color), `fz` (fontSize) props over inline styles
- Use `Stack` and `Group` for layout instead of flexbox divs

### 6. Type Safety

Use shared types from `packages/shared`:

```typescript
import type { 
  Project, 
  Scan, 
  TechnologyFinding, 
  ScanStatusResponse 
} from '@shared/types';

// Status handling (can be string or number)
const statusLabel = typeof scan.status === 'string' 
  ? scan.status 
  : ['Unknown', 'Pending', 'Running', 'Completed', 'Failed'][scan.status];
```

**Requirements:**
- Import types from `@shared/types`, never redefine
- Handle `ScanStatusResponse.status` as union type (string | number)
- Use TypeScript strict mode; avoid `any` type
- Define prop types with TypeScript interfaces, not PropTypes

### 7. Forms and Validation

Use controlled components with Mantine forms:

```typescript
import { useForm } from '@mantine/form';

const form = useForm({
  initialValues: { projectName: '', rootPath: '' },
  validate: {
    projectName: (value) => (value ? null : 'Required'),
    rootPath: (value) => (value ? null : 'Required'),
  },
});

<form onSubmit={form.onSubmit(handleSubmit)}>
  <TextInput
    label="Project Name"
    {...form.getInputProps('projectName')}
  />
</form>
```

**Requirements:**
- Use `useForm` hook from `@mantine/form`
- Define validation inline or separate `validate` object
- Use `form.getInputProps()` to bind inputs
- Handle submission via `form.onSubmit()`

### 8. Protected Routes

Implement route protection pattern:

```typescript
// components/ProtectedRoute.tsx
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';

export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to={`/login?from=${location.pathname}`} />;
  }

  return children;
}
```

**Requirements:**
- Check `isAuthenticated` from `useAuth()`
- Preserve current location in `from` query param
- After login, redirect back using `from` param
- Hide nav links for protected routes when logged out

### 9. Error Handling

Handle errors gracefully:

```typescript
const { data, isLoading, isError, error } = useQuery({
  queryKey: ['projects'],
  queryFn: fetchProjects,
});

if (isLoading) return <Loader />;
if (isError) return <Alert color="red">{error.message}</Alert>;
```

**Requirements:**
- Always check `isLoading`, `isError` states
- Display user-friendly error messages
- Use Mantine's `Alert` or `Notification` for errors
- Log errors to console in development only

### 10. Testing (Vitest)

Write tests for components and hooks:

```typescript
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClientProvider } from '@tanstack/react-query';

describe('ProjectCard', () => {
  it('renders project name', () => {
    render(
      <QueryClientProvider client={queryClient}>
        <ProjectCard project={mockProject} />
      </QueryClientProvider>
    );
    expect(screen.getByText('My Project')).toBeInTheDocument();
  });
});
```

**Requirements:**
- Use Vitest and Testing Library
- Wrap components requiring React Query in `QueryClientProvider`
- Mock API calls with `vi.mock()`
- Test user interactions with `userEvent` from Testing Library
- Run tests: `pnpm test:web`

## Code Style

### Imports
```typescript
// Group imports: external, internal, types
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';

import { useAuth } from '@/contexts/AuthContext';
import api from '@/services/api';

import type { Project } from '@shared/types';
```

### Component Structure
```typescript
interface Props {
  project: Project;
  onDelete?: (id: number) => void;
}

export function ProjectCard({ project, onDelete }: Props) {
  const [isDeleting, setIsDeleting] = useState(false);
  const { isAuthenticated } = useAuth();

  const handleDelete = async () => {
    // Logic here
  };

  return (
    <Card>
      {/* JSX */}
    </Card>
  );
}
```

**Requirements:**
- Define `Props` interface above component
- Use named exports, not default exports
- Destructure props in function signature
- Group: state → contexts → queries → handlers → render

### Naming Conventions
- Components: PascalCase (`ProjectCard`, `AppBreadcrumbs`)
- Files: kebab-case for utilities, PascalCase for components
- Hooks: camelCase starting with `use` (`useProjects`, `useAuth`)
- Event handlers: `handleEventName` (`handleSubmit`, `handleDelete`)
- Boolean props/state: `isLoading`, `hasError`, `shouldShow`

## Environment Variables

Define in `.env` files:
```env
VITE_API_URL=http://localhost:5000
```

Access via `import.meta.env`:
```typescript
const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000';
```

## Build Commands

```bash
# Development
pnpm dev:web              # Start dev server on :5173

# Build
pnpm build:web            # Production build to dist/

# Testing
pnpm test:web             # Run Vitest tests
pnpm test:web --watch     # Watch mode

# Linting
pnpm lint:web             # Run ESLint
pnpm lint:web --fix       # Fix auto-fixable issues
```

## Performance Best Practices

1. **Code Splitting:** Use React Router's route-based splitting automatically
2. **Memoization:** Use `useMemo` for expensive calculations, `useCallback` for handlers passed to children
3. **Query Optimization:** Set `staleTime` and `cacheTime` appropriately in TanStack Query
4. **Avoid Re-renders:** Use React DevTools Profiler to identify unnecessary renders

## Common Pitfalls

❌ **Don't:**
- Create new Axios instances
- Manually set `Authorization` headers
- Bypass `useAuth` hook for auth checks
- Use `window.location` for navigation
- Redefine types from `@shared/types`
- Ignore `enabled` flag in queries

✅ **Do:**
- Use provided `api` instance
- Use `useAuth` + `ProtectedRoute` for auth
- Use `useNavigate()` for navigation
- Import types from shared package
- Handle loading/error states
- Test components with Vitest

## Debug Checklist

When things don't work:
1. Check browser console for errors
2. Check Network tab for failed requests
3. Verify token exists in localStorage (`tss-token`)
4. Verify API is running on port 5000
5. Check React Query DevTools for query states
6. Verify VITE_API_URL env var is set correctly

## Additional Resources

- [React 18 Docs](https://react.dev/)
- [React Router v7](https://reactrouter.com/)
- [TanStack Query](https://tanstack.com/query/latest)
- [Mantine UI](https://mantine.dev/)
- [Vitest](https://vitest.dev/)
