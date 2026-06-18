---
name: my-delete-objects
description: 删除名称匹配的 GameObject。nameMatch 支持部分匹配（Contains）。
---

# 按名称删除物体

## How to Call

```bash
unity-mcp-cli run-tool my-delete-objects --input '{
  "nameMatch": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool my-delete-objects --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool my-delete-objects --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `nameMatch` | `string` | Yes | 名称匹配关键字（如 'Sphere' 删除所有含 Sphere 的物体） |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "nameMatch": {
      "type": "string"
    }
  },
  "required": [
    "nameMatch"
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

