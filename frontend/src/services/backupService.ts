import api from './api';

export interface EntityImportResult {
  imported: number;
  skipped: number;
}

export interface BackupInfo {
  entities: Record<string, number>;
  totalRegistros: number;
}

export interface ImportResult {
  clearedExisting: boolean;
  details: Record<string, EntityImportResult>;
  totalImported: number;
  totalSkipped: number;
}

export const backupService = {
  /**
   * Exporta todos os dados do sistema
   * Faz download direto do arquivo JSON
   */
  async exportBackup(): Promise<void> {
    const response = await api.get('/backup/export', {
      responseType: 'blob',
    });

    // Extrai o nome do arquivo do header ou gera um padrão
    const contentDisposition = response.headers['content-disposition'];
    let fileName = `fincontrol_backup_${new Date().toISOString().slice(0, 19).replace(/[-:]/g, '').replace('T', '_')}.json`;
    
    if (contentDisposition) {
      const fileNameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
      if (fileNameMatch && fileNameMatch[1]) {
        fileName = fileNameMatch[1].replace(/['"]/g, '');
      }
    }

    // Cria o download
    const blob = new Blob([response.data], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  },

  /**
   * Importa dados de um arquivo de backup
   * @param file Arquivo JSON de backup
   * @param clearExisting Se true, limpa todos os dados existentes antes de importar
   */
  async importBackup(file: File, clearExisting: boolean = false): Promise<ImportResult> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await api.post<ImportResult>(
      `/backup/import?clearExisting=${clearExisting}`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );

    return response.data;
  },

  /**
   * Retorna informações sobre os dados atuais do sistema
   */
  async getInfo(): Promise<BackupInfo> {
    const response = await api.get<BackupInfo>('/backup/info');
    return response.data;
  },
};

export default backupService;
