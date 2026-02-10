import React, { useState } from 'react';
import { TextInput, PasswordInput, Button, Stack, Card, Title } from '@mantine/core';
import authService from '../services/authService';
import { notifications } from '@mantine/notifications';

export function RegistrarPage() {
  const [login, setLogin] = useState('');
  const [senha, setSenha] = useState('');
  const [loading, setLoading] = useState(false);

  const submit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    setLoading(true);
    try {
      await authService.registrar(login, senha);
      notifications.show({ title: 'Usuário criado', message: 'Usuário criado com sucesso', color: 'green' });
      setLogin('');
      setSenha('');
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
            <Button type="submit" loading={loading}>Criar</Button>
          </Stack>
        </form>
      </Card>
    </div>
  );
}

export default RegistrarPage;
