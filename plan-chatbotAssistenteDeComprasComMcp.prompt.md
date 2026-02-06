# Plan: Chatbot Assistente de Compras com MCP

**TL;DR:** Criar um chatbot inteligente integrado ao MarketList que permite ao usuário conversar sobre suas listas, produtos e preços usando linguagem natural. A implementação usa um cliente MCP nativo em C# que se comunica com servidores MCP (local ou cloud), expondo ferramentas (tools) que dão ao LLM acesso ao contexto completo do sistema (listas, produtos, histórico). O frontend React terá uma interface de chat moderna com suporte a streaming de respostas.

**Decisões chave:**
- Integração nativa C# permite manter stack unificado e facilita deploy
- Arquitetura agnóstica de LLM (suporta Ollama local ou APIs cloud como Claude/OpenAI)
- Tools MCP expõem apenas operações seguras de leitura + criação de listas (sem delete/modificação destrutiva)
- Interface de chat no frontend como componente standalone, não invasivo

---

**Steps**
0. Crie uma branch `feature/chatbot-assistente` a partir da master para organizar o desenvolvimento.
0.1 Faça checkout da branch criada.
1. **Criar infraestrutura MCP em C#**
   - Adicionar pacote NuGet `ModelContextProtocol.NET` (ou criar cliente HTTP custom) em [MarketList.Infrastructure](src/MarketList.Infrastructure)
   - Criar `IMcpClientService` interface em [MarketList.Application/Interfaces](src/MarketList.Application/Interfaces)
   - Implementar `McpClientService` em [MarketList.Infrastructure/Services](src/MarketList.Infrastructure/Services) com suporte a múltiplos backends (Ollama local, OpenAI API, Anthropic API)
   - Configurar via `appsettings.json` com seções: `MCP:Provider` (local/openai/anthropic), `MCP:Endpoint`, `MCP:ApiKey`
   - Registrar serviço no [DependencyInjection.cs](src/MarketList.Infrastructure/DependencyInjection.cs)

2. **Implementar MCP Tools como Application Services**
   - Criar `ChatAssistantService` em [MarketList.Application/Services](src/MarketList.Application/Services) que define os tools disponíveis:
     - `get_shopping_lists`: Lista últimas N listas do usuário (usa `IListaDeComprasRepository`)
     - `get_list_details`: Detalhes completos de uma lista específica (produtos, quantidades, status)
     - `search_products`: Busca produtos por nome/categoria (usa `IProdutoRepository`)
     - `get_price_history`: Histórico de preços de um produto (usa `IHistoricoPrecoRepository`)
     - `create_shopping_list`: Cria nova lista a partir de conversa (usa `IListaDeComprasService`)
     - `get_categories`: Lista todas as categorias disponíveis
     - `get_stores`: Lista empresas/supermercados cadastrados
   - Cada tool retorna JSON estruturado que o LLM pode interpretar
   - Implementar validações de segurança: rate limiting, sanitização de inputs, limitar tamanho de contexto

3. **Criar ChatController na API**
   - Adicionar `ChatController` em [MarketList.API/Controllers](src/MarketList.API/Controllers)
   - Endpoint `POST /api/chat/message`: Recebe mensagem do usuário + histórico de conversa
   - Endpoint `GET /api/chat/stream`: Retorna respostas via Server-Sent Events (SSE) para streaming em tempo real
   - Controller orquestra: recebe mensagem → chama `ChatAssistantService` → envia para MCP client → processa tool calls → retorna resposta
   - Adicionar autenticação se não existir (o chat deve ter contexto do usuário logado)

4. **Implementar lógica de tool execution**
   - Criar `ToolExecutor` que mapeia tool names para métodos dos services existentes
   - Quando LLM solicita tool call: extrair parâmetros → validar → executar serviço correspondente → retornar resultado ao LLM
   - Implementar loop de conversação: mensagem inicial → tool calls → resultados → resposta final
   - Adicionar logging detalhado de todas as interações (para debug e melhoria de prompts)

