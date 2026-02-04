// Status como const object (compatível com erasableSyntaxOnly)
export const StatusLista = {
  Pendente: 0,
  Processando: 1,
  Concluida: 2,
  Erro: 3,
} as const;

export type StatusLista = (typeof StatusLista)[keyof typeof StatusLista];

// DTOs
export interface CategoriaDto {
  id: string;
  nome: string;
  descricao: string | null;
  createdAt: string;
  quantidadeProdutos: number;
}

export interface CategoriaCreateDto {
  nome: string;
  descricao?: string | null;
}

export interface CategoriaUpdateDto {
  nome: string;
  descricao?: string | null;
}

export interface ProdutoDto {
  id: string;
  nome: string;
  descricao: string | null;
  unidade: string | null;
  categoriaId: string;
  categoriaNome: string;
  ultimoPreco: number | null;
  createdAt: string;
}

export interface ProdutoCreateDto {
  nome: string;
  descricao?: string | null;
  unidade?: string | null;
  categoriaId: string;
}

export interface ProdutoUpdateDto {
  nome: string;
  descricao?: string | null;
  unidade?: string | null;
  categoriaId: string;
}

export interface HistoricoPrecoDto {
  id: string;
  produtoId: string;
  produtoNome: string;
  precoUnitario: number;
  dataConsulta: string;
  fontePreco: string | null;
}

export interface ListaDeComprasDto {
  id: string;
  nome: string;
  textoOriginal: string | null;
  status: StatusLista;
  createdAt: string;
  processadoEm: string | null;
  erroProcessamento: string | null;
  quantidadeItens: number;
  valorTotal: number | null;
}

export interface ListaDeComprasDetalhadaDto {
  id: string;
  nome: string;
  textoOriginal: string | null;
  status: StatusLista;
  createdAt: string;
  processadoEm: string | null;
  erroProcessamento: string | null;
  itens: ItemListaDeComprasDto[];
}

export interface ListaDeComprasCreateDto {
  nome: string;
  textoOriginal: string;
}

export interface ListaDeComprasUpdateDto {
  nome: string;
}

export interface ItemListaDeComprasDto {
  id: string;
  produtoId: string;
  produtoNome: string;
  produtoUnidade: string | null;
  quantidade: number;
  precoUnitario: number | null;
  subTotal: number | null;
  textoOriginal: string | null;
  comprado: boolean;
}

export interface ItemListaDeComprasCreateDto {
  produtoId: string;
  quantidade: number;
}

export interface ItemListaDeComprasUpdateDto {
  quantidade: number;
  comprado: boolean;
}
