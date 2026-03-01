import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Table, Button, Text, Title, Paper, Modal, TextInput, PasswordInput, Stack } from '@mantine/core';
import usuariosService, { type UsuarioDto } from '../services/usuariosService';
import authService from '../services/authService';
import { IconPlus, IconTrash } from '@tabler/icons-react';
import { formatDateTimeInUserTimeZone } from '../utils/date';
import { LoadingState, ErrorState, PaginationControls } from '../components';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';

export function UsuariosPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['usuarios', page, pageSize],
    queryFn: () => usuariosService.getAll(page, pageSize),
  });

  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);

  const form = useForm({
    initialValues: {
      login: '',
      senha: '',
      confirm: '',
    },
    validate: {
      login: (v: string) => (v.trim() ? null : 'Login é obrigatório'),
      senha: (v: string) => (v.trim() ? null : 'Senha é obrigatória'),
      confirm: (v: string, values) => (v === values.senha ? null : 'Senhas não conferem'),
    },
  });

  const createMutation = useMutation({
    mutationFn: ({ login, senha }: { login: string; senha: string }) => authService.registrar(login, senha),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['usuarios'] });
      setCreateOpen(false);
      form.reset();
      notifications.show({ title: 'Sucesso', message: 'Usuário criado com sucesso', color: 'green' });
    },
    onError: (err: unknown) => {
      const message = err instanceof Error ? err.message : 'Erro ao criar usuário';
      notifications.show({ title: 'Erro', message, color: 'red' });
    },
  });

  const [confirmOpen, setConfirmOpen] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [deletingLogin, setDeletingLogin] = useState<string | null>(null);

  const deleteMutation = useMutation({
    mutationFn: (id: string) => usuariosService.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['usuarios'] });
      setConfirmOpen(false);
      setDeletingId(null);
      setDeletingLogin(null);
      notifications.show({ title: 'Sucesso', message: 'Usuário excluído com sucesso', color: 'green' });
    },
    onError: (err: unknown) => {
      // try to read a friendly message from API
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const message = (err as any)?.response?.data?.error ?? (err instanceof Error ? err.message : 'Erro ao excluir usuário');
      notifications.show({ title: 'Erro', message, color: 'red' });
    },
  });

  const rows = (data?.items ?? []).map((u: UsuarioDto) => (
    <Table.Tr key={u.id}>
      <Table.Td>
        <Text fw={500}>{u.login}</Text>
      </Table.Td>
      <Table.Td>{formatDateTimeInUserTimeZone(u.criadoEm)}</Table.Td>
      <Table.Td>
        <Button
          color="red"
          variant="outline"
          leftSection={<IconTrash size={14} />}
          onClick={() => { setDeletingId(u.id); setDeletingLogin(u.login); setConfirmOpen(true); }}
          loading={deleteMutation.status === 'pending' && deletingId === u.id}
        >
          Excluir
        </Button>
      </Table.Td>
    </Table.Tr>
  ));

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title order={2}>Usuários</Title>
        <Button leftSection={<IconPlus size={16} />} onClick={() => setCreateOpen(true)}>
          Novo Usuário
        </Button>
      </div>

      <Paper shadow="xs" p="md">
        {isLoading ? (
          <LoadingState />
        ) : isError ? (
          <ErrorState />
        ) : (
          <>
            <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Login</Table.Th>
              <Table.Th>Data de criação</Table.Th>
              <Table.Th>Ações</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>{rows}</Table.Tbody>
            </Table>

            {(!data || data.items.length === 0) && (
              <Text c="dimmed" ta="center" py="xl">
                Nenhum usuário encontrado
              </Text>
            )}
            {data && (
              <PaginationControls
                page={page}
                pageSize={pageSize}
                totalCount={data.totalCount}
                totalPages={data.totalPages}
                onPageChange={setPage}
                onPageSizeChange={(size) => { setPageSize(size); setPage(1); }}
              />
            )}
          </>
        )}
      </Paper>

      <Modal opened={createOpen} onClose={() => setCreateOpen(false)} title="Novo Usuário">
        <form onSubmit={form.onSubmit((values) => createMutation.mutate({ login: values.login, senha: values.senha }))}>
          <Stack>
            <TextInput label="Login" {...form.getInputProps('login')} />
            <PasswordInput label="Senha" {...form.getInputProps('senha')} />
            <PasswordInput label="Confirmar Senha" {...form.getInputProps('confirm')} />
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
              <Button variant="outline" onClick={() => setCreateOpen(false)}>Cancelar</Button>
              <Button type="submit" loading={createMutation.status === 'pending'}>Criar Usuário</Button>
            </div>
          </Stack>
        </form>
      </Modal>

      <Modal opened={confirmOpen} onClose={() => setConfirmOpen(false)} title="Confirmar exclusão">
        <Stack>
          <Text>
            {deletingLogin
              ? `Tem certeza que deseja excluir o usuário ${deletingLogin}?`
              : 'Tem certeza que deseja excluir este usuário?'}
          </Text>
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
            <Button variant="outline" onClick={() => setConfirmOpen(false)} disabled={deleteMutation.status === 'pending'}>Cancelar</Button>
            <Button color="red" onClick={() => deletingId && deleteMutation.mutate(deletingId)} loading={deleteMutation.status === 'pending'}>Excluir</Button>
          </div>
        </Stack>
      </Modal>
    </>
  );
}

export default UsuariosPage;
