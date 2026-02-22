import api from './api';
import type { 
  ProdutoDto, 
  ProdutoCreateDto, 
  ProdutoUpdateDto, 
  HistoricoPrecoDto,
  ProdutoPendenteDto,
  ProdutoAprovacaoDto,
  ProdutoResumoDto 
} from '../types';

type ProductCatalogDto = {
  id: string;
  nameCanonical: string;
  categoryId: string;
  categoryName: string;
  subcategoryId: string | null;
  isActive: boolean;
  createdAt: string;
  ultimoPreco?: number | null;
};

const toProdutoDto = (item: ProductCatalogDto): ProdutoDto => ({
  id: item.id,
  nome: item.nameCanonical,
  descricao: null,
  unidade: null,
  categoriaId: item.categoryId,
  categoriaNome: item.categoryName,
  ultimoPreco: item.ultimoPreco ?? null,
  createdAt: item.createdAt,
});

const getCatalogById = async (id: string, includeInactive = false): Promise<ProductCatalogDto> => {
  // TODO: substituir por GET /admin/catalog-products/{id} quando o endpoint existir no backend.
  const response = await api.get<ProductCatalogDto[]>('/admin/catalog-products');
  const item = response.data.find((x) => x.id === id && (includeInactive || x.isActive));
  if (!item) {
    throw new Error('Produto não encontrado');
  }

  return item;
};

export const produtoService = {
  getAll: async (): Promise<ProdutoDto[]> => {
    const response = await api.get<ProductCatalogDto[]>('/admin/catalog-products');
    return response.data.filter((x) => x.isActive).map(toProdutoDto);
  },

  getById: async (id: string): Promise<ProdutoDto> => {
    return toProdutoDto(await getCatalogById(id));
  },

  getByCategoria: async (categoriaId: string): Promise<ProdutoDto[]> => {
    const response = await api.get<ProductCatalogDto[]>('/admin/catalog-products');
    return response.data
      .filter((x) => x.categoryId === categoriaId && x.isActive)
      .map(toProdutoDto);
  },

  create: async (data: ProdutoCreateDto): Promise<ProdutoDto> => {
    const response = await api.post<ProductCatalogDto>('/admin/catalog-products', {
      nameCanonical: data.nome,
      categoryId: data.categoriaId,
      subcategoryId: data.subcategoriaId ?? null,
    });
    return toProdutoDto(response.data);
  },

  update: async (id: string, data: ProdutoUpdateDto): Promise<ProdutoDto> => {
    const current = await getCatalogById(id, true);
    const response = await api.put<ProductCatalogDto>(`/admin/catalog-products/${id}`, {
      nameCanonical: data.nome,
      categoryId: data.categoriaId,
      subcategoryId: current.subcategoryId,
      isActive: current.isActive,
    });
    return toProdutoDto(response.data);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/admin/catalog-products/${id}`);
  },

  getHistoricoPrecos: async (id: string): Promise<HistoricoPrecoDto[]> => {
    const response = await api.get<HistoricoPrecoDto[]>(`/produtos/${id}/historico-precos`);
    return response.data;
  },

  // Revisão de Produtos
  getPendentes: async (): Promise<ProdutoPendenteDto[]> => {
    const response = await api.get<ProdutoPendenteDto[]>('/revisao-produtos/pendentes');
    return response.data;
  },

  aprovar: async (id: string, data: ProdutoAprovacaoDto): Promise<void> => {
    await api.post(`/revisao-produtos/${id}/aprovar`, data);
  },

  vincular: async (idOrigem: string, idDestino: string): Promise<void> => {
    await api.post(`/revisao-produtos/${idOrigem}/vincular/${idDestino}`);
  },

  getSimilares: async (id: string): Promise<ProdutoResumoDto[]> => {
    const response = await api.get<ProdutoResumoDto[]>(`/revisao-produtos/${id}/similares`);
    return response.data;
  },
  gerarListaSimples: async (): Promise<string> => {
    const response = await api.get<string>('/produtos/lista-simples');
    return response.data;
  },
};
