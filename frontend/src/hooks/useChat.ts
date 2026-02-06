import { useState, useCallback } from "react";
import { ChatMessage } from "@/types";
import { sendChatMessage, streamChatMessage } from "@/services/chatService";

interface UseChat {
  messages: ChatMessage[];
  isLoading: boolean;
  error: string | null;
  sendMessage: (userMessage: string) => Promise<void>;
  clearHistory: () => void;
}

/**
 * Hook para gerenciar o estado da conversa de chat
 */
export function useChat(): UseChat {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const addMessage = useCallback((message: ChatMessage) => {
    setMessages((prev) => [...prev, message]);
  }, []);

  const sendMessage = useCallback(
    async (userMessage: string) => {
      if (!userMessage.trim()) {
        setError("Mensagem vazia");
        return;
      }

      try {
        setError(null);
        setIsLoading(true);

        // Adiciona mensagem do usuário
        addMessage({
          role: "user",
          content: userMessage,
          timestamp: new Date().toISOString(),
        });

        // Coleta resposta em streaming
        let fullResponse = "";
        await streamChatMessage(userMessage, messages, (chunk) => {
          fullResponse += chunk;
          // Atualiza última mensagem (assistente) com o conteúdo acumulado
          setMessages((prev) => {
            if (prev.length === 0) return prev;
            const lastMsg = prev[prev.length - 1];
            if (lastMsg.role === "assistant") {
              return [
                ...prev.slice(0, -1),
                { ...lastMsg, content: fullResponse },
              ];
            }
            return prev;
          });
        });

        // Se não temos a mensagem do assistente ainda, adiciona
        setMessages((prev) => {
          const lastMsg = prev[prev.length - 1];
          if (lastMsg?.role === "assistant") {
            return prev;
          }
          return [
            ...prev,
            {
              role: "assistant",
              content: fullResponse,
              timestamp: new Date().toISOString(),
            },
          ];
        });
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : "Erro desconhecido";
        setError(errorMessage);
        console.error("Erro ao enviar mensagem:", err);
      } finally {
        setIsLoading(false);
      }
    },
    [messages, addMessage]
  );

  const clearHistory = useCallback(() => {
    setMessages([]);
    setError(null);
  }, []);

  return { messages, isLoading, error, sendMessage, clearHistory };
}
