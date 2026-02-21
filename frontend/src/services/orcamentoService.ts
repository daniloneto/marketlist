import api from './api';
import type {
  CriarOrcamentoCategoriaRequest,
  OrcamentoCategoriaDto,
  PeriodoOrcamentoTipo,
} from '../types';

export const orcamentoService = {
  createOrUpdate: async (data: CriarOrcamentoCategoriaRequest): Promise<OrcamentoCategoriaDto> => {
    const response = await api.post<OrcamentoCategoriaDto>('/orcamentos', data);
    return response.data;
  },

  listByPeriodo: async (
    periodoTipo: PeriodoOrcamentoTipo,
    periodoRef?: string | null
  ): Promise<OrcamentoCategoriaDto[]> => {
    const response = await api.get<OrcamentoCategoriaDto[]>('/orcamentos', {
      params: {
        periodoTipo,
        periodoRef: periodoRef || undefined,
      },
    });
    return response.data;
  },
};
