using UnityEngine;
using UnityEngine.UI;

public class RuntimeDebugConsoleSearchOverlay : MonoBehaviour
{
    private const float FieldHeight = 24f;
    private const int FontSize = 16;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private InputField _hierarchyInput;
    private InputField _logInput;
    private RectTransform _hierarchyRectTransform;
    private RectTransform _logRectTransform;
    private Font _dynamicFont;

    public string HierarchyText => _hierarchyInput != null ? _hierarchyInput.text : string.Empty;
    public string LogText => _logInput != null ? _logInput.text : string.Empty;
    public bool IsHierarchyFocused => _hierarchyInput != null && _hierarchyInput.isFocused;
    public bool IsLogFocused => _logInput != null && _logInput.isFocused;

    public void Initialize()
    {
        if (_canvas != null)
            return;

        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = short.MaxValue - 10;

        gameObject.AddComponent<GraphicRaycaster>();

        _canvasRect = _canvas.GetComponent<RectTransform>();
        _canvasRect.anchorMin = Vector2.zero;
        _canvasRect.anchorMax = Vector2.one;
        _canvasRect.offsetMin = Vector2.zero;
        _canvasRect.offsetMax = Vector2.zero;

        _dynamicFont = CreateDynamicFont();

        _hierarchyInput = CreateInputField("HierarchySearchInput", out _hierarchyRectTransform);
        _logInput = CreateInputField("LogSearchInput", out _logRectTransform);

        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (_canvas == null)
            return;

        _canvas.enabled = visible;

        if (_hierarchyInput != null)
            _hierarchyInput.gameObject.SetActive(visible);

        if (_logInput != null)
            _logInput.gameObject.SetActive(visible);
    }

    public void SetTexts(string hierarchyText, string logText)
    {
        hierarchyText ??= string.Empty;
        logText ??= string.Empty;

        if (_hierarchyInput != null && !_hierarchyInput.isFocused && _hierarchyInput.text != hierarchyText)
            _hierarchyInput.SetTextWithoutNotify(hierarchyText);

        if (_logInput != null && !_logInput.isFocused && _logInput.text != logText)
            _logInput.SetTextWithoutNotify(logText);
    }

    public void ClearTexts()
    {
        if (_hierarchyInput != null)
            _hierarchyInput.SetTextWithoutNotify(string.Empty);

        if (_logInput != null)
            _logInput.SetTextWithoutNotify(string.Empty);
    }

    public void SetHierarchyRect(Rect screenRect)
    {
        ApplyScreenRect(_hierarchyRectTransform, screenRect);
    }

    public void SetLogRect(Rect screenRect)
    {
        ApplyScreenRect(_logRectTransform, screenRect);
    }

    private InputField CreateInputField(string objectName, out RectTransform rootRect)
    {
        GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(InputField));
        root.transform.SetParent(transform, false);

        rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.sizeDelta = new Vector2(240f, FieldHeight);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.10f, 0.17f, 0.16f, 0.02f);
        background.raycastTarget = true;

        InputField inputField = root.GetComponent<InputField>();
        inputField.lineType = InputField.LineType.SingleLine;
        inputField.contentType = InputField.ContentType.Standard;
        inputField.shouldHideMobileInput = false;
        inputField.caretWidth = 2;
        inputField.caretColor = new Color(0.78f, 0.93f, 0.89f, 1f);
        inputField.selectionColor = new Color(0.20f, 0.36f, 0.33f, 0.85f);

        RectTransform viewportRect;
        Text text = CreateTextChild(root.transform, "Text", out viewportRect, new Color(0.82f, 0.95f, 0.91f, 1f));
        Text placeholder = CreateTextChild(root.transform, "Placeholder", out _, new Color(0.46f, 0.60f, 0.58f, 0.95f));
        placeholder.text = string.Empty;

        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        inputField.textViewport = viewportRect;

        return inputField;
    }

    private Text CreateTextChild(Transform parent, string objectName, out RectTransform viewportRect, Color color)
    {
        Transform viewport = parent.Find("Viewport");
        if (viewport == null)
        {
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(parent, false);
            viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 3f);
            viewportRect.offsetMax = new Vector2(-8f, -3f);
            viewport = viewportObject.transform;
        }
        else
        {
            viewportRect = viewport.GetComponent<RectTransform>();
        }

        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(viewport, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = _dynamicFont;
        text.fontSize = FontSize;
        text.supportRichText = false;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.color = color;

        return text;
    }

    private Font CreateDynamicFont()
    {
        string[] candidates =
        {
            "Malgun Gothic",
            "맑은 고딕",
            "Noto Sans CJK KR",
            "Arial Unicode MS",
            "Arial"
        };

        Font font = Font.CreateDynamicFontFromOSFont(candidates, FontSize);
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return font;
    }

    private void ApplyScreenRect(RectTransform rectTransform, Rect screenRect)
    {
        if (rectTransform == null || screenRect.width <= 0f || screenRect.height <= 0f)
            return;

        rectTransform.anchoredPosition = new Vector2(screenRect.xMin, -screenRect.yMin);
        rectTransform.sizeDelta = new Vector2(screenRect.width, Mathf.Max(FieldHeight, screenRect.height));
    }
}
