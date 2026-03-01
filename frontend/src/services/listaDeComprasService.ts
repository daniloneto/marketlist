import api from './api';
import type {
  ListaDeComprasDto,
  ListaDeComprasDetalhadaDto,
  ListaDeComprasCreateDto,
  ListaDeComprasUpdateDto,
  ItemListaDeComprasDto,
  ItemListaDeComprasCreateDto,
  ItemListaDeComprasUpdateDto,
  ResumoOrcamentoListaDto,
  PaginatedResponse
} from '../types';

export const listaDeComprasService = {
  getAll: async (): Promise<ListaDeComprasDto[]> => {
    const response = await api.get<PaginatedResponse<ListaDeComprasDto>>('/listasdecompras', { params: { pageNumber: 1, pageSize: 100 } });
    return response.data.items;
  },

  getById: async (id: string): Promise<ListaDeComprasDetalhadaDto> => {
    const response = await api.get<ListaDeComprasDetalhadaDto>(`/listasdecompras/${id}`);
    return response.data;
  },

  create: async (data: ListaDeComprasCreateDto): Promise<ListaDeComprasDto> => {
    const response = await api.post<ListaDeComprasDto>('/listasdecompras', data);
    return response.data;
  },

  update: async (id: string, data: ListaDeComprasUpdateDto): Promise<ListaDeComprasDto> => {
    const response = await api.put<ListaDeComprasDto>(`/listasdecompras/${id}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/listasdecompras/${id}`);
  },

  addItem: async (listaId: string, data: ItemListaDeComprasCreateDto): Promise<ItemListaDeComprasDto> => {
    const response = await api.post<ItemListaDeComprasDto>(`/listasdecompras/${listaId}/itens`, data);
    return response.data;
  },

  updateItem: async (
    listaId: string,
    itemId: string,
    data: ItemListaDeComprasUpdateDto
  ): Promise<ItemListaDeComprasDto> => {
    const response = await api.put<ItemListaDeComprasDto>(
      `/listasdecompras/${listaId}/itens/${itemId}`,
      data
    );
    return response.data;
  },

  removeItem: async (listaId: string, itemId: string): Promise<void> => {
    await api.delete(`/listasdecompras/${listaId}/itens/${itemId}`);
  },

  getResumoOrcamento: async (listaId: string): Promise<ResumoOrcamentoListaDto> => {
    const response = await api.get<ResumoOrcamentoListaDto>(`/listasdecompras/${listaId}/resumo-orcamento`);
    return response.data;
  },
};
