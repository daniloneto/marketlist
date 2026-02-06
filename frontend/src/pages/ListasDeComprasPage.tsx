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
  Select,
  Alert,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconPlus, IconEdit, IconTrash, IconEye, IconInfoCircle, IconBuilding } from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';
import { listaDeComprasService, empresaService } from '../services';
import { LoadingState, ErrorState, StatusBadge } from '../components';
import type { ListaDeComprasDto, ListaDeComprasCreateDto, EmpresaCreateDto } from '../types';
import { TipoEntrada } from '../types';

export function ListasDeComprasPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [empresaModalOpen, setEmpresaModalOpen] = useState(false);
  const [selectedLista, setSelectedLista] = useState<ListaDeComprasDto | null>(null);

  const { data: listas, isLoading, error, refetch } = useQuery({
    queryKey: ['listas'],
    queryFn: listaDeComprasService.getAll,
  });

  const { data: empresas } = useQuery({
    queryKey: ['empresas'],
    queryFn: empresaService.getAll,
  });  const createForm = useForm<ListaDeComprasCreateDto>({
    initialValues: {
      nome: '',
      textoOriginal: '',
      tipoEntrada: TipoEntrada.ListaSimples,
      empresaId: undefined,
    },
    validate: {
      nome: (value) => (value.trim() ? null : 'Nome é obrigatório'),
      textoOriginal: (value) => (value.trim() ? null : 'Lista de itens é obrigatória'),
      empresaId: (value, values) => {
        if (values.tipoEntrada === TipoEntrada.NotaFiscal && !value) {
          return 'Empresa é obrigatória para Nota Fiscal';
        }
        return null;
      },
    },
  });

  const empresaForm = useForm<EmpresaCreateDto>({
    initialValues: {
      nome: '',
      cnpj: '',
    },
    validate: {
      nome: (value) => (value.trim() ? null : 'Nome é obrigatório'),
    },
  });

  const editForm = useForm({
    initialValues: {
      nome: '',
    },
  });

  const createMutation = useMutation({
    mutationFn: listaDeComprasService.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['listas'] });
      setCreateModalOpen(false);
      createForm.reset();
      notifications.show({
        title: 'Sucesso',
        message: 'Lista criada! O processamento está sendo executado em segundo plano.',
        color: 'green',
      });
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Erro ao criar lista',
        color: 'red',
      });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: { nome: string } }) =>
      listaDeComprasService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['listas'] });
      setEditModalOpen(false);
      notifications.show({
        title: 'Sucesso',
        message: 'Lista atualizada com sucesso!',
        color: 'green',
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: listaDeComprasService.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['listas'] });
      notifications.show({
        title: 'Sucesso',
        message: 'Lista excluída com sucesso!',
        color: 'green',
      });
    },
  });

  const createEmpresaMutation = useMutation({
    mutationFn: empresaService.create,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['empresas'] });
      setEmpresaModalOpen(false);
      empresaForm.reset();
      createForm.setFieldValue('empresaId', data.id);
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

  const handleEdit = (lista: ListaDeComprasDto) => {
    setSelectedLista(lista);
    editForm.setValues({ nome: lista.nome });
    setEditModalOpen(true);
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

  if (isLoading) return <LoadingState />;
  if (error) return <ErrorState onRetry={refetch} />;

  return (
    <>
      <Group justify="space-between" mb="md">
        <Title order={2}>Listas de Compras</Title>
        <Button leftSection={<IconPlus size={16} />} onClick={() => setCreateModalOpen(true)}>
          Nova Lista
        </Button>
      </Group>

      <Paper shadow="xs" p="md">        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Nome</Table.Th>
              <Table.Th>Tipo</Table.Th>
              <Table.Th>Empresa</Table.Th>
              <Table.Th>Status</Table.Th>
              <Table.Th>Itens</Table.Th>
              <Table.Th>Valor Total</Table.Th>
              <Table.Th>Criado em</Table.Th>
              <Table.Th>Ações</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {listas?.map((lista) => (
              <Table.Tr key={lista.id}>
                <Table.Td>{lista.nome}</Table.Td>
                <Table.Td>
                  <Badge color={lista.tipoEntrada === 0 ? 'blue' : 'green'} variant="light">
                    {lista.tipoEntrada === 0 ? 'Lista Simples' : 'Nota Fiscal'}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  {lista.empresaNome ? (
                    <Group gap="xs">
                      <IconBuilding size={14} />
                      <Text size="sm">{lista.empresaNome}</Text>
                    </Group>
                  ) : (
                    '-'
                  )}
                </Table.Td>
                <Table.Td>
                  <StatusBadge status={lista.status} />
                </Table.Td>
                <Table.Td>
                  <Badge variant="light">{lista.quantidadeItens}</Badge>
                </Table.Td>
                <Table.Td>{formatCurrency(lista.valorTotal)}</Table.Td>
                <Table.Td>{formatDate(lista.createdAt)}</Table.Td>
                <Table.Td>
                  <Group gap="xs">
                    <ActionIcon
                      variant="subtle"
                      color="blue"
                      onClick={() => navigate(`/listas/${lista.id}`)}
                    >
                      <IconEye size={16} />
                    </ActionIcon>
                    <ActionIcon variant="subtle" color="yellow" onClick={() => handleEdit(lista)}>
                      <IconEdit size={16} />
                    </ActionIcon>
                    <ActionIcon
                      variant="subtle"
                      color="red"
                      onClick={() => deleteMutation.mutate(lista.id)}
                    >
                      <IconTrash size={16} />
                    </ActionIcon>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>

        {listas?.length === 0 && (
          <Text c="dimmed" ta="center" py="xl">
            Nenhuma lista de compras encontrada
          </Text>
        )}
      </Paper>

      {/* Modal de Criação */}
      <Modal
        opened={createModalOpen}
        onClose={() => setCreateModalOpen(false)}
        title="Nova Lista de Compras"
        size="lg"
        centered
        styles={{
          body: { maxHeight: '70vh', overflow: 'auto' },
        }}
      >        <form onSubmit={createForm.onSubmit((values) => createMutation.mutate(values))}>
          <Stack>
            <TextInput
              label="Nome da Lista"
              placeholder="Ex: Compras do mês"
              {...createForm.getInputProps('nome')}
            />
              <Select
              label="Tipo de Entrada"
              placeholder="Selecione o tipo"
              data={[
                { value: '0', label: 'Lista Simples' },
                { value: '1', label: 'Nota Fiscal' },
              ]}
              value={createForm.values.tipoEntrada.toString()}
              onChange={(value) => {
                createForm.setFieldValue('tipoEntrada', Number.parseInt(value || '0', 10) as typeof TipoEntrada.ListaSimples);
                // Limpa empresa se mudar para Lista Simples
                if (Number.parseInt(value || '0', 10) === TipoEntrada.ListaSimples) {
                  createForm.setFieldValue('empresaId', undefined);
                }
              }}
            />

            {createForm.values.tipoEntrada === TipoEntrada.NotaFiscal && (
              <>
                <Group align="flex-end">
                  <Select
                    label="Empresa"
                    placeholder="Selecione a empresa"
                    data={empresas?.map((e) => ({ value: e.id, label: e.nome })) || []}
                    {...createForm.getInputProps('empresaId')}
                    style={{ flex: 1 }}
                  />
                  <Button
                    leftSection={<IconBuilding size={16} />}
                    onClick={() => setEmpresaModalOpen(true)}
                  >
                    Nova Empresa
                  </Button>
                </Group>
                <Alert icon={<IconInfoCircle size={16} />} color="blue" variant="light">
                  Cole aqui o texto da nota fiscal. O sistema irá extrair automaticamente os produtos, quantidades, unidades e preços.
                </Alert>
              </>
            )}

            {createForm.values.tipoEntrada === 1 && (
              <Alert icon={<IconInfoCircle size={16} />} color="blue" variant="light">
                Cole aqui o texto da nota fiscal. O sistema irá extrair automaticamente os produtos, quantidades, unidades e preços.
              </Alert>
            )}
            
            <Textarea
              label="Lista de Itens"
              placeholder={
                createForm.values.tipoEntrada === 0
                  ? `Cole aqui sua lista de compras, um item por linha.
Exemplos:
Leite 2
Arroz 5kg
Pão
Queijo 500g`
                  : `Cole aqui o texto da nota fiscal com os itens:

BANANA TERRA (Código: AR004808)
Qtde.:1,915   UN: KG9   Vl. Unit.: 6,99
Vl. Total
13,39

CEBOLINHA (Código: AR007093)
Qtde.:1   UN: MCO1   Vl. Unit.: 1,89
Vl. Total
1,89`
              }
              minRows={15}
              {...createForm.getInputProps('textoOriginal')}
            />
            
            {createForm.values.tipoEntrada === 0 && (
              <Text size="sm" c="dimmed">
                O sistema irá processar automaticamente cada item, detectando quantidades e categorias.
              </Text>
            )}
            
            <Group justify="flex-end">
              <Button variant="outline" onClick={() => setCreateModalOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" loading={createMutation.isPending}>
                Criar Lista
              </Button>
            </Group>
          </Stack>
        </form>
      </Modal>

      {/* Modal de Edição */}
      <Modal
        opened={editModalOpen}
        onClose={() => setEditModalOpen(false)}
        title="Editar Lista"
      >
        <form
          onSubmit={editForm.onSubmit((values) =>
            updateMutation.mutate({ id: selectedLista!.id, data: values })
          )}
        >
          <Stack>
            <TextInput label="Nome da Lista" {...editForm.getInputProps('nome')} />
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

      {/* Modal de Cadastro R\u00e1pido de Empresa */}
      <Modal
        opened={empresaModalOpen}
        onClose={() => setEmpresaModalOpen(false)}
        title="Nova Empresa"
      >
        <form onSubmit={empresaForm.onSubmit((values) => createEmpresaMutation.mutate(values))}>
          <Stack>
            <TextInput
              label="Nome"
              placeholder="Nome da empresa"
              required
              {...empresaForm.getInputProps('nome')}
            />
            <TextInput
              label="CNPJ"
              placeholder="00.000.000/0000-00"
              {...empresaForm.getInputProps('cnpj')}
            />
            <Group justify="flex-end">
              <Button variant="outline" onClick={() => setEmpresaModalOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" loading={createEmpresaMutation.isPending}>
                Criar e Selecionar
              </Button>
            </Group>
          </Stack>
        </form>
      </Modal>
    </>
  );
}
