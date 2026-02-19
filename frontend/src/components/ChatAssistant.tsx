import React, { useState, useRef, useEffect } from "react";
import Markdown from "react-markdown";
import { useChat } from "@/hooks/useChat";
import type { ChatMessage } from "@/types";
import "./ChatAssistant.css";

const ChatAssistantComponent: React.FC = () => {
  const { messages, isLoading, error, sendMessage, clearHistory } = useChat();
  const [input, setInput] = useState("");
  const [isExpanded, setIsExpanded] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Auto-scroll para o final
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim() || isLoading) return;

    const messageToSend = input;
    setInput("");

    await sendMessage(messageToSend);
  };

  const handleQuickAction = async (action: string) => {
    const prompts: Record<string, string> = {
      listas:
        "Quais são minhas últimas listas de compras? Mostre em formato resumido.",
      precos: "Qual o histórico de preços dos produtos mais comuns?",
      criar:
        "Crie uma lista de compras com: arroz, feijão, café, óleo e sal",
      resumo: "Me faça um resumo do que comprei no último mês",
    };

    await sendMessage(prompts[action] || action);
  };

  if (!isExpanded) {
    return (
      <button
        className="chat-fab"
        onClick={() => setIsExpanded(true)}
        title="Abrir assistente de compras"
      >
        💬
      </button>
    );
  }

  return (
    <div className="chat-container">
      <div className="chat-header">
        <h2>Assistente de Compras</h2>
        <button
          className="chat-close-btn"
          onClick={() => {
            setIsExpanded(false);
            clearHistory();
          }}
        >
          ✕
        </button>
      </div>

      <div className="chat-messages">
        {messages.length === 0 && (
          <div className="chat-welcome">
            <h3>Bem-vindo! 👋</h3>
            <p>Faça perguntas sobre suas listas de compras e preços.</p>
            <div className="quick-actions">
              <button
                className="quick-action-btn"
                onClick={() => handleQuickAction("listas")}
              >
                📋 Minhas Listas
              </button>
              <button
                className="quick-action-btn"
                onClick={() => handleQuickAction("precos")}
              >
                💰 Histórico de Preços
              </button>
              <button
                className="quick-action-btn"
                onClick={() => handleQuickAction("criar")}
              >
                ➕ Criar Lista
              </button>
              <button
                className="quick-action-btn"
                onClick={() => handleQuickAction("resumo")}
              >
                📊 Resumo de Compras
              </button>
            </div>
          </div>
        )}

        {messages.map((msg: ChatMessage, idx: number) => (
          <div
            key={idx}
            className={`chat-message ${msg.role}`}
          >
            <div className="message-avatar">
              {msg.role === "user" ? "👤" : "🤖"}
            </div>
            <div className="message-content">
              {msg.role === "assistant" ? (
                <Markdown>{msg.content}</Markdown>
              ) : (
                msg.content
              )}
            </div>
            {msg.timestamp && (
              <div className="message-time">
                {new Date(msg.timestamp).toLocaleTimeString()}
              </div>
            )}
          </div>
        ))}

        {isLoading && (
          <div className="chat-message assistant">
            <div className="message-avatar">🤖</div>
            <div className="message-content">
              <div className="typing-indicator">
                <span></span>
                <span></span>
                <span></span>
              </div>
            </div>
          </div>
        )}

        {error && (
          <div className="chat-error">
            <strong>Erro:</strong> {error}
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      <form className="chat-input-form" onSubmit={handleSendMessage}>
        <input
          ref={inputRef}
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Faça uma pergunta sobre suas compras..."
          disabled={isLoading}
          className="chat-input"
        />
        <button
          type="submit"
          disabled={isLoading || !input.trim()}
          className="chat-send-btn"
        >
          {isLoading ? "⏳" : "➤"}
        </button>
      </form>
    </div>
  );
};

export default ChatAssistantComponent;
