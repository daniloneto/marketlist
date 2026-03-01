import api from './api';
import type { PaginatedResponse } from '../types';

export type UsuarioDto = {
  id: string;
  login: string;
  criadoEm: string;
};

const getAll = async (pageNumber = 1, pageSize = 10): Promise<PaginatedResponse<UsuarioDto>> => {
  const res = await api.get<PaginatedResponse<UsuarioDto>>('/usuarios', {
    params: { pageNumber, pageSize },
  });
  return res.data;
};

const remove = async (id: string) => api.delete(`/usuarios/${id}`);

export default { getAll, remove };
