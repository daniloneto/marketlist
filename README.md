# FinControl

Sistema de listas de compras com automação de processamento de itens.

## Stack

- **Backend**: .NET 9, Clean Architecture
- **ORM**: Entity Framework Core 9
- **Banco de dados**: PostgreSQL 16 ou SQLite (configurável)
- **Processamento assíncrono**: Hangfire com PostgreSQL Storage (ou InMemory com SQLite)
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

#### 1a. Com PostgreSQL (padrão)

```bash
docker-compose up --build
```

Isso irá:
- Criar e iniciar o PostgreSQL
- Compilar e iniciar a API
- Aplicar migrations automaticamente

A API estará disponível em: http://localhost:5000

#### 1b. Com SQLite (sem banco de dados externo)

```bash
docker-compose -f docker-compose.sqlite.yml up --build
```

Isso irá:
- Compilar e iniciar a API com SQLite
- Aplicar migrations automaticamente
- Salvar o banco de dados em `./data/marketlist.db`

A API estará disponível em: http://localhost:5000

**Nota:** Para executar o frontend, ainda é necessário rodá-lo separadamente (veja passo 3 abaixo).

### Opção 2: Executar Manualmente

#### 2a. Com PostgreSQL

##### 1. Iniciar o PostgreSQL

```bash
docker-compose up -d postgres
```

##### 2. Executar a API

```bash
cd src/MarketList.API
dotnet run
```

**Nota:** As migrations são aplicadas automaticamente na inicialização da API.

#### 2b. Com SQLite

##### 1. Executar a API com SQLite

Edite `appsettings.Development.json`:
```json
{
  "Database": {
    "Provider": "Sqlite"
  }
}
```

Então:
```bash
cd src/MarketList.API
dotnet run
```

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

## Banco de Dados

O FinControl suporta dois provedores de banco de dados:

### PostgreSQL (Recomendado para Produção)

- **Vantagens**: Maior performance, melhor para múltiplos usuários, suporte completo a jobs do Hangfire
- **Configuração**: Editar `appsettings.json`:

```json
{
  "Database": {
    "Provider": "Postgres",
    "ConnectionStrings": {
      "Postgres": "Host=localhost;Port=5432;Database=marketlist;Username=postgres;Password=postgres"
    }
  }
}
```

### SQLite (Desenvolvimento Local Simplificado)

- **Vantagens**: Sem dependências externas, fácil para testes locais, arquivo único
- **Desvantagens**: Menos adequado para múltiplos usuários simultâneos
- **Configuração**: Editar `appsettings.json`:

```json
{
  "Database": {
    "Provider": "Sqlite",
    "ConnectionStrings": {
      "Sqlite": "Data Source=marketlist.db"
    }
  }
}
```

### Alternando Providers

#### Via Arquivo de Configuração

Edite `appsettings.Development.json` ou `appsettings.json`:

```json
{
  "Database": {
    "Provider": "Sqlite" // ou "Postgres"
  }
}
```

#### Via Variáveis de Ambiente

```bash
# Usar SQLite
export Database__Provider=Sqlite
export Database__ConnectionStrings__Sqlite=Data Source=marketlist.db

# Ou usar PostgreSQL
export Database__Provider=Postgres
export Database__ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=marketlist;Username=postgres;Password=postgres
```

#### Via Docker Compose

O docker-compose já vem configurado com as variáveis de ambiente corretas:

```yaml
# docker-compose.yml (PostgreSQL)
environment:
  - Database__Provider=Postgres
  - Database__ConnectionStrings__Postgres=Host=postgres;Port=5432;...

# docker-compose.sqlite.yml (SQLite)
environment:
  - Database__Provider=Sqlite
  - Database__ConnectionStrings__Sqlite=Data Source=/data/marketlist.db
```

## Configuração

Todas as URLs, tokens e endpoints de integração não devem ficar hardcoded no código. Use as configurações em `src/MarketList.API/appsettings.json`, `appsettings.Development.json` ou variáveis de ambiente.

Principais chaves:
- `Database:Provider` - Provider do banco: "Postgres" ou "Sqlite"
- `Database:ConnectionStrings:Postgres` - Connection string PostgreSQL
- `Database:ConnectionStrings:Sqlite` - Connection string SQLite
- `Api:BaseUrl` - URL base da API (ex: http://localhost:5000)
- `Api:AllowedOrigins` - origins permitidos para CORS
- `MCP:Endpoint` - endpoint do provedor MCP (ollama, openai, etc.)
- `Telegram:BotToken` - token do bot do Telegram
- `Telegram:BaseUrl` - base URL do Telegram (ex: https://api.telegram.org)
- `Telegram:WebhookPath` - path do webhook da API (ex: /api/integracoes/telegram/webhook)

Exemplo de variáveis de ambiente no `.env` ou `docker-compose`:

```
ASPNETCORE_URLS=http://+:5000
Database__Provider=Postgres
Database__ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=marketlist;Username=postgres;Password=postgres
MCP_ENDPOINT=http://localhost:11434/api/generate
MCP_API_KEY=
TELEGRAM_BOT_TOKEN=
VITE_API_URL=http://localhost:5000/api
```

## 🤖 Assistente de Compras (Chat com IA)

O FinControl inclui um assistente inteligente baseado em Model Context Protocol (MCP) que permite conversar sobre suas listas, produtos e preços.

### Características do Assistente

- **Buscas inteligentes**: Consulte histórico de compras e preços
- **Sugestões de economia**: O assistente identifica oportunidades de economizar
- **Criação assistida de listas**: Crie listas por conversa natural
- **Análise de despesas**: Resumo de quanto você gastou

### Configuração

Os diferentes provedores de LLM podem ser configurados via variáveis de ambiente:

#### 1. **Ollama (Gratuito, Local)**  [Recomendado para desenvolvimento]

```bash
docker-compose up -d ollama

# Puxar modelo (primeira vez)
docker exec marketlist-ollama ollama pull mistral

# Variables no docker-compose ou .env:
MCP_PROVIDER=ollama
MCP_ENDPOINT=http://localhost:11434/api/chat
MCP_MODEL=mistral
```

#### 2. **OpenAI GPT**

```bash
# .env ou docker-compose
MCP_PROVIDER=openai
MCP_ENDPOINT=https://api.openai.com/v1/chat/completions
MCP_MODEL=gpt-3.5-turbo
MCP_API_KEY=sk-...
```

#### 3. **Anthropic Claude**

```bash
# .env ou docker-compose
MCP_PROVIDER=anthropic
MCP_ENDPOINT=https://api.anthropic.com/v1/messages
MCP_MODEL=claude-3-sonnet-20240229
MCP_API_KEY=sk-ant-...
```

### Como Usar o Assistente

1. Clique no botão de chat (💬) no canto inferior direito
2. Faça suas perguntas em linguagem natural:
   - "Quais são minhas últimas listas?"
   - "Qual o preço do arroz agora?"
   - "Crie uma lista com itens básicos"
   - "Quanto gastei este mês?"

### Tools Disponíveis

O assistente tem acesso às seguintes ferramentas:

- `get_shopping_lists` - Obtém últimas listas do usuário
- `get_list_details` - Detalhes de uma lista específica
- `search_products` - Busca de produtos por nome/categoria
- `get_price_history` - Histórico de preços
- `get_categories` - Lista de categorias
- `get_stores` - Lista de supermercados

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
