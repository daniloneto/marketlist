import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
import '@mantine/dates/styles.css';

import { MantineProvider, createTheme } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Routes, Route } from 'react-router-dom';

import { Layout } from './components';
import {
  ListasDeComprasPage,
  ListaDetalhePage,
  CategoriasPage,
  ProdutosPage,
  HistoricoPrecosPage,
  EmpresasPage,
  RevisaoProdutosPage,
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
  primaryColor: 'teal',
  fontFamily: 'Inter, system-ui, sans-serif',
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <MantineProvider theme={theme} defaultColorScheme="light">
        <Notifications position="top-right" />
        <BrowserRouter>
          <Layout>
            <Routes>
              <Route path="/" element={<ListasDeComprasPage />} />
              <Route path="/listas/:id" element={<ListaDetalhePage />} />
              <Route path="/categorias" element={<CategoriasPage />} />
              <Route path="/produtos" element={<ProdutosPage />} />
              <Route path="/revisao-produtos" element={<RevisaoProdutosPage />} />
              <Route path="/historico-precos" element={<HistoricoPrecosPage />} />
              <Route path="/empresas" element={<EmpresasPage />} />
            </Routes>
          </Layout>
        </BrowserRouter>
      </MantineProvider>
    </QueryClientProvider>
  );
}

export default App;
