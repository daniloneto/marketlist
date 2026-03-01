import { Group, Pagination, Select, Text } from '@mantine/core';

interface PaginationControlsProps {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

export function PaginationControls({
  page,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
  onPageSizeChange,
}: PaginationControlsProps) {
  const start = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = totalCount === 0 ? 0 : Math.min(page * pageSize, totalCount);

  return (
    <Group justify="space-between" mt="md" align="center">
      <Text size="sm" c="dimmed">Exibindo {start}–{end} de {totalCount} registros</Text>
      <Group>
        <Select
          size="xs"
          value={String(pageSize)}
          onChange={(v) => onPageSizeChange(Number(v ?? '10'))}
          data={['10', '20', '50', '100'].map((v) => ({ value: v, label: `${v} / página` }))}
          w={120}
        />
        <Pagination value={page} onChange={onPageChange} total={Math.max(totalPages, 1)} />
      </Group>
    </Group>
  );
}
