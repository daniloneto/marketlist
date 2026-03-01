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

export function OrcamentosPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const form = useForm<FormValues>({
    initialValues: {
      periodoTipo: String(PeriodoOrcamentoTipo.Mensal),
      periodoReferencia: getDefaultPeriodoRef(PeriodoOrcamentoTipo.Mensal),
      categoriaId: '',
      valorLimite: 0,
    },
    validate: {
      categoriaId: (value) => (value ? null : 'Categoria obrigatoria'),
      periodoReferencia: (value) => (value.trim() ? null : 'Periodo de referencia obrigatorio'),
      valorLimite: (value) => (value < 0 ? 'Valor limite deve ser maior ou igual a zero' : null),
    },
  });

  const periodoTipoSelecionado = Number(form.values.periodoTipo) as PeriodoOrcamentoTipoType;
  const periodoRefSelecionado = form.values.periodoReferencia;

  const { data: categorias, isLoading: categoriasLoading, error: categoriasError, refetch: refetchCategorias } = useQuery({
    queryKey: ['categorias'],
    queryFn: categoriaService.getAllItems,
  });

  const { data: orcamentos, isLoading: orcamentosLoading, error: orcamentosError, refetch: refetchOrcamentos } = useQuery({
    queryKey: ['orcamentos', periodoTipoSelecionado, periodoRefSelecionado, page, pageSize],
    queryFn: () => orcamentoService.listByPeriodo(periodoTipoSelecionado, periodoRefSelecionado, page, pageSize),
  });

  const categoriaOptions = useMemo(
    () => (categorias ?? []).map((categoria) => ({ value: categoria.id, label: categoria.nome })),
    [categorias]
  );

  const createOrUpdateMutation = useMutation({
    mutationFn: (payload: CriarOrcamentoCategoriaRequest) => orcamentoService.createOrUpdate(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ['orcamentos', periodoTipoSelecionado, periodoRefSelecionado],
      });
      notifications.show({
        title: 'Sucesso',
        message: 'Orcamento salvo com sucesso.',
        color: 'green',
      });
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Nao foi possivel salvar o orcamento.',
        color: 'red',
      });
    },
  });

  const handleSubmit = (values: FormValues) => {
    const payload: CriarOrcamentoCategoriaRequest = {
      categoriaId: values.categoriaId,
      periodoTipo: Number(values.periodoTipo) as PeriodoOrcamentoTipoType,
      periodoReferencia: values.periodoReferencia,
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
    orcamentosContent = <LoadingState message="Carregando orcamentos..." />;
  } else if (orcamentosError) {
    orcamentosContent = <ErrorState onRetry={refetchOrcamentos} />;
  } else {
    orcamentosContent = (
      <>
      <Table striped highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Categoria</Table.Th>
            <Table.Th>Periodo</Table.Th>
            <Table.Th>Limite</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {orcamentos?.items.map((orcamento) => (
            <Table.Tr key={orcamento.id}>
              <Table.Td>{orcamento.nomeCategoria}</Table.Td>
              <Table.Td>{orcamento.periodoReferencia}</Table.Td>
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
          onPageSizeChange={(size) => { setPageSize(size); setPage(1); }}
        />
      )}
    </>
    );
  }

  return (
    <Stack>
      <Title order={2}>Orcamentos por Categoria</Title>

      <Paper shadow="xs" p="md">
        <form onSubmit={form.onSubmit(handleSubmit)}>
          <Stack>
            <Group grow>
              <Select
                label="Periodo"
                data={[
                  { value: String(PeriodoOrcamentoTipo.Mensal), label: 'Mensal' },
                  { value: String(PeriodoOrcamentoTipo.Semanal), label: 'Semanal' },
                ]}
                value={form.values.periodoTipo}
                onChange={(value) => {
                  if (!value) return;
                  const tipo = Number(value) as PeriodoOrcamentoTipoType;
                  form.setFieldValue('periodoTipo', value);
                  form.setFieldValue('periodoReferencia', getDefaultPeriodoRef(tipo));
                  setPage(1);
                }}
                required
              />

              <TextInput
                label="Periodo de referencia"
                placeholder={periodoTipoSelecionado === PeriodoOrcamentoTipo.Mensal ? 'YYYY-MM' : 'YYYY-Www'}
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
                label="Valor do orcamento"
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
          <Text fw={600}>Orcamentos do periodo</Text>
          <Badge variant="light">{periodoRefSelecionado}</Badge>
        </Group>

        {orcamentosContent}

        {(orcamentos?.items.length ?? 0) === 0 && !orcamentosLoading && (
          <Text c="dimmed" ta="center" py="md">
            Nenhum orcamento cadastrado para este periodo.
          </Text>
        )}
      </Paper>
    </Stack>
  );
}
