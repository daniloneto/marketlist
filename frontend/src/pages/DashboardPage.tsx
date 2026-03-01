import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Anchor,
  Badge,
  Button,
  Card,
  Checkbox,
  Group,
  Modal,
  NumberInput,
  Paper,
  Progress,
  RingProgress,
  Select,
  SimpleGrid,
  Stack,
  Table,
  Text,
  Title,
  Tooltip,
} from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { IconAlertTriangle } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { categoriaService, orcamentoService } from '../services';
import { ErrorState, LoadingState } from '../components';
import { PeriodoOrcamentoTipo, type DashboardFinanceiroCategoriaDto } from '../types';

const currency = (value: number | null) => {
  if (value === null) return 'Não definido';
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
};

const monthOptions = [
  { value: '1', label: 'Janeiro' },
  { value: '2', label: 'Fevereiro' },
  { value: '3', label: 'Março' },
  { value: '4', label: 'Abril' },
  { value: '5', label: 'Maio' },
  { value: '6', label: 'Junho' },
  { value: '7', label: 'Julho' },
  { value: '8', label: 'Agosto' },
  { value: '9', label: 'Setembro' },
  { value: '10', label: 'Outubro' },
  { value: '11', label: 'Novembro' },
  { value: '12', label: 'Dezembro' },
];

const toMonthlyPeriodoReferencia = (year: string, month: string) => `${year}-${month.padStart(2, '0')}`;

