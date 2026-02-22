import api from './api';
import type { CategoriaDto, CategoriaCreateDto, CategoriaUpdateDto } from '../types';

type CatalogCategoryDto = {
  id: string;
  name: string;
};

const toCategoriaDto = (item: CatalogCategoryDto): CategoriaDto => ({
  id: item.id,
  nome: item.name,
  descricao: null,
  createdAt: new Date(0).toISOString(),
  quantidadeProdutos: 0,
});

export const categoriaService = {
  getAll: async (): Promise<CategoriaDto[]> => {
    const response = await api.get<CatalogCategoryDto[]>('/admin/catalog-taxonomy/categories');
    return response.data.map(toCategoriaDto);
  },

  getById: async (id: string): Promise<CategoriaDto> => {
    const response = await api.get<CatalogCategoryDto[]>('/admin/catalog-taxonomy/categories');
    const item = response.data.find((x) => x.id === id);
    if (!item) {
      throw new Error('Categoria não encontrada');
    }

    return toCategoriaDto(item);
  },

  create: async (data: CategoriaCreateDto): Promise<CategoriaDto> => {
    const response = await api.post<CatalogCategoryDto>('/admin/catalog-taxonomy/categories', { name: data.nome });
    return toCategoriaDto(response.data);
  },

  update: async (id: string, data: CategoriaUpdateDto): Promise<CategoriaDto> => {
    const response = await api.put<CatalogCategoryDto>(`/admin/catalog-taxonomy/categories/${id}`, { name: data.nome });
    return toCategoriaDto(response.data);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/admin/catalog-taxonomy/categories/${id}`);
  },
};
