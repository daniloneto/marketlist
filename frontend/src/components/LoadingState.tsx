import { Center, Loader, Text, Stack } from '@mantine/core';

interface LoadingStateProps {
  message?: string;
}

export function LoadingState({ message = 'Carregando...' }: LoadingStateProps) {
  return (
    <Center h={200}>
      <Stack align="center">
        <Loader size="lg" />
        <Text c="dimmed">{message}</Text>
      </Stack>
    </Center>
  );
}
