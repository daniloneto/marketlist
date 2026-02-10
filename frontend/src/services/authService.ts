import api from './api';

const login = async (login: string, senha: string): Promise<string> => {
  const res = await api.post('/auth/login', { login, senha });
  // backend returns { token }
  return res.data?.token;
};

const registrar = async (login: string, senha: string) => {
  await api.post('/auth/registrar', { login, senha });
};

const alterarSenha = async (senhaAtual: string, novaSenha: string) => {
  await api.post('/auth/alterar-senha', { senhaAtual, novaSenha });
};

export default { login, registrar, alterarSenha };
