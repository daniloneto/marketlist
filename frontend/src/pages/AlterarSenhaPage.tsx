import React, { useState } from 'react';
import { PasswordInput, Button, Stack, Card, Title } from '@mantine/core';
import authService from '../services/authService';
import { notifications } from '@mantine/notifications';

export function AlterarSenhaPage() {
  const [senhaAtual, setSenhaAtual] = useState('');
  const [novaSenha, setNovaSenha] = useState('');
  const [confirm, setConfirm] = useState('');
  const [loading, setLoading] = useState(false);

  const submit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (novaSenha !== confirm) {
      notifications.show({ title: 'Erro', message: 'Senha e confirmação não conferem', color: 'red' });
      return;
    }
    setLoading(true);
    try {
      await authService.alterarSenha(senhaAtual, novaSenha);
      notifications.show({ title: 'Senha', message: 'Senha alterada com sucesso', color: 'green' });
      setSenhaAtual('');
      setNovaSenha('');
      setConfirm('');
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Erro ao alterar senha';
      notifications.show({ title: 'Erro', message, color: 'red' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', justifyContent: 'center', paddingTop: 40 }}>
      <Card shadow="sm" style={{ width: 420 }}>
        <Title order={3} mb="md">Alterar Senha</Title>
        <form onSubmit={submit}>
          <Stack>
            <PasswordInput label="Senha atual" value={senhaAtual} onChange={(e) => setSenhaAtual(e.currentTarget.value)} required />
            <PasswordInput label="Nova senha" value={novaSenha} onChange={(e) => setNovaSenha(e.currentTarget.value)} required />
            <PasswordInput label="Confirmar nova senha" value={confirm} onChange={(e) => setConfirm(e.currentTarget.value)} required />
            <Button type="submit" loading={loading}>Alterar</Button>
          </Stack>
        </form>
      </Card>
    </div>
  );
}

export default AlterarSenhaPage;
