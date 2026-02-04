import api from './api';
import type { HistoricoPrecoDto } from '../types';

export const historicoPrecoService = {
  getAll: async (): Promise<HistoricoPrecoDto[]> => {
    const response = await api.get<HistoricoPrecoDto[]>('/historicoprecos');
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
