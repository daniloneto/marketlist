import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
import '@mantine/dates/styles.css';

import { MantineProvider, createTheme } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Routes, Route } from 'react-router-dom';

import { Layout } from './components';
import { PrivateRoute } from './components/PrivateRoute';
import {
  LoginPage,
  RegistrarPage,
  AlterarSenhaPage,
  CriarUsuarioPage,
  UsuariosPage,
  ImportarNotaQrCodePage,
  ImportarNotaEnderecoPage,
  ListasDeComprasPage,
  ListaDetalhePage,
  CategoriasPage,
  ProdutosPage,
  HistoricoPrecosPage,
  EmpresasPage,
  RevisaoProdutosPage,
  OrcamentosPage,
} from './pages';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

const theme = createTheme({
  primaryColor: 'blue',
  fontFamily: 'Inter, system-ui, sans-serif',
  colors: {
    blue: [
      '#e6f2ff',
      '#b3d9ff',
      '#80c0ff',
      '#4da6ff',
      '#1a8cff',
      '#0257B2',
      '#014a9a',
      '#013d82',
      '#01306a',
      '#012352',
    ],
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <MantineProvider theme={theme} defaultColorScheme="light">
        <Notifications position="top-right" />
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/registrar" element={<PrivateRoute><RegistrarPage /></PrivateRoute>} />

          <Route
            path="/*"
            element={
              <PrivateRoute>
                <Layout>
                  <Routes>
                    <Route path="/" element={<ListasDeComprasPage />} />
                    <Route path="/listas/:id" element={<ListaDetalhePage />} />
                    <Route path="/categorias" element={<CategoriasPage />} />
                    <Route path="/produtos" element={<ProdutosPage />} />
                    <Route path="/revisao-produtos" element={<RevisaoProdutosPage />} />
                    <Route path="/historico-precos" element={<HistoricoPrecosPage />} />
                    <Route path="/orcamentos" element={<OrcamentosPage />} />
                    <Route path="/empresas" element={<EmpresasPage />} />
                    <Route path="/usuarios" element={<UsuariosPage />} />
                    <Route path="/usuarios/novo" element={<CriarUsuarioPage />} />
                    <Route path="/minha-conta/senha" element={<AlterarSenhaPage />} />
                    <Route path="/importar/nota-qrcode" element={<ImportarNotaQrCodePage />} />
                    <Route path="/importar/nota-endereco" element={<ImportarNotaEnderecoPage />} />
                  </Routes>
                </Layout>
              </PrivateRoute>
            }
          />
        </Routes>
      </MantineProvider>
    </QueryClientProvider>
  );
}

export default App;
