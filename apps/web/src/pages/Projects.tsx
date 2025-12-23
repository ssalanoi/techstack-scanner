import type { ProjectListItem } from '@techstack-scanner/shared';
import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Group,
  Loader,
  Paper,
  Stack,
  Table,
  Text,
  TextInput,
  Title,
} from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { useDeleteProject, usePagedProjects } from '../hooks/queries';
import { ScanModal } from '../components/ScanModal';
import { useAuth } from '../contexts/AuthContext';

export default function Projects() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(0);
  const pageSize = 20;
  const [scanModalOpen, setScanModalOpen] = useState(false);
  const projectsQuery = usePagedProjects(search, page, pageSize);
  const deleteMutation = useDeleteProject();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();

  const loading = projectsQuery.isLoading;
  const data = projectsQuery.data ?? [];
  const isLastPage = data.length < pageSize;

  const handleRowClick = (id: string) => navigate(`/projects/${id}`);

  useEffect(() => {
    setPage(0);
  }, [search]);

  useEffect(() => {
    if (!loading && page > 0 && data.length === 0) {
      setPage((p) => Math.max(0, p - 1));
    }
  }, [data.length, loading, page]);

  const handleDelete = (projectId: string, projectName: string): void => {
    const confirmed = window.confirm(`Delete project "${projectName}"? This cannot be undone.`);
    if (!confirmed) return;
    deleteMutation.mutate(projectId, {
      onSuccess: () => {
        void projectsQuery.refetch();
      },
      onError: (error: unknown) => {
        console.error('Delete failed:', String(error));
      },
    });
  };

  return (
    <Stack gap="md">
      <Group justify="space-between" align="center">
        <Title order={3}>Projects</Title>
        <Button onClick={() => setScanModalOpen(true)} disabled={!isAuthenticated}>
          New scan
        </Button>
      </Group>

      {!isAuthenticated && <Alert color="yellow">Login to load and manage projects.</Alert>}

      <Paper withBorder p="md" radius="md">
        <Group justify="space-between" mb="md" align="flex-end">
          <TextInput
            label="Search"
            placeholder="Filter by name or path"
            value={search}
            onChange={(e) => setSearch(e.currentTarget.value)}
            style={{ flex: 1 }}
          />
          <Group gap="xs">
            <Button variant="light" onClick={() => void projectsQuery.refetch()} disabled={loading || !isAuthenticated}>
              Refresh
            </Button>
            <Button
              variant="default"
              onClick={() => setPage((p) => Math.max(0, p - 1))}
              disabled={page === 0 || loading}
            >
              Prev
            </Button>
            <Button
              variant="default"
              onClick={() => setPage((p) => (isLastPage ? p : p + 1))}
              disabled={isLastPage || loading}
            >
              Next
            </Button>
          </Group>
        </Group>

        <Group justify="space-between" mb="sm">
          <Text size="sm" c="dimmed">
            Page {page + 1}
          </Text>
          {deleteMutation.isError && <Text c="red">Delete failed. Try again.</Text>}
        </Group>

        {loading && (
          <Group justify="center">
            <Loader />
            <Text c="dimmed">Loading projects...</Text>
          </Group>
        )}

        {!loading && (projectsQuery.data?.length ?? 0) === 0 && <Text c="dimmed">No projects yet.</Text>}

        {projectsQuery.isError && <Alert color="red">Failed to load projects.</Alert>}

        {data && data.length > 0 && (
          <Table striped highlightOnHover withColumnBorders stickyHeader>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Name</Table.Th>
                <Table.Th>Path</Table.Th>
                <Table.Th>Last scanned</Table.Th>
                <Table.Th>Technologies</Table.Th>
                <Table.Th>Status</Table.Th>
                <Table.Th></Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {data.map((project: ProjectListItem) => (
                <Table.Tr
                  key={project.id}
                  onClick={() => handleRowClick(project.id)}
                  style={{ cursor: 'pointer' }}
                >
                  <Table.Td>{project.name}</Table.Td>
                  <Table.Td>{project.path}</Table.Td>
                  <Table.Td>{formatDate(project.lastScannedAt)}</Table.Td>
                  <Table.Td>{project.technologyCount}</Table.Td>
                  <Table.Td>{project.lastStatus ?? 'Pending'}</Table.Td>
                  <Table.Td>
                    <Button
                      size="xs"
                      color="red"
                      variant="light"
                      onClick={(e) => {
                        e.stopPropagation();
                        handleDelete(project.id, project.name);
                      }}
                      loading={deleteMutation.isPending && deleteMutation.variables === project.id}
                      disabled={!isAuthenticated}
                    >
                      Delete
                    </Button>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        )}
      </Paper>

      <ScanModal opened={scanModalOpen} onClose={() => setScanModalOpen(false)} />
    </Stack>
  );
}

function formatDate(value: string | null | undefined) {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return new Intl.DateTimeFormat('en', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date);
}
