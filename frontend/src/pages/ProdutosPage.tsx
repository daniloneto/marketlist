import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Title,
  Button,
  Table,
  Group,
  ActionIcon,
  Modal,
  TextInput,
  Textarea,
  Stack,
  Text,
  Paper,
  Select,
  Badge,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconPlus, IconEdit, IconTrash, IconHistory, IconList, IconCopy } from '@tabler/icons-react';
import { produtoService, categoriaService } from '../services';
import { LoadingState, ErrorState, FormGrid, PaginationControls } from '../components';
import type { ProdutoDto, ProdutoCreateDto, HistoricoPrecoDto } from '../types';

export function ProdutosPage() {
  const queryClient = useQueryClient();
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [historicoModalOpen, setHistoricoModalOpen] = useState(false);
  const [selectedProduto, setSelectedProduto] = useState<ProdutoDto | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [historico, setHistorico] = useState<HistoricoPrecoDto[]>([]);
  const [listaModalOpen, setListaModalOpen] = useState(false);
  const [listaTexto, setListaTexto] = useState('');

  const { data: produtos, isLoading, error, refetch } = useQuery({
    queryKey: ['produtos', page, pageSize],
    queryFn: () => produtoService.getAll(page, pageSize),
  });

  const { data: categorias } = useQuery({
    queryKey: ['categorias'],
    queryFn: categoriaService.getAllItems,
  });

  const form = useForm<ProdutoCreateDto>({
    initialValues: {
      nome: '',
      descricao: '',
      unidade: '',
      categoriaId: '',
    },
    validate: {
      nome: (value) => (value.trim() ? null : 'Nome é obrigatório'),
      categoriaId: (value) => (value ? null : 'Categoria é obrigatória'),
    },
  });

  const createMutation = useMutation({
    mutationFn: produtoService.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['produtos'] });
      setCreateModalOpen(false);
      form.reset();
      notifications.show({
        title: 'Sucesso',
        message: 'Produto criado com sucesso!',
        color: 'green',
      });
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Erro ao criar produto',
        color: 'red',
      });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: ProdutoCreateDto }) =>
      produtoService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['produtos'] });
      setEditModalOpen(false);
      notifications.show({
        title: 'Sucesso',
        message: 'Produto atualizado com sucesso!',
        color: 'green',
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: produtoService.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['produtos'] });
      notifications.show({
        title: 'Sucesso',
        message: 'Produto excluído com sucesso!',
        color: 'green',
      });
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Erro ao excluir produto',
        color: 'red',
      });
    },
  });

  const handleEdit = (produto: ProdutoDto) => {
    setSelectedProduto(produto);
    form.setValues({
      nome: produto.nome,
      descricao: produto.descricao || '',
      unidade: produto.unidade || '',
      categoriaId: produto.categoriaId,
    });
    setEditModalOpen(true);
  };

  const handleCreate = () => {
    form.reset();
    setCreateModalOpen(true);
  };

  const handleViewHistorico = async (produto: ProdutoDto) => {
    setSelectedProduto(produto);
    const data = await produtoService.getHistoricoPrecos(produto.id);
    setHistorico(data);
    setHistoricoModalOpen(true);
  };

  const handleGerarLista = async () => {
    try {
      const texto = await produtoService.gerarListaSimples();
      setListaTexto(texto);
      setListaModalOpen(true);
    } catch {
      notifications.show({
        title: 'Erro',
        message: 'Erro ao gerar lista simples',
        color: 'red',
      });
    }
  };

  const handleCopiarLista = async () => {
    try {
      await navigator.clipboard.writeText(listaTexto);
      notifications.show({
        title: 'Copiado!',
        message: 'Lista copiada para a área de transferência',
        color: 'green',
      });
    } catch {
      notifications.show({
        title: 'Erro',
        message: 'Não foi possível copiar',
        color: 'red',
      });
    }
  };

  const formatCurrency = (value: number | null) => {
    if (value === null) return '-';
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

  const categoriasOptions = (categorias ?? []).map((c) => ({
    value: c.id,
    label: c.nome,
  })) || [];

  return (
    <>
      <Group justify="space-between" mb="md">
        <Title order={2}>Produtos</Title>
        <Group>
          <Button variant="light" leftSection={<IconList size={16} />} onClick={handleGerarLista}>
            Gerar lista simples
          </Button>
          <Button leftSection={<IconPlus size={16} />} onClick={handleCreate}>
            Novo Produto
          </Button>
        </Group>
      </Group>

      <Paper shadow="xs" p="md">
        {isLoading ? (
          <LoadingState />
        ) : error ? (
          <ErrorState onRetry={refetch} />
        ) : (
          <>
            <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Nome</Table.Th>
              <Table.Th>Categoria</Table.Th>
              <Table.Th>Unidade</Table.Th>
              <Table.Th>Último Preço</Table.Th>
              <Table.Th>Ações</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {produtos?.items.map((produto) => (
              <Table.Tr key={produto.id}>
                <Table.Td>
                  <Stack gap={0}>
                    <Text fw={500}>{produto.nome}</Text>
                    {produto.descricao && (
                      <Text size="xs" c="dimmed">
                        {produto.descricao}
                      </Text>
                    )}
                  </Stack>
                </Table.Td>
                <Table.Td>
                  <Badge variant="light">{produto.categoriaNome}</Badge>
                </Table.Td>
                <Table.Td>{produto.unidade || '-'}</Table.Td>
                <Table.Td>{formatCurrency(produto.ultimoPreco)}</Table.Td>
                <Table.Td>
                  <Group gap="xs">
                    <ActionIcon
                      variant="subtle"
                      color="blue"
                      onClick={() => handleViewHistorico(produto)}
                    >
                      <IconHistory size={16} />
                    </ActionIcon>
                    <ActionIcon variant="subtle" color="yellow" onClick={() => handleEdit(produto)}>
                      <IconEdit size={16} />
                    </ActionIcon>
                    <ActionIcon
                      variant="subtle"
                      color="red"
                      onClick={() => deleteMutation.mutate(produto.id)}
                    >
                      <IconTrash size={16} />
                    </ActionIcon>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
            </Table>

            {produtos?.items.length === 0 && (
              <Text c="dimmed" ta="center" py="xl">
                Nenhum produto encontrado
              </Text>
            )}
            {produtos && (
              <PaginationControls
                page={page}
                pageSize={pageSize}
                totalCount={produtos.totalCount}
                totalPages={produtos.totalPages}
                onPageChange={setPage}
                onPageSizeChange={(size) => { setPageSize(size); setPage(1); }}
              />
            )}
          </>
        )}
      </Paper>

      {/* Modal de Criação */}
      <Modal
        opened={createModalOpen}
        onClose={() => setCreateModalOpen(false)}
        title="Novo Produto"
      >
        <form onSubmit={form.onSubmit((values) => createMutation.mutate(values))}>
          <FormGrid>
            <TextInput
              label="Nome"
              placeholder="Ex: Leite Integral"
              {...form.getInputProps('nome')}
            />
            <TextInput
              label="Unidade"
              placeholder="Ex: L, kg, un"
              {...form.getInputProps('unidade')}
            />
            <Select
              label="Categoria"
              placeholder="Selecione uma categoria"
              data={categoriasOptions}
              searchable
              {...form.getInputProps('categoriaId')}
            />
            <Textarea
              label="Descrição"
              placeholder="Descrição opcional"
              {...form.getInputProps('descricao')}
              style={{ gridColumn: '1 / -1' }}
            />
          </FormGrid>
          <Group justify="flex-end" mt="md">
            <Button variant="outline" onClick={() => setCreateModalOpen(false)}>
              Cancelar
            </Button>
            <Button type="submit" loading={createMutation.isPending}>
              Criar
            </Button>
          </Group>
        </form>
      </Modal>

      {/* Modal de Edição */}
      <Modal
        opened={editModalOpen}
        onClose={() => setEditModalOpen(false)}
        title="Editar Produto"
      >
        <form
          onSubmit={form.onSubmit((values) =>
            updateMutation.mutate({ id: selectedProduto!.id, data: values })
          )}
        >
          <FormGrid>
            <TextInput label="Nome" {...form.getInputProps('nome')} />
            <TextInput label="Unidade" {...form.getInputProps('unidade')} />
            <Select
              label="Categoria"
              data={categoriasOptions}
              searchable
              {...form.getInputProps('categoriaId')}
            />
            <Textarea label="Descrição" {...form.getInputProps('descricao')} style={{ gridColumn: '1 / -1' }} />
          </FormGrid>
          <Group justify="flex-end" mt="md">
            <Button variant="outline" onClick={() => setEditModalOpen(false)}>
              Cancelar
            </Button>
            <Button type="submit" loading={updateMutation.isPending}>
              Salvar
            </Button>
          </Group>
        </form>
      </Modal>

      {/* Modal de Histórico de Preços */}
      <Modal
        opened={historicoModalOpen}
        onClose={() => setHistoricoModalOpen(false)}
        title={`Histórico de Preços - ${selectedProduto?.nome}`}
        size="lg"
      >
        {historico.length === 0 ? (
          <Text c="dimmed" ta="center" py="xl">
            Nenhum histórico de preços encontrado
          </Text>
        ) : (
          <Table striped>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Data</Table.Th>
                <Table.Th>Preço</Table.Th>
                <Table.Th>Fonte</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {historico.map((h) => (
                <Table.Tr key={h.id}>
                  <Table.Td>{formatDate(h.dataConsulta)}</Table.Td>
                  <Table.Td fw={500}>{formatCurrency(h.precoUnitario)}</Table.Td>
                  <Table.Td>{h.fontePreco || '-'}</Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        )}
      </Modal>

      {/* Modal de Lista Simples */}
      <Modal
        opened={listaModalOpen}
        onClose={() => setListaModalOpen(false)}
        title="Lista de Compras Simples"
        size="md"
      >
        <Stack>
          <Textarea
            value={listaTexto}
            readOnly
            autosize
            minRows={10}
            maxRows={20}
          />
          <Group justify="flex-end">
            <Button
              leftSection={<IconCopy size={16} />}
              onClick={handleCopiarLista}
            >
              Copiar
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}
