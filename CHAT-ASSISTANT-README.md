# 🛍️ MarketList - Assistente de Compras com Chat IA

Sistema de lista de compras inteligente com assistente conversacional alimentado por Model Context Protocol (MCP) para integração com LLMs.

## 🎯 Funcionalidades

### ✨ Core Features
- 📝 **Listas de Compras Inteligentes** - Crie e gerencie listas de compras
- 💬 **Assistente de Chat** - Converse com IA para gerenciar compras
- 💰 **Histórico de Preços** - Acompanhe variação de valores
- 🏪 **Integração de Lojas** - Compare preços entre supermercados
- 📊 **Análise de Gastos** - Visualize padrões de consumo
- 🔄 **Sincronização** - Backup automático na nuvem (MCP)

### 🤖 Integração MCP
O sistema suporta múltiplos provedores de LLM:
- **Ollama** (Local) - Privada, grátis
- **OpenAI** (Cloud) - Modelos otimizados
- **Anthropic Claude** - Estado da arte
- **MockService** - Desenvolvimento/testes

## 📋 Arquitetura

### Backend (.NET 9)
```
src/MarketList.API/              # Controllers & endpoints
src/MarketList.Application/      # Lógica de negócio
  ├── Services/
  │   ├── ChatAssistantService   # Orquestração de chat
  │   └── ToolExecutor           # Mapeamento de ferramentas
  └── Interfaces/
      ├── IChatAssistantService  # Contrato do assistente
      └── IMcpClientService      # Contrato do cliente MCP

src/MarketList.Infrastructure/   # Implementações
  ├── Services/
  │   ├── McpClientService       # Cliente HTTP MCP real
  │   ├── MockMcpClientService   # Mock para desenvolvimento
  │   └── ChatPrompts            # Prompts do sistema
  └── Repositories/
      ├── ListaDeComprasRepository
      ├── CategoriaRepository
      ├── EmpresaRepository
      └── HistoricoPrecoRepository

src/MarketList.Domain/           # Entidades & interfaces
```

### Frontend (React 19 + TypeScript)
```
frontend/src/
├── components/
│   ├── ChatAssistant.tsx        # Widget do chat
│   ├── Layout.tsx               # Layout principal
│   └── StatusBadge.tsx          # Indicadores
├── hooks/
│   └── useChat.ts               # Hook de estado do chat
├── services/
│   └── chatService.ts           # Cliente HTTP do chat
├── pages/                       # Páginas da aplicação
└── types/                       # TypeScript types globais
```

### Database (PostgreSQL 16)
- Esquema completo com migrations
- Hangfire para jobs em background
- Suporte a múltiplos usuários

## 🚀 Início Rápido

### Pré-requisitos
- Docker & Docker Compose
- ou .NET 9 SDK + Node.js 18+
- PostgreSQL 16

### Opção 1: Docker (Recomendado)
```bash
docker-compose up -d
```

Serviços:
- **API**: http://localhost:5000
- **Frontend**: Servir `frontend/dist` via HTTP
- **PostgreSQL**: localhost:5432
- **Ollama**: localhost:11434
- **Hangfire Dashboard**: http://localhost:5000/hangfire

### Opção 2: Local Development

#### Backend
```bash
cd src/MarketList.API
dotnet ef database update
dotnet run
```

#### Frontend
```bash
cd frontend
npm install
npm run dev
```

## 🔧 Configuração

### appsettings.json
```json
{
  "MCP": {
    "Provider": "ollama|openai|anthropic|mock",
    "Endpoint": "http://ollama:11434/api/chat",
    "ApiKey": null,
    "Model": "mistral",
    "Temperature": 0.7,
    "MaxTokens": 2048,
    "UseMock": true  // Para usar MockService
  }
}
```

### Variáveis de Ambiente (Docker)
```env
# MCP Configuration
MCP__Provider=ollama
MCP__Endpoint=http://ollama:11434/api/chat
MCP__Model=mistral
MCP__Temperature=0.7
MCP__MaxTokens=2048

# Database
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=marketlist;Username=postgres;Password=postgres

# ASP.NET
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5000
```

## 📚 Ferramentas MCP Disponíveis

O assistente tem acesso a 6 ferramentas principais:

1. **get_shopping_lists**
   - Retorna últimas listas do usuário
   - Parâmetro: `limit` (número máximo)

2. **get_list_details**
   - Detalhes completos de uma lista
   - Parâmetro: `list_id` (ID da lista)

3. **search_products**
   - Busca produtos por nome/categoria
   - Parâmetros: `query`, `limit`

4. **get_price_history**
   - Histórico de preços de um produto
   - Parâmetros: `product_id`, `days`

5. **get_categories**
   - Lista todas as categorias
   - Sem parâmetros

6. **get_stores**
   - Lista todos os supermercados
   - Sem parâmetros

## 🔗 API Endpoints

