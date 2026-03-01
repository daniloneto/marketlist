import { NavLink } from 'react-router-dom';
import React, { useState, useRef, lazy, Suspense } from 'react';
import {
  AppShell,
  Group,
  NavLink as MantineNavLink,
  Stack,
  Button,
  Divider,
  Text,
  Modal,
  Checkbox,
  Alert,
  Image,
  Burger,
  Drawer,
} from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { notifications } from '@mantine/notifications';
import {
  IconShoppingCart,
  IconCategory,
  IconPackage,
  IconChartLine,
  IconBuilding,
  IconChecklist,
  IconWallet,
  IconDownload,
  IconUpload,
  IconAlertCircle,
  IconLogout,
  IconUser,
  IconKey,
} from '@tabler/icons-react';
import { useAuth } from '../contexts/useAuth';
import { backupService, type ImportResult } from '../services/backupService';
import marketlistLogo from '../assets/marketlist.png';

const ChatAssistant = lazy(async () => {
  const mod = await import('./ChatAssistant');
  return { default: mod.default };
});

interface LayoutProps {
  readonly children: React.ReactNode;
}

const navItems = [
  { label: 'Listas de Compras', icon: IconShoppingCart, to: '/' },
  { label: 'Produtos', icon: IconPackage, to: '/produtos' },
  { label: 'Revisão de Produtos', icon: IconChecklist, to: '/revisao-produtos' },
  { label: 'Categorias', icon: IconCategory, to: '/categorias' },
  { label: 'Orcamentos', icon: IconWallet, to: '/orcamentos' },
  { label: 'Empresas', icon: IconBuilding, to: '/empresas' },
  { label: 'Histórico de Preços', icon: IconChartLine, to: '/historico-precos' },
  { label: 'Importar Nota (QR Code)', icon: IconUpload, to: '/importar/nota-qrcode' },
  { label: 'Importar Nota (Endereço)', icon: IconUpload, to: '/importar/nota-endereco' },
];

