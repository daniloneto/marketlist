import { ChatMessage } from "@/types";

export interface ChatMessageRequest {
  message: string;
  conversationHistory: ChatMessage[];
}

export interface ChatMessageResponse {
  message: string;
  toolCalls?: ToolCall[];
  timestamp: string;
}

export interface ToolCall {
  toolName: string;
  parameters: Record<string, unknown>;
  result?: string;
}

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

/**
 * Envia uma mensagem e obtém resposta completa
 */
export async function sendChatMessage(
  message: string,
  conversationHistory: ChatMessage[]
): Promise<ChatMessageResponse> {
  const request: ChatMessageRequest = {
    message,
    conversationHistory,
  };

  const response = await fetch(`${API_BASE_URL}/chat/message`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Erro ao enviar mensagem: ${response.statusText}`);
  }

  return response.json();
}

/**
 * Envia uma mensagem e retorna stream de resposta
 */
export async function streamChatMessage(
  message: string,
  conversationHistory: ChatMessage[],
  onChunk: (chunk: string) => void
): Promise<void> {
  const request: ChatMessageRequest = {
    message,
    conversationHistory,
  };

  const response = await fetch(`${API_BASE_URL}/chat/stream`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Erro ao fazer stream: ${response.statusText}`);
  }

  if (!response.body) {
    throw new Error("Resposta sem body");
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      const chunk = decoder.decode(value);
      const lines = chunk.split("\n").filter((line) => line.trim());

      for (const line of lines) {
        if (line.startsWith("data: ")) {
          const data = line.substring(6).trim();
          if (data && data !== "[DONE]") {
            onChunk(data);
          }
        }
      }
    }
  } finally {
    reader.releaseLock();
  }
}

/**
 * Obtém lista de ferramentas disponíveis
 */
export async function getAvailableTools(): Promise<any[]> {
  const response = await fetch(`${API_BASE_URL}/chat/tools`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
  });

  if (!response.ok) {
    throw new Error(`Erro ao obter ferramentas: ${response.statusText}`);
  }

  return response.json();
}