### Chat
```
POST /api/chat/message
  Request: { "message": "...", "conversationHistory": [...] }
  Response: { "message": "...", "timestamp": "..." }

POST /api/chat/stream
  Response: Server-Sent Events (SSE) com streaming de texto
  Formato: "data: {chunk}\n\n"

GET /api/chat/tools
  Response: Array<ToolDefinition> com 6 ferramentas disponíveis
```

### Exemplo com cURL
```bash
# Enviar mensagem
curl -X POST http://localhost:5000/api/chat/message \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Quais são minhas listas de compras?",
    "conversationHistory": []
  }'

# Stream
curl -N -X POST http://localhost:5000/api/chat/stream \
  -H "Content-Type: application/json" \
  -d '{"message":"Crie uma lista para meu carro","conversationHistory":[]}'
```

## 📖 Demonstração do Chat

### Exemplo 1: Criar Lista
```
Usuário: "Quero criar uma lista para meu carro"
Assistente: Perfeito! Que tal nomeá-la? 🚗
  Posso ajudar com:
  - Manutenção: óleo, filtros...
  - Limpeza: cera, pano...
  - Segurança: acessórios de proteção...
```

### Exemplo 2: Consultar Preços
```
Usuário: "Qual é o preço do leite ultimamente?"
Assistente: Consultando histórico...
  Leite integral:
  - Ontem: R$ 4,50
  - 7 dias atrás: R$ 4,30
  - 30 dias atrás: R$ 4,20
  Tendência: Subindo ↗️
```

### Exemplo 3: Comparar Lojas
```
Usuário: "Qual supermercado tem melhor preço em arroz?"
Assistente: Consultando bases de dados...
  Arroz integral 5kg:
  - Carrefour: R$ 23,50
  - Extra: R$ 24,00
  - Pão de Açúcar: R$ 25,30
  Melhor opção: Carrefour ✓
```

## 🧪 Testing

### Backend
```bash
# Build
dotnet build

# Testes unitários
dotnet test

# Verificar migrações
dotnet ef migrations list
```

### Frontend
```bash
# Build
npm run build

# Lint
npm run lint

# Type checking
npx tsc --noEmit
```

### Integração
```powershell
# Windows PowerShell
.\test-api.ps1

# Docker
docker exec marketlist-api curl http://localhost:5000/api/chat/tools
```

## 📦 Tecnologias

### Backend
- **.NET 9** - Framework
- **C#** - Linguagem
- **Entity Framework Core 9** - ORM
- **PostgreSQL** - Banco de dados
- **Hangfire** - Job scheduler
- **MCP** - Model Context Protocol

### Frontend
- **React 19** - UI library
- **TypeScript** - Type safety
- **Vite 7** - Build tool
- **Mantine UI v8** - Component library
- **react-markdown** - Markdown rendering
- **TanStack Query** - State management

### DevOps
- **Docker** - Containerização
- **Docker Compose** - Orquestração local
- **Ollama** - LLM local
- **.http** - REST Client (VS Code)

## 🐛 Troubleshooting

### API retorna 500
```bash
# Verificar logs
docker logs marketlist-api

# Verificar banco de dados
docker exec marketlist-db psql -U postgres -d marketlist -c "SELECT 1"

# Mock habilitado?
curl http://localhost:5000/api/chat/tools
```

### Ollama não inicia
```bash
# Verificar saúde
docker ps | grep ollama

# Logs
docker logs marketlist-ollama

# Remover container e recriar
docker-compose up -d --force-recreate ollama
```

### Frontend de build grande
O bundle está ~760KB porque inclui todas as dependências. Para produção:
- Dynamic imports para pages
- Tree-shaking de dependências não usadas
- Minificação agressiva

## 📝 Migração de Dados

```bash
# Dentro do container
docker exec -it marketlist-api bash

# Aplicar migrations
dotnet ef database update

# Criar nova migration
dotnet ef migrations add NomeDaMigracao
```

## 🔐 Segurança

- [ ] Autenticação de usuários
- [ ] Rate limiting para chat
- [ ] Validação de entrada
- [ ] HTTPS em produção
- [ ] Sanitização de prompts

## 📋 Roadmap

- ✅ Chat com MCP integrado
- ✅ Ferramentas de dados
- ✅ Docker Compose stack
- ⏳ Autenticação de usuários
- ⏳ Integração com mercados reais
- ⏳ Mobile app (React Native)
- ⏳ WebSocket para real-time sync

## 🤝 Contribuindo

1. Fazer fork do repositório
2. Criar branch feature (`git checkout -b feature/AmazingFeature`)
3. Commit mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para o branch (`git push origin feature/AmazingFeature`)
5. Abrir Pull Request

## 📄 Licença

MIT License - veja LICENSE.md

## 📧 Suporte

- Issues: GitHub Issues
- Email: contato@marketlist.com
- Docs: [Wiki](https://github.com/seu-usuario/marketlist/wiki)

---

**Desenvolvido com ❤️ usando .NET, React e MCP**
