import api from './api';
import type { PaginatedResponse } from '../types';

export type UsuarioDto = {
  id: string;
  login: string;
  criadoEm: string;
};

const getAll = async (): Promise<UsuarioDto[]> => {
  const res = await api.get<PaginatedResponse<UsuarioDto>>('/usuarios', { params: { pageNumber: 1, pageSize: 100 } });
  return res.data.items;
};

const remove = async (id: string) => {
  return api.delete(`/usuarios/${id}`);
};

export default { getAll, remove };
