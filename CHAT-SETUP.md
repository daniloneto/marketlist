# Guia de Teste do Chat Assistant

## 🚀 Ambiente de Desenvolvimento Rápido

O assistente de chat foi implementado com suporte a **MockMcpClientService**, que permite testar sem depender do Ollama ou API externa.

### ⚙️ Configuração Automática

Em desenvolvimento (ASPNETCORE_ENVIRONMENT=Development), o MockMcpClientService é ativado automaticamente. Ele retorna respostas pré-definidas e inteligentes.

### 🧪 Testando o Chat

#### Opção 1: Linha de Comando (cURL/PowerShell)

```powershell
# 1. Get ferramentas disponíveis
curl http://localhost:5000/api/chat/tools | jq

# 2. Enviar mensagem
$body = @{
    message = "Quais são minhas últimas listas?"
    conversationHistory = @()
} | ConvertTo-Json

curl -X POST http://localhost:5000/api/chat/message `
  -Header "Content-Type: application/json" `
  -Body $body | jq

# 3. Stream
curl -X POST http://localhost:5000/api/chat/stream `
  -Header "Content-Type: application/json" `
  -Body $body
```

#### Opção 2: Frontend React

```bash
# 1. Instalar dependências
cd frontend
npm install

# 2. Iniciar dev server
npm run dev
```

Acesse http://localhost:5173 e procure o botão de chat (💬) no canto inferior direito.

### 🎯 Palavras-chave para teste

O MockMcpClientService responde inteligentemente a:

- **"lista"** ou **"compra"** → Retorna listas recentes
- **"preço"** ou **"quanto custa"** → Mostra histórico de preços
- **"criar"** ou **"nova"** → Oferece criar lista
- **"gasto"** → Mostra análise de despesas

### 🔄 Usando Ollama Real (Opcional)

Se quiser usar Ollama ao invés do mock:

```bash
# 1. Instale Ollama: https://ollama.ai

# 2. Puxe um modelo
ollama pull mistral

# 3. Inicie o serviço
ollama serve

# 4. Desabilite o mock em appsettings.Development.json
{
  "MCP": {
    "UseMock": "false"
  }
}

# 5. Reinicie a aplicação
```

### 📊 Arquitetura do Chat

```
Frontend (React)
    ↓ HTTP/SSE
ChatController API
    ↓
ChatAssistantService (orquestra)
    ↓
MockMcpClientService ← Respostas pré-definidas
    ↓ (stream)
Frontend (atualiza em tempo real)
```

### 🛠️ Troubleshooting

**Se der erro 404 no Ollama:**
- Verifique se `UseMock: "true"` em `appsettings.Development.json`
- Ou desabilite Ollama e use o mock normalmente

**Se der erro de conexão PostgreSQL:**
- O chat com mock funciona sem PostgreSQL
- Para persistência, execute: `docker-compose up -d postgres`
- Aguarde 10 segundos para o container inicializar

**Se o frontend não conectar:**
- Verifique a URL base em `frontend/src/services/chatService.ts`
- Port padrão: `http://localhost:5000/api`

### 📝 Próximos Passos

1. **Testar pelo frontend** (recomendado)
2. **Integrar com Ollama real** quando estiver pronto
3. **Adicionar autenticação** para identificar usuários
4. **Persistir conversas** (requer DB)
