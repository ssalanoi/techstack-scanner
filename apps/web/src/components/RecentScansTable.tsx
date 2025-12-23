import { Badge, Table, Text } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import type { ProjectListItem } from '@techstack-scanner/shared';

interface RecentScansTableProps {
  projects: ProjectListItem[];
}

export function RecentScansTable({ projects }: RecentScansTableProps) {
  const navigate = useNavigate();
  if (projects.length === 0) {
    return <Text c="dimmed">No scans yet.</Text>;
  }

  return (
    <Table striped highlightOnHover withColumnBorders>
      <Table.Thead>
        <Table.Tr>
          <Table.Th>Name</Table.Th>
          <Table.Th>Last scanned</Table.Th>
          <Table.Th>Status</Table.Th>
          <Table.Th>Tech count</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {projects.map((project) => (
          <Table.Tr
            key={project.id}
            style={{ cursor: 'pointer' }}
            onClick={() => navigate(`/projects/${project.id}`)}
          >
            <Table.Td>{project.name}</Table.Td>
            <Table.Td>{formatDate(project.lastScannedAt)}</Table.Td>
            <Table.Td>
              <Badge color={statusColor(project.lastStatus)} variant="light">
                {project.lastStatus ?? 'Pending'}
              </Badge>
            </Table.Td>
            <Table.Td>{project.technologyCount}</Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
}

function statusColor(status?: string | null) {
  if (!status) return 'gray';
  if (status === 'Completed') return 'green';
  if (status === 'Running') return 'blue';
  if (status === 'Failed') return 'red';
  return 'gray';
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
