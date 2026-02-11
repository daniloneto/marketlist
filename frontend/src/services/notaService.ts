import api from './api';

export interface ImportQrCodeResult {
  success: boolean;
  message?: string;
  listaId?: string;
  empresa?: string;
  empresaId?: string | null;
}

export const notaService = {
  async importarNotaPorQrCode(url: string): Promise<ImportQrCodeResult> {
    const resp = await api.post<ImportQrCodeResult>('/api/importacoes/nota-fiscal/qrcode', { url });
    return resp.data;
  },
};
