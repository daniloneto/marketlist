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
  Badge,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconPlus, IconEdit, IconTrash } from '@tabler/icons-react';
import { categoriaService } from '../services';
import { LoadingState, ErrorState, PaginationControls } from '../components';
import type { CategoriaDto, CategoriaCreateDto } from '../types';

export function CategoriasPage() {
  const queryClient = useQueryClient();
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [selectedCategoria, setSelectedCategoria] = useState<CategoriaDto | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const { data: categorias, isLoading, error, refetch } = useQuery({
    queryKey: ['categorias', page, pageSize],
    queryFn: () => categoriaService.getAll(page, pageSize),
  });

  const form = useForm<CategoriaCreateDto>({
    initialValues: {
      nome: '',
      descricao: '',
    },
    validate: {
      nome: (value) => (value.trim() ? null : 'Nome é obrigatório'),
    },
  });

  const createMutation = useMutation({
    mutationFn: categoriaService.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categorias'] });
      setCreateModalOpen(false);
      form.reset();
      notifications.show({
        title: 'Sucesso',
        message: 'Categoria criada com sucesso!',
        color: 'green',
      });
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Erro ao criar categoria',
        color: 'red',
      });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: CategoriaCreateDto }) =>
      categoriaService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categorias'] });
      setEditModalOpen(false);
      notifications.show({
        title: 'Sucesso',
        message: 'Categoria atualizada com sucesso!',
        color: 'green',
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: categoriaService.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categorias'] });
      notifications.show({
        title: 'Sucesso',
        message: 'Categoria excluída com sucesso!',
        color: 'green',
      });
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Não é possível excluir uma categoria com produtos associados',
        color: 'red',
      });
    },
  });

  const handleEdit = (categoria: CategoriaDto) => {
    setSelectedCategoria(categoria);
    form.setValues({ nome: categoria.nome, descricao: categoria.descricao || '' });
    setEditModalOpen(true);
  };

  const handleCreate = () => {
    form.reset();
    setCreateModalOpen(true);
  };

  return (
    <>
      <Group justify="space-between" mb="md">
        <Title order={2}>Categorias</Title>
        <Button leftSection={<IconPlus size={16} />} onClick={handleCreate}>
          Nova Categoria
        </Button>
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
              <Table.Th>Descrição</Table.Th>
              <Table.Th>Produtos</Table.Th>
              <Table.Th>Ações</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {categorias?.items.map((categoria) => (
              <Table.Tr key={categoria.id}>
                <Table.Td fw={500}>{categoria.nome}</Table.Td>
                <Table.Td>{categoria.descricao || '-'}</Table.Td>
                <Table.Td>
                  <Badge variant="light">{categoria.quantidadeProdutos}</Badge>
                </Table.Td>
                <Table.Td>
                  <Group gap="xs">
                    <ActionIcon variant="subtle" color="yellow" onClick={() => handleEdit(categoria)}>
                      <IconEdit size={16} />
                    </ActionIcon>
                    <ActionIcon
                      variant="subtle"
                      color="red"
                      onClick={() => deleteMutation.mutate(categoria.id)}
                      disabled={categoria.quantidadeProdutos > 0}
                    >
                      <IconTrash size={16} />
                    </ActionIcon>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
            </Table>

            {categorias?.items.length === 0 && (
              <Text c="dimmed" ta="center" py="xl">
                Nenhuma categoria encontrada
              </Text>
            )}
            {categorias && (
              <PaginationControls
                page={page}
                pageSize={pageSize}
                totalCount={categorias.totalCount}
                totalPages={categorias.totalPages}
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
        title="Nova Categoria"
      >
        <form onSubmit={form.onSubmit((values) => createMutation.mutate(values))}>
          <Stack>
            <TextInput
              label="Nome"
              placeholder="Ex: Laticínios"
              {...form.getInputProps('nome')}
            />
            <Textarea
              label="Descrição"
              placeholder="Descrição opcional"
              {...form.getInputProps('descricao')}
            />
            <Group justify="flex-end">
              <Button variant="outline" onClick={() => setCreateModalOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" loading={createMutation.isPending}>
                Criar
              </Button>
            </Group>
          </Stack>
        </form>
      </Modal>

      {/* Modal de Edição */}
      <Modal
        opened={editModalOpen}
        onClose={() => setEditModalOpen(false)}
        title="Editar Categoria"
      >
        <form
          onSubmit={form.onSubmit((values) =>
            updateMutation.mutate({ id: selectedCategoria!.id, data: values })
          )}
        >
          <Stack>
            <TextInput label="Nome" {...form.getInputProps('nome')} />
            <Textarea label="Descrição" {...form.getInputProps('descricao')} />
            <Group justify="flex-end">
              <Button variant="outline" onClick={() => setEditModalOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" loading={updateMutation.isPending}>
                Salvar
              </Button>
            </Group>
          </Stack>
        </form>
      </Modal>
    </>
  );
}
