import { useState } from 'react';
import { Container, Text, Stack, Button, Alert, TextInput } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import axios from 'axios';
import { notaService } from '../services/notaService';

export function ImportarNotaEnderecoPage() {
  const [urlNota, setUrlNota] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const validarUrl = (valor: string) => {
    if (!valor.trim()) {
      return 'O endereço da nota fiscal é obrigatório.';
    }

    try {
      new URL(valor);
      return null;
    } catch {
      return 'Informe um endereço válido da nota fiscal.';
    }
  };

  const handleImportar = async () => {
    const erroValidacao = validarUrl(urlNota);
    if (erroValidacao) {
      setError(erroValidacao);
      notifications.show({ title: 'Atenção', message: erroValidacao, color: 'yellow' });
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const data = await notaService.importarNotaPorUrl(urlNota.trim());
      notifications.show({
        title: 'Importação iniciada',
        message: data.message || 'Nota enviada para processamento',
        color: 'green',
      });
      setUrlNota('');
    } catch (e: unknown) {
      let msg = 'Erro ao importar a nota fiscal';
      if (axios.isAxiosError(e) && e.response?.data?.message) {
        msg = e.response.data.message;
      } else if (e instanceof Error) {
        msg = e.message;
      }

      setError(msg);
      notifications.show({ title: 'Erro', message: msg, color: 'red' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container size="sm" py="lg">
      <Stack gap="md">
        <Text size="lg" fw={600}>
          Importar Nota por Endereço
        </Text>

        <TextInput
          label="Endereço da Nota Fiscal"
          placeholder="Cole aqui o link da nota da SEFAZ"
          value={urlNota}
          onChange={(event) => setUrlNota(event.currentTarget.value)}
          disabled={loading}
        />

        {error && (
          <Alert color="red" title="Erro">
            {error}
          </Alert>
        )}

        <Button onClick={handleImportar} loading={loading} disabled={loading}>
          Importar
        </Button>
      </Stack>
    </Container>
  );
}
