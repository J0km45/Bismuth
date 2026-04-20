
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Text = UnityEngine.UI.Text;

public class RuntimeDebugConsoleSearchOverlay : MonoBehaviour
{
    private const float FieldHeight = 24f;
    private const int FontSize = 14;

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

        _dynamicFont = CreateDynamicUiFont();

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

    public void FocusHierarchy()
    {
        if (_hierarchyInput == null)
            return;

        EnsureEventSystemExists();
        _hierarchyInput.gameObject.SetActive(true);
        _hierarchyInput.Select();
        _hierarchyInput.ActivateInputField();
        _hierarchyInput.MoveTextEnd(false);
    }

    public void FocusLog()
    {
        if (_logInput == null)
            return;

        EnsureEventSystemExists();
        _logInput.gameObject.SetActive(true);
        _logInput.Select();
        _logInput.ActivateInputField();
        _logInput.MoveTextEnd(false);
    }

    public void SetTexts(string hierarchyText, string logText)
    {
        hierarchyText ??= string.Empty;
        logText ??= string.Empty;

        if (_hierarchyInput != null && !_hierarchyInput.isFocused && _hierarchyInput.text != hierarchyText)
            SetInputText(_hierarchyInput, hierarchyText);

        if (_logInput != null && !_logInput.isFocused && _logInput.text != logText)
            SetInputText(_logInput, logText);
    }

    public void ClearTexts()
    {
        if (_hierarchyInput != null)
            SetInputText(_hierarchyInput, string.Empty);

        if (_logInput != null)
            SetInputText(_logInput, string.Empty);
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
        background.color = new Color(0.10f, 0.17f, 0.16f, 0.01f);
        background.raycastTarget = true;

        InputField inputField = root.GetComponent<InputField>();
        inputField.targetGraphic = background;
        inputField.lineType = InputField.LineType.SingleLine;
        inputField.contentType = InputField.ContentType.Standard;
        inputField.shouldHideMobileInput = false;
        inputField.caretWidth = 2;
        inputField.customCaretColor = true;
        inputField.caretColor = new Color(0.78f, 0.93f, 0.89f, 1f);
        inputField.selectionColor = new Color(0.20f, 0.36f, 0.33f, 0.85f);

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(root.transform, false);

        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(8f, 3f);
        textAreaRect.offsetMax = new Vector2(-8f, -3f);

        Text placeholder = CreateTextChild(textArea.transform, "Placeholder", new Color(0.46f, 0.60f, 0.58f, 0.95f));
        placeholder.text = string.Empty;

        Text text = CreateTextChild(textArea.transform, "Text", new Color(0.82f, 0.95f, 0.91f, 1f));

        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        inputField.onSelect.AddListener(_ => inputField.ActivateInputField());

        return inputField;
    }


    private void SetInputText(InputField inputField, string value)
    {
        if (inputField == null)
            return;

        inputField.text = value ?? string.Empty;
        inputField.ForceLabelUpdate();
    }
    private Text CreateTextChild(Transform parent, string objectName, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = _dynamicFont;
        text.fontSize = FontSize;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        return text;
    }

    private Font CreateDynamicUiFont()
    {
        string[] candidates =
        {
            "Arial Unicode MS",
            "Segoe UI",
            "Malgun Gothic",
            "맑은 고딕",
            "Arial"
        };

        Font sourceFont = Font.CreateDynamicFontFromOSFont(candidates, FontSize);
        if (sourceFont == null)
            sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return sourceFont;
    }

    private void ApplyScreenRect(RectTransform rectTransform, Rect screenRect)
    {
        if (rectTransform == null || screenRect.width <= 0f || screenRect.height <= 0f)
            return;

        rectTransform.anchoredPosition = new Vector2(screenRect.xMin, -screenRect.yMin);
        rectTransform.sizeDelta = new Vector2(screenRect.width, Mathf.Max(FieldHeight, screenRect.height));
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }
}
