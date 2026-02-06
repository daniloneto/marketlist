import { NavLink } from 'react-router-dom';
import { AppShell, Group, Title, NavLink as MantineNavLink, Stack } from '@mantine/core';
import {
  IconShoppingCart,
  IconCategory,
  IconPackage,
  IconChartLine,
  IconBuilding,
} from '@tabler/icons-react';

interface LayoutProps {
  children: React.ReactNode;
}

const navItems = [
  { label: 'Listas de Compras', icon: IconShoppingCart, to: '/' },
  { label: 'Produtos', icon: IconPackage, to: '/produtos' },
  { label: 'Categorias', icon: IconCategory, to: '/categorias' },
  { label: 'Empresas', icon: IconBuilding, to: '/empresas' },
  { label: 'Histórico de Preços', icon: IconChartLine, to: '/historico-precos' },
];

export function Layout({ children }: LayoutProps) {
  return (
    <AppShell
      header={{ height: 60 }}
      navbar={{ width: 250, breakpoint: 'sm' }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md">
          <IconShoppingCart size={30} />
          <Title order={3}>MarketList</Title>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="md">
        <Stack gap="xs">
          {navItems.map((item) => (
            <MantineNavLink
              key={item.to}
              component={NavLink}
              to={item.to}
              label={item.label}
              leftSection={<item.icon size={20} />}
            />
          ))}
        </Stack>
      </AppShell.Navbar>

      <AppShell.Main>{children}</AppShell.Main>
    </AppShell>
  );
}
