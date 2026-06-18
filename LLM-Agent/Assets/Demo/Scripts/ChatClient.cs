using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Unity → Bun Chat Server 的 HTTP/SSE 客户端。
/// 发送聊天消息，接收流式 SSE 响应。
/// </summary>
public class ChatClient : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string serverUrl = "http://localhost:3000";

    [Header("Settings")]
    [SerializeField] private float requestTimeout = 60f;
    [SerializeField] private bool debugLog = true;

    /// <summary>收到一个文本块时触发（流式）</summary>
    public event Action<string> OnTextChunk;

    /// <summary>收到工具调用通知时触发</summary>
    public event Action<string> OnToolCall;

    /// <summary>完整响应结束时触发</summary>
    public event Action<string> OnComplete;

    /// <summary>发生错误时触发</summary>
    public event Action<string> OnError;

    private UnityWebRequest _currentRequest;
    private StringBuilder _fullResponse = new StringBuilder();

    /// <summary>发送聊天消息，通过事件回调返回流式结果</summary>
    public void SendMessage(string message)
    {
        if (_currentRequest != null)
        {
            Debug.LogWarning("[ChatClient] Request already in progress, aborting...");
            _currentRequest.Abort();
        }

        StartCoroutine(SendChatCoroutine(message));
    }

    /// <summary>取消当前请求</summary>
    public void Cancel()
    {
        _currentRequest?.Abort();
        _currentRequest = null;
    }

    private IEnumerator SendChatCoroutine(string message)
    {
        _fullResponse.Clear();

        // 构建 JSON body
        var payload = $"{{\"message\":\"{JsonEscape(message)}\"}}";
        if (debugLog) Debug.Log($"[ChatClient] Sending: \"{message}\"");

        using var request = new UnityWebRequest($"{serverUrl}/chat", "POST");
        request.timeout = (int)requestTimeout;

        // 设置 body
        byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.uploadHandler.contentType = "application/json";

        // 自定义 download handler 处理流式 SSE
        var sseHandler = new SSEDownloadHandler();
        sseHandler.OnDataLine += HandleSSELine;
        request.downloadHandler = sseHandler;

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "text/event-stream");

        _currentRequest = request;

        yield return request.SendWebRequest();

        _currentRequest = null;

        if (request.result != UnityWebRequest.Result.Success)
        {
            var error = $"[ChatClient] Request failed: {request.error}";
            Debug.LogError(error);
            OnError?.Invoke(request.error);
        }
    }

    private void HandleSSELine(string data)
    {
        if (data.StartsWith("🔧"))
        {
            // 工具调用通知
            if (debugLog) Debug.Log($"[ChatClient] Tool: {data}");
            OnToolCall?.Invoke(data);
        }
        else if (data == "[DONE]")
        {
            // SSE 结束
            var response = _fullResponse.ToString();
            if (debugLog) Debug.Log($"[ChatClient] Complete: \"{response.Truncate(100)}\"");
            OnComplete?.Invoke(response);
        }
        else if (data.StartsWith("Error:"))
        {
            Debug.LogError($"[ChatClient] Server error: {data}");
            OnError?.Invoke(data);
        }
        else
        {
            // 普通文本块
            _fullResponse.Append(data);
            OnTextChunk?.Invoke(data);
        }
    }

    /// <summary>简单的 JSON 字符串转义</summary>
    private static string JsonEscape(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }

    private void OnDestroy()
    {
        _currentRequest?.Abort();
    }
}

/// <summary>
/// 自定义 DownloadHandler：逐行解析 SSE 流。
/// 每收到一个 "data: ..." 行就触发回调。
/// </summary>
public class SSEDownloadHandler : DownloadHandlerScript
{
    public event Action<string> OnDataLine;

    private readonly StringBuilder _lineBuffer = new StringBuilder();

    public SSEDownloadHandler() : base(new byte[4096]) { }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || dataLength == 0) return true;

        var text = Encoding.UTF8.GetString(data, 0, dataLength);
        _lineBuffer.Append(text);

        // 按行解析
        while (true)
        {
            var buf = _lineBuffer.ToString();
            var newlinePos = buf.IndexOf('\n');
            if (newlinePos < 0) break;

            var line = buf.Substring(0, newlinePos).TrimEnd('\r');
            _lineBuffer.Remove(0, newlinePos + 1);

            if (line.StartsWith("data: "))
            {
                var dataContent = line.Substring(6);
                OnDataLine?.Invoke(dataContent);
            }
        }

        return true;
    }

    protected override byte[] GetData() => null;
}

/// <summary>字符串扩展</summary>
public static class StringExtensions
{
    public static string Truncate(this string s, int maxLength)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= maxLength) return s;
        return s.Substring(0, maxLength) + "...";
    }
}