export function Layout({ children }: LayoutProps) {
  const [opened, setOpened] = useState(false);
  const isMobile = useMediaQuery('(max-width: 768px)');
  const [exportLoading, setExportLoading] = useState(false);
  const [importModalOpen, setImportModalOpen] = useState(false);
  const [importLoading, setImportLoading] = useState(false);
  const [clearExisting, setClearExisting] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [importResult, setImportResult] = useState<ImportResult | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { logout, isAuthenticated } = useAuth();

  const handleExport = async () => {
    setExportLoading(true);
    try {
      await backupService.exportBackup();
      notifications.show({
        title: 'Backup exportado',
        message: 'Download do arquivo de backup iniciado com sucesso!',
        color: 'green',
      });
    } catch (error) {
      notifications.show({
        title: 'Erro ao exportar',
        message: 'Não foi possível exportar o backup. Tente novamente.',
        color: 'red',
      });
      console.error('Erro ao exportar backup:', error);
    } finally {
      setExportLoading(false);
    }
  };

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setSelectedFile(file);
      setImportResult(null);
    }
  };

  const handleImport = async () => {
    if (!selectedFile) return;

    setImportLoading(true);
    try {
      const result = await backupService.importBackup(selectedFile, clearExisting);
      setImportResult(result);
      notifications.show({
        title: 'Backup importado',
        message: `${result.totalImported} registros importados com sucesso!`,
        color: 'green',
      });
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Erro desconhecido';
      notifications.show({
        title: 'Erro ao importar',
        message: `Não foi possível importar o backup: ${errorMessage}`,
        color: 'red',
      });
      console.error('Erro ao importar backup:', error);
    } finally {
      setImportLoading(false);
    }
  };

  const closeImportModal = () => {
    setImportModalOpen(false);
    setSelectedFile(null);
    setClearExisting(false);
    setImportResult(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <AppShell
      header={{ height: 60 }}
      navbar={{ width: 250, breakpoint: 'sm' }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md">
          {isMobile && (
            <Burger
              opened={opened}
              onClick={() => setOpened((o) => !o)}
              size="sm"
              mr="md"
            />
          )}

          <Image src={marketlistLogo} alt="MarketList" h={40} fit="contain" />
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="md" style={{ display: isMobile ? 'none' : undefined }}>
        <Stack gap="xs" style={{ height: '100%' }}>
          {navItems.map((item) => (
            <MantineNavLink
              key={item.to}
              component={NavLink}
              to={item.to}
              label={item.label}
              leftSection={<item.icon size={20} />}
              onClick={() => setOpened(false)}
            />
          ))}

          {isAuthenticated && (
            <>
              <MantineNavLink
                component={NavLink}
                to="/usuarios"
                label="Usuários"
                leftSection={<IconUser size={20} />}
              />
              <MantineNavLink
                component={NavLink}
                to="/minha-conta/senha"
                label="Alterar Senha"
                leftSection={<IconKey size={20} />}
              />
            </>
          )}

          <div style={{ flex: 1 }} />

          <Divider my="sm" label="Backup" labelPosition="center" />

          <Button
            variant="light"
            leftSection={<IconDownload size={18} />}
            onClick={handleExport}
            loading={exportLoading}
            fullWidth
            size="sm"
          >
            Exportar Backup
          </Button>

          <Button
            variant="light"
            leftSection={<IconUpload size={18} />}
            onClick={() => setImportModalOpen(true)}
            fullWidth
            size="sm"
          >
            Importar Backup
          </Button>

          <Button
            variant="outline"
            color="red"
            leftSection={<IconLogout size={18} />}
            onClick={() => logout()}
            fullWidth
            size="sm"
          >
            Logout
          </Button>
        </Stack>
      </AppShell.Navbar>

      <Drawer opened={opened} onClose={() => setOpened(false)} padding="md" position="left" size="75%">
        <Stack gap="xs" style={{ height: '100%' }}>
          {navItems.map((item) => (
            <MantineNavLink
              key={item.to + '-drawer'}
              component={NavLink}
              to={item.to}
              label={item.label}
              leftSection={<item.icon size={20} />}
              onClick={() => setOpened(false)}
            />
          ))}

          {isAuthenticated && (
            <>
              <MantineNavLink
                component={NavLink}
                to="/usuarios"
                label="Usuários"
                leftSection={<IconUser size={20} />}
                onClick={() => setOpened(false)}
              />
              <MantineNavLink
                component={NavLink}
                to="/minha-conta/senha"
                label="Alterar Senha"
                leftSection={<IconKey size={20} />}
                onClick={() => setOpened(false)}
              />
            </>
          )}

          <div style={{ flex: 1 }} />

          <Divider my="sm" label="Backup" labelPosition="center" />

          <Button
            variant="light"
            leftSection={<IconDownload size={18} />}
            onClick={handleExport}
            loading={exportLoading}
            fullWidth
            size="sm"
          >
            Exportar Backup
          </Button>

          <Button
            variant="light"
            leftSection={<IconUpload size={18} />}
            onClick={() => { setImportModalOpen(true); setOpened(false); }}
            fullWidth
            size="sm"
          >
            Importar Backup
          </Button>

          <Button
            variant="outline"
            color="red"
            leftSection={<IconLogout size={18} />}
            onClick={() => { logout(); setOpened(false); }}
            fullWidth
            size="sm"
          >
            Logout
          </Button>
        </Stack>
      </Drawer>

      <AppShell.Main
        style={{
          minWidth: 0,
          paddingTop: 'calc(var(--app-shell-header-height, 0px) + 20px)',
        }}
      >
        {children}
      </AppShell.Main>

      <Modal
        opened={importModalOpen}
        onClose={closeImportModal}
        title="Importar Backup"
        size="md"
      >
        <Stack gap="md">
          <Alert
            icon={<IconAlertCircle size={16} />}
            color="yellow"
            variant="light"
          >
            Selecione um arquivo JSON de backup exportado anteriormente.
          </Alert>

          <input
            type="file"
            accept=".json"
            onChange={handleFileSelect}
            ref={fileInputRef}
            style={{ display: 'none' }}
          />

          <Button
            variant="outline"
            onClick={() => fileInputRef.current?.click()}
            fullWidth
          >
            {selectedFile ? selectedFile.name : 'Selecionar arquivo...'}
          </Button>

          <Checkbox
            label="Limpar dados existentes antes de importar"
            description="ATENÇÃO: Isso apagará todos os dados atuais permanentemente!"
            checked={clearExisting}
            onChange={(e) => setClearExisting(e.currentTarget.checked)}
            color="red"
          />

          {importResult && (
            <Alert color="green" variant="light">
              <Text size="sm" fw={500}>Importação concluída!</Text>
              <Text size="xs">
                Importados: {importResult.totalImported} registros
                {importResult.totalSkipped > 0 && (
                  <> | Ignorados (já existentes): {importResult.totalSkipped}</>
                )}
              </Text>
            </Alert>
          )}

          <Group justify="flex-end" gap="sm">
            <Button variant="default" onClick={closeImportModal}>
              {importResult ? 'Fechar' : 'Cancelar'}
            </Button>
            {!importResult && (
              <Button
                onClick={handleImport}
                loading={importLoading}
                disabled={!selectedFile}
                color={clearExisting ? 'red' : 'teal'}
              >
                {clearExisting ? 'Substituir Dados' : 'Importar'}
              </Button>
            )}
          </Group>
        </Stack>
      </Modal>

      {import.meta.env.VITE_CHATBOT_ENABLED === 'true' && (
        // Dynamic import ensures the chat bundle is not loaded when disabled
        <Suspense fallback={null}>
          <ChatAssistant />
        </Suspense>
      )}
    </AppShell>
  );
}
