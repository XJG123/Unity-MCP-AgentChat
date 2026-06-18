using System.Reflection;
using com.IvanMurzak.Unity.MCP;
using UnityEngine;

public class McpPluginDemo : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    async void McppluginTest()
    {
        // Build MCP plugin
    var mcpPlugin = UnityMcpPluginRuntime.Initialize(builder =>
    {
        builder.WithConfig(config =>
        {
            config.Host = "http://localhost:8080";
            config.Token = "your-token";
        });
        // Automatically register all tools from the current assembly
        builder.WithToolsFromAssembly(Assembly.GetExecutingAssembly());
    })
    .Build();

        await mcpPlugin.Connect(); // Start active connection with retry to the MCP server

        await mcpPlugin.Disconnect(); // Stop active connection and close existed connection
    }
}
