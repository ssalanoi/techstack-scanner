import { Card, Text, Title } from '@mantine/core';

interface StatisticsCardProps {
  label: string;
  value: number | string;
  tone?: 'blue' | 'green' | 'red' | 'gray';
}

export function StatisticsCard({ label, value, tone = 'blue' }: StatisticsCardProps) {
  // Use smaller font for date/time values (like 'Last scan')
  const isDate = typeof value === 'string' && /\d{4}|AM|PM|\d{1,2}:\d{2}/.test(value);
  return (
    <Card withBorder padding="md" radius="md">
      <Text c="dimmed" size="sm">
        {label}
      </Text>
      {isDate ? (
        <Text c={tone} fw={700} fz="lg">
          {value}
        </Text>
      ) : (
        <Title order={2} c={tone}>
          {value}
        </Title>
      )}
    </Card>
  );
}
