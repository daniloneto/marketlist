import React, { useState } from 'react';
import { TextInput, PasswordInput, Button, Stack, Card, Title } from '@mantine/core';
import { useAuth } from '../contexts/AuthContext';

export function LoginPage() {
  const [login, setLogin] = useState('');
  const [senha, setSenha] = useState('');
  const [loading, setLoading] = useState(false);
  const { login: doLogin } = useAuth();

  const submit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    setLoading(true);
    try {
      await doLogin(login, senha);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', justifyContent: 'center', paddingTop: 40 }}>
      <Card shadow="sm" style={{ width: 420 }}>
        <Title order={3} mb="md">Login</Title>
        <form onSubmit={submit}>
          <Stack>
            <TextInput label="Login" value={login} onChange={(e) => setLogin(e.currentTarget.value)} required />
            <PasswordInput label="Senha" value={senha} onChange={(e) => setSenha(e.currentTarget.value)} required />
            <Button type="submit" loading={loading}>Entrar</Button>
          </Stack>
        </form>
      </Card>
    </div>
  );
}

export default LoginPage;
