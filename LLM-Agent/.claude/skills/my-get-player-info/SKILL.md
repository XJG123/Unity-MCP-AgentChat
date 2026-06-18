---
name: my-get-player-info
description: 返回当前玩家的位置和状态。按名称查找（默认 'Player'）。
---

# 获取玩家信息

## How to Call

```bash
unity-mcp-cli run-tool my-get-player-info --input '{
  "playerName": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool my-get-player-info --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool my-get-player-info --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `playerName` | `string` | No | 玩家对象名称，默认 'Player' |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "playerName": {
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
      "$ref": "#/$defs/PlayerInfo"
    }
  },
  "$defs": {
    "UnityEngine.Vector3": {
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
        }
      },
      "required": [
        "x",
        "y",
        "z"
      ],
      "additionalProperties": false
    },
    "PlayerInfo": {
      "type": "object",
      "properties": {
        "found": {
          "type": "boolean"
        },
        "position": {
          "$ref": "#/$defs/UnityEngine.Vector3"
        },
        "active": {
          "type": "boolean"
        }
      },
      "required": [
        "found",
        "position",
        "active"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

