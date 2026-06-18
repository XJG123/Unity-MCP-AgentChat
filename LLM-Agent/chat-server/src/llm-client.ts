/**
 * LLM Client — calls DeepSeek API with OpenAI-compatible function calling.
 * Converts MCP tools → OpenAI tool format and handles tool-call loops.
 */

import { listTools, callTool, type McpTool } from "./mcp-client";

const DEEPSEEK_API_KEY = process.env.DEEPSEEK_API_KEY || "";
const DEEPSEEK_BASE_URL = process.env.DEEPSEEK_BASE_URL || "https://api.deepseek.com";
const MODEL = process.env.LLM_MODEL || "deepseek-chat";

// --- Type Definitions ---

interface ChatMessage {
  role: "system" | "user" | "assistant" | "tool";
  content: string;
  tool_calls?: ToolCall[];
  tool_call_id?: string;
  name?: string;
}

interface ToolCall {
  id: string;
  type: "function";
  function: {
    name: string;
    arguments: string;
  };
}

interface OpenAITool {
  type: "function";
  function: {
    name: string;
    description: string;
    parameters: McpTool["inputSchema"];
  };
}

// --- Tool Name Sanitization ---
// DeepSeek requires tool names matching ^[a-zA-Z0-9_-]+$
const nameMap = new Map<string, string>(); // sanitized → original

function sanitizeName(name: string): string {
  const sanitized = name.replace(/[^a-zA-Z0-9_-]/g, "_").replace(/_+/g, "_");
  nameMap.set(sanitized, name);
  return sanitized;
}

function resolveName(sanitized: string): string {
  return nameMap.get(sanitized) || sanitized;
}

// --- Tool Format Conversion ---

function mcpToOpenAITool(tool: McpTool): OpenAITool {
  return {
    type: "function",
    function: {
      name: sanitizeName(tool.name),
      description: tool.description || `Call the "${tool.name}" tool`,
      parameters: tool.inputSchema,
    },
  };
}

// --- LLM API Call ---

let toolCache: OpenAITool[] | null = null;

/** Refresh tool definitions from MCP server */
async function refreshTools(): Promise<OpenAITool[]> {
  try {
    const tools = await listTools();
    toolCache = tools.map(mcpToOpenAITool);
    console.log(`[LLM] Loaded ${toolCache.length} MCP tools`);
    return toolCache;
  } catch (err) {
    console.warn("[LLM] Failed to load tools, using cache:", (err as Error).message);
    return toolCache ?? [];
  }
}

/** Call DeepSeek API (non-streaming for tool-call loop) */
async function callLLM(messages: ChatMessage[], tools?: OpenAITool[]): Promise<{
  content: string | null;
  toolCalls: ToolCall[] | null;
}> {
  const body: any = {
    model: MODEL,
    messages,
    temperature: 0.7,
    max_tokens: 4096,
  };

  if (tools && tools.length > 0) {
    body.tools = tools;
    body.tool_choice = "auto";
  }

  const resp = await fetch(`${DEEPSEEK_BASE_URL}/v1/chat/completions`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${DEEPSEEK_API_KEY}`,
    },
    body: JSON.stringify(body),
  });

  if (!resp.ok) {
    const errText = await resp.text();
    throw new Error(`DeepSeek API error ${resp.status}: ${errText.slice(0, 300)}`);
  }

  const data = await resp.json();
  const choice = data.choices?.[0];
  const msg = choice?.message;

  return {
    content: msg?.content ?? null,
    toolCalls: msg?.tool_calls ?? null,
  };
}

/** Call DeepSeek API with streaming — yields content chunks */
async function* callLLMStream(
  messages: ChatMessage[],
  tools?: OpenAITool[]
): AsyncGenerator<string> {
  const body: any = {
    model: MODEL,
    messages,
    temperature: 0.7,
    max_tokens: 4096,
    stream: true,
    stream_options: { include_usage: false },
  };

  if (tools && tools.length > 0) {
    body.tools = tools;
    body.tool_choice = "auto";
  }

  const resp = await fetch(`${DEEPSEEK_BASE_URL}/v1/chat/completions`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${DEEPSEEK_API_KEY}`,
    },
    body: JSON.stringify(body),
  });

  if (!resp.ok) {
    const errText = await resp.text();
    throw new Error(`DeepSeek API error ${resp.status}: ${errText.slice(0, 300)}`);
  }

  const reader = resp.body!.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split("\n");
    buffer = lines.pop() || "";

    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed || !trimmed.startsWith("data: ")) continue;
      const dataStr = trimmed.slice(6);
      if (dataStr === "[DONE]") return;

      try {
        const data = JSON.parse(dataStr);
        const delta = data.choices?.[0]?.delta;
        if (delta?.content) yield delta.content;
      } catch {
        // skip malformed chunks
      }
    }
  }
}

// --- Chat Orchestration ---

const SYSTEM_PROMPT = `You are a helpful AI assistant embedded in a Unity game. 
You have access to tools that can interact with the game world. 
When the player asks you to do something in the game, use the available tools.
Be friendly and concise. Respond in Chinese if the player writes in Chinese.`;

/** Full chat turn: user message → LLM → (tool calls) → final response */
export async function* chat(userMessage: string, history: ChatMessage[]): AsyncGenerator<string> {
  // Ensure tools are loaded
  const tools = toolCache ?? (await refreshTools());

  // Build messages
  const messages: ChatMessage[] = [
    { role: "system", content: SYSTEM_PROMPT },
    ...history,
    { role: "user", content: userMessage },
  ];

  // Step 1: Call LLM (non-streaming first to detect tool calls)
  const response = await callLLM(messages, tools);

  // If LLM wants to call tools
  if (response.toolCalls && response.toolCalls.length > 0) {
    // Add assistant message with tool calls to history
    messages.push({
      role: "assistant",
      content: response.content || "",
      tool_calls: response.toolCalls,
    });

    // Execute each tool call
    for (const tc of response.toolCalls) {
      const llmToolName = tc.function.name;
      const toolName = resolveName(llmToolName); // sanitized → original MCP name
      let args: Record<string, any>;
      try {
        args = JSON.parse(tc.function.arguments);
      } catch {
        args = {};
      }

      yield `🔧 Calling: ${toolName}...\n`;
      const result = await callTool(toolName, args);

      messages.push({
        role: "tool",
        tool_call_id: tc.id,
        name: llmToolName, // use the name LLM knows (sanitized)
        content: result,
      });
    }

    // Step 2: Call LLM again with tool results (streaming this time)
    yield `\n`;
    for await (const chunk of callLLMStream(messages, tools)) {
      yield chunk;
    }
  } else if (response.content) {
    // No tool calls — stream the response directly
    // (we already got the full content, so just yield it)
    yield response.content;
  } else {
    yield "(No response from LLM)";
  }
}

/** Health check */
export async function healthCheck(): Promise<{ ok: boolean; tools: number; api: boolean }> {
  let tools = 0;
  let api = false;
  try {
    const t = await refreshTools();
    tools = t.length;
  } catch {}
  try {
    const resp = await fetch(`${DEEPSEEK_BASE_URL}/v1/models`, {
      headers: { Authorization: `Bearer ${DEEPSEEK_API_KEY}` },
    });
    api = resp.ok;
  } catch {}
  return { ok: tools > 0 || api, tools, api };
}
