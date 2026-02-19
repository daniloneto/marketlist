import api from './api';

export interface ImportNotaResult {
  success: boolean;
  message?: string;
  listaId?: string;
  empresa?: string;
  empresaId?: string | null;
}

export const notaService = {
  async importarNotaPorQrCode(url: string): Promise<ImportNotaResult> {
    const resp = await api.post<ImportNotaResult>('/importacoes/nota-fiscal/qrcode', { url });
    return resp.data;
  },

  async importarNotaPorUrl(urlNota: string): Promise<ImportNotaResult> {
    const resp = await api.post<ImportNotaResult>('/importacoes/nota-fiscal/por-url', { urlNota });
    return resp.data;
  },
};
