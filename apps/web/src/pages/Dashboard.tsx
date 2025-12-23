import { useMemo, useState } from 'react';
import ReactMarkdown from 'react-markdown';
import {
  Alert,
  Badge,
  Button,
  Grid,
  Group,
  Loader,
  Paper,
  Stack,
  Table,
  Text,
  Title,
} from '@mantine/core';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';
import { useDashboardData } from '../hooks/useDashboardData';
import { useAuth } from '../contexts/AuthContext';
import { ScanModal } from '../components/ScanModal';
import { StatisticsCard } from '../components/StatisticsCard';
import { RecentScansTable } from '../components/RecentScansTable';

const CHART_COLORS = ['#4C6FFF', '#12B886', '#F76707', '#F03E3E', '#7950F2', '#1098AD', '#FAB005'];

export default function Dashboard() {
  const { isAuthenticated } = useAuth();
  const { projectsQuery, technologiesQuery, insightsQuery } = useDashboardData();
  const [scanModalOpen, setScanModalOpen] = useState(false);

  const totalProjects = projectsQuery.data?.length ?? 0;
  const totalTechnologies = useMemo(
    () => technologiesQuery.data?.reduce((sum, tech) => sum + tech.total, 0) ?? 0,
    [technologiesQuery.data]
  );
  const outdatedCount = useMemo(
    () => technologiesQuery.data?.reduce((sum, tech) => sum + tech.outdatedCount, 0) ?? 0,
    [technologiesQuery.data]
  );

  const lastScan = useMemo(() => {
    if (!projectsQuery.data || projectsQuery.data.length === 0) return null;
    return projectsQuery.data
      .map((p) => new Date(p.lastScannedAt))
      .filter((d) => !Number.isNaN(d.getTime()))
      .sort((a, b) => b.getTime() - a.getTime())[0];
  }, [projectsQuery.data]);

  const recentScans = useMemo(() => {
    if (!projectsQuery.data) return [];
    return [...projectsQuery.data]
      .sort((a, b) => new Date(b.lastScannedAt).getTime() - new Date(a.lastScannedAt).getTime())
      .slice(0, 5);
  }, [projectsQuery.data]);

  const techTop = useMemo(() => {
    if (!technologiesQuery.data) return [];
    return [...technologiesQuery.data]
      .sort((a, b) => b.total - a.total)
      .slice(0, 7);
  }, [technologiesQuery.data]);

  const loading = projectsQuery.isLoading || technologiesQuery.isLoading;

  return (
    <Stack gap="md">
      <Group justify="space-between" align="center">
        <Title order={3}>Dashboard</Title>
        <Button onClick={() => setScanModalOpen(true)} disabled={!isAuthenticated}>
          New scan
        </Button>
      </Group>

      {!isAuthenticated && <Alert color="yellow">Login to load data from the API.</Alert>}

      {loading && (
        <Group justify="center">
          <Loader />
          <Text c="dimmed">Loading stats...</Text>
        </Group>
      )}

      <Grid>
        <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
          <StatisticsCard label="Projects scanned" value={totalProjects} />
        </Grid.Col>
        <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
          <StatisticsCard label="Technologies detected" value={totalTechnologies} />
        </Grid.Col>
        <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
          <StatisticsCard label="Outdated dependencies" value={outdatedCount} tone="red" />
        </Grid.Col>
        <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
          <StatisticsCard label="Last scan" value={lastScan ? formatDate(lastScan) : '—'} tone="gray" />
        </Grid.Col>
      </Grid>

      <Grid>
        <Grid.Col span={{ base: 12, md: 6 }}>
          <Paper withBorder p="md" radius="md">
            <Group justify="space-between" mb="sm">
              <Title order={5}>Technology distribution</Title>
              <Badge color="gray" variant="light">
                Top {techTop.length}
              </Badge>
            </Group>
            {techTop.length === 0 && <Text c="dimmed">No technologies yet.</Text>}
            {techTop.length > 0 && (
              <div style={{ height: 260 }}>
                <ResponsiveContainer>
                  <PieChart>
                    <Pie dataKey="total" data={techTop} outerRadius={100} label={(entry: { name: string }) => entry.name}>
                      {techTop.map((entry, index) => (
                        <Cell key={entry.name} fill={CHART_COLORS[index % CHART_COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip formatter={(value: number, _name, payload: { name?: string }) => [`${value} findings`, payload?.name ?? '']} />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            )}
          </Paper>
        </Grid.Col>

        <Grid.Col span={{ base: 12, md: 6 }}>
          <Paper withBorder p="md" radius="md">
            <Group justify="space-between" mb="sm">
              <Title order={5}>Recent scans</Title>
              <Badge color="gray" variant="light">
                Last {recentScans.length}
              </Badge>
            </Group>
            <RecentScansTable projects={recentScans} />
          </Paper>
        </Grid.Col>
      </Grid>

      <Paper withBorder p="md" radius="md">
        <Group justify="space-between" mb="sm">
          <Title order={5}>AI insights</Title>
          <Badge color="gray" variant="light">
            Latest scan
          </Badge>
        </Group>
        {insightsQuery.isLoading && (
          <Group gap="xs">
            <Loader size="sm" />
            <Text c="dimmed">Fetching insights...</Text>
          </Group>
        )}
        {insightsQuery.isError && <Text c="dimmed">No insights available yet.</Text>}
        {!insightsQuery.isLoading && !insightsQuery.data && !insightsQuery.isError && (
          <Text c="dimmed">No insights generated yet. Run a scan to populate insights.</Text>
        )}
        {insightsQuery.data && (
          <MarkdownWithTables content={insightsQuery.data} />
        )}
      </Paper>

      <ScanModal opened={scanModalOpen} onClose={() => setScanModalOpen(false)} />
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

function formatDate(value: string | Date | null | undefined) {
  if (!value) return '—';
  const date = typeof value === 'string' ? new Date(value) : value;
  if (Number.isNaN(date.getTime())) return '—';
  return new Intl.DateTimeFormat('en', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date);
}

