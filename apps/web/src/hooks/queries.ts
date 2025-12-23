import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type {
  ProjectDetail,
  ProjectListItem,
  ScanStatusResponse,
  StartScanRequest,
  TechnologyAggregate,
} from '@techstack-scanner/shared';
import {
  fetchProject,
  fetchProjects,
  fetchInsights,
  fetchScanStatus,
  fetchTechnologies,
  deleteProject,
  triggerScan,
} from '../services/api';
import { useAuth } from '../contexts/AuthContext';

const STALE_TIME = 30_000;

export function useProjects(search?: string) {
  const { token } = useAuth();
  return useQuery<ProjectListItem[]>({
    queryKey: ['projects', search],
    queryFn: () => fetchProjects(search ? { search } : undefined),
    enabled: Boolean(token),
    staleTime: STALE_TIME,
  });
}

export function usePagedProjects(search: string, page: number, pageSize: number) {
  const { token } = useAuth();
  return useQuery<ProjectListItem[]>({
    queryKey: ['projects', search, page, pageSize],
    queryFn: () =>
      fetchProjects({
        search: search || undefined,
        skip: page * pageSize,
        take: pageSize,
      }),
    enabled: Boolean(token),
    staleTime: STALE_TIME,
    placeholderData: (previousData) => previousData,
  });
}

export function useProject(projectId?: string) {
  const { token } = useAuth();
  return useQuery<ProjectDetail>({
    queryKey: ['project', projectId],
    queryFn: () => fetchProject(projectId!),
    enabled: Boolean(token && projectId),
    staleTime: STALE_TIME,
  });
}

export function useTechnologies() {
  const { token } = useAuth();
  return useQuery<TechnologyAggregate[]>({
    queryKey: ['technologies'],
    queryFn: fetchTechnologies,
    enabled: Boolean(token),
    staleTime: STALE_TIME,
  });
}

export function useProjectInsights(projectId?: string) {
  const { token } = useAuth();
  return useQuery<string>({
    queryKey: ['project-insights', projectId],
    queryFn: () => fetchInsights(projectId!),
    enabled: Boolean(token && projectId),
    staleTime: STALE_TIME,
    retry: 1,
  });
}

export function useTriggerScan() {
  const { token } = useAuth();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: StartScanRequest) => {
      if (!token) {
        throw new Error('Login required');
      }
      return triggerScan(payload);
    },
    onSuccess: (response) => {
      void queryClient.invalidateQueries({ queryKey: ['projects'] });
      if (response.scanId) {
        void queryClient.invalidateQueries({ queryKey: ['scan-status', response.scanId] });
      }
    },
  });
}

export function useDeleteProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (projectId: string) => deleteProject(projectId),
    onSuccess: (_, projectId) => {
      void queryClient.invalidateQueries({ queryKey: ['projects'] });
      void queryClient.invalidateQueries({ queryKey: ['project', projectId] });
    },
  });
}

export function useScanStatus(scanId?: string) {
  const { token } = useAuth();

  return useQuery<ScanStatusResponse>({
    queryKey: ['scan-status', scanId],
    queryFn: () => fetchScanStatus(scanId!),
    enabled: Boolean(token && scanId),
    refetchInterval: (query) => getRefetchInterval(query.state.data?.status),
  });
}

function getRefetchInterval(status: string | undefined): number | false {
  if (status === 'Pending' || status === 'Running') {
    return 3000;
  }
  return false;
}
