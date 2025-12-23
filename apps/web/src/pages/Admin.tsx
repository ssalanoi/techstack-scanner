import { useState } from 'react';
import {
  Alert,
  Button,
  Group,
  Modal,
  NumberInput,
  Paper,
  Stack,
  Tabs,
  Text,
  Textarea,
  TextInput,
  Title,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';

export default function Admin() {
  const [scanSettings, setScanSettings] = useState({
    maxDepth: 5,
    filePatterns: '**/*.ts\n**/*.tsx\n**/*.cs\n**/*.csproj\npackage.json',
  });

  const [llmSettings, setLlmSettings] = useState({
    host: 'http://localhost:11434',
    model: 'llama3.2',
    timeoutSeconds: 60,
  });

  const [databasePaths, setDatabasePaths] = useState({
    exportPath: 'App_Data/scan-export.json',
    importPath: '',
  });

  const [confirmAction, setConfirmAction] = useState<null | 'clearHistory' | 'resetSettings'>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const openConfirm = (action: 'clearHistory' | 'resetSettings') => {
    setConfirmAction(action);
    setConfirmOpen(true);
  };

  const handleConfirm = () => {
    if (!confirmAction) return;
    setSaving(true);
    setTimeout(() => {
      setSaving(false);
      setConfirmOpen(false);
      notifications.show({
        color: 'green',
        title: 'Action completed',
        message:
          confirmAction === 'clearHistory'
            ? 'Scan history cleared (simulated).'
            : 'Settings reset to defaults.',
      });
      if (confirmAction === 'resetSettings') {
        setScanSettings({
          maxDepth: 5,
          filePatterns: '**/*.ts\n**/*.tsx\n**/*.cs\n**/*.csproj\npackage.json',
        });
        setLlmSettings({ host: 'http://localhost:11434', model: 'llama3.2', timeoutSeconds: 60 });
      }
    }, 300);
  };

  const saveScanSettings = () => {
    setSaving(true);
    setTimeout(() => {
      setSaving(false);
      notifications.show({ color: 'green', title: 'Scan settings saved', message: 'Your scan configuration has been saved' });
    }, 300);
  };

  const saveLlmSettings = () => {
    setSaving(true);
    setTimeout(() => {
      setSaving(false);
      notifications.show({ color: 'green', title: 'LLM settings saved', message: 'Your Ollama configuration has been saved' });
    }, 300);
  };

  const exportData = () => {
    notifications.show({ color: 'blue', title: 'Export started', message: `Exporting to ${databasePaths.exportPath}` });
  };

  const importData = () => {
    if (!databasePaths.importPath.trim()) {
      notifications.show({ color: 'red', title: 'Import path required', message: 'Please provide a valid file path' });
      return;
    }
    notifications.show({ color: 'green', title: 'Import started', message: `Importing from ${databasePaths.importPath}` });
  };

  return (
    <Stack gap="md">
      <Title order={3}>Admin Settings</Title>
      
      <Alert color="blue" variant="light">
        These controls are protected by JWT. Settings are saved locally for now; wire them to backend endpoints when available.
      </Alert>

      <Paper withBorder p="md" radius="md">
        <Tabs defaultValue="scan">
          <Tabs.List>
            <Tabs.Tab value="scan">Scan Settings</Tabs.Tab>
            <Tabs.Tab value="llm">LLM Settings</Tabs.Tab>
            <Tabs.Tab value="database">Database</Tabs.Tab>
          </Tabs.List>

          <Tabs.Panel value="scan" pt="md">
            <Stack gap="sm">
              <NumberInput
                label="Max depth"
                min={1}
                value={scanSettings.maxDepth}
                onChange={(value) => setScanSettings((s) => ({ ...s, maxDepth: Number(value) || 1 }))}
              />
              <Textarea
                label="File patterns (one per line)"
                minRows={4}
                value={scanSettings.filePatterns}
                onChange={(e) => setScanSettings((s) => ({ ...s, filePatterns: e.currentTarget.value }))}
              />
              <Group justify="flex-end">
                <Button variant="light" color="red" onClick={() => openConfirm('resetSettings')}>
                  Reset to defaults
                </Button>
                <Button onClick={saveScanSettings} loading={saving}>
                  Save scan settings
                </Button>
              </Group>
            </Stack>
          </Tabs.Panel>

          <Tabs.Panel value="llm" pt="md">
            <Stack gap="sm">
              <TextInput
                label="Ollama host"
                value={llmSettings.host}
                onChange={(e) => setLlmSettings((s) => ({ ...s, host: e.currentTarget.value }))}
                placeholder="http://localhost:11434"
              />
              <TextInput
                label="Model"
                value={llmSettings.model}
                onChange={(e) => setLlmSettings((s) => ({ ...s, model: e.currentTarget.value }))}
              />
              <NumberInput
                label="Timeout (seconds)"
                min={5}
                max={300}
                value={llmSettings.timeoutSeconds}
                onChange={(value) => setLlmSettings((s) => ({ ...s, timeoutSeconds: Number(value) || 60 }))}
              />
              <Group justify="flex-end">
                <Button onClick={saveLlmSettings} loading={saving}>
                  Save LLM settings
                </Button>
              </Group>
            </Stack>
          </Tabs.Panel>

          <Tabs.Panel value="database" pt="md">
            <Stack gap="sm">
              <Group grow align="end">
                <TextInput
                  label="Export path"
                  value={databasePaths.exportPath}
                  onChange={(e) => setDatabasePaths((s) => ({ ...s, exportPath: e.currentTarget.value }))}
                />
                <Button onClick={exportData}>Export</Button>
              </Group>
              <Group grow align="end">
                <TextInput
                  label="Import path"
                  placeholder="Select a file path"
                  value={databasePaths.importPath}
                  onChange={(e) => setDatabasePaths((s) => ({ ...s, importPath: e.currentTarget.value }))}
                />
                <Button onClick={importData}>Import</Button>
              </Group>
              <Button color="red" variant="light" onClick={() => openConfirm('clearHistory')}>
                Clear scan history
              </Button>
            </Stack>
          </Tabs.Panel>
        </Tabs>
      </Paper>

      <Modal opened={confirmOpen} onClose={() => setConfirmOpen(false)} title="Are you sure?" centered>
        <Stack>
          <Text>
            {confirmAction === 'clearHistory'
              ? 'This will delete all stored scan history. This action cannot be undone.'
              : 'This will reset scan and LLM settings back to their defaults.'}
          </Text>
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setConfirmOpen(false)} disabled={saving}>
              Cancel
            </Button>
            <Button color="red" onClick={() => handleConfirm()} loading={saving}>
              Confirm
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}
