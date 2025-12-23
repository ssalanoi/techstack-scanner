import axios from 'axios';
import type {
  ProjectDetail,
  ProjectListItem,
  ScanStatusResponse,
  StartScanRequest,
  TechnologyAggregate,
} from '@techstack-scanner/shared';
import { TOKEN_KEY } from '../contexts/AuthContext';

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

const client = axios.create({
  baseURL: API_URL,
});

client.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY);
  if (token) {
    config.headers.Authorization = config.headers.Authorization ?? `Bearer ${token}`;
  }
  return config;
});

client.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      localStorage.removeItem(TOKEN_KEY);
      const isOnLogin = window.location.pathname.startsWith('/login');
      if (!isOnLogin) {
        const from = `${window.location.pathname}${window.location.search}`;
        const search = new URLSearchParams({ from }).toString();
        window.location.href = `/login?${search}`;
      }
    }
    return Promise.reject(error as Error);
  }
);

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
}

export async function login(email: string, password: string): Promise<LoginResponse> {
  const { data } = await client.post<unknown>('/api/auth/login', { email, password });
  return data as LoginResponse;
}

export async function fetchProjects(params?: {
  search?: string;
  skip?: number;
  take?: number;
}): Promise<ProjectListItem[]> {
  const { data } = await client.get<ProjectListItem[]>('/api/projects', {
    params,
  });
  return data;
}

export async function fetchProject(projectId: string): Promise<ProjectDetail> {
  const { data } = await client.get<ProjectDetail>(`/api/projects/${projectId}`);
  return data;
}

export async function fetchTechnologies(): Promise<TechnologyAggregate[]> {
  const { data } = await client.get<TechnologyAggregate[]>('/api/technologies');
  return data;
}

export async function fetchInsights(projectId: string): Promise<string> {
  const { data } = await client.get<string>(`/api/projects/${projectId}/insights`);
  return data;
}

export async function triggerScan(payload: StartScanRequest): Promise<ScanStatusResponse> {
  const { data } = await client.post<ScanStatusResponse>('/api/scan', payload);
  return data;
}

export async function fetchScanStatus(scanId: string): Promise<ScanStatusResponse> {
  const { data } = await client.get<ScanStatusResponse>(`/api/scan/${scanId}/status`);
  return data;
}

export async function deleteProject(projectId: string): Promise<void> {
  await client.delete(`/api/projects/${projectId}`);
}

export function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string } | string | undefined;
    const message = typeof data === 'string' ? data : data?.message;
    return message || error.message || 'Request failed';
  }
  if (error instanceof Error) return error.message;
  return 'Unexpected error';
}

export { API_URL, client };
