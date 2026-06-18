---
name: my-spawn-object
description: 在指定坐标创建 GameObject。shape 可选 Sphere/Cube/Capsule/Cylinder/Plane。color 用 hex 格式如 #FF0000。
---

# 生成物体

## How to Call

```bash
unity-mcp-cli run-tool my-spawn-object --input '{
  "x": 0,
  "y": 0,
  "z": 0,
  "objectName": "string_value",
  "shape": "string_value",
  "color": "string_value",
  "count": 0
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool my-spawn-object --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool my-spawn-object --input-file - <<'EOF'
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
| `objectName` | `string` | No | 物体名称 |
| `shape` | `string` | No | 形状: Sphere, Cube, Capsule, Cylinder, Plane |
| `color` | `string` | No | 颜色 (hex, 如 #FF4444) |
| `count` | `integer` | No | 数量 |

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
    "objectName": {
      "type": "string"
    },
    "shape": {
      "type": "string"
    },
    "color": {
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

