---
name: my-set-time-scale
description: 设置 Time.timeScale。1=正常，0.5=慢动作，2=双倍速。
---

# 设置时间缩放

## How to Call

```bash
unity-mcp-cli run-tool my-set-time-scale --input '{
  "scale": 0
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool my-set-time-scale --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool my-set-time-scale --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `scale` | `number` | Yes | 时间缩放值 (0.0-10.0) |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "scale": {
      "type": "number"
    }
  },
  "required": [
    "scale"
  ]
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

