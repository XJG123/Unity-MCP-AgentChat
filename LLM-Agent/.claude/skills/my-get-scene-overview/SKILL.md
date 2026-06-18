---
name: my-get-scene-overview
description: 返回场景中所有重要对象的统计信息：敌人数、玩家位置。
---

# 获取场景概览

## How to Call

```bash
unity-mcp-cli run-tool my-get-scene-overview --input '{}'
```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

This tool takes no input parameters.

### Input JSON Schema

```json
{
  "type": "object",
  "additionalProperties": false
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/SceneOverview"
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
    "SceneOverview": {
      "type": "object",
      "properties": {
        "enemyCount": {
          "type": "integer"
        },
        "playerPosition": {
          "$ref": "#/$defs/UnityEngine.Vector3"
        }
      },
      "required": [
        "enemyCount",
        "playerPosition"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

