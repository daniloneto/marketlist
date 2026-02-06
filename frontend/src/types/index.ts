// Status como const object (compatível com erasableSyntaxOnly)
export const StatusLista = {
  Pendente: 0,
  Processando: 1,
  Concluida: 2,
  Erro: 3,
} as const;

export type StatusLista = (typeof StatusLista)[keyof typeof StatusLista];

// Tipo de Entrada
export const TipoEntrada = {
  ListaSimples: 0,
  NotaFiscal: 1,
} as const;

export type TipoEntrada = (typeof TipoEntrada)[keyof typeof TipoEntrada];

// Unidade de Medida
export const UnidadeDeMedida = {
  Unidade: 0,
  Quilograma: 1,
  Pacote: 2,
  Bandeja: 3,
  Maco: 4,
  Frasco: 5,
  Litro: 6,
  Grama: 7,
  Caixa: 8,
} as const;

export type UnidadeDeMedida = (typeof UnidadeDeMedida)[keyof typeof UnidadeDeMedida];

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

export interface EmpresaDto {
  id: string;
  nome: string;
  cnpj: string | null;
  createdAt: string;
  quantidadeListas: number;
}

export interface EmpresaCreateDto {
  nome: string;
  cnpj?: string | null;
}

export interface EmpresaUpdateDto {
  nome: string;
  cnpj?: string | null;
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

export interface ProdutoResumoDto {
  id: string;
  nome: string;
  unidade: string | null;
}

export interface ProdutoPendenteDto {
  id: string;
  nome: string;
  descricao: string | null;
  unidade: string | null;
  codigoLoja: string | null;
  categoriaId: string;
  categoriaNome: string;
  precisaRevisao: boolean;
  categoriaPrecisaRevisao: boolean;
  createdAt: string;
  produtosSimilares: ProdutoResumoDto[];
}

export interface ProdutoAprovacaoDto {
  nomeCorrigido?: string | null;
  categoriaIdCorrigida?: string | null;
  vincularAoProdutoId?: string | null;
}

export interface HistoricoPrecoDto {
  id: string;
  produtoId: string;
  produtoNome: string;
  precoUnitario: number;
  dataConsulta: string;
  fontePreco: string | null;
  empresaId: string | null;
  empresaNome: string | null;
}

export interface ListaDeComprasDto {
  id: string;
  nome: string;
  textoOriginal: string | null;
  tipoEntrada: TipoEntrada;
  status: StatusLista;
  createdAt: string;
  processadoEm: string | null;
  erroProcessamento: string | null;
  quantidadeItens: number;
  valorTotal: number | null;
  empresaId: string | null;
  empresaNome: string | null;
}

export interface ListaDeComprasDetalhadaDto {
  id: string;
  nome: string;
  textoOriginal: string | null;
  tipoEntrada: TipoEntrada;
  status: StatusLista;
  createdAt: string;
  processadoEm: string | null;
  erroProcessamento: string | null;
  itens: ItemListaDeComprasDto[];
  empresaId: string | null;
  empresaNome: string | null;
}

export interface ListaDeComprasCreateDto {
  nome: string;
  textoOriginal: string;
  tipoEntrada: TipoEntrada;  empresaId?: string | null;}

export interface ListaDeComprasUpdateDto {
  nome: string;
}

export interface ItemListaDeComprasDto {
  id: string;
  produtoId: string;
  produtoNome: string;
  produtoUnidade: string | null;
  quantidade: number;
  unidadeDeMedida: UnidadeDeMedida | null;
  precoUnitario: number | null;
  precoTotal: number | null;
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
