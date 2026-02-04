# MarketList

Sistema de listas de compras com automação de processamento de itens.

## Stack

- **Backend**: .NET 9, Clean Architecture
- **ORM**: Entity Framework Core
- **Banco de dados**: PostgreSQL
- **Processamento assíncrono**: Hangfire
- **Frontend**: React (TypeScript), Vite, Mantine UI

## Estrutura do Projeto

```
marketlist/
├── src/
│   ├── MarketList.Domain/           # Entidades, Enums, Interfaces base
│   ├── MarketList.Application/      # DTOs, Commands, Queries, Services
│   ├── MarketList.Infrastructure/   # EF Core, Repositories, External APIs
│   └── MarketList.API/              # Controllers, Configurações, Hangfire
├── frontend/                         # React + Vite + Mantine
└── docker-compose.yml
```

## Como Executar

### Pré-requisitos

- .NET 9 SDK
- Node.js 18+
- Docker e Docker Compose (para PostgreSQL)

### 1. Iniciar o PostgreSQL

```bash
docker-compose up -d postgres
```

### 2. Executar a API

```bash
cd src/MarketList.API
dotnet run
```

A API estará disponível em: http://localhost:5000

Dashboard do Hangfire: http://localhost:5000/hangfire

Swagger: http://localhost:5000/swagger

### 3. Executar o Frontend

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
- Associação com categorias
- Visualização do histórico de preços

### Categorias
- CRUD completo
- Contagem de produtos por categoria

### Histórico de Preços
- Listagem com filtro por produto
- Ordenação por data

## Fluxo de Processamento Batch

1. Usuário cola texto com lista de compras
2. Sistema salva a lista com status "Pendente"
3. Job do Hangfire é enfileirado
4. O Job processa cada linha:
   - Detecta nome e quantidade
   - Encontra ou cria categoria
   - Encontra ou cria produto
   - Consulta preço externo (API mockada)
   - Registra no histórico de preços
   - Cria item da lista com preço atual

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
