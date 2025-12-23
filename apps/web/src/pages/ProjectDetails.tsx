import { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Badge,
  Button,
  Group,
  Loader,
  Paper,
  Stack,
  Table,
  Tabs,
  Text,
  Title,
} from '@mantine/core';
import ReactMarkdown from 'react-markdown';
import { useDeleteProject, useProject, useProjectInsights } from '../hooks/queries';
import { useAuth } from '../contexts/AuthContext';
import { ScanModal } from '../components/ScanModal';

export default function ProjectDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const projectQuery = useProject(id);
  const insightsQuery = useProjectInsights(id);
  const deleteMutation = useDeleteProject();
  const { isAuthenticated } = useAuth();
  const [scanModalOpen, setScanModalOpen] = useState(false);

  const loading = projectQuery.isLoading;
  const project = projectQuery.data;

  const history = useMemo(() => {
    return (project?.scanHistory ?? []).slice().sort((a, b) => {
      return new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime();
    });
  }, [project?.scanHistory]);

  const outdatedCount = useMemo(() => {
    return (project?.technologyFindings ?? []).filter((finding) => finding.isOutdated).length;
  }, [project?.technologyFindings]);

  const handleDelete = () => {
    if (!project) return;
    const confirmed = window.confirm(
      `Delete project "${project.name}" and its scans? This cannot be undone.`
    );
    if (!confirmed) return;
    deleteMutation.mutate(project.id, {
      onSuccess: () => {
        void navigate('/projects');
      },
    });
  };

  return (
    <Stack gap="md">
      <Group justify="space-between" align="center">
        <Title order={3}>Project details</Title>
        <Group>
          <Button variant="light" onClick={() => void projectQuery.refetch()} disabled={loading}>
            Refresh
          </Button>
          <Button color="red" variant="light" onClick={() => handleDelete()} disabled={!project || deleteMutation.isPending}>
            Delete
          </Button>
          <Button
            onClick={() => setScanModalOpen(true)}
            disabled={!isAuthenticated || !project}
            color="blue"
          >
            Rescan
          </Button>
        </Group>
      </Group>

      {!isAuthenticated && <Alert color="yellow">Login to load project details.</Alert>}

      {loading && (
        <Group justify="center">
          <Loader />
          <Text c="dimmed">Loading project...</Text>
        </Group>
      )}

      {projectQuery.isError && <Alert color="red">Failed to load project.</Alert>}

      {project && (
        <Stack gap="md">
          <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
              <Title order={4}>{project.name}</Title>
              <Text size="sm">Path: {project.path}</Text>
              <Text size="sm">Last scanned: {formatDate(project.lastScannedAt)}</Text>
              <Group gap="xs">
                <Badge color="blue" variant="light">
                  {project.scanHistory.length} scans
                </Badge>
                <Badge color={outdatedCount > 0 ? 'red' : 'green'} variant="light">
                  {outdatedCount} outdated
                </Badge>
                <Badge color="gray" variant="light">
                  {project.technologyFindings.length} technologies
                </Badge>
              </Group>
            </Stack>
          </Paper>

          <Paper withBorder p="md" radius="md">
            <Tabs defaultValue="technologies">
              <Tabs.List>
                <Tabs.Tab value="technologies">Technologies</Tabs.Tab>
                <Tabs.Tab value="insights">AI insights</Tabs.Tab>
                <Tabs.Tab value="history">Scan history</Tabs.Tab>
              </Tabs.List>

              <Tabs.Panel value="technologies" pt="md">
                <Group justify="space-between" mb="sm">
                  <Title order={5}>Technologies</Title>
                  <Badge color="gray" variant="light">
                    {project.technologyFindings.length}
                  </Badge>
                </Group>
                {project.technologyFindings.length === 0 && <Text c="dimmed">No findings yet.</Text>}
                {project.technologyFindings.length > 0 && (
                  <Table striped highlightOnHover withColumnBorders>
                    <Table.Thead>
                      <Table.Tr>
                        <Table.Th>Name</Table.Th>
                        <Table.Th>Version</Table.Th>
                        <Table.Th>Source</Table.Th>
                        <Table.Th>Status</Table.Th>
                      </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                      {project.technologyFindings.map((finding) => (
                        <Table.Tr key={`${finding.name}-${finding.sourceFile ?? ''}-${finding.version ?? ''}`}>
                          <Table.Td>{finding.name}</Table.Td>
                          <Table.Td>{finding.version ?? '—'}</Table.Td>
                          <Table.Td>{finding.sourceFile ?? '—'}</Table.Td>
                          <Table.Td>
                            {finding.isOutdated ? (
                              <Badge color="red" variant="light">
                                Outdated {finding.latestVersion ? `(latest ${finding.latestVersion})` : ''}
                              </Badge>
                            ) : (
                              <Badge color="green" variant="light">
                                Current
                              </Badge>
                            )}
                          </Table.Td>
                        </Table.Tr>
                      ))}
                    </Table.Tbody>
                  </Table>
                )}
              </Tabs.Panel>

              <Tabs.Panel value="insights" pt="md">
                <Group justify="space-between" mb="sm">
                  <Title order={5}>AI insights</Title>
                  <Badge color="gray" variant="light">
                    {insightsQuery.data || project.aiInsights ? 'Ready' : 'Pending'}
                  </Badge>
                </Group>
                {insightsQuery.isLoading && (
                  <Group gap="xs">
                    <Loader size="sm" />
                    <Text c="dimmed">Loading insights...</Text>
                  </Group>
                )}
                {insightsQuery.isError && <Alert color="red">Failed to load insights.</Alert>}
                {!insightsQuery.isLoading && !insightsQuery.data && !project.aiInsights && (
                  <Text c="dimmed">Insights will appear after the next scan.</Text>
                )}
                {(insightsQuery.data || project.aiInsights) && (
                  <MarkdownWithTables content={insightsQuery.data ?? project.aiInsights ?? ''} />
                )}
              </Tabs.Panel>

              <Tabs.Panel value="history" pt="md">
                <Group justify="space-between" mb="sm">
                  <Title order={5}>Scan history</Title>
                  <Badge color="gray" variant="light">
                    {history.length}
                  </Badge>
                </Group>
                {history.length === 0 && <Text c="dimmed">No scans recorded.</Text>}
                {history.length > 0 && (
                  <Table striped highlightOnHover withColumnBorders>
                    <Table.Thead>
                      <Table.Tr>
                        <Table.Th>Scan ID</Table.Th>
                        <Table.Th>Started</Table.Th>
                        <Table.Th>Finished</Table.Th>
                        <Table.Th>Status</Table.Th>
                      </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                      {history.map((item) => (
                        <Table.Tr key={item.scanId}>
                          <Table.Td>{item.scanId}</Table.Td>
                          <Table.Td>{formatDate(item.startedAt)}</Table.Td>
                          <Table.Td>{formatDate(item.finishedAt)}</Table.Td>
                          <Table.Td>
                              <Badge color={getStatusColor(item.status)} variant="light">
                                {getStatusName(item.status)}
                              </Badge>
                          </Table.Td>
                        </Table.Tr>
                      ))}
                    </Table.Tbody>
                  </Table>
                )}
              </Tabs.Panel>
            </Tabs>
          </Paper>
        </Stack>
      )}

      <ScanModal
        opened={scanModalOpen}
        onClose={() => setScanModalOpen(false)}
        defaultProjectName={project?.name}
        defaultPath={project?.path}
      />
    </Stack>
  );
}

