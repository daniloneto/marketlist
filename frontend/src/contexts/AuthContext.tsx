import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import axios from 'axios';
import authService from '../services/authService';
import { AuthContext, STORAGE_KEY } from './auth-context';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(STORAGE_KEY));
  const navigate = useNavigate();

  useEffect(() => {
    if (token) localStorage.setItem(STORAGE_KEY, token);
    else localStorage.removeItem(STORAGE_KEY);
  }, [token]);

  const login = async (login: string, senha: string) => {
    try {
      const t = await authService.login(login, senha);
      setToken(t);
      notifications.show({ title: 'Login', message: 'Autenticado com sucesso', color: 'green' });
      navigate('/');
    } catch (err: unknown) {
      let message = 'Erro no login';
      if (axios.isAxiosError(err)) {
        const status = err.response?.status;
        if (status === 401) message = 'Usuário e/ou senha inválidos ou inexistente';
        else if (err.response?.data && typeof err.response.data === 'string') message = err.response.data;
      } else if (err instanceof Error) {
        message = err.message;
      }
      notifications.show({ title: 'Erro', message, color: 'red' });
      // erro já tratado e mostrado ao usuário; não relançar para evitar "Uncaught (in promise)"
    }
  };

  const logout = () => {
    setToken(null);
    navigate('/login');
  };

  return (
    <AuthContext.Provider value={{ token, isAuthenticated: !!token, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
