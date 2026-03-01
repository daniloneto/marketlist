import api from './api';
import type { 
  ProdutoDto, 
  ProdutoCreateDto, 
  ProdutoUpdateDto, 
  HistoricoPrecoDto,
  ProdutoPendenteDto,
  ProdutoAprovacaoDto,
  ProdutoResumoDto ,
  PaginatedResponse
} from '../types';

export const produtoService = {
  getAll: async (): Promise<ProdutoDto[]> => {
    const response = await api.get<PaginatedResponse<ProdutoDto>>('/produtos', { params: { pageNumber: 1, pageSize: 100 } });
    return response.data.items;
  },

  getById: async (id: string): Promise<ProdutoDto> => {
    const response = await api.get<ProdutoDto>(`/produtos/${id}`);
    return response.data;
  },

  getByCategoria: async (categoriaId: string): Promise<ProdutoDto[]> => {
    const response = await api.get<ProdutoDto[]>(`/produtos/categoria/${categoriaId}`);
    return response.data;
  },

  create: async (data: ProdutoCreateDto): Promise<ProdutoDto> => {
    const response = await api.post<ProdutoDto>('/produtos', data);
    return response.data;
  },

  update: async (id: string, data: ProdutoUpdateDto): Promise<ProdutoDto> => {
    const response = await api.put<ProdutoDto>(`/produtos/${id}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/produtos/${id}`);
  },

  getHistoricoPrecos: async (id: string): Promise<HistoricoPrecoDto[]> => {
    const response = await api.get<HistoricoPrecoDto[]>(`/produtos/${id}/historico-precos`);
    return response.data;
  },

  // Revisão de Produtos
  getPendentes: async (): Promise<ProdutoPendenteDto[]> => {
    const response = await api.get<ProdutoPendenteDto[]>('/revisao-produtos/pendentes');
    return response.data;
  },

  aprovar: async (id: string, data: ProdutoAprovacaoDto): Promise<void> => {
    await api.post(`/revisao-produtos/${id}/aprovar`, data);
  },

  vincular: async (idOrigem: string, idDestino: string): Promise<void> => {
    await api.post(`/revisao-produtos/${idOrigem}/vincular/${idDestino}`);
  },

  getSimilares: async (id: string): Promise<ProdutoResumoDto[]> => {
    const response = await api.get<ProdutoResumoDto[]>(`/revisao-produtos/${id}/similares`);
    return response.data;
  },
  gerarListaSimples: async (): Promise<string> => {
    const response = await api.get<string>('/produtos/lista-simples');
    return response.data;
  },
};
