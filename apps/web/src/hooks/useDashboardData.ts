import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import type { ProjectListItem, TechnologyAggregate } from '@techstack-scanner/shared';
import { fetchProjects, fetchTechnologies, fetchInsights } from '../services/api';
import { useAuth } from '../contexts/AuthContext';

const REFRESH_MS = 30_000;

export function useDashboardData() {
  const { token } = useAuth();

  const projectsQuery = useQuery<ProjectListItem[]>({
    queryKey: ['projects', 'dashboard'],
    queryFn: () => fetchProjects(),
    enabled: Boolean(token),
    refetchInterval: REFRESH_MS,
  });

  const technologiesQuery = useQuery<TechnologyAggregate[]>({
    queryKey: ['technologies', 'dashboard'],
    queryFn: fetchTechnologies,
    enabled: Boolean(token),
    refetchInterval: REFRESH_MS,
  });

  const latestProjectId = useMemo(() => {
    if (!projectsQuery.data || projectsQuery.data.length === 0) return undefined;
    return [...projectsQuery.data]
      .sort((a, b) => new Date(b.lastScannedAt).getTime() - new Date(a.lastScannedAt).getTime())
      .at(0)?.id;
  }, [projectsQuery.data]);

  const insightsQuery = useQuery<string>({
    queryKey: ['project-insights', latestProjectId, 'dashboard'],
    queryFn: () => fetchInsights(latestProjectId!),
    enabled: Boolean(token && latestProjectId),
    staleTime: REFRESH_MS,
    retry: 1,
  });

  return { projectsQuery, technologiesQuery, insightsQuery, latestProjectId };
}
