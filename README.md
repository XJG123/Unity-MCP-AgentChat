# Unity-MCP AI 聊天系统 — 项目文档

> 生成日期: 2026-06-18 | Unity-MCP v0.81.0 | GameDev-MCP-Server v8.0.0.0

项目底座：https://github.com/IvanMurzak/Unity-MCP
---

## 1. 架构全景

```
┌─────────────────────────────────────────────────────────────┐
│  Unity Runtime App (编译后的游戏)                            │
│                                                             │
│  ChatUI.cs          ChatClient.cs        McpBootstrapper.cs │
│  ┌──────────┐       ┌──────────────┐     ┌───────────────┐  │
│  │ UGUI聊天  │──▶──▶│ SSE流式接收   │     │ MCP插件初始化  │  │
│  │ 自动创建  │◀──◀──│ HTTP POST    │     │ 注册7个自定义  │  │
│  │ Canvas   │       │ :3000/chat   │     │ Tool          │  │
│  └──────────┘       └──────────────┘     └───────┬───────┘  │
│                                                  │ SignalR  │
│  MyTools.cs ── 7 个自定义 MCP Tool               │          │
│  ├── open the menu                                │          │
│  ├── my-get-player-info                           │          │
│  ├── my-spawn-object (坐标/形状/颜色/数量)         │          │
│  ├── my-list-all-objects                          │          │
│  ├── my-delete-objects (名称匹配)                 │          │
│  ├── my-set-time-scale                            │          │
│  └── my-do-heavy-work (异步)                      │          │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────┼──────────────────────────────────────┐
│  gamedev-mcp-server  │  :8080                               │
│  (GameDev-MCP-Server │  v8.0.0.0)                           │
│                      │  streamableHttp                       │
└──────────────────────┼──────────────────────────────────────┘
                       │ MCP protocol
┌──────────────────────┼──────────────────────────────────────┐
│  Bun Chat Server     │  :3000                               │
│                      │                                      │
│  src/index.ts        │  HTTP + SSE                          │
│  src/llm-client.ts   │  DeepSeek API + function calling     │
│  src/mcp-client.ts   │  MCP streamableHttp 客户端           │
│                      │                                      │
│  流程: 用户消息 → 非流式LLM(探测function call)               │
│       → 执行MCP工具 → 结果喂回LLM → 流式SSE返回文本         │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 项目文件清单

```
LLM-Agent/
│
├── chat-server/                    ← Bun 服务器（3 个源文件）
│   ├── .env                        ← API Key 配置
│   ├── package.json                ← Bun 依赖
│   ├── tsconfig.json
│   └── src/
│       ├── index.ts                ← 入口：Bun.serve() HTTP + SSE
│       ├── llm-client.ts           ← DeepSeek API + tool calling 编排
│       └── mcp-client.ts           ← MCP streamableHttp 协议客户端
│
└── Assets/Demo/Scripts/
    ├── ChatClient.cs               ← Unity SSE 客户端（UnityWebRequest）
    ├── ChatUI.cs                   ← UGUI 聊天界面（自动创建 Canvas）
    ├── MyTools.cs                  ← 7 个自定义 MCP Tool
    └── McpBootstrapper.cs          ← Runtime MCP 初始化
```

---

## 3. 数据流（一次完整对话）

```
Player: "生成3个红绿蓝方块"
  │
  ▼
ChatClient.SendMessage()
  │  POST /chat {"message":"生成3个红绿蓝方块"}
  ▼
