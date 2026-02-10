import api from './api';

export const notaService = {
  async importarNotaPorQrCode(url: string) {
    const resp = await api.post('/api/importacoes/nota-fiscal/qrcode', { url });
    return resp.data;
  },
};

export type ImportResult = {
  success: boolean;
  message?: string;
};