export function DashboardPage() {
  const queryClient = useQueryClient();
  const now = new Date();
  const [year, setYear] = useState(String(now.getFullYear()));
  const [month, setMonth] = useState(String(now.getMonth() + 1));
  const [categoriaId, setCategoriaId] = useState<string | null>(null);
  const [dataInicio, setDataInicio] = useState<string | null>(null);
  const [dataFim, setDataFim] = useState<string | null>(null);
  const [somenteComOrcamento, setSomenteComOrcamento] = useState(false);
  const [somenteComGasto, setSomenteComGasto] = useState(false);

  const [orcamentoModalAberto, setOrcamentoModalAberto] = useState(false);
  const [categoriaSelecionada, setCategoriaSelecionada] = useState<DashboardFinanceiroCategoriaDto | null>(null);
  const [novoOrcamento, setNovoOrcamento] = useState<number | string>(0);

  const { data: categorias } = useQuery({
    queryKey: ['categorias'],
    queryFn: categoriaService.getAllItems,
  });

  const dashboardQuery = useQuery({
    queryKey: ['dashboard-financeiro', year, month, categoriaId, dataInicio, dataFim, somenteComOrcamento, somenteComGasto],
    queryFn: () =>
      orcamentoService.getDashboardFinanceiro({
        year: Number.parseInt(year, 10),
        month: Number.parseInt(month, 10),
        categoriaId: categoriaId || undefined,
        dataInicio: dataInicio || undefined,
        dataFim: dataFim || undefined,
        somenteComOrcamento,
        somenteComGasto,
      }),
  });

  const definirOrcamentoMutation = useMutation({
    mutationFn: (payload: { categoriaId: string; valorLimite: number }) =>
      orcamentoService.createOrUpdate({
        categoriaId: payload.categoriaId,
        periodoTipo: PeriodoOrcamentoTipo.Mensal,
        periodoReferencia: toMonthlyPeriodoReferencia(year, month),
        valorLimite: payload.valorLimite,
      }),
    onSuccess: () => {
      notifications.show({
        title: 'Sucesso',
        message: 'Orçamento definido com sucesso.',
        color: 'green',
      });
      setOrcamentoModalAberto(false);
      setCategoriaSelecionada(null);
      setNovoOrcamento(0);
      queryClient.invalidateQueries({ queryKey: ['dashboard-financeiro'] });
      dashboardQuery.refetch();
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Não foi possível salvar o orçamento.',
        color: 'red',
      });
    },
  });

  const totalSpent = dashboardQuery.data?.summary.totalSpent ?? 0;

  const barData = useMemo(
    () => [...(dashboardQuery.data?.categories ?? [])].sort((a, b) => b.spentAmount - a.spentAmount),
    [dashboardQuery.data?.categories],
  );

  const percentageColor = (percent: number | null) => {
    if (percent === null) return 'gray';
    if (percent > 100) return 'red';
    if (percent >= 80) return 'yellow';
    return 'green';
  };

  const topPercentage = dashboardQuery.data?.summary.totalPercentageUsed ?? 0;

  const abrirModalDefinicaoOrcamento = (categoria: DashboardFinanceiroCategoriaDto) => {
    setCategoriaSelecionada(categoria);
    setNovoOrcamento(0);
    setOrcamentoModalAberto(true);
  };

  const salvarOrcamentoDaCategoria = () => {
    if (!categoriaSelecionada) return;
    const valorNormalizado = Number(novoOrcamento);

    if (!Number.isFinite(valorNormalizado) || valorNormalizado < 0) {
      notifications.show({
        title: 'Valor inválido',
        message: 'Informe um valor de orçamento maior ou igual a zero.',
        color: 'yellow',
      });
      return;
    }

    definirOrcamentoMutation.mutate({
      categoriaId: categoriaSelecionada.categoryId,
      valorLimite: valorNormalizado,
    });
  };

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Dashboard Financeiro</Title>
        <Button variant="light" onClick={() => dashboardQuery.refetch()}>Atualizar</Button>
      </Group>

      <Paper shadow="xs" p="md">
        <SimpleGrid cols={{ base: 1, md: 3 }}>
          <Select label="Ano" value={year} onChange={(v) => setYear(v || String(now.getFullYear()))} data={Array.from({ length: 6 }, (_, i) => String(now.getFullYear() - i)).map((v) => ({ value: v, label: v }))} />
          <Select label="Mês" value={month} onChange={(v) => setMonth(v || String(now.getMonth() + 1))} data={monthOptions} />
          <Select label="Categoria específica" placeholder="Todas" clearable value={categoriaId} onChange={setCategoriaId} data={(categorias ?? []).map((c) => ({ value: c.id, label: c.nome }))} />
          <DateInput label="Data início" clearable value={dataInicio} onChange={setDataInicio} valueFormat="DD/MM/YYYY" />
          <DateInput label="Data fim" clearable value={dataFim} onChange={setDataFim} valueFormat="DD/MM/YYYY" />
          <Stack gap={4} justify="flex-end">
            <Checkbox label="Somente categorias com orçamento" checked={somenteComOrcamento} onChange={(e) => setSomenteComOrcamento(e.currentTarget.checked)} />
            <Checkbox label="Somente categorias com gasto" checked={somenteComGasto} onChange={(e) => setSomenteComGasto(e.currentTarget.checked)} />
          </Stack>
        </SimpleGrid>
      </Paper>

      <Modal
        opened={orcamentoModalAberto}
        onClose={() => {
          if (definirOrcamentoMutation.isPending) return;
          setOrcamentoModalAberto(false);
        }}
        title="Definir orçamento da categoria"
        centered
      >
        <Stack>
          <Text size="sm">
            Categoria: <Text span fw={700}>{categoriaSelecionada?.categoryName ?? '-'}</Text>
          </Text>
          <Text size="sm">
            Período: <Text span fw={700}>{month.padStart(2, '0')}/{year}</Text>
          </Text>
          <NumberInput
            label="Valor do orçamento"
            min={0}
            step={10}
            decimalScale={2}
            fixedDecimalScale
            prefix="R$ "
            value={novoOrcamento}
            onChange={setNovoOrcamento}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setOrcamentoModalAberto(false)} disabled={definirOrcamentoMutation.isPending}>Cancelar</Button>
            <Button onClick={salvarOrcamentoDaCategoria} loading={definirOrcamentoMutation.isPending}>Salvar orçamento</Button>
          </Group>
        </Stack>
      </Modal>

      {dashboardQuery.isLoading ? (
        <LoadingState />
      ) : dashboardQuery.error ? (
        <ErrorState onRetry={dashboardQuery.refetch} />
      ) : (
        <>
          <SimpleGrid cols={{ base: 1, md: 4 }}>
            <Card withBorder><Text c="dimmed" size="sm">Total Orçado</Text><Title order={3}>{currency(dashboardQuery.data?.summary.totalBudget ?? 0)}</Title></Card>
            <Card withBorder><Text c="dimmed" size="sm">Total Gasto</Text><Title order={3}>{currency(dashboardQuery.data?.summary.totalSpent ?? 0)}</Title></Card>
            <Card withBorder><Text c="dimmed" size="sm">Total Restante</Text><Title order={3}>{currency(dashboardQuery.data?.summary.totalRemaining ?? 0)}</Title></Card>
            <Card withBorder>
              <Text c="dimmed" size="sm">Percentual utilizado</Text>
              <Group justify="space-between"><Title order={3}>{dashboardQuery.data?.summary.totalPercentageUsed?.toFixed(1) ?? 'Não definido'}{dashboardQuery.data?.summary.totalPercentageUsed !== null ? '%' : ''}</Title><Badge color={percentageColor(dashboardQuery.data?.summary.totalPercentageUsed ?? null)}>{percentageColor(dashboardQuery.data?.summary.totalPercentageUsed ?? null).toUpperCase()}</Badge></Group>
              <Progress value={Math.min(topPercentage, 100)} color={percentageColor(dashboardQuery.data?.summary.totalPercentageUsed ?? null)} mt="xs" />
            </Card>
          </SimpleGrid>

          <SimpleGrid cols={{ base: 1, lg: 2 }}>
            <Paper p="md" withBorder>
              <Title order={4} mb="md">Gasto por Categoria</Title>
              <Stack>
                {barData.map((item) => {
                  const width = totalSpent > 0 ? (item.spentAmount / totalSpent) * 100 : 0;
                  return (
                    <Stack key={item.categoryId} gap={4}>
                      <Group justify="space-between">
                        <Text size="sm">{item.categoryName}</Text>
                        <Text size="sm" fw={600}>{currency(item.spentAmount)}</Text>
                      </Group>
                      <Progress value={width} color="blue" />
                    </Stack>
                  );
                })}
              </Stack>
            </Paper>

            <Paper p="md" withBorder>
              <Title order={4} mb="md">Distribuição Percentual dos Gastos</Title>
              <SimpleGrid cols={2}>
                <Stack>
                  {barData.map((item) => {
                    const pct = totalSpent > 0 ? (item.spentAmount / totalSpent) * 100 : 0;
                    return (
                      <Button key={item.categoryId} variant={categoriaId === item.categoryId ? 'filled' : 'light'} justify="space-between" onClick={() => setCategoriaId((prev) => (prev === item.categoryId ? null : item.categoryId))}>
                        {item.categoryName} ({pct.toFixed(1)}%)
                      </Button>
                    );
                  })}
                </Stack>
                <RingProgress
                  size={220}
                  thickness={24}
                  sections={barData.slice(0, 8).map((item, index) => ({ value: totalSpent > 0 ? (item.spentAmount / totalSpent) * 100 : 0, color: ['blue', 'teal', 'grape', 'orange', 'indigo', 'cyan', 'lime', 'pink'][index % 8] }))}
                  label={<Text ta="center" fw={700}>{currency(totalSpent)}</Text>}
                />
              </SimpleGrid>
            </Paper>
          </SimpleGrid>

          <Paper p="md" withBorder>
            <Title order={4} mb="md">Situação por Categoria</Title>
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Categoria</Table.Th>
                  <Table.Th>Orçamento</Table.Th>
                  <Table.Th>Gasto</Table.Th>
                  <Table.Th>Restante</Table.Th>
                  <Table.Th>% Utilizado</Table.Th>
                  <Table.Th>Progresso</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {dashboardQuery.data?.categories.map((item) => (
                  <Table.Tr key={item.categoryId}>
                    <Table.Td>{item.categoryName}</Table.Td>
                    <Table.Td>
                      {item.budgetAmount === null ? (
                        <Group gap="xs">
                          <Text size="sm">Não definido</Text>
                          <Tooltip label="Definir orçamento para este período">
                            <Anchor size="sm" onClick={() => abrirModalDefinicaoOrcamento(item)}>
                              Definir orçamento
                            </Anchor>
                          </Tooltip>
                        </Group>
                      ) : (
                        currency(item.budgetAmount)
                      )}
                    </Table.Td>
                    <Table.Td>{currency(item.spentAmount)}</Table.Td>
                    <Table.Td>{currency(item.remainingAmount)}</Table.Td>
                    <Table.Td>
                      {item.percentageUsed === null ? 'Não definido' : `${item.percentageUsed.toFixed(1)}%`}
                      {item.percentageUsed !== null && item.percentageUsed > 100 && (
                        <Alert color="red" mt="xs" py={6} px="xs" icon={<IconAlertTriangle size={14} />}>Acima do orçamento</Alert>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Progress value={Math.min(item.percentageUsed ?? 0, 100)} color={percentageColor(item.percentageUsed)} />
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </Paper>
        </>
      )}
    </Stack>
  );
}
