import { Badge } from '@mantine/core';
import { StatusLista } from '../types';

interface StatusBadgeProps {
  status: StatusLista;
}

const statusConfig: Record<StatusLista, { color: string; label: string }> = {
  [StatusLista.Pendente]: { color: 'yellow', label: 'Pendente' },
  [StatusLista.Processando]: { color: 'blue', label: 'Processando' },
  [StatusLista.Concluida]: { color: 'green', label: 'Concluída' },
  [StatusLista.Erro]: { color: 'red', label: 'Erro' },
};

export function StatusBadge({ status }: StatusBadgeProps) {
  const config = statusConfig[status];
  return <Badge color={config.color}>{config.label}</Badge>;
}
