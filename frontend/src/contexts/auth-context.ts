import { createContext } from 'react';

export type AuthContextType = {
  token: string | null;
  isAuthenticated: boolean;
  login: (username: string, senha: string) => Promise<boolean>;
  logout: () => void;
};

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const STORAGE_KEY = 'ml_token';
