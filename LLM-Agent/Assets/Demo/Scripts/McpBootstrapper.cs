#nullable enable
using System.Reflection;
using com.IvanMurzak.Unity.MCP;
using UnityEngine;

/// <summary>
/// 游戏启动时自动初始化 MCP 连接。
/// 挂载到第一个场景的任意 GameObject，或在 Build Settings 中设为第一个场景。
/// </summary>
public class McpBootstrapper : MonoBehaviour
{
    [Header("MCP Server 配置")]
    [Tooltip("你的 MCP Server 地址")]
    [SerializeField] private string _host = "http://localhost:8080";

    [Tooltip("认证 token（服务器端没开 auth 就留空）")]
    [SerializeField] private string _token = "";

    [Tooltip("是否在 Start 时自动连接")]
    [SerializeField] private bool _autoConnect = true;

    private async void Start()
    {
        if (!_autoConnect) return;

        Debug.Log($"[MCP Boot] 正在连接 {_host} ...");

        try
        {
            var mcpPlugin = UnityMcpPluginRuntime.Initialize(builder =>
                {
                    builder.WithConfig(config =>
                    {
                        config.Host = _host;
                        if (!string.IsNullOrEmpty(_token))
                            config.Token = _token;
                    });

                    // 注册当前 Assembly 中所有带 [AiToolType] 的类
                    builder.WithToolsFromAssembly(Assembly.GetExecutingAssembly());
                })
                .Build();

            await mcpPlugin.Connect();
            Debug.Log($"[MCP Boot] ✅ 已连接 {_host}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MCP Boot] ❌ 连接失败: {ex.Message}");
        }
    }
}
