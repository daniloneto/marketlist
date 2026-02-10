import api from './api';

export type UsuarioDto = {
  id: string;
  login: string;
  criadoEm: string;
};

const getAll = async (): Promise<UsuarioDto[]> => {
  const res = await api.get('/usuarios');
  return res.data as UsuarioDto[];
};

const remove = async (id: string) => {
  return api.delete(`/usuarios/${id}`);
};

export default { getAll, remove };