function MarkdownWithTables({ content }: { content: string }) {
  // Parse content to find markdown tables and convert them to structured data
  const parts: Array<{ type: 'text' | 'table'; content: string; data?: { headers: string[]; rows: string[][] } }> = [];
  const lines = content.split('\n');
  let currentText: string[] = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];
    // Check if this is a table header (line with pipes)
    if (line.trim().startsWith('|') && line.trim().endsWith('|')) {
      // Check if next line is separator (contains ---)
      if (i + 1 < lines.length && lines[i + 1].includes('---')) {
        // Save accumulated text
        if (currentText.length > 0) {
          parts.push({ type: 'text', content: currentText.join('\n') });
          currentText = [];
        }

        // Parse table
        const headers = line.split('|').map(h => h.trim()).filter(Boolean);
        i += 2; // Skip header and separator
        const rows: string[][] = [];

        // Collect table rows
        while (i < lines.length && lines[i].trim().startsWith('|') && lines[i].trim().endsWith('|')) {
          const row = lines[i].split('|').map(c => c.trim()).filter(Boolean);
          if (row.length > 0) {
            rows.push(row);
          }
          i++;
        }

        parts.push({ type: 'table', content: '', data: { headers, rows } });
        continue;
      }
    }
    currentText.push(line);
    i++;
  }

  // Save any remaining text
  if (currentText.length > 0) {
    parts.push({ type: 'text', content: currentText.join('\n') });
  }

  return (
    <Stack gap="xs">
      {parts.map((part, idx) => {
        if (part.type === 'text' && part.content.trim()) {
          return <ReactMarkdown key={idx}>{part.content}</ReactMarkdown>;
        }
        if (part.type === 'table' && part.data) {
          return (
            <Table striped highlightOnHover withColumnBorders key={idx}>
              <Table.Thead>
                <Table.Tr>
                  {part.data.headers.map((header, hIdx) => (
                    <Table.Th key={hIdx}>{header}</Table.Th>
                  ))}
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {part.data.rows.map((row, rIdx) => (
                  <Table.Tr key={rIdx}>
                    {row.map((cell, cIdx) => (
                      <Table.Td key={cIdx}>{cell}</Table.Td>
                    ))}
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          );
        }
        return null;
      })}
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

function getStatusColor(status: string | number) {
  const name = getStatusName(status);
  if (name === 'Failed') return 'red';
  if (name === 'Completed') return 'green';
  if (name === 'Running') return 'blue';
  return 'gray';
}

function getStatusName(status: string | number) {
  // Map numeric or string status to human-readable name
  if (typeof status === 'string') {
    // Already a name
    return status;
  }
  switch (status) {
    case 0:
      return 'Pending';
    case 1:
      return 'Running';
    case 2:
      return 'Completed';
    case 3:
      return 'Failed';
    default:
      return String(status);
  }
}
