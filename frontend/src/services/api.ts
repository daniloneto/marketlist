import axios from 'axios';
import { STORAGE_KEY } from '../contexts/auth-context';

const API_BASE_URL = import.meta.env.VITE_API_URL || '';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Attach token from localStorage automatically
api.interceptors.request.use((config) => {
  try {
    const token = localStorage.getItem(STORAGE_KEY);
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  } catch {
    // ignore
  }
  return config;
});

export default api;
