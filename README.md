# MarketList

Sistema de listas de compras com automação de processamento de itens.

## Stack

- **Backend**: .NET 9, Clean Architecture
- **ORM**: Entity Framework Core 9
- **Banco de dados**: PostgreSQL 16
- **Processamento assíncrono**: Hangfire com PostgreSQL Storage
- **Frontend**: React 19 (TypeScript), Vite 7, Mantine UI v8
- **State Management**: TanStack Query (React Query)
- **Containerização**: Docker e Docker Compose

## Estrutura do Projeto

```
marketlist/
├── src/
│   ├── MarketList.Domain/           # Entidades, Enums, Interfaces base
│   ├── MarketList.Application/      # DTOs, Commands, Queries, Services
│   ├── MarketList.Infrastructure/   # EF Core, Repositories, External APIs
│   └── MarketList.API/              # Controllers, Configurações, Hangfire Jobs
├── frontend/                         # React + Vite + Mantine
│   ├── src/
│   │   ├── pages/                   # Páginas da aplicação
│   │   │   ├── ListasDeComprasPage  # Gestão de listas
│   │   │   ├── ListaDetalhePage     # Detalhes e itens da lista
│   │   │   ├── ProdutosPage         # CRUD de produtos
│   │   │   ├── CategoriasPage       # CRUD de categorias
│   │   │   ├── EmpresasPage         # CRUD de empresas
│   │   │   ├── HistoricoPrecosPage  # Consulta histórico
│   │   │   └── RevisaoProdutosPage  # Aprovação de produtos
│   │   ├── services/                # Clients da API
│   │   ├── components/              # Componentes reutilizáveis
│   │   └── types/                   # TypeScript types
└── docker-compose.yml                # Orquestração de containers
```

### Arquitetura

O projeto segue os princípios de **Clean Architecture**:

- **Domain**: Contém as entidades de negócio e interfaces base (sem dependências externas)
- **Application**: Lógica de aplicação, DTOs, Services, Commands e Queries
- **Infrastructure**: Implementações concretas (EF Core, Repositories, APIs externas)
- **API**: Camada de apresentação (Controllers, Configuração, Jobs do Hangfire)

## Como Executar

### Pré-requisitos

- .NET 9 SDK
- Node.js 18+
- Docker e Docker Compose (para PostgreSQL ou execução completa)

### Opção 1: Executar tudo com Docker Compose (Recomendado)

```bash
docker-compose up --build
```

Isso irá:
- Criar e iniciar o PostgreSQL
- Compilar e iniciar a API
- Aplicar migrations automaticamente

A API estará disponível em: http://localhost:5000

**Nota:** Para executar o frontend, ainda é necessário rodá-lo separadamente (veja passo 3 abaixo).

### Opção 2: Executar Manualmente

#### 1. Iniciar o PostgreSQL

```bash
docker-compose up -d postgres
```

#### 2. Executar a API

```bash
cd src/MarketList.API
dotnet run
```

**Nota:** As migrations são aplicadas automaticamente na inicialização da API.

A API estará disponível em: http://localhost:5000

Dashboard do Hangfire: http://localhost:5000/hangfire

Swagger: http://localhost:5000/swagger

#### 3. Executar o Frontend

```bash
cd frontend
npm install
npm run dev
```

O frontend estará disponível em: http://localhost:5173

## Funcionalidades

### Listas de Compras
- Criação a partir de texto livre
- Processamento automático via Hangfire:
  - Análise do texto (nome do produto, quantidade)
  - Detecção automática de categoria
  - Criação de produtos e categorias inexistentes
  - Consulta de preços em API externa
  - Registro no histórico de preços

### Produtos
- CRUD completo
- Associação com categorias e empresas
- Visualização do histórico de preços
- Sistema de sinônimos para facilitar identificação
- Revisão de produtos criados automaticamente

### Revisão de Produtos
- Listagem de produtos pendentes de revisão (nome ou categoria)
- Aprovação de produtos com correções
- Rejeição de produtos incorretos
- Controle de produtos que necessitam validação manual

### Categorias
- CRUD completo
- Contagem de produtos por categoria
- Regras de classificação automática

### Empresas
- CRUD completo
- Associação com produtos e histórico de preços
- Gestão de diferentes fornecedores/supermercados

### Histórico de Preços
- Listagem com filtro por produto
- Ordenação por data
- Limpeza automática: mantém apenas últimos 120 dias
- Registro por empresa e data

### Backup e Restore
- Exportação completa do banco de dados em JSON
- Importação de dados respeitando dependências
- Listagem de entidades disponíveis para backup

### Manutenção
- Renormalização de sinônimos de produtos
- Limpeza automática de dados antigos via Jobs Hangfire

## Fluxo de Processamento Batch

### Processamento de Listas (Job Principal)

1. Usuário cola texto com lista de compras
2. Sistema salva a lista com status "Pendente"
3. Job do Hangfire é enfileirado
4. O Job processa cada linha:
   - Detecta nome e quantidade
   - Encontra ou cria categoria
   - Encontra ou cria produto (marcado para revisão se criado automaticamente)
   - Consulta preço externo (API mockada)
   - Registra no histórico de preços
   - Cria item da lista com preço atual

### Limpeza Automática de Histórico

