import api from './api';
import type { EmpresaDto, EmpresaCreateDto, EmpresaUpdateDto, PaginatedResponse } from '../types';

export const empresaService = {
  getAll: async (pageNumber = 1, pageSize = 10): Promise<PaginatedResponse<EmpresaDto>> => {
    const response = await api.get<PaginatedResponse<EmpresaDto>>('/empresas', {
      params: { pageNumber, pageSize },
    });
    return response.data;
  },

  getAllItems: async (): Promise<EmpresaDto[]> => {
    const response = await api.get<PaginatedResponse<EmpresaDto>>('/empresas', {
      params: { pageNumber: 1, pageSize: 100 },
    });
    return response.data.items;
  },

  getById: async (id: string): Promise<EmpresaDto> => {
    const response = await api.get<EmpresaDto>(`/empresas/${id}`);
    return response.data;
  },

  create: async (data: EmpresaCreateDto): Promise<EmpresaDto> => {
    const response = await api.post<EmpresaDto>('/empresas', data);
    return response.data;
  },

  update: async (id: string, data: EmpresaUpdateDto): Promise<EmpresaDto> => {
    const response = await api.put<EmpresaDto>(`/empresas/${id}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/empresas/${id}`);
  },
};
