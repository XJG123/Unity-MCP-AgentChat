---
name: my-spawn-enemy
description: "在指定坐标生成敌人。type: melee/ranged/boss。count: 一次生成几个。"
---

# 生成敌人

## How to Call

```bash
unity-mcp-cli run-tool my-spawn-enemy --input '{
  "x": 0,
  "y": 0,
  "z": 0,
  "enemyType": "string_value",
  "count": 0
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool my-spawn-enemy --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool my-spawn-enemy --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `x` | `number` | Yes | X 坐标 |
| `y` | `number` | Yes | Y 坐标 |
| `z` | `number` | Yes | Z 坐标 |
| `enemyType` | `string` | No | 敌人类型: melee, ranged, boss |
| `count` | `integer` | No | 生成数量 |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "x": {
      "type": "number"
    },
    "y": {
      "type": "number"
    },
    "z": {
      "type": "number"
    },
    "enemyType": {
      "type": "string"
    },
    "count": {
      "type": "integer"
    }
  },
  "required": [
    "x",
    "y",
    "z"
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

