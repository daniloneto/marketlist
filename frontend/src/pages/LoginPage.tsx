import React, { useState } from 'react';
import { TextInput, PasswordInput, Button, Stack, Card, Image } from '@mantine/core';
import marketlistLogo from '../assets/marketlist.png';
import { useAuth } from '../contexts/useAuth';

export function LoginPage() {
  const [login, setLogin] = useState('');
  const [senha, setSenha] = useState('');
  const [loading, setLoading] = useState(false);
  const { login: doLogin } = useAuth();

  const submit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    setLoading(true);
    try {
      const isAuthenticated = await doLogin(login, senha);
      if (!isAuthenticated) {
        setSenha('');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', justifyContent: 'center', padding: '40px 16px 16px' }}>
      <Card shadow="sm" style={{ width: '100%', maxWidth: 420 }}>
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 12 }}>
          <Image src={marketlistLogo} alt="MarketList" h={56} fit="contain" />
        </div>        
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
