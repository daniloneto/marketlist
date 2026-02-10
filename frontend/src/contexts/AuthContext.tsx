import React, { createContext, useContext, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import authService from '../services/authService';

type AuthContextType = {
  token: string | null;
  isAuthenticated: boolean;
  login: (login: string, senha: string) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const STORAGE_KEY = 'ml_token';

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
      const message = err instanceof Error ? err.message : 'Erro no login';
      notifications.show({ title: 'Erro', message, color: 'red' });
      throw err;
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

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

export default AuthContext;
