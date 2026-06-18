---
name: my-do-heavy-work
description: 执行一个需要等待的操作（模拟加载、计算等）。异步方法。
---

# 执行耗时操作

## How to Call

```bash
unity-mcp-cli run-tool my-do-heavy-work --input '{
  "operationName": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool my-do-heavy-work --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool my-do-heavy-work --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `operationName` | `string` | No | 操作名称 |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "operationName": {
      "type": "string"
    }
  }
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

