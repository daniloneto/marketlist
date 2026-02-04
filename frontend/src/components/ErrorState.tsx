import { Center, Text, Stack, Button } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';

interface ErrorStateProps {
  message?: string;
  onRetry?: () => void;
}

export function ErrorState({ message = 'Ocorreu um erro ao carregar os dados.', onRetry }: ErrorStateProps) {
  return (
    <Center h={200}>
      <Stack align="center">
        <IconAlertCircle size={48} color="red" />
        <Text c="red">{message}</Text>
        {onRetry && (
          <Button variant="outline" onClick={onRetry}>
            Tentar novamente
          </Button>
        )}
      </Stack>
    </Center>
  );
}