llm-client.ts: chat()
  │
  ├─ ① callLLM(messages, tools)
  │     DeepSeek 返回 3 个 tool_calls:
  │     ├── my-spawn-object(x=-3, color=#FF0000, shape=Cube)
  │     ├── my-spawn-object(x=0,  color=#00FF00, shape=Cube)
  │     └── my-spawn-object(x=3,  color=#0000FF, shape=Cube)
  │
  ├─ ② mcp-client.ts: callTool() x3
  │     MCP Server → Unity Plugin → 生成 3 个 Cube
  │
  └─ ③ callLLMStream(messages, tools)
        DeepSeek 流式返回中文总结
  │
  ▼
SSE stream → ChatClient.SSEDownloadHandler → ChatUI 逐字更新
```

---

## 4. 关键技术决策与解决的问题

| 问题 | 解决方案 |
|------|----------|
| DeepSeek API 不允许工具名含空格 | `open the menu` → `open_the_menu`，nameMap 反查 |
| UGUI 内容堆叠/不换行 | ContentSizeFitter: horizontal=Unconstrained, vertical=PreferredSize + ScrollView/Viewport 标准结构 |
| 聊天UI透明度与可读性 | Panel α=0.12, ScrollView α=0.08, 文字加 Shadow 组件 |
| Unity JSON 构造缺引号 | `JsonEscape` 外补 `"` 包裹 |
| MCP 二进制 GitHub 下载超时 | 手动复制 gamedev-mcp-server 到 `Library/mcp-server/osx-arm64/` |
| 服务器进程自动退出 | 用 `terminal(background=true)` 维持后台运行 |

---

## 5. 启动顺序

```bash
# 1. MCP Server（如果还未运行）
cd /path/to/project
./osx-arm64/gamedev-mcp-server --port 8080 --client-transport streamableHttp

# 2. Bun Chat Server
cd chat-server
bun run src/index.ts

# 3. Unity Build & Run
#    McpBootstrapper 自动连接 MCP Server
#    场景中挂 ChatSystem GameObject（ChatClient + ChatUI）

# 4. 游戏内 ChatUI 输入消息即可聊天
```

### 健康检查

```bash
# 检查 MCP Server
curl -s http://127.0.0.1:8080/   # 应返回 400（正常，需要 MCP 握手）

# 检查 Chat Server
curl -s http://127.0.0.1:3000/health
# {"status":"ok","mcpConnected":true,"toolsAvailable":7,"llmApiOk":true}

# 直接测试聊天
curl -s -N -X POST http://127.0.0.1:3000/chat \
  -H 'Content-Type: application/json' \
  -d '{"message":"你好"}'
```

---

## 6. MCP 自定义工具清单

在 Runtime 模式下，仅以下 7 个自定义工具可用（定义于 `Assets/Demo/Scripts/MyTools.cs`）：

| 工具名 | 功能 | 参数 |
|--------|------|------|
| `open the menu` | 返回 cube 状态 | 无 |
| `my-get-player-info` | 获取玩家位置和状态 | `playerName` (默认 "Player") |
| `my-spawn-object` | 在指定坐标生成物体 | `x, y, z, objectName, shape, color, count` |
| `my-list-all-objects` | 列出场景所有根级 GameObject | 无 |
| `my-delete-objects` | 按名称匹配删除物体 | `nameMatch` |
| `my-set-time-scale` | 设置游戏时间缩放 | `scale` (0-10) |
| `my-do-heavy-work` | 模拟耗时异步操作 | `operationName` (默认 "default") |

---

## 7. 服务端模块说明

### mcp-client.ts

- 实现 MCP streamableHttp 协议
- 自动握手（initialize → notifications/initialized）
- 提供 `listTools()` 获取工具定义
- 提供 `callTool()` 调用工具
- 自动管理 Session ID

### llm-client.ts

- 调用 DeepSeek API（OpenAI 兼容格式）
- MCP 工具 → OpenAI function calling 格式转换
- 工具名 sanitize（空格 → 下划线）以满足 `^[a-zA-Z0-9_-]+$` 约束
- 双阶段对话：
  - 阶段1: 非流式调用，探测 function call
  - 阶段2: 执行工具后流式返回最终响应
- 内存对话历史（最多 40 条消息）

### index.ts

- Bun.serve() HTTP 服务器
- POST /chat: 接收消息，返回 SSE 流
- GET /health: 健康检查
- POST /reset: 清空对话历史
- CORS headers 支持

---

## 8. Unity 模块说明

### ChatClient.cs

- 使用 `UnityWebRequest` + 自定义 `SSEDownloadHandler`
- 发送 JSON POST 请求到 Chat Server
- 解析 SSE 流（逐行读取 `data:` 前缀）
- 事件驱动：`OnTextChunk`, `OnToolCall`, `OnComplete`, `OnError`

### ChatUI.cs

- `[RequireComponent(typeof(ChatClient))]`
- 运行时自动创建完整 UGUI 界面：
  - StatusBar（顶部状态栏）
  - ScrollView（消息区域，带垂直滚动条）
  - InputField（多行输入）
  - SendButton（发送按钮）
- CanvasScaler: referenceResolution=1920x1080
- 文字 Shadow 组件保证透明背景可读性

---

## 9. 后续可优化方向

| 方向 | 说明 |
|------|------|
| 持久化对话历史 | 目前内存存储，重启丢失 → 接入 `bun:sqlite` |
| 多会话支持 | 全局单一 history → 按 session ID 隔离 |
| Unity UI 增强 | 消息气泡样式、打字动画、语音输入 |
| 虚拟列表 | 大文本量时 ContentSizeFitter 性能差 |
| API Key 安全 | 不应打包进 Unity build → 服务端令牌验证 |
| 多 Provider 支持 | Anthropic, OpenAI, 本地 Ollama |
| WebSocket 替代 HTTP | 减少延迟，支持真正的双向流 |

---

## 10. 环境依赖

| 组件 | 版本 | 端口 |
|------|------|------|
| Unity-MCP Plugin | 0.81.0 | — |
| GameDev-MCP-Server | 8.0.0.0 | 8080 |
| Bun | 1.3.12 | — |
| DeepSeek API | deepseek-chat | — |
| Chat Server | — | 3000 |
| Unity Editor/Runtime | 2022.3+ | — |

---

## 11. 目录索引

| 路径 | 说明 |
|------|------|
| `chat-server/src/index.ts` | Bun HTTP 服务器入口 |
| `chat-server/src/llm-client.ts` | LLM API + function calling |
| `chat-server/src/mcp-client.ts` | MCP 协议客户端 |
| `Assets/Demo/Scripts/ChatClient.cs` | Unity SSE 客户端 |
| `Assets/Demo/Scripts/ChatUI.cs` | Unity UGUI 聊天界面 |
| `Assets/Demo/Scripts/MyTools.cs` | 自定义 MCP 工具定义 |
| `Assets/Demo/Scripts/McpBootstrapper.cs` | Runtime MCP 初始化 |
| `Library/mcp-server/osx-arm64/gamedev-mcp-server` | MCP Server 二进制 |
