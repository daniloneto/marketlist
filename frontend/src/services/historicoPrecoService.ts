import api from './api';
import type { HistoricoPrecoDto, PaginatedResponse } from '../types';

export const historicoPrecoService = {
  getAll: async (pageNumber = 1, pageSize = 10): Promise<PaginatedResponse<HistoricoPrecoDto>> => {
    const response = await api.get<PaginatedResponse<HistoricoPrecoDto>>('/historicoprecos', {
      params: { pageNumber, pageSize },
    });
    return response.data;
  },

  getByProduto: async (produtoId: string): Promise<HistoricoPrecoDto[]> => {
    const response = await api.get<HistoricoPrecoDto[]>(`/historicoprecos/produto/${produtoId}`);
    return response.data;
  },

  getUltimoPreco: async (produtoId: string): Promise<HistoricoPrecoDto | null> => {
    try {
      const response = await api.get<HistoricoPrecoDto>(`/historicoprecos/produto/${produtoId}/ultimo`);
      return response.data;
    } catch {
      return null;
    }
  },
};
