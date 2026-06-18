using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 使用 UGUI 的简易聊天 UI。
/// 需要 ChatClient 组件挂载在同一个 GameObject 上。
/// 如果没有现成的 UI Canvas，运行时会自动创建一个。
/// </summary>
[RequireComponent(typeof(ChatClient))]
public class ChatUI : MonoBehaviour
{
    [Header("UI References (可选留空则自动创建)")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Text messageArea;
    [SerializeField] private InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Text statusText;

    [Header("Settings")]
    [SerializeField] private int maxMessages = 100;
    [SerializeField] private string placeholderText = "输入消息...";

    private ChatClient _client;
    private string _currentAssistantMessage = "";
    private float _lastAutoScrollTime;

    private void Awake()
    {
        _client = GetComponent<ChatClient>();

        if (canvas == null)
            CreateUI();
    }

    private void Start()
    {
        _client.OnTextChunk += OnTextChunk;
        _client.OnToolCall += OnToolCall;
        _client.OnComplete += OnComplete;
        _client.OnError += OnError;

        sendButton?.onClick.AddListener(SendMessage);
        inputField?.onEndEdit.AddListener(text =>
        {
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
                SendMessage();
        });
    }

    /// <summary>自动创建 UI</summary>
    private void CreateUI()
    {
        // ── Canvas ──
        var canvasGO = new GameObject("ChatCanvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Panel (全屏半透明背景) ──
        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGO.transform, false);
        panel.AddComponent<RectTransform>().Stretch();
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.12f); // 几乎全透，微微压暗背景

        // ── Status bar (顶部) ──
        var statusBar = new GameObject("StatusBar");
        statusBar.transform.SetParent(panel.transform, false);
        var statusBarRT = statusBar.AddComponent<RectTransform>();
        statusBarRT.SetTopStretch(30);

        statusText = statusBar.AddComponent<Text>();
        statusText.font = GetFont();
        statusText.fontSize = 14;
        statusText.color = new Color(1, 1, 1, 0.6f);
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.text = "Chat ready — type a message";
        var statusShadow = statusBar.AddComponent<Shadow>();
        statusShadow.effectColor = new Color(0, 0, 0, 0.7f);
        statusShadow.effectDistance = new Vector2(1, -1);

        // ── Input area (底部) ──
        var inputArea = new GameObject("InputArea");
        inputArea.transform.SetParent(panel.transform, false);
        var inputAreaRT = inputArea.AddComponent<RectTransform>();
        inputAreaRT.SetBottomStretch(50);
        var inputLayout = inputArea.AddComponent<HorizontalLayoutGroup>();
        inputLayout.padding = new RectOffset(8, 8, 8, 8);
        inputLayout.spacing = 8;
        inputLayout.childAlignment = TextAnchor.MiddleCenter;
        inputLayout.childControlWidth = true;
        inputLayout.childControlHeight = true;
        inputLayout.childForceExpandWidth = false;
        inputLayout.childForceExpandHeight = true;

        // InputField
        var inputGO = CreateUIElement("InputField", inputArea.transform);
        var inputImg = inputGO.AddComponent<Image>();
        inputImg.color = new Color(0, 0, 0, 0.18f); // 极淡半透明，打字时能看清
        inputField = inputGO.AddComponent<InputField>();
        inputField.lineType = InputField.LineType.MultiLineNewline;

        // InputField LayoutElement — 让它占据剩余空间
        var inputLE = inputGO.AddComponent<LayoutElement>();
        inputLE.flexibleWidth = 1;

        // Placeholder
        var placeholderGO = CreateUIElement("Placeholder", inputGO.transform);
        placeholderGO.AddComponent<RectTransform>().Stretch();
        var phText = placeholderGO.AddComponent<Text>();
        phText.text = placeholderText;
        phText.font = GetFont();
        phText.fontSize = 16;
        phText.color = new Color(0.4f, 0.4f, 0.4f);
        phText.alignment = TextAnchor.MiddleLeft;
        inputField.placeholder = phText;

        // InputField text
        var textGO = CreateUIElement("Text", inputGO.transform);
        textGO.AddComponent<RectTransform>().Stretch();
        var inputText = textGO.AddComponent<Text>();
        inputText.font = GetFont();
        inputText.fontSize = 16;
        inputText.color = Color.white;
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.supportRichText = false;
        inputField.textComponent = inputText;

        // Send Button
        var btnGO = CreateUIElement("SendButton", inputArea.transform);
        var btnLE = btnGO.AddComponent<LayoutElement>();
        btnLE.minWidth = 70;
        btnLE.preferredWidth = 70;
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.5f, 0.9f);
        sendButton = btnGO.AddComponent<Button>();
        var colors = sendButton.colors;
        colors.highlightedColor = new Color(0.3f, 0.6f, 1f);
        colors.pressedColor = new Color(0.15f, 0.4f, 0.7f);
        sendButton.colors = colors;

        var btnTextGO = CreateUIElement("Text", btnGO.transform);
        btnTextGO.AddComponent<RectTransform>().Stretch();
        var btnText = btnTextGO.AddComponent<Text>();
        btnText.text = "Send";
        btnText.font = GetFont();
        btnText.fontSize = 16;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;

