import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Title,
  Button,
  Table,
  Group,
  Modal,
  TextInput,
  Stack,
  Text,
  Paper,
  Select,
  Badge,
  Tabs,
  Card,
  ActionIcon,
  Tooltip,
  List,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconCheck, IconLink } from '@tabler/icons-react';
import { produtoService, categoriaService } from '../services';
import { LoadingState, ErrorState } from '../components';
import type { ProdutoPendenteDto, ProdutoAprovacaoDto } from '../types';

export function RevisaoProdutosPage() {
  const queryClient = useQueryClient();
  const [revisaoModalOpen, setRevisaoModalOpen] = useState(false);
  const [selectedProduto, setSelectedProduto] = useState<ProdutoPendenteDto | null>(null);

  const { data: produtosPendentes, isLoading, error } = useQuery({
    queryKey: ['produtosPendentes'],
    queryFn: produtoService.getPendentes,
  });

  const { data: categorias } = useQuery({
    queryKey: ['categorias'],
    queryFn: categoriaService.getAll,
  });

  const form = useForm<ProdutoAprovacaoDto>({
    initialValues: {
      nomeCorrigido: null,
      categoriaIdCorrigida: null,
      vincularAoProdutoId: null,
    },
  });

  const aprovarMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: ProdutoAprovacaoDto }) =>
      produtoService.aprovar(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['produtosPendentes'] });
      queryClient.invalidateQueries({ queryKey: ['produtos'] });
      setRevisaoModalOpen(false);
      notifications.show({
        title: 'Sucesso',
        message: 'Produto aprovado com sucesso!',
        color: 'green',
      });
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Erro ao aprovar produto',
        color: 'red',
      });
    },
  });

  const vincularMutation = useMutation({
    mutationFn: ({ idOrigem, idDestino }: { idOrigem: string; idDestino: string }) =>
      produtoService.vincular(idOrigem, idDestino),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['produtosPendentes'] });
      queryClient.invalidateQueries({ queryKey: ['produtos'] });
      setRevisaoModalOpen(false);
      notifications.show({
        title: 'Sucesso',
        message: 'Produtos vinculados com sucesso!',
        color: 'green',
      });
    },
    onError: () => {
      notifications.show({
        title: 'Erro',
        message: 'Erro ao vincular produtos',
        color: 'red',
      });
    },
  });

  const handleRevisar = (produto: ProdutoPendenteDto) => {
    setSelectedProduto(produto);
    form.setValues({
      nomeCorrigido: produto.nome,
      categoriaIdCorrigida: produto.categoriaId,
      vincularAoProdutoId: null,
    });
    setRevisaoModalOpen(true);
  };

  const handleAprovar = () => {
    if (!selectedProduto) return;

    const data: ProdutoAprovacaoDto = {
      nomeCorrigido: form.values.nomeCorrigido !== selectedProduto.nome ? form.values.nomeCorrigido : null,
      categoriaIdCorrigida: form.values.categoriaIdCorrigida !== selectedProduto.categoriaId ? form.values.categoriaIdCorrigida : null,
      vincularAoProdutoId: form.values.vincularAoProdutoId,
    };

    aprovarMutation.mutate({ id: selectedProduto.id, data });
  };

  const handleVincular = (idDestino: string) => {
    if (!selectedProduto) return;
    vincularMutation.mutate({ idOrigem: selectedProduto.id, idDestino });
  };

  if (isLoading) return <LoadingState />;
  if (error) return <ErrorState message="Erro ao carregar produtos pendentes" onRetry={() => queryClient.invalidateQueries({ queryKey: ['produtosPendentes'] })} />;

  const produtosNomePendente = produtosPendentes?.filter(p => p.precisaRevisao) || [];
  const produtosCategoriaPendente = produtosPendentes?.filter(p => p.categoriaPrecisaRevisao) || [];

  return (
    <Stack gap="md">
      <Group justify="space-between">
        <Title order={2}>Revisão de Produtos</Title>
        <Badge size="lg" color="yellow">
          {produtosPendentes?.length || 0} pendentes
        </Badge>
      </Group>

      <Tabs defaultValue="nome">
        <Tabs.List>
          <Tabs.Tab value="nome">
            Nome Pendente
            {produtosNomePendente.length > 0 && (
              <Badge ml="xs" size="sm" color="red">{produtosNomePendente.length}</Badge>
            )}
          </Tabs.Tab>
          <Tabs.Tab value="categoria">
            Categoria Pendente
            {produtosCategoriaPendente.length > 0 && (
              <Badge ml="xs" size="sm" color="orange">{produtosCategoriaPendente.length}</Badge>
            )}
          </Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="nome" pt="md">
          {produtosNomePendente.length === 0 ? (
            <Paper p="xl" withBorder>
              <Text c="dimmed" ta="center">
                Nenhum produto com nome pendente de revisão
              </Text>
            </Paper>
          ) : (
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Nome</Table.Th>
                  <Table.Th>Código Loja</Table.Th>
                  <Table.Th>Categoria</Table.Th>
                  <Table.Th>Data Criação</Table.Th>
                  <Table.Th>Ações</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {produtosNomePendente.map((produto) => (
                  <Table.Tr key={produto.id}>
                    <Table.Td>
                      <Text fw={500}>{produto.nome}</Text>
                    </Table.Td>
                    <Table.Td>
                      {produto.codigoLoja ? (
                        <Badge color="blue" variant="light">{produto.codigoLoja}</Badge>
                      ) : (
                        <Text c="dimmed" size="sm">N/A</Text>
                      )}
                    </Table.Td>
                    <Table.Td>{produto.categoriaNome}</Table.Td>
                    <Table.Td>
                      <Text size="sm">{new Date(produto.createdAt).toLocaleDateString('pt-BR')}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Button
                        size="xs"
                        variant="light"
                        leftSection={<IconCheck size={16} />}
                        onClick={() => handleRevisar(produto)}
                      >
                        Revisar
                      </Button>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </Tabs.Panel>

        <Tabs.Panel value="categoria" pt="md">
          {produtosCategoriaPendente.length === 0 ? (
            <Paper p="xl" withBorder>
              <Text c="dimmed" ta="center">
                Nenhum produto com categoria pendente de revisão
              </Text>
            </Paper>
          ) : (
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Nome</Table.Th>
                  <Table.Th>Categoria Atual</Table.Th>
                  <Table.Th>Data Criação</Table.Th>
                  <Table.Th>Ações</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {produtosCategoriaPendente.map((produto) => (
                  <Table.Tr key={produto.id}>
                    <Table.Td>
                      <Text fw={500}>{produto.nome}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Badge color="orange" variant="light">
                        {produto.categoriaNome}
                      </Badge>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{new Date(produto.createdAt).toLocaleDateString('pt-BR')}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Button
                        size="xs"
                        variant="light"
                        leftSection={<IconCheck size={16} />}
                        onClick={() => handleRevisar(produto)}
                      >
                        Revisar
                      </Button>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </Tabs.Panel>
      </Tabs>

      {/* Modal de Revisão */}
      <Modal
        opened={revisaoModalOpen}
        onClose={() => setRevisaoModalOpen(false)}
        title={<Text fw={700}>Revisar Produto</Text>}
        size="lg"
      >
        {selectedProduto && (
          <Stack gap="md">
            {selectedProduto.codigoLoja && (
              <Card withBorder padding="sm">
                <Text size="sm" c="dimmed">Código da Loja</Text>
                <Text fw={500}>{selectedProduto.codigoLoja}</Text>
              </Card>
            )}

            <TextInput
              label="Nome do Produto"
              {...form.getInputProps('nomeCorrigido')}
              placeholder="Corrija o nome se necessário"
            />

            <Select
              label="Categoria"
              data={categorias?.map((c) => ({ value: c.id, label: c.nome })) || []}
              {...form.getInputProps('categoriaIdCorrigida')}
              searchable
            />

            {selectedProduto.produtosSimilares.length > 0 && (
              <Card withBorder padding="md">
                <Text size="sm" fw={600} mb="sm">
                  Produtos Similares (Vincular?)
                </Text>
                <List size="sm">
                  {selectedProduto.produtosSimilares.map((similar) => (
                    <List.Item key={similar.id}>
                      <Group justify="space-between">
                        <div>
                          <Text size="sm">{similar.nome}</Text>
                          {similar.unidade && (
                            <Text size="xs" c="dimmed">{similar.unidade}</Text>
                          )}
                        </div>
                        <Tooltip label="Vincular a este produto">
                          <ActionIcon
                            variant="light"
                            color="blue"
                            onClick={() => handleVincular(similar.id)}
                          >
                            <IconLink size={16} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </List.Item>
                  ))}
                </List>
              </Card>
            )}

            <Group justify="flex-end" mt="md">
              <Button variant="default" onClick={() => setRevisaoModalOpen(false)}>
                Cancelar
              </Button>
              <Button onClick={handleAprovar} loading={aprovarMutation.isPending}>
                Aprovar Alterações
              </Button>
            </Group>
          </Stack>
        )}
      </Modal>
    </Stack>
  );
}
