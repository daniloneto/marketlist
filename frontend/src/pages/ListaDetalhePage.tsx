import type { ReactNode } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Title,
  Paper,
  Group,
  Button,
  Table,
  Text,
  Checkbox,
  NumberInput,
  ActionIcon,
  Stack,
  Card,
  Grid,
  Alert,
  Progress,
  Badge,
} from '@mantine/core';
import { IconArrowLeft, IconTrash, IconAlertCircle, IconRefresh } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { listaDeComprasService } from '../services';
import { formatDateTimeInUserTimeZone, formatExtractedDateTime } from '../utils/date';
import { LoadingState, ErrorState, StatusBadge } from '../components';
import {
  StatusConsumoOrcamento,
  StatusLista,
  type ItemListaDeComprasDto,
} from '../types';

export function ListaDetalhePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: lista, isLoading, error, refetch } = useQuery({
    queryKey: ['lista', id],
    queryFn: () => listaDeComprasService.getById(id!),
    refetchInterval: (query) => {
      const data = query.state.data;
      return data?.status === StatusLista.Processando ? 2000 : false;
    },
  });

  const {
    data: resumoOrcamento,
    isLoading: resumoLoading,
    error: resumoError,
    refetch: refetchResumo,
  } = useQuery({
    queryKey: ['resumo-orcamento', id],
    queryFn: () => listaDeComprasService.getResumoOrcamento(id!),
    enabled: !!id,
  });

  const updateItemMutation = useMutation({
    mutationFn: ({ itemId, data }: { itemId: string; data: { quantidade: number; comprado: boolean } }) =>
      listaDeComprasService.updateItem(id!, itemId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lista', id] });
      queryClient.invalidateQueries({ queryKey: ['resumo-orcamento', id] });
    },
  });

  const removeItemMutation = useMutation({
    mutationFn: (itemId: string) => listaDeComprasService.removeItem(id!, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lista', id] });
      queryClient.invalidateQueries({ queryKey: ['resumo-orcamento', id] });
      notifications.show({
        title: 'Sucesso',
        message: 'Item removido com sucesso!',
        color: 'green',
      });
    },
  });

  const handleToggleComprado = (item: ItemListaDeComprasDto) => {
    updateItemMutation.mutate({
      itemId: item.id,
      data: { quantidade: item.quantidade, comprado: !item.comprado },
    });
  };

  const handleQuantidadeChange = (item: ItemListaDeComprasDto, quantidade: number) => {
    updateItemMutation.mutate({
      itemId: item.id,
      data: { quantidade, comprado: item.comprado },
    });
  };

  const formatCurrency = (value: number | null) => {
    if (value === null) return '-';
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const formatDate = (dateString: string | null) => {
    if (!dateString) return '-';
    return formatDateTimeInUserTimeZone(dateString);
  };

  const alertas = resumoOrcamento?.itensPorCategoria.filter((item) => item.status === StatusConsumoOrcamento.Alerta).length ?? 0;
  const estouradas = resumoOrcamento?.itensPorCategoria.filter((item) => item.status === StatusConsumoOrcamento.Estourado).length ?? 0;
  const resumoVazio = (resumoOrcamento?.itensPorCategoria.length ?? 0) === 0;

  let resumoContent: ReactNode;
  if (resumoLoading) {
    resumoContent = <LoadingState message="Carregando resumo de orcamento..." />;
  } else if (resumoError) {
    resumoContent = <ErrorState onRetry={refetchResumo} message="Nao foi possivel carregar o resumo de orcamento." />;
  } else if (resumoVazio) {
    resumoContent = <Text c="dimmed">Nenhum item para calcular resumo de orcamento.</Text>;
  } else {
    resumoContent = (
      <Stack gap="md">
        {resumoOrcamento?.itensPorCategoria.map((item) => {
          const progressValue = Math.min(item.percentualConsumido, 100);
          let color: 'green' | 'yellow' | 'red' = 'green';

          if (item.status === StatusConsumoOrcamento.Estourado) {
            color = 'red';
          } else if (item.status === StatusConsumoOrcamento.Alerta) {
            color = 'yellow';
          }

          return (
            <Stack key={item.categoriaId} gap={4}>
              <Group justify="space-between">
                <Text fw={600}>{item.nomeCategoria}</Text>
                <Text size="sm">
                  {formatCurrency(item.totalEstimado)} / {formatCurrency(item.valorLimite)}
                </Text>
              </Group>
              <Progress value={progressValue} color={color} />
              <Group justify="space-between">
                <Text size="xs" c="dimmed">{item.percentualConsumido.toFixed(1)}%</Text>
                {item.itensSemPreco > 0 && (
                  <Text size="xs" c="dimmed">{item.itensSemPreco} item(ns) sem preco.</Text>
                )}
              </Group>
            </Stack>
          );
        })}
      </Stack>
    );
  }

  if (isLoading) return <LoadingState />;
  if (error) return <ErrorState onRetry={refetch} />;
  if (!lista) return <ErrorState message="Lista nao encontrada" />;

  return (
    <>
      <Group justify="space-between" mb="md">
        <Group>
          <Button variant="subtle" leftSection={<IconArrowLeft size={16} />} onClick={() => navigate('/')}>
            Voltar
          </Button>
          <Title order={2}>{lista.nome}</Title>
          <StatusBadge status={lista.status} />
        </Group>
        <Button
          variant="light"
          leftSection={<IconRefresh size={16} />}
          onClick={() => {
            refetch();
            refetchResumo();
          }}
        >
          Atualizar
        </Button>
      </Group>

      {lista.status === StatusLista.Processando && (
        <Alert icon={<IconAlertCircle size={16} />} color="blue" mb="md">
          A lista esta sendo processada. Os itens serao exibidos em breve.
        </Alert>
      )}

      {lista.status === StatusLista.Erro && (
        <Alert icon={<IconAlertCircle size={16} />} color="red" mb="md">
          Erro no processamento: {lista.erroProcessamento}
        </Alert>
      )}

      {estouradas > 0 && (
        <Alert icon={<IconAlertCircle size={16} />} color="red" mb="md">
          Orcamento estourado em {estouradas} categoria(s).
        </Alert>
      )}

      {estouradas === 0 && alertas > 0 && (
        <Alert icon={<IconAlertCircle size={16} />} color="yellow" mb="md">
          Atencao: voce esta perto do limite em {alertas} categoria(s).
        </Alert>
      )}

      <Grid mb="md">
        <Grid.Col span={{ base: 12, md: 4 }}>
          <Card shadow="xs" padding="md">
            <Text size="sm" c="dimmed">Total de Itens</Text>
            <Text size="xl" fw={700}>{lista.itens.length}</Text>
          </Card>
        </Grid.Col>
        <Grid.Col span={{ base: 12, md: 4 }}>
          <Card shadow="xs" padding="md">
            <Text size="sm" c="dimmed">Valor Total Estimado</Text>
            <Text size="xl" fw={700}>
              {formatCurrency(resumoOrcamento ? resumoOrcamento.totalLista : null)}
            </Text>
            {(resumoOrcamento?.totalItensSemPreco ?? 0) > 0 && (
              <Text size="xs" c="dimmed" mt={4}>
                {resumoOrcamento?.totalItensSemPreco} item(ns) sem preco: considerado R$ 0,00.
              </Text>
            )}
          </Card>
        </Grid.Col>
        <Grid.Col span={{ base: 12, md: 4 }}>
          <Card shadow="xs" padding="md">
            <Text size="sm" c="dimmed">Processado em</Text>
            <Text size="xl" fw={700}>{formatDate(lista.processadoEm)}</Text>
            <Text size="sm" c="dimmed" mt="sm">Data da Compra</Text>
            <Text size="lg" fw={600}>{formatExtractedDateTime(lista.dataCompra ?? null)}</Text>
          </Card>
        </Grid.Col>
      </Grid>

      <Paper shadow="xs" p="md" mb="md">
        <Group justify="space-between" mb="sm">
          <Title order={4}>Resumo de Orcamento por Categoria</Title>
          {resumoOrcamento && (
            <Badge variant="light">{resumoOrcamento.periodoReferencia}</Badge>
          )}
        </Group>

        {resumoContent}
      </Paper>

      {lista.textoOriginal && (
        <Paper shadow="xs" p="md" mb="md">
          <Text size="sm" fw={500} mb="xs">Texto Original:</Text>
          <Text size="sm" c="dimmed" style={{ whiteSpace: 'pre-wrap' }}>
            {lista.textoOriginal}
          </Text>
        </Paper>
      )}

      <Paper shadow="xs" p="md">
        <Title order={4} mb="md">Itens da Lista</Title>

        {lista.itens.length === 0 ? (
          <Text c="dimmed" ta="center" py="xl">
            {lista.status === StatusLista.Processando
              ? 'Aguardando processamento...'
              : 'Nenhum item na lista'}
          </Text>
        ) : (
          <Table striped highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th style={{ width: 50 }}>OK</Table.Th>
                <Table.Th>Produto</Table.Th>
                <Table.Th>Quantidade</Table.Th>
                <Table.Th>Unidade</Table.Th>
                <Table.Th>Preco Un.</Table.Th>
                <Table.Th>Subtotal</Table.Th>
                <Table.Th>Acoes</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {lista.itens.map((item) => (
                <Table.Tr
                  key={item.id}
                  style={{
                    textDecoration: item.comprado ? 'line-through' : 'none',
                    opacity: item.comprado ? 0.6 : 1,
                  }}
                >
                  <Table.Td>
                    <Checkbox
                      checked={item.comprado}
                      onChange={() => handleToggleComprado(item)}
                    />
                  </Table.Td>
                  <Table.Td>
                    <Stack gap={0}>
                      <Text fw={500}>{item.produtoNome}</Text>
                      {item.textoOriginal && (
                        <Text size="xs" c="dimmed">
                          Original: {item.textoOriginal}
                        </Text>
                      )}
                    </Stack>
                  </Table.Td>
                  <Table.Td>
                    <NumberInput
                      value={item.quantidade}
                      onChange={(value) => handleQuantidadeChange(item, Number(value) || 1)}
                      min={0.1}
                      step={0.5}
                      decimalScale={2}
                      style={{ width: 80 }}
                      size="xs"
                    />
                  </Table.Td>
                  <Table.Td>{item.produtoUnidade || '-'}</Table.Td>
                  <Table.Td>{formatCurrency(item.precoUnitario)}</Table.Td>
                  <Table.Td fw={500}>{formatCurrency(item.subTotal)}</Table.Td>
                  <Table.Td>
                    <ActionIcon
                      variant="subtle"
                      color="red"
                      onClick={() => removeItemMutation.mutate(item.id)}
                    >
                      <IconTrash size={16} />
                    </ActionIcon>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        )}
      </Paper>
    </>
  );
}
