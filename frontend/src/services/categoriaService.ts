import api from './api';
import type { CategoriaDto, CategoriaCreateDto, CategoriaUpdateDto ,
  PaginatedResponse
} from '../types';

export const categoriaService = {
  getAll: async (): Promise<CategoriaDto[]> => {
    const response = await api.get<PaginatedResponse<CategoriaDto>>('/categorias', { params: { pageNumber: 1, pageSize: 100 } });
    return response.data.items;
  },

  getById: async (id: string): Promise<CategoriaDto> => {
    const response = await api.get<CategoriaDto>(`/categorias/${id}`);
    return response.data;
  },

  create: async (data: CategoriaCreateDto): Promise<CategoriaDto> => {
    const response = await api.post<CategoriaDto>('/categorias', data);
    return response.data;
  },

  update: async (id: string, data: CategoriaUpdateDto): Promise<CategoriaDto> => {
    const response = await api.put<CategoriaDto>(`/categorias/${id}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/categorias/${id}`);
  },
};
