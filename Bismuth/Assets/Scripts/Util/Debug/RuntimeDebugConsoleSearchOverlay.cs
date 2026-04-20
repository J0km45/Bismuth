using UnityEngine.TextCore.LowLevel;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuntimeDebugConsoleSearchOverlay : MonoBehaviour
{
    private const float FieldHeight = 24f;
    private const float FontSize = 16f;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private TMP_InputField _hierarchyInput;
    private RectTransform _hierarchyRectTransform;
    private TMP_FontAsset _dynamicFontAsset;

    public string HierarchyText => _hierarchyInput != null ? _hierarchyInput.text : string.Empty;
    public string LogText => string.Empty;
    public bool IsHierarchyFocused => _hierarchyInput != null && _hierarchyInput.isFocused;
    public bool IsLogFocused => false;

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

        _dynamicFontAsset = CreateDynamicTMPFontAsset();
        _hierarchyInput = CreateInputField("HierarchySearchInput", out _hierarchyRectTransform);

        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (_canvas == null)
            return;

        _canvas.enabled = visible;

        if (_hierarchyInput != null)
            _hierarchyInput.gameObject.SetActive(visible);
    }

    public void FocusHierarchy()
    {
        if (_hierarchyInput == null)
            return;

        _hierarchyInput.gameObject.SetActive(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(_hierarchyInput.gameObject);

        _hierarchyInput.Select();
        _hierarchyInput.ActivateInputField();
        _hierarchyInput.MoveTextEnd(false);
    }

    public void FocusLog()
    {
    }

    public void SetTexts(string hierarchyText, string logText)
    {
        SetText(hierarchyText);
    }

    public void SetText(string hierarchyText)
    {
        hierarchyText ??= string.Empty;

        if (_hierarchyInput != null && !_hierarchyInput.isFocused && _hierarchyInput.text != hierarchyText)
            _hierarchyInput.SetTextWithoutNotify(hierarchyText);
    }

    public void ClearTexts()
    {
        ClearText();
    }

    public void ClearText()
    {
        if (_hierarchyInput != null)
            _hierarchyInput.SetTextWithoutNotify(string.Empty);
    }

    public void SetHierarchyRect(Rect screenRect)
    {
        ApplyScreenRect(_hierarchyRectTransform, screenRect);
    }

    public void SetLogRect(Rect screenRect)
    {
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
        background.color = new Color(0.10f, 0.17f, 0.16f, 0.02f);
        background.raycastTarget = true;

        TMP_InputField inputField = root.GetComponent<TMP_InputField>();
        inputField.targetGraphic = background;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.richText = false;
        inputField.shouldHideMobileInput = false;
        inputField.resetOnDeActivation = false;
        inputField.restoreOriginalTextOnEscape = false;
        inputField.customCaretColor = true;
        inputField.caretColor = new Color(0.78f, 0.93f, 0.89f, 1f);
        inputField.selectionColor = new Color(0.20f, 0.36f, 0.33f, 0.85f);
        inputField.caretWidth = 2;

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(root.transform, false);

        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(8f, 3f);
        textAreaRect.offsetMax = new Vector2(-8f, -3f);

        TextMeshProUGUI placeholder = CreateTextChild(textArea.transform, "Placeholder", new Color(0.46f, 0.60f, 0.58f, 0.95f));
        placeholder.text = string.Empty;

        TextMeshProUGUI text = CreateTextChild(textArea.transform, "Text", new Color(0.82f, 0.95f, 0.91f, 1f));

        inputField.textViewport = textAreaRect;
        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        inputField.onSelect.AddListener(_ =>
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);

            inputField.ActivateInputField();
        });

        AddFocusTrigger(root, inputField);
        return inputField;
    }

    private void AddFocusTrigger(GameObject target, TMP_InputField inputField)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();
        trigger.triggers.Clear();

        AddTrigger(trigger, EventTriggerType.PointerDown, _ => FocusInput(inputField));
        AddTrigger(trigger, EventTriggerType.Select, _ => FocusInput(inputField));
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data => action(data));
        trigger.triggers.Add(entry);
    }

    private void FocusInput(TMP_InputField inputField)
    {
        if (inputField == null)
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);

        inputField.Select();
        inputField.ActivateInputField();
        inputField.MoveTextEnd(false);
    }

    private TextMeshProUGUI CreateTextChild(Transform parent, string objectName, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = _dynamicFontAsset;
        text.fontSize = FontSize;
        text.richText = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Masking;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = color;
        return text;
    }

    private TMP_FontAsset CreateDynamicTMPFontAsset()
    {
        string[] candidates =
        {
            "Malgun Gothic",
            "맑은 고딕",
            "Noto Sans CJK KR",
            "Arial Unicode MS",
            "Arial"
        };

        Font sourceFont = Font.CreateDynamicFontFromOSFont(candidates, Mathf.RoundToInt(FontSize));
        if (sourceFont == null)
            sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return TMP_FontAsset.CreateFontAsset(
            sourceFont,
            samplingPointSize: 90,
            atlasPadding: 9,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: 1024,
            atlasHeight: 1024,
            atlasPopulationMode: AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true);
    }

    private void ApplyScreenRect(RectTransform rectTransform, Rect screenRect)
    {
        if (rectTransform == null || screenRect.width <= 0f || screenRect.height <= 0f)
            return;

        rectTransform.anchoredPosition = new Vector2(screenRect.xMin, -screenRect.yMin);
        rectTransform.sizeDelta = new Vector2(screenRect.width, Mathf.Max(FieldHeight, screenRect.height));
    }
}
