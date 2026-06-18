/**
 * MCP Client for gamedev-mcp-server (streamableHttp transport).
 * Handles: initialize → tools/list → tools/call
 */

const MCP_SERVER_URL = process.env.MCP_SERVER_URL || "http://127.0.0.1:8080";
const MCP_ENDPOINT = `${MCP_SERVER_URL}/mcp`;

export interface McpTool {
  name: string;
  description: string;
  inputSchema: {
    type: "object";
    properties: Record<string, any>;
    required?: string[];
  };
}

interface McpResponse {
  jsonrpc: "2.0";
  id?: number;
  result?: any;
  error?: { code: number; message: string };
}

let sessionId: string | null = null;
let requestId = 0;

async function mcpRequest(method: string, params?: any): Promise<any> {
  const id = ++requestId;
  const body: any = { jsonrpc: "2.0", id, method };
  if (params) body.params = params;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    Accept: "application/json, text/event-stream",
  };
  if (sessionId) headers["Mcp-Session-Id"] = sessionId;

  const resp = await fetch(MCP_ENDPOINT, {
    method: "POST",
    headers,
    body: JSON.stringify(body),
  });

  // Extract session ID from response headers
  const newSessionId = resp.headers.get("Mcp-Session-Id");
  if (newSessionId) sessionId = newSessionId;

  const text = await resp.text();

  // Parse SSE: lines starting with "data:"
  for (const line of text.split("\n")) {
    if (line.startsWith("data:")) {
      const data: McpResponse = JSON.parse(line.slice(5).trim());
      if (data.error) throw new Error(`MCP Error [${data.error.code}]: ${data.error.message}`);
      return data.result;
    }
  }

  // Some responses (like notifications/initialized) have no body
  if (!text.trim()) return null;

  throw new Error(`Unexpected MCP response: ${text.slice(0, 200)}`);
}

/** Initialize MCP session — must be called once at startup */
export async function initialize(): Promise<void> {
  console.log("[MCP] Connecting to", MCP_SERVER_URL);
  const result = await mcpRequest("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "unity-chat-server", version: "1.0" },
  });
  console.log("[MCP] Connected:", result.serverInfo?.name, result.serverInfo?.version);

  // Send initialized notification
  await fetch(MCP_ENDPOINT, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json, text/event-stream",
      "Mcp-Session-Id": sessionId!,
    },
    body: JSON.stringify({ jsonrpc: "2.0", method: "notifications/initialized" }),
  });
}

/** List all available tools from the MCP server */
export async function listTools(): Promise<McpTool[]> {
  const result = await mcpRequest("tools/list");
  return result?.tools ?? [];
}

/** Call a tool on the MCP server */
export async function callTool(name: string, args: Record<string, any>): Promise<string> {
  console.log(`[MCP] Calling tool: ${name}`, JSON.stringify(args).slice(0, 200));
  try {
    const result = await mcpRequest("tools/call", { name, arguments: args });
    // Extract text content from MCP response
    const content = result?.content ?? [];
    const textParts = content
      .filter((c: any) => c.type === "text")
      .map((c: any) => c.text)
      .join("\n");
    return textParts || JSON.stringify(result);
  } catch (err: any) {
    console.error(`[MCP] Tool ${name} failed:`, err.message);
    return `Error: ${err.message}`;
  }
}

/** Check if MCP session is alive */
export function isConnected(): boolean {
  return sessionId !== null;
}
