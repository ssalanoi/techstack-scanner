import { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Group, Modal, Stack, Text, TextInput } from '@mantine/core';
import { useScanStatus, useTriggerScan } from '../hooks/queries';

interface ScanModalProps {
  opened: boolean;
  onClose: () => void;
  defaultProjectName?: string;
  defaultPath?: string;
}

export function ScanModal({ opened, onClose, defaultProjectName, defaultPath }: ScanModalProps) {
  const [projectName, setProjectName] = useState(defaultProjectName ?? '');
  const [path, setPath] = useState(defaultPath ?? '');
  const [scanId, setScanId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (opened) {
      setProjectName(defaultProjectName ?? '');
      setPath(defaultPath ?? '');
      setScanId(null);
      setError(null);
    }
  }, [opened, defaultProjectName, defaultPath]);

  const triggerScan = useTriggerScan();
  const statusQuery = useScanStatus(scanId ?? undefined);

  const statusMessage = useMemo(() => {
    if (!scanId) return null;
    if (statusQuery.isError) return 'Failed to check scan status';
    if (statusQuery.isLoading) return 'Starting scan...';
    if (!statusQuery.data) return 'Waiting for status...';

    const status = statusQuery.data.status;
    
    const statusStr = typeof status === 'number' 
      ? ['Pending', 'Running', 'Completed', 'Failed'][status] || 'Unknown'
      : status;
    
    if (statusStr === 'Completed') return 'Scan completed';
    if (statusStr === 'Failed') return statusQuery.data.error ?? 'Scan failed';
    return `Scan is ${statusStr.toLowerCase()}`;
  }, [scanId, statusQuery.data, statusQuery.isError, statusQuery.isLoading]);

  const statusColor = useMemo(() => {
    const status = statusQuery.data?.status;
    if (!status) return 'blue';
    const statusStr = typeof status === 'number' 
      ? ['Pending', 'Running', 'Completed', 'Failed'][status]
      : status;
    return statusStr === 'Failed' ? 'red' : statusStr === 'Completed' ? 'green' : 'blue';
  }, [statusQuery.data?.status]);

  const handleSubmit = async () => {
    const trimmedName = projectName.trim();
    const trimmedPath = path.trim();
    if (!trimmedName || !trimmedPath) return;

    setError(null);
    try {
      const response = await triggerScan.mutateAsync({ projectName: trimmedName, path: trimmedPath });
      setScanId(response.scanId);
      onClose();
    } catch (err) {
      console.error('Scan start failed', err);
      const errorMessage = err instanceof Error ? err.message : 'Failed to start scan. Please check the console for details.';
      setError(errorMessage);
    }
  };

  return (
    <Modal opened={opened} onClose={onClose} title="Start a new scan" centered>
      <Stack>
        <TextInput
          label="Project name"
          value={projectName}
          onChange={(e) => setProjectName(e.currentTarget.value)}
          placeholder="My project"
          required
        />
        <TextInput
          label="Folder path"
          value={path}
          onChange={(e) => setPath(e.currentTarget.value)}
          placeholder="C:/work/my-repo"
          required
        />
        {error && <Alert color="red" title="Error">{error}</Alert>}
        {statusMessage && <Alert color={statusColor}>{statusMessage}</Alert>}
        <Group justify="space-between">
          <Text size="sm" c="dimmed">
            Provide a local path accessible to the API container.
          </Text>
          <Button
            onClick={() => void handleSubmit()}
            loading={triggerScan.isPending || statusQuery.isLoading}
            disabled={!projectName.trim() || !path.trim()}
          >
            Start scan
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
}
