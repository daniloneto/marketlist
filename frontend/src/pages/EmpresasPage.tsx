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
  Stack,
  Paper,
  Badge,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconPlus, IconEdit, IconTrash, IconBuilding } from '@tabler/icons-react';
import { empresaService } from '../services';
import { LoadingState, ErrorState } from '../components';
import type { EmpresaDto, EmpresaCreateDto } from '../types';

export function EmpresasPage() {
  const queryClient = useQueryClient();
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [selectedEmpresa, setSelectedEmpresa] = useState<EmpresaDto | null>(null);

  const { data: empresas, isLoading, error, refetch } = useQuery({
    queryKey: ['empresas'],
    queryFn: empresaService.getAll,
  });

  const form = useForm<EmpresaCreateDto>({
    initialValues: {
      nome: '',
      cnpj: '',
    },
    validate: {
      nome: (value) => (value.trim() ? null : 'Nome é obrigatório'),
    },
  });

  const createMutation = useMutation({
    mutationFn: empresaService.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['empresas'] });
      setCreateModalOpen(false);
      form.reset();
      notifications.show({
        title: 'Sucesso',
        message: 'Empresa criada com sucesso!',
        color: 'green',
      });
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Erro ao criar empresa',
        color: 'red',
      });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: EmpresaCreateDto }) =>
      empresaService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['empresas'] });
      setEditModalOpen(false);
      notifications.show({
        title: 'Sucesso',
        message: 'Empresa atualizada com sucesso!',
        color: 'green',
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: empresaService.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['empresas'] });
      notifications.show({
        title: 'Sucesso',
        message: 'Empresa excluída com sucesso!',
        color: 'green',
      });
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Não é possível excluir uma empresa com listas associadas',
        color: 'red',
      });
    },
  });

  const handleEdit = (empresa: EmpresaDto) => {
    setSelectedEmpresa(empresa);
    form.setValues({ nome: empresa.nome, cnpj: empresa.cnpj || '' });
    setEditModalOpen(true);
  };

  const handleCreate = () => {
    form.reset();
    setCreateModalOpen(true);
  };

  return (
    <>
      <Group justify="space-between" mb="md">
        <Title order={2}>Empresas</Title>
        <Button leftSection={<IconPlus size={16} />} onClick={handleCreate}>
          Nova Empresa
        </Button>
      </Group>

      <Paper shadow="xs" p="md">
        {isLoading ? (
          <LoadingState />
        ) : error ? (
          <ErrorState onRetry={refetch} />
        ) : (
          <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Nome</Table.Th>
              <Table.Th>CNPJ</Table.Th>
              <Table.Th>Listas</Table.Th>
              <Table.Th>Ações</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {empresas?.map((empresa) => (
              <Table.Tr key={empresa.id}>
                <Table.Td>
                  <Group gap="xs">
                    <IconBuilding size={16} />
                    <span style={{ fontWeight: 500 }}>{empresa.nome}</span>
                  </Group>
                </Table.Td>
                <Table.Td>{empresa.cnpj || '-'}</Table.Td>
                <Table.Td>
                  <Badge variant="light">{empresa.quantidadeListas}</Badge>
                </Table.Td>
                <Table.Td>
                  <Group gap="xs">
                    <ActionIcon variant="subtle" color="yellow" onClick={() => handleEdit(empresa)}>
                      <IconEdit size={16} />
                    </ActionIcon>
                    <ActionIcon
                      variant="subtle"
                      color="red"
                      onClick={() => deleteMutation.mutate(empresa.id)}
                      disabled={empresa.quantidadeListas > 0}
                    >
                      <IconTrash size={16} />
                    </ActionIcon>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
          </Table>
        )}
      </Paper>

      {/* Modal para criar empresa */}
      <Modal
        opened={createModalOpen}
        onClose={() => setCreateModalOpen(false)}
        title="Nova Empresa"
      >
        <form
          onSubmit={form.onSubmit((values) => {
            createMutation.mutate(values);
          })}
        >
          <Stack>
            <TextInput
              label="Nome"
              placeholder="Nome da empresa"
              required
              {...form.getInputProps('nome')}
            />
            <TextInput
              label="CNPJ"
              placeholder="00.000.000/0000-00"
              {...form.getInputProps('cnpj')}
            />
            <Button type="submit" loading={createMutation.isPending}>
              Criar
            </Button>
          </Stack>
        </form>
      </Modal>

      {/* Modal para editar empresa */}
      <Modal
        opened={editModalOpen}
        onClose={() => setEditModalOpen(false)}
        title="Editar Empresa"
      >
        <form
          onSubmit={form.onSubmit((values) => {
            if (selectedEmpresa) {
              updateMutation.mutate({ id: selectedEmpresa.id, data: values });
            }
          })}
        >
          <Stack>
            <TextInput
              label="Nome"
              placeholder="Nome da empresa"
              required
              {...form.getInputProps('nome')}
            />
            <TextInput
              label="CNPJ"
              placeholder="00.000.000/0000-00"
              {...form.getInputProps('cnpj')}
            />
            <Button type="submit" loading={updateMutation.isPending}>
              Atualizar
            </Button>
          </Stack>
        </form>
      </Modal>
    </>
  );
}
