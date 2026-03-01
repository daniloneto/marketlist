import { useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from '@mantine/form';
import {
  Title,
  Paper,
  Stack,
  Group,
  Select,
  TextInput,
  NumberInput,
  Button,
  Table,
  Text,
  Badge,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { categoriaService, orcamentoService } from '../services';
import { ErrorState, LoadingState, PaginationControls } from '../components';
import {
  PeriodoOrcamentoTipo,
  type CriarOrcamentoCategoriaRequest,
  type PeriodoOrcamentoTipo as PeriodoOrcamentoTipoType,
} from '../types';

interface FormValues {
  periodoTipo: string;
  periodoReferencia: string;
  categoriaId: string;
  valorLimite: number;
}

const PERIODOS_REGEX: Record<PeriodoOrcamentoTipoType, RegExp> = {
  [PeriodoOrcamentoTipo.Mensal]: /^(0[1-9]|1[0-2])-\d{4}$/,
  [PeriodoOrcamentoTipo.Semanal]: /^\d{4}-W(0[1-9]|[1-4][0-9]|5[0-3])$/,
};

const getDefaultPeriodoRef = (periodoTipo: PeriodoOrcamentoTipoType): string => {
  const now = new Date();
  if (periodoTipo === PeriodoOrcamentoTipo.Semanal) {
    const date = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
    const dayNum = date.getUTCDay() || 7;
    date.setUTCDate(date.getUTCDate() + 4 - dayNum);
    const isoYear = date.getUTCFullYear();
    const yearStart = new Date(Date.UTC(isoYear, 0, 1));
    const week = Math.ceil((((date.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
    return `${isoYear}-W${String(week).padStart(2, '0')}`;
  }

  return `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(2, '0')}`;
};

const toDisplayPeriodoRef = (periodoTipo: PeriodoOrcamentoTipoType, value: string): string => {
  if (periodoTipo !== PeriodoOrcamentoTipo.Mensal) return value;

  const isoMatch = value.match(/^(\d{4})-(\d{2})$/);
  if (isoMatch) {
    return `${isoMatch[2]}-${isoMatch[1]}`;
  }

  return value;
};

const toApiPeriodoRef = (periodoTipo: PeriodoOrcamentoTipoType, value: string): string => {
  if (periodoTipo !== PeriodoOrcamentoTipo.Mensal) return value;

  const brMatch = value.match(/^(\d{2})-(\d{4})$/);
  if (brMatch) {
    return `${brMatch[2]}-${brMatch[1]}`;
  }

  return value;
};

export function OrcamentosPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const form = useForm<FormValues>({
    initialValues: {
      periodoTipo: String(PeriodoOrcamentoTipo.Mensal),
      periodoReferencia: toDisplayPeriodoRef(
        PeriodoOrcamentoTipo.Mensal,
        getDefaultPeriodoRef(PeriodoOrcamentoTipo.Mensal),
      ),
      categoriaId: '',
      valorLimite: 0,
    },
    validate: {
      categoriaId: (value) => (value ? null : 'Categoria obrigatória'),
      periodoReferencia: (value, values) => {
        const trimmedValue = value.trim();
        if (!trimmedValue) return 'Período de referência obrigatório';

        const tipo = Number(values.periodoTipo) as PeriodoOrcamentoTipoType;
        const isPeriodoValido = PERIODOS_REGEX[tipo].test(trimmedValue);
        if (isPeriodoValido) return null;

        return tipo === PeriodoOrcamentoTipo.Mensal
          ? 'Use o formato MM-YYYY para período mensal'
          : 'Use o formato YYYY-Www para período semanal';
      },
      valorLimite: (value) => (value < 0 ? 'Valor limite deve ser maior ou igual a zero' : null),
    },
  });

  const periodoTipoSelecionado = Number(form.values.periodoTipo) as PeriodoOrcamentoTipoType;
  const periodoRefSelecionado = form.values.periodoReferencia;
  const periodoRefSelecionadoApi = toApiPeriodoRef(periodoTipoSelecionado, periodoRefSelecionado);

  const { data: categorias, isLoading: categoriasLoading, error: categoriasError, refetch: refetchCategorias } = useQuery({
    queryKey: ['categorias'],
    queryFn: categoriaService.getAllItems,
  });

  const { data: orcamentos, isLoading: orcamentosLoading, error: orcamentosError, refetch: refetchOrcamentos } = useQuery({
    queryKey: ['orcamentos', periodoTipoSelecionado, periodoRefSelecionadoApi, page, pageSize],
    queryFn: () => orcamentoService.listByPeriodo(periodoTipoSelecionado, periodoRefSelecionadoApi, page, pageSize),
  });

  const categoriaOptions = useMemo(
    () => (categorias ?? []).map((categoria) => ({ value: categoria.id, label: categoria.nome })),
    [categorias],
  );

  const createOrUpdateMutation = useMutation({
    mutationFn: (payload: CriarOrcamentoCategoriaRequest) => orcamentoService.createOrUpdate(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ['orcamentos', periodoTipoSelecionado, periodoRefSelecionadoApi],
      });
      notifications.show({
        title: 'Sucesso',
        message: 'Orçamento salvo com sucesso.',
        color: 'green',
      });
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Não foi possível salvar o orçamento.',
        color: 'red',
      });
    },
  });

  const handleSubmit = (values: FormValues) => {
    const tipo = Number(values.periodoTipo) as PeriodoOrcamentoTipoType;

    const payload: CriarOrcamentoCategoriaRequest = {
      categoriaId: values.categoriaId,
      periodoTipo: tipo,
      periodoReferencia: toApiPeriodoRef(tipo, values.periodoReferencia.trim()),
      valorLimite: values.valorLimite,
    };

    createOrUpdateMutation.mutate(payload);
  };

  const formatCurrency = (value: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

  if (categoriasLoading) return <LoadingState />;
  if (categoriasError) return <ErrorState onRetry={refetchCategorias} />;

  let orcamentosContent: ReactNode;
  if (orcamentosLoading) {
    orcamentosContent = <LoadingState message="Carregando orçamentos..." />;
  } else if (orcamentosError) {
    orcamentosContent = <ErrorState onRetry={refetchOrcamentos} />;
  } else {
    orcamentosContent = (
      <>
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Categoria</Table.Th>
              <Table.Th>Período</Table.Th>
              <Table.Th>Limite</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {orcamentos?.items.map((orcamento) => (
              <Table.Tr key={orcamento.id}>
                <Table.Td>{orcamento.nomeCategoria}</Table.Td>
                <Table.Td>{toDisplayPeriodoRef(periodoTipoSelecionado, orcamento.periodoReferencia)}</Table.Td>
                <Table.Td>{formatCurrency(orcamento.valorLimite)}</Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
        {orcamentos && (
          <PaginationControls
            page={page}
            pageSize={pageSize}
            totalCount={orcamentos.totalCount}
            totalPages={orcamentos.totalPages}
            onPageChange={setPage}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(1);
            }}
          />
        )}
      </>
    );
  }

  return (
    <Stack>
      <Title order={2}>Orçamentos por Categoria</Title>

      <Paper shadow="xs" p="md">
        <form onSubmit={form.onSubmit(handleSubmit)}>
          <Stack>
            <Group grow>
              <Select
                label="Período"
                data={[
                  { value: String(PeriodoOrcamentoTipo.Mensal), label: 'Mensal' },
                  { value: String(PeriodoOrcamentoTipo.Semanal), label: 'Semanal' },
                ]}
                value={form.values.periodoTipo}
                onChange={(value) => {
                  if (!value) return;
                  const tipo = Number(value) as PeriodoOrcamentoTipoType;
                  form.setFieldValue('periodoTipo', value);
                  form.setFieldValue('periodoReferencia', toDisplayPeriodoRef(tipo, getDefaultPeriodoRef(tipo)));
                  setPage(1);
                }}
                required
              />

              <TextInput
                label="Período de referência"
                placeholder={periodoTipoSelecionado === PeriodoOrcamentoTipo.Mensal ? 'MM-YYYY' : 'YYYY-Www'}
                {...form.getInputProps('periodoReferencia')}
                onBlur={() => setPage(1)}
              />
            </Group>

            <Group grow align="flex-end">
              <Select
                label="Categoria"
                placeholder="Selecione uma categoria"
                data={categoriaOptions}
                searchable
                {...form.getInputProps('categoriaId')}
              />

              <NumberInput
                label="Valor do orçamento"
                min={0}
                step={10}
                decimalScale={2}
                fixedDecimalScale
                prefix="R$ "
                {...form.getInputProps('valorLimite')}
              />
            </Group>

            <Group justify="flex-end">
              <Button type="submit" loading={createOrUpdateMutation.isPending}>
                Salvar
              </Button>
            </Group>
          </Stack>
        </form>
      </Paper>

      <Paper shadow="xs" p="md">
        <Group justify="space-between" mb="sm">
          <Text fw={600}>Orçamentos do período</Text>
          <Badge variant="light">{toDisplayPeriodoRef(periodoTipoSelecionado, periodoRefSelecionado)}</Badge>
        </Group>

        {orcamentosContent}

        {(orcamentos?.items.length ?? 0) === 0 && !orcamentosLoading && (
          <Text c="dimmed" ta="center" py="md">
            Nenhum orçamento cadastrado para este período.
          </Text>
        )}
      </Paper>
    </Stack>
  );
}