        // ── ScrollView (中间区域) ──
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(panel.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.SetBetween(30, 50, 8, 8); // between status bar and input area
        scrollRT.anchoredPosition = new Vector2(0, 0);

        var scrollImg = scrollGO.AddComponent<Image>();
        scrollImg.color = new Color(0, 0, 0, 0.08f); // 极淡背景
        scrollGO.AddComponent<Mask>().showMaskGraphic = true;

        scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20;

        // Viewport
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        var viewportRT = viewport.AddComponent<RectTransform>();
        viewportRT.Stretch();
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewportRT;

        // Content
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewport.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        // ContentSizeFitter: 只控制高度，宽度由 viewport 决定
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        messageArea = contentGO.AddComponent<Text>();
        messageArea.font = GetFont();
        messageArea.fontSize = 16;
        messageArea.color = Color.white;
        messageArea.alignment = TextAnchor.UpperLeft;
        messageArea.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageArea.verticalOverflow = VerticalWrapMode.Overflow;
        messageArea.supportRichText = true;
        messageArea.raycastTarget = false;

        // 文字阴影：保证在透明背景上可读
        var msgShadow = contentGO.AddComponent<Shadow>();
        msgShadow.effectColor = new Color(0, 0, 0, 0.85f);
        msgShadow.effectDistance = new Vector2(1, -1);

        scrollRect.content = contentRT;

        // Vertical scrollbar
        var scrollbarGO = new GameObject("Scrollbar");
        scrollbarGO.transform.SetParent(scrollGO.transform, false);
        var scrollbarRT = scrollbarGO.AddComponent<RectTransform>();
        scrollbarRT.anchorMin = new Vector2(1, 0);
        scrollbarRT.anchorMax = new Vector2(1, 1);
        scrollbarRT.pivot = new Vector2(1, 1);
        scrollbarRT.sizeDelta = new Vector2(10, 0);
        var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var handle = new GameObject("Handle");
        handle.transform.SetParent(scrollbarGO.transform, false);
        handle.AddComponent<RectTransform>().Stretch();
        handle.AddComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        scrollbar.handleRect = handle.GetComponent<RectTransform>();
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Font GetFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public void SendMessage()
    {
        var text = inputField?.text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // 显示用户消息
        AppendMessage("<color=#88ccff><b>You:</b></color> " + text);

        // 发送
        _currentAssistantMessage = "";
        SetStatus("Thinking...");
        _client.SendMessage(text);

        // 清空输入
        inputField.text = "";
        inputField.ActivateInputField();
    }

    private void OnTextChunk(string chunk)
    {
        _currentAssistantMessage += chunk;
        // 实时更新（创建/替换最后一条 assistant 消息）
        UpdateAssistantMessage();
        AutoScroll();
    }

    private void OnToolCall(string toolInfo)
    {
        AppendMessage($"<color=#ffaa00><i>🔧 {toolInfo}</i></color>");
        AutoScroll();
    }

    private void OnComplete(string fullResponse)
    {
        SetStatus("Ready");
        _currentAssistantMessage = "";
        AutoScroll();
    }

    private void OnError(string error)
    {
        SetStatus($"Error: {error.Truncate(80)}");
        AppendMessage($"<color=#ff4444><b>Error:</b> {error}</color>");
        _currentAssistantMessage = "";
    }

    private void UpdateAssistantMessage()
    {
        // 简单实现：重新设置全部文本
        // 生产环境中应该用更高效的方式（如维护消息列表）
        var lines = messageArea.text;
        var lastNewline = lines.LastIndexOf('\n');
        var lastAssistantStart = lines.LastIndexOf("<color=#88ffcc>");

        if (lastAssistantStart >= 0)
        {
            messageArea.text = lines.Substring(0, lastAssistantStart) +
                $"<color=#88ffcc><b>AI:</b></color> {_currentAssistantMessage}";
        }
        else
        {
            messageArea.text = lines +
                $"\n<color=#88ffcc><b>AI:</b></color> {_currentAssistantMessage}";
        }
    }

    private void AppendMessage(string msg)
    {
        if (!string.IsNullOrEmpty(messageArea.text))
            messageArea.text += "\n";
        messageArea.text += msg;

        // 截断过长历史
        var lines = messageArea.text.Split('\n');
        if (lines.Length > maxMessages)
            messageArea.text = string.Join("\n", lines, lines.Length - maxMessages, maxMessages);
    }

    private void AutoScroll()
    {
        // 延迟一帧让 ContentSizeFitter 更新
        StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    private void SetStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    private void OnDestroy()
    {
        if (_client != null)
        {
            _client.OnTextChunk -= OnTextChunk;
            _client.OnToolCall -= OnToolCall;
            _client.OnComplete -= OnComplete;
            _client.OnError -= OnError;
        }
    }
}

/// <summary>RectTransform 布局快捷扩展</summary>
public static class RectTransformExtensions
{
    /// <summary>拉伸填满父容器</summary>
    public static RectTransform Stretch(this RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    /// <summary>顶部横条：固定高度，左右撑满</summary>
    public static RectTransform SetTopStretch(this RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    /// <summary>底部横条：固定高度，左右撑满</summary>
    public static RectTransform SetBottomStretch(this RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    /// <summary>填充两个固定条之间的区域</summary>
    public static RectTransform SetBetween(this RectTransform rt, float topHeight, float bottomHeight, float marginX, float marginY)
    {
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(marginX, bottomHeight + marginY);
        rt.offsetMax = new Vector2(-marginX, -topHeight - marginY);
        return rt;
    }
}
