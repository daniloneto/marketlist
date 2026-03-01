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

export const PeriodoOrcamentoTipo = {
  Semanal: 1,
  Mensal: 2,
} as const;

export type PeriodoOrcamentoTipo = (typeof PeriodoOrcamentoTipo)[keyof typeof PeriodoOrcamentoTipo];

export const StatusConsumoOrcamento = {
  Normal: 0,
  Alerta: 1,
  Estourado: 2,
} as const;

export type StatusConsumoOrcamento = (typeof StatusConsumoOrcamento)[keyof typeof StatusConsumoOrcamento];

// DTOs
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

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
  dataCompra?: string | null;
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
  dataCompra?: string | null;
}

export interface ListaDeComprasCreateDto {
  nome: string;
  textoOriginal: string;
  tipoEntrada: TipoEntrada;
  empresaId?: string | null;
  dataCompra?: string | null;
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

export interface OrcamentoCategoriaDto {
  id: string;
  usuarioId: string;
  categoriaId: string;
  nomeCategoria: string;
  periodoTipo: PeriodoOrcamentoTipo;
  periodoReferencia: string;
  valorLimite: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface CriarOrcamentoCategoriaRequest {
  categoriaId: string;
  periodoTipo: PeriodoOrcamentoTipo;
  periodoReferencia?: string | null;
  valorLimite: number;
}

export interface ItemResumoOrcamentoCategoriaDto {
  categoriaId: string;
  nomeCategoria: string;
  totalEstimado: number;
  itensSemPreco: number;
  valorLimite: number;
  percentualConsumido: number;
  status: StatusConsumoOrcamento;
}

export interface ResumoOrcamentoListaDto {
  listaId: string;
  periodoReferencia: string;
  periodoTipo: PeriodoOrcamentoTipo;
  totalLista: number;
  totalItensSemPreco: number;
  itensPorCategoria: ItemResumoOrcamentoCategoriaDto[];
}

export interface DashboardFinanceiroResumoDto {
  totalBudget: number;
  totalSpent: number;
  totalRemaining: number;
  totalPercentageUsed: number | null;
}

export interface DashboardFinanceiroCategoriaDto {
  categoryId: string;
  categoryName: string;
  budgetAmount: number | null;
  spentAmount: number;
  remainingAmount: number | null;
  percentageUsed: number | null;
}

export interface DashboardFinanceiroResponseDto {
  year: number;
  month: number;
  periodStart: string;
  periodEnd: string;
  summary: DashboardFinanceiroResumoDto;
  categories: DashboardFinanceiroCategoriaDto[];
}

// Chat Types
export interface ChatMessage {
  role: "user" | "assistant" | "system";
  content: string;
  timestamp?: string;
}

export interface ToolDefinition {
  name: string;
  description: string;
  parameters: Record<string, ParameterDefinition>;
}

export interface ParameterDefinition {
  type: string;
  description: string;
  required?: boolean;
}
