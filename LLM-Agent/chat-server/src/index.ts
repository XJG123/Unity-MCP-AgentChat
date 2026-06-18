/**
 * Unity Chat Server — HTTP server with SSE streaming.
 * 
 * Endpoints:
 *   POST /chat     — Send message, receive SSE stream
 *   GET  /health   — Health check
 * 
 * Usage: bun run src/index.ts
 */

import { initialize } from "./mcp-client";
import { chat, healthCheck } from "./llm-client";

const PORT = parseInt(process.env.SERVER_PORT || "3456");

// --- Conversation history (in-memory, one session) ---
interface ChatMessage {
  role: "system" | "user" | "assistant" | "tool";
  content: string;
  tool_calls?: any[];
  tool_call_id?: string;
  name?: string;
}

let history: ChatMessage[] = [];
const MAX_HISTORY = 40;

// --- ReadableStream → SSE helper ---
function createSSEStream(
  generator: AsyncGenerator<string>
): ReadableStream<Uint8Array> {
  const encoder = new TextEncoder();

  return new ReadableStream({
    async start(controller) {
      try {
        for await (const chunk of generator) {
          // Split by newlines for proper SSE format
          for (const line of chunk.split("\n")) {
            if (line) {
              controller.enqueue(encoder.encode(`data: ${line}\n`));
            }
          }
        }
        controller.enqueue(encoder.encode("data: [DONE]\n\n"));
        controller.close();
      } catch (err: any) {
        console.error("[SSE] Stream error:", err.message);
        controller.enqueue(
          encoder.encode(`data: Error: ${err.message}\n\n`)
        );
        controller.enqueue(encoder.encode("data: [DONE]\n\n"));
        controller.close();
      }
    },
  });
}

// --- Server ---

const server = Bun.serve({
  port: PORT,
  async fetch(req) {
    const url = new URL(req.url);

    // CORS headers for Unity
    const corsHeaders: Record<string, string> = {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
      "Access-Control-Allow-Headers": "Content-Type",
    };

    // Handle CORS preflight
    if (req.method === "OPTIONS") {
      return new Response(null, { status: 204, headers: corsHeaders });
    }

    // Health check
    if (url.pathname === "/health" && req.method === "GET") {
      const health = await healthCheck();
      return Response.json(
        {
          status: "ok",
          uptime: process.uptime(),
          mcpConnected: health.tools >= 0,
          toolsAvailable: health.tools,
          llmApiOk: health.api,
        },
        { headers: corsHeaders }
      );
    }

    // Chat endpoint
    if (url.pathname === "/chat" && req.method === "POST") {
      try {
        const body = await req.json();
        const userMessage: string = body.message?.trim();

        if (!userMessage) {
          return Response.json(
            { error: "Missing 'message' field" },
            { status: 400, headers: corsHeaders }
          );
        }

        // Trim history to MAX_HISTORY
        if (history.length > MAX_HISTORY) {
          history = history.slice(-MAX_HISTORY);
        }

        console.log(`[Chat] User: "${userMessage.slice(0, 100)}"`);

        // Create SSE stream
        const stream = createSSEStream(chat(userMessage, history));

        // Collect full response for history
        const fullResponse: string[] = [];
        const tee = stream.tee();
        const reader = tee[1].getReader();

        // Background: collect response text for history
        (async () => {
          const decoder = new TextDecoder();
          while (true) {
            const { done, value } = await reader.read();
            if (done) break;
            const text = decoder.decode(value, { stream: true });
            for (const line of text.split("\n")) {
              if (line.startsWith("data: ") && line !== "data: [DONE]") {
                const content = line.slice(6);
                // Skip tool-call notifications (they start with 🔧)
                if (!content.startsWith("🔧")) {
                  fullResponse.push(content);
                }
              }
            }
          }
          // Save to history
          history.push({ role: "user", content: userMessage });
          const responseText = fullResponse.join("").trim();
          if (responseText) {
            history.push({ role: "assistant", content: responseText });
          }
        })();

        return new Response(tee[0], {
          headers: {
            "Content-Type": "text/event-stream",
            "Cache-Control": "no-cache",
            Connection: "keep-alive",
            ...corsHeaders,
          },
        });
      } catch (err: any) {
        console.error("[Chat] Error:", err.message);
        return Response.json(
          { error: err.message },
          { status: 500, headers: corsHeaders }
        );
      }
    }

    // Reset history
    if (url.pathname === "/reset" && req.method === "POST") {
      history = [];
      return Response.json({ ok: true, message: "History cleared" }, { headers: corsHeaders });
    }

    return new Response("Unity Chat Server — POST /chat", {
      status: 404,
      headers: corsHeaders,
    });
  },
});

console.log(`🚀 Unity Chat Server running at http://localhost:${PORT}`);
console.log(`   POST /chat  — Send message, get SSE stream`);
console.log(`   GET  /health — Health check`);
console.log(`   POST /reset — Clear conversation history`);

// Initialize MCP connection on startup
(async () => {
  try {
    await initialize();
    console.log("[Init] MCP connected");
  } catch (err: any) {
    console.warn("[Init] MCP not available yet (Unity may not be connected):", err.message);
    console.warn("[Init] Server will still accept chat requests (without tool access)");
  }
})();
