import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Title,
  Table,
  Group,
  Text,
  Paper,
  Select,
} from '@mantine/core';
import { historicoPrecoService, produtoService } from '../services';
import { LoadingState, ErrorState } from '../components';

export function HistoricoPrecosPage() {
  const [produtoId, setProdutoId] = useState<string | null>(null);

  const { data: produtos } = useQuery({
    queryKey: ['produtos'],
    queryFn: produtoService.getAll,
  });

  const { data: historico, isLoading, error, refetch } = useQuery({
    queryKey: ['historico', produtoId],
    queryFn: () =>
      produtoId
        ? historicoPrecoService.getByProduto(produtoId)
        : historicoPrecoService.getAll(),
  });

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const produtosOptions = [
    { value: '', label: 'Todos os produtos' },
    ...(produtos?.map((p) => ({
      value: p.id,
      label: p.nome,
    })) || []),
  ];

  if (isLoading) return <LoadingState />;
  if (error) return <ErrorState onRetry={refetch} />;

  return (
    <>
      <Group justify="space-between" mb="md">
        <Title order={2}>Histórico de Preços</Title>
      </Group>

      <Paper shadow="xs" p="md" mb="md">
        <Select
          label="Filtrar por Produto"
          placeholder="Selecione um produto"
          data={produtosOptions}
          value={produtoId || ''}
          onChange={(value) => setProdutoId(value || null)}
          searchable
          clearable
          style={{ maxWidth: 300 }}
        />
      </Paper>

      <Paper shadow="xs" p="md">
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Produto</Table.Th>
              <Table.Th>Preço</Table.Th>
              <Table.Th>Data da Consulta</Table.Th>
              <Table.Th>Fonte</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {historico?.map((h) => (
              <Table.Tr key={h.id}>
                <Table.Td fw={500}>{h.produtoNome}</Table.Td>
                <Table.Td>{formatCurrency(h.precoUnitario)}</Table.Td>
                <Table.Td>{formatDate(h.dataConsulta)}</Table.Td>
                <Table.Td>{h.fontePreco || '-'}</Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>

        {historico?.length === 0 && (
          <Text c="dimmed" ta="center" py="xl">
            Nenhum histórico de preços encontrado
          </Text>
        )}
      </Paper>
    </>
  );
}