5. **Criar serviço frontend para chat**
   - Adicionar `chatService.ts` em [frontend/src/services](frontend/src/services) com métodos:
     - `sendMessage(message, conversationHistory)`: POST para `/api/chat/message`
     - `streamResponse(message, onChunk)`: Consome SSE de `/api/chat/stream`
   - Gerenciar estado de conversação (histórico de mensagens, contexto)
   - Exportar em [frontend/src/services/index.ts](frontend/src/services/index.ts)

6. **Criar componente ChatAssistant no frontend**
   - Criar `ChatAssistant.tsx` em [frontend/src/components](frontend/src/components) com:
     - Interface de mensagens (balões de chat, timestamp, indicador "pensando...")
     - Input com autocomplete/sugestões
     - Botões de ação rápida ("Criar lista", "Ver preços", "Ultimas compras")
     - Suporte a markdown nas respostas do LLM
   - Criar hook `useChat.ts` em [frontend/src/hooks](frontend/src/hooks) para gerenciar estado e lógica
   - Integrar streaming de respostas (mostrar palavra por palavra)
   - Adicionar componente ao [Layout.tsx](frontend/src/components/Layout.tsx) como botão flutuante ou sidebar

7. **Configurar prompts do sistema**
   - Criar arquivo `prompts.json` ou classe `ChatPrompts` com system prompt base:
     - Definir personalidade do assistente ("assistente útil de compras")
     - Instruções sobre como usar os tools
     - Exemplos de conversas bem-sucedidas (few-shot)
     - Regras: sempre confirmar antes de criar listas, sugerir economias, ser conciso
   - Tornar prompts configuráveis via appsettings ou arquivo externo
   - Incluir contexto dinâmico: nome do usuário, listas recentes, preferências

8. **Testes e refinamento**
   - Criar testes unitários para `ChatAssistantService` e tool execution
   - Criar testes de integração para fluxo completo (mensagem → tools → resposta)
   - Testar com perguntas reais: "O que falta comprar?", "Quanto gastei no último mês?", "Crie lista com itens básicos"
   - Ajustar system prompt baseado nos resultados
   - Adicionar tratamento de erros: LLM indisponível, timeout, tool failures

9. **Docker e deployment**
   - Atualizar [docker-compose.yml](docker-compose.yml) com variáveis de ambiente para MCP
   - Se usar Ollama local: adicionar service `ollama` no docker-compose
   - Documentar no [README.md](README.md) como configurar diferentes provedores de LLM
   - Criar exemplo de `.env` com todas as configurações necessárias

10. **Features não implementar agora (pós-MVP)**
    - Suporte a voz (Speech-to-Text via Web Speech API)
    - Histórico persistente de conversas
    - Compartilhamento de conversas
    - Análise de sentimento sobre preços ("Esse preço está caro!")
    - Integração com outras tools: análise de imagem de nota fiscal via visão

---

**Verification**

**Testes Manuais:**
1. Iniciar aplicação com `docker-compose up`
2. Abrir frontend e clicar no ícone de chat
3. Testar perguntas básicas:
   - "Quais são minhas últimas listas?"
   - "Qual o preço do arroz no supermercado X?"
   - "Crie uma lista com arroz, feijão e café"
   - "O que comprei mais neste mês?"
4. Verificar streaming de respostas (aparecem palavra por palavra)
5. Verificar que tools são executados corretamente (logs no backend)


**Decisions**

- **Integração nativa em C#** (vs server standalone): Mantém stack unificado, facilita deploy e debugging. Trade-off: menos flexibilidade para trocar de linguagem depois, mas ganho em manutenibilidade.

- **Arquitetura agnóstica de LLM**: Criar abstração `IMcpClientService` permite testar Ollama gratuitamente e migrar para Claude/GPT quando viável. Configuração via appsettings evita lock-in.

- **Tools apenas de leitura + criação de lista**: Por segurança, chatbot não pode deletar dados. Operações destrutivas exigem confirmação explícita na UI tradicional.

- **Streaming via SSE** (vs WebSocket): Mais simples de implementar, compatível com infraestrutura HTTP existente, suficiente para chat unidirecional.

- **Component flutuante vs página dedicada**: Iniciar como botão flutuante (menos invasivo), pode evoluir para página dedicada baseado em uso real.