- Job programado executa periodicamente
- Remove registros de histórico de preços com mais de 120 dias
- Mantém sempre o registro mais recente de cada produto
- Otimiza espaço em disco e performance de consultas

## API Endpoints

### Categorias
- `GET /api/categorias` - Lista todas
- `GET /api/categorias/{id}` - Busca por ID
- `POST /api/categorias` - Cria nova
- `PUT /api/categorias/{id}` - Atualiza
- `DELETE /api/categorias/{id}` - Remove

### Produtos
- `GET /api/produtos` - Lista todos
- `GET /api/produtos/{id}` - Busca por ID
- `GET /api/produtos/categoria/{categoriaId}` - Lista por categoria
- `GET /api/produtos/{id}/historico-precos` - Histórico de preços
- `POST /api/produtos` - Cria novo
- `PUT /api/produtos/{id}` - Atualiza
- `DELETE /api/produtos/{id}` - Remove

### Empresas
- `GET /api/empresas` - Lista todas
- `GET /api/empresas/{id}` - Busca por ID
- `POST /api/empresas` - Cria nova
- `PUT /api/empresas/{id}` - Atualiza
- `DELETE /api/empresas/{id}` - Remove

### Histórico de Preços
- `GET /api/historicoprecos` - Lista todos
- `GET /api/historicoprecos/produto/{produtoId}` - Por produto
- `GET /api/historicoprecos/produto/{produtoId}/ultimo` - Último preço
- `POST /api/historicoprecos` - Registra novo preço

### Listas de Compras
- `GET /api/listasdecompras` - Lista todas
- `GET /api/listasdecompras/{id}` - Busca por ID (com itens)
- `POST /api/listasdecompras` - Cria nova (dispara processamento)
- `PUT /api/listasdecompras/{id}` - Atualiza nome
- `DELETE /api/listasdecompras/{id}` - Remove
- `POST /api/listasdecompras/{id}/itens` - Adiciona item
- `PUT /api/listasdecompras/{id}/itens/{itemId}` - Atualiza item
- `DELETE /api/listasdecompras/{id}/itens/{itemId}` - Remove item

### Revisão de Produtos
- `GET /api/revisao-produtos/pendentes` - Lista produtos pendentes de revisão
- `POST /api/revisao-produtos/{id}/aprovar` - Aprova produto com correções
- `POST /api/revisao-produtos/{id}/rejeitar` - Rejeita e remove produto

### Backup
- `GET /api/backup/export` - Exporta todo o banco em JSON
- `POST /api/backup/import` - Importa dados de JSON (com opção de limpar antes)
- `GET /api/backup/entities` - Lista entidades disponíveis para backup

### Manutenção
- `POST /api/manutencao/renormalizar-sinonimos` - Re-normaliza todos os sinônimos

## Exemplo de Texto para Lista

```
Leite 6
Arroz 5kg
Feijão 2
Pão
Queijo 500g
Macarrão 3
Tomate 1kg
Cebola
Alho
```

O sistema irá:
- Detectar "Leite" → Quantidade: 6, Categoria: Laticínios
- Detectar "Arroz" → Quantidade: 5, Unidade: kg, Categoria: Grãos e Cereais
- Itens sem quantidade assumem 1 unidade

## Jobs Hangfire

O sistema utiliza dois jobs principais:

### ProcessamentoListaJob
- **Tipo**: Job sob demanda (enfileirado ao criar lista)
- **Função**: Processa texto da lista e cria produtos/itens automaticamente
- **Monitora**: Status da lista (Pendente → Processada/Erro)

### LimpezaHistoricoJob
- **Tipo**: Job recorrente (agendado automaticamente)
- **Função**: Remove histórico de preços com mais de 120 dias
- **Mantém**: Sempre o registro mais recente de cada produto

**Dashboard**: Acesse http://localhost:5000/hangfire para monitorar Jobs, filas e histórico de execuções.

## Tecnologias Frontend

- **React 19**: Biblioteca UI com suporte a concurrent features
- **TypeScript**: Type safety e melhor developer experience
- **Vite 7**: Build tool rápida com HMR
- **Mantine UI v8**: Biblioteca de componentes com tema customizável
- **TanStack Query**: Cache e sincronização de estado servidor
- **React Router DOM**: Roteamento declarativo
- **Axios**: Cliente HTTP
- **Day.js**: Manipulação de datas

## Migrations

As migrations do Entity Framework Core são aplicadas automaticamente na inicialização da API. A ordem de criação das tabelas respeita as dependências de chaves estrangeiras.

Para criar uma nova migration:

```bash
cd src/MarketList.Infrastructure
dotnet ef migrations add NomeDaMigracao --startup-project ../MarketList.API
```

## Desenvolvimento

### Configuração CORS

A API está configurada para aceitar requisições de:
- `http://localhost:3000`
- `http://localhost:5173`

Para adicionar novas origens, edite [Program.cs](src/MarketList.API/Program.cs).

### Banco de Dados

O projeto usa PostgreSQL com as seguintes credenciais padrão (desenvolvimento):
- **Host**: localhost
- **Port**: 5432
- **Database**: marketlist
- **User**: postgres
- **Password**: postgres

Para produção, utilize variáveis de ambiente para configurar a connection string.
