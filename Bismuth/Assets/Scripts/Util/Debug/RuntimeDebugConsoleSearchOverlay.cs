using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeDebugConsoleSearchOverlay : MonoBehaviour
{
    private const float FieldHeight = 24f;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private TMP_InputField _hierarchyInput;
    private TMP_InputField _logInput;
    private RectTransform _hierarchyRectTransform;
    private RectTransform _logRectTransform;

    private string _lastHierarchyText = string.Empty;
    private string _lastLogText = string.Empty;

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
        if (_hierarchyInput != null && !_hierarchyInput.isFocused && _hierarchyInput.text != hierarchyText)
            _hierarchyInput.SetTextWithoutNotify(hierarchyText ?? string.Empty);

        if (_logInput != null && !_logInput.isFocused && _logInput.text != logText)
            _logInput.SetTextWithoutNotify(logText ?? string.Empty);

        _lastHierarchyText = hierarchyText ?? string.Empty;
        _lastLogText = logText ?? string.Empty;
    }

    public void ClearTexts()
    {
        _lastHierarchyText = string.Empty;
        _lastLogText = string.Empty;

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

    private TMP_InputField CreateInputField(string objectName, out RectTransform rootRect)
    {
        GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        root.transform.SetParent(transform, false);

        rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.sizeDelta = new Vector2(240f, FieldHeight);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.10f, 0.17f, 0.16f, 0.96f);
        background.raycastTarget = true;

        TMP_InputField inputField = root.GetComponent<TMP_InputField>();
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.richText = false;
        inputField.caretWidth = 2;
        inputField.caretColor = new Color(0.78f, 0.93f, 0.89f, 1f);
        inputField.selectionColor = new Color(0.20f, 0.36f, 0.33f, 0.85f);
        inputField.customCaretColor = true;
        inputField.resetOnDeActivation = false;
        inputField.restoreOriginalTextOnEscape = false;

        TextMeshProUGUI text = CreateTextChild(root.transform, "Text", new Color(0.82f, 0.95f, 0.91f, 1f));
        TextMeshProUGUI placeholder = CreateTextChild(root.transform, "Placeholder", new Color(0.46f, 0.60f, 0.58f, 0.95f));
        placeholder.text = string.Empty;

        inputField.textViewport = text.rectTransform.parent as RectTransform;
        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        inputField.onValueChanged.AddListener(_ => CacheTexts());
        inputField.onSelect.AddListener(_ => CacheTexts());
        inputField.onDeselect.AddListener(_ => CacheTexts());

        return inputField;
    }

    private TextMeshProUGUI CreateTextChild(Transform parent, string objectName, Color color)
    {
        GameObject viewport = parent.Find("Viewport")?.gameObject;

        if (viewport == null)
        {
            viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(parent, false);

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 3f);
            viewportRect.offsetMax = new Vector2(-8f, -3f);
        }

        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(viewport.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = 19f;
        tmp.enableWordWrapping = false;
        tmp.richText = false;
        tmp.margin = Vector4.zero;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.extraPadding = false;

        return tmp;
    }

    private void CacheTexts()
    {
        _lastHierarchyText = _hierarchyInput != null ? _hierarchyInput.text : string.Empty;
        _lastLogText = _logInput != null ? _logInput.text : string.Empty;
    }

    private void ApplyScreenRect(RectTransform rectTransform, Rect screenRect)
    {
        if (rectTransform == null || screenRect.width <= 0f || screenRect.height <= 0f)
            return;

        rectTransform.anchoredPosition = new Vector2(screenRect.xMin, -screenRect.yMin);
        rectTransform.sizeDelta = new Vector2(screenRect.width, Mathf.Max(FieldHeight, screenRect.height));
    }
}
