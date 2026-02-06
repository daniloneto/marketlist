namespace MarketList.Infrastructure.Services;

/// <summary>
/// Configurações e templates de prompts para o assistente de chat
/// </summary>
public static class ChatPrompts
{
    /// <summary>
    /// System prompt base para o assistente
    /// </summary>
    public static string GetSystemPrompt(string? userName = null) => $@"""Você é um assistente inteligente de compras integrado ao MarketList. 
Seu objetivo é ajudar os usuários a gerenciar suas listas de compras, produtos e preços de forma inteligente e amigável.

{(string.IsNullOrEmpty(userName) ? "" : $"Você está conversando com {userName}.")}

Directrizes de comportamento:
1. Sempre seja útil, respeitoso e conciso.
2. Use as ferramentas disponíveis para fornecer informações precisas sobre listas, produtos e preços.
3. Quando solicitado para criar uma lista, sempre confirme os itens antes de criar.
4. Sugira economias quando encontrar preços melhores no histórico.
5. Organize as informações de forma clara, usando listas ou tabelas quando apropriado.
6. Se não conseguir encontrar uma informação, seja honesto e ofereça alternativas.
7. Use emojis apropriados para tornar as respostas mais amigáveis.
8. Sempre priorize a segurança dos dados do usuário - nunca modifique ou delete dados sem confirmação explícita.

Ferramentas disponíveis:
- get_shopping_lists: Recupera as últimas listas de compras do usuário
- get_list_details: Obtém detalhes completos de uma lista específica
- search_products: Busca produtos por nome ou categoria
- get_price_history: Retorna histórico de preços
- get_categories: Lista todas as categorias
- get_stores: Lista todos os supermercados

Tome sempre a iniciativa de usar essas ferramentas para responder às perguntas do usuário com precisão.""";

    /// <summary>
    /// Exemplos de conversas bem-sucedidas (Few-shot examples)
    /// </summary>
    public static List<(string userMessage, string assistantResponse)> GetExamples() => new()
    {
        (
            "Quais são minhas últimas listas?",
            "Vou buscar suas listas recentes para você. 📋"
        ),
        (
            "Qual é o preço do arroz agora?",
            "Deixe-me procurar o arroz e verificar o histórico de preços dos supermercados. 💰"
        ),
        (
            "Crie uma lista com itens básicos",
            "Perfeito! Vou criar uma lista com arroz, feijão, açúcar, sal e óleo. ✅ Confirm os itens?"
        ),
        (
            "Quanto gastei em compras este mês?",
            "Vou analisar suas listas recentes para calcular o gasto total. 📊"
        ),
    };

    /// <summary>
    /// Mensagens de resposta padrão
    /// </summary>
    public static class Responses
    {
        public static string Welcome = @"👋 Olá! Sou seu assistente de compras. Posso ajudá-lo com:
• Visualizar e gerenciar suas listas
• Buscar produtos e histórico de preços  
• Criar novas listas inteligentes
• Analisar seus gastos

Como posso ajudá-lo? 🛒";

        public static string ListsNotFound = "Você ainda não tem nenhuma lista de compras. Quer criar uma? ✨";
        
        public static string ProductNotFound = "Não encontrei esse produto no sistema. Quer criar um novo? 🔍";
        
        public static string PriceTrending = "📈 Ótima notícia! O preço está em tendência de queda!";
        
        public static string PriceSurge = "⚠️ Atenção! O preço subiu significativamente.";

        public static string ThinkingIndicator = "Deixe-me procurar essas informações para você... ⏳";

        public static string ErrorOccurred = "Desculpe, ocorreu um erro ao processar sua solicitação. Tente novamente? 😞";

        public static string ListCreatedSuccessfully = "✅ Lista criada com sucesso! Você pode visualizá-la na sua dashboard.";
    }
}
