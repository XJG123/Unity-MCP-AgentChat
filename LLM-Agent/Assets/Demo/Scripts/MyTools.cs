#nullable enable
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using UnityEngine;

/// <summary>
/// Unity-MCP Runtime 自定义工具。
/// 在编译后的应用中，AI Agent 可以通过 MCP 协议调用这些方法。
/// </summary>
[AiToolType]
public static class MyTools
{

    [AiTool("open the cube",Title ="打开cube")]
    [Description("打开cube")]
    public static string OpenCube()
    {
        OpenCubeTee.Instance.OpenCubeTees();
        return "success";
    }


    // // ── 示例 1：获取玩家信息 ──────────────────────────

    // [AiTool("my-get-player-info", Title = "获取玩家信息")]
    // [Description("返回当前玩家的位置和状态。按名称查找（默认 'Player'）。")]
    // public static PlayerInfo GetPlayerInfo(
    //     [Description("玩家对象名称，默认 'Player'")] string playerName = "Player")
    // {
    //     return MainThread.Instance.Run(() =>
    //     {
    //         var player = GameObject.Find(playerName);
    //         if (player == null)
    //             return new PlayerInfo { found = false };

    //         return new PlayerInfo
    //         {
    //             found = true,
    //             position = player.transform.position,
    //             active = player.activeSelf
    //         };
    //     });
    // }

    // // ── 示例 2：生成物体 ──────────────────────────────

    // [AiTool("my-spawn-object", Title = "生成物体")]
    // [Description("在指定坐标创建 GameObject。shape 可选 Sphere/Cube/Capsule/Cylinder/Plane。color 用 hex 格式如 #FF0000。")]
    // public static string SpawnObject(
    //     [Description("X 坐标")] float x,
    //     [Description("Y 坐标")] float y,
    //     [Description("Z 坐标")] float z,
    //     [Description("物体名称")] string objectName = "Object",
    //     [Description("形状: Sphere, Cube, Capsule, Cylinder, Plane")] string shape = "Sphere",
    //     [Description("颜色 (hex, 如 #FF4444)")] string color = "#FFFFFF",
    //     [Description("数量")] int count = 1)
    // {
    //     return MainThread.Instance.Run(() =>
    //     {
    //         for (int i = 0; i < count; i++)
    //         {
    //             var pos = new Vector3(x + i * 2f, y, z);
    //             var obj = GameObject.CreatePrimitive(
    //                 shape switch
    //                 {
    //                     "Cube" => PrimitiveType.Cube,
    //                     "Capsule" => PrimitiveType.Capsule,
    //                     "Cylinder" => PrimitiveType.Cylinder,
    //                     "Plane" => PrimitiveType.Plane,
    //                     _ => PrimitiveType.Sphere
    //                 });
    //             obj.name = count == 1 ? objectName : $"{objectName}_{i}";
    //             obj.transform.position = pos;

    //             // 设置颜色 (兼容 URP/Built-in)
    //             if (ColorUtility.TryParseHtmlString(color, out var c))
    //             {
    //                 var renderer = obj.GetComponent<Renderer>();
    //                 if (renderer != null)
    //                 {
    //                     var mat = renderer.material;
    //                     // URP 用 _BaseColor，Built-in 用 _Color
    //                     if (mat.HasProperty("_BaseColor"))
    //                         mat.SetColor("_BaseColor", c);
    //                     else
    //                         mat.color = c;
    //                 }
    //             }
    //         }
    //         return $"[OK] 生成了 {count} 个 {shape} '{objectName}'";
    //     });
    // }

    // // ── 示例 3：列出场景所有 GameObject ──────────────

    // [AiTool("my-list-all-objects", Title = "列出场景所有物体")]
    // [Description("返回当前场景中所有根级 GameObject 的名字、位置、激活状态。")]
    // public static SceneObjectInfo[] ListAllObjects()
    // {
    //     return MainThread.Instance.Run(() =>
    //     {
    //         var all = UnityEngine.Object.FindObjectsByType<GameObject>(
    //             FindObjectsSortMode.None);
    //         var result = new SceneObjectInfo[all.Length];
    //         for (int i = 0; i < all.Length; i++)
    //         {
    //             var go = all[i];
    //             result[i] = new SceneObjectInfo
    //             {
    //                 name = go.name,
    //                 position = go.transform.position,
    //                 active = go.activeSelf,
    //                 childCount = go.transform.childCount
    //             };
    //         }
    //         return result;
    //     });
    // }

    // // ── 清理工具 ────────────────────────────────────

    // [AiTool("my-delete-objects", Title = "按名称删除物体")]
    // [Description("删除名称匹配的 GameObject。nameMatch 支持部分匹配（Contains）。")]
    // public static string DeleteObjects(
    //     [Description("名称匹配关键字（如 'Sphere' 删除所有含 Sphere 的物体）")] string nameMatch)
    // {
    //     return MainThread.Instance.Run(() =>
    //     {
    //         var all = UnityEngine.Object.FindObjectsByType<GameObject>(
    //             FindObjectsSortMode.None);
    //         int deleted = 0;
    //         foreach (var go in all)
    //         {
    //             if (go.name.Contains(nameMatch, StringComparison.OrdinalIgnoreCase))
    //             {
    //                 UnityEngine.Object.Destroy(go);
    //                 deleted++;
    //             }
    //         }
    //         return $"[OK] 删除了 {deleted} 个名称含 '{nameMatch}' 的物体";
    //     });
    // }

    // // ── 示例 4：控制游戏速度 ──────────────────────────

    // [AiTool("my-set-time-scale", Title = "设置时间缩放")]
    // [Description("设置 Time.timeScale。1=正常，0.5=慢动作，2=双倍速。")]
    // public static string SetTimeScale(
    //     [Description("时间缩放值 (0.0-10.0)")] float scale)
    // {
    //     return MainThread.Instance.Run(() =>
    //     {
    //         scale = Mathf.Clamp(scale, 0f, 10f);
    //         Time.timeScale = scale;
    //         return $"[OK] Time.timeScale = {scale}";
    //     });
    // }

    // // ── 示例 5：异步耗时操作 ──────────────────────────

    // [AiTool("my-do-heavy-work", Title = "执行耗时操作")]
    // [Description("执行一个需要等待的操作（模拟加载、计算等）。异步方法。")]
    // public static async Task<string> DoHeavyWork(
    //     [Description("操作名称")] string operationName = "default")
    // {
    //     // 后台等待
    //     await Task.Delay(500);
    //     // 回到主线程
    //     return await MainThread.Instance.RunAsync(() =>
    //         $"[OK] '{operationName}' 操作完成"
    //     );
    // }
}

// ── 数据模型 ─────────────────────────────────────────

public class PlayerInfo
{
    public bool found;
    public Vector3 position;
    public bool active;
}

public class SceneObjectInfo
{
    public string name = "";
    public Vector3 position;
    public bool active;
    public int childCount;
}
