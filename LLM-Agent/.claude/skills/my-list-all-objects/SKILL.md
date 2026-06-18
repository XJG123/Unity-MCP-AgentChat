---
name: my-list-all-objects
description: 返回当前场景中所有根级 GameObject 的名字、位置、激活状态。
---

# 列出场景所有物体

## How to Call

```bash
unity-mcp-cli run-tool my-list-all-objects --input '{}'
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
      "$ref": "#/$defs/SceneObjectInfo-1"
    }
  },
  "$defs": {
    "SceneObjectInfo": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string"
        },
        "position": {
          "$ref": "#/$defs/UnityEngine.Vector3"
        },
        "active": {
          "type": "boolean"
        },
        "childCount": {
          "type": "integer"
        }
      },
      "required": [
        "position",
        "active",
        "childCount"
      ]
    },
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
    "SceneObjectInfo-1": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/SceneObjectInfo"
      }
    }
  },
  "required": [
    "result"
  ]
}
```

