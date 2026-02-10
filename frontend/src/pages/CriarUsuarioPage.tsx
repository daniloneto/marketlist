import React, { useState } from 'react';
import { TextInput, PasswordInput, Button, Stack, Card, Title } from '@mantine/core';
import authService from '../services/authService';
import { notifications } from '@mantine/notifications';

export function CriarUsuarioPage() {
  const [login, setLogin] = useState('');
  const [senha, setSenha] = useState('');
  const [confirm, setConfirm] = useState('');
  const [loading, setLoading] = useState(false);

  const submit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!login || !senha || !confirm) {
      notifications.show({ title: 'Erro', message: 'Preencha todos os campos', color: 'red' });
      return;
    }
    if (senha !== confirm) {
      notifications.show({ title: 'Erro', message: 'Senha e confirmação não conferem', color: 'red' });
      return;
    }

    setLoading(true);
    try {
      await authService.registrar(login, senha);
      notifications.show({ title: 'Sucesso', message: 'Usuário criado com sucesso', color: 'green' });
      setLogin('');
      setSenha('');
      setConfirm('');
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Erro ao criar usuário';
      notifications.show({ title: 'Erro', message, color: 'red' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', justifyContent: 'center', paddingTop: 40 }}>
      <Card shadow="sm" style={{ width: 420 }}>
        <Title order={3} mb="md">Criar Usuário</Title>
        <form onSubmit={submit}>
          <Stack>
            <TextInput label="Login" value={login} onChange={(e) => setLogin(e.currentTarget.value)} required />
            <PasswordInput label="Senha" value={senha} onChange={(e) => setSenha(e.currentTarget.value)} required />
            <PasswordInput label="Confirmar Senha" value={confirm} onChange={(e) => setConfirm(e.currentTarget.value)} required />
            <Button type="submit" loading={loading}>Criar Usuário</Button>
          </Stack>
        </form>
      </Card>
    </div>
  );
}

export default CriarUsuarioPage;
