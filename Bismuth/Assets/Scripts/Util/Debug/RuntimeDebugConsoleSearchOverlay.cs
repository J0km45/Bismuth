using UnityEngine.TextCore.LowLevel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class RuntimeDebugConsoleSearchOverlay : MonoBehaviour
{
    private const float FieldHeight = 24f;
    private const float FontSize = 16f;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private TMP_InputField _hierarchyInput;
    private TMP_InputField _logInput;
    private RectTransform _hierarchyRectTransform;
    private RectTransform _logRectTransform;
    private TMP_FontAsset _dynamicFontAsset;
    private Image _inputBlocker;
    private IMECompositionMode _previousImeCompositionMode = IMECompositionMode.Auto;
    private bool _imeCompositionCaptured;
    private EventSystem _fallbackEventSystem;
    private bool _ownsFallbackEventSystem;

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

        _inputBlocker = CreateInputBlocker();

        _dynamicFontAsset = CreateDynamicTMPFontAsset();

        _hierarchyInput = CreateInputField("HierarchySearchInput", out _hierarchyRectTransform);
        _logInput = CreateInputField("LogSearchInput", out _logRectTransform);

        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (_canvas == null)
            return;

        if (visible)
            EnsureEventSystem();

        _canvas.enabled = visible;

        if (_hierarchyInput != null)
            _hierarchyInput.gameObject.SetActive(visible);

        if (_logInput != null)
            _logInput.gameObject.SetActive(visible);

        if (_ownsFallbackEventSystem && _fallbackEventSystem != null)
            _fallbackEventSystem.gameObject.SetActive(visible);

        if (!visible)
            RestoreImeCompositionMode();
    }

    public void SetInputBlockerVisible(bool visible)
    {
        if (_inputBlocker == null)
            return;

        _inputBlocker.gameObject.SetActive(visible);
        _inputBlocker.raycastTarget = visible;
    }


    public void FocusHierarchy()
    {
        FocusInput(_hierarchyInput);
    }

    public void FocusLog()
    {
        FocusInput(_logInput);
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

    private void LateUpdate()
    {
        TMP_InputField focusedInput = GetFocusedInput();
        if (focusedInput == null)
        {
            RestoreImeCompositionMode();
            return;
        }

        if (!_imeCompositionCaptured)
        {
            _previousImeCompositionMode = Input.imeCompositionMode;
            _imeCompositionCaptured = true;
        }

        Input.imeCompositionMode = IMECompositionMode.On;
        Input.compositionCursorPos = GetCompositionCursorPosition(focusedInput);
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
        background.color = new Color(0f, 0f, 0f, 0.01f);
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

        inputField.onSelect.AddListener(_ => FocusInput(inputField));
        inputField.onEndEdit.AddListener(_ => RestoreImeCompositionMode());

        AddPointerFocusTrigger(root, inputField);

        return inputField;
    }

    private Image CreateInputBlocker()
    {
        GameObject blockerObject = new GameObject("InputBlocker", typeof(RectTransform), typeof(Image));
        blockerObject.transform.SetParent(transform, false);
        blockerObject.transform.SetAsFirstSibling();

        RectTransform blockerRect = blockerObject.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        Image blocker = blockerObject.GetComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.001f);
        blocker.raycastTarget = false;
        blockerObject.SetActive(false);
        return blocker;
    }

    private void AddPointerFocusTrigger(GameObject targetObject, TMP_InputField inputField)
    {
        EventTrigger eventTrigger = targetObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
            eventTrigger = targetObject.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerDown
        };
        pointerDownEntry.callback.AddListener(_ => FocusInput(inputField));
        eventTrigger.triggers.Add(pointerDownEntry);

        EventTrigger.Entry selectEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Select
        };
        selectEntry.callback.AddListener(_ => FocusInput(inputField));
        eventTrigger.triggers.Add(selectEntry);
    }

    private void FocusInput(TMP_InputField inputField)
    {
        if (inputField == null)
            return;

        EnsureEventSystem();

        inputField.gameObject.SetActive(true);

        EventSystem activeEventSystem = EventSystem.current != null ? EventSystem.current : _fallbackEventSystem;
        if (activeEventSystem != null)
            activeEventSystem.SetSelectedGameObject(inputField.gameObject);

        inputField.Select();
        inputField.ActivateInputField();
        inputField.MoveTextEnd(false);
    }



    private void EnsureEventSystem()
    {
        EventSystem activeEventSystem = EventSystem.current;
        if (activeEventSystem != null)
        {
            if (_ownsFallbackEventSystem && _fallbackEventSystem != null && activeEventSystem != _fallbackEventSystem)
            {
                Destroy(_fallbackEventSystem.gameObject);
                _fallbackEventSystem = null;
                _ownsFallbackEventSystem = false;
            }

            return;
        }

        if (_fallbackEventSystem != null)
        {
            if (!_fallbackEventSystem.gameObject.activeSelf)
                _fallbackEventSystem.gameObject.SetActive(true);

            return;
        }

        EventSystem existingEventSystem = FindAnyObjectByType<EventSystem>();
        if (existingEventSystem != null)
        {
            if (!existingEventSystem.gameObject.activeSelf)
                existingEventSystem.gameObject.SetActive(true);

            return;
        }

        GameObject eventSystemObject = new GameObject("RuntimeDebugConsoleEventSystem", typeof(EventSystem));
        eventSystemObject.transform.SetParent(transform, false);

#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif

        _fallbackEventSystem = eventSystemObject.GetComponent<EventSystem>();
        _ownsFallbackEventSystem = true;
    }

    private TMP_InputField GetFocusedInput()
    {
        if (_hierarchyInput != null && _hierarchyInput.isFocused)
            return _hierarchyInput;

        if (_logInput != null && _logInput.isFocused)
            return _logInput;

        return null;
    }

    private Vector2 GetCompositionCursorPosition(TMP_InputField inputField)
    {
        RectTransform targetRect = inputField == _hierarchyInput ? _hierarchyRectTransform : _logRectTransform;
        if (targetRect == null)
            return new Vector2(16f, 16f);

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(null, corners[1]);
        return new Vector2(topLeft.x + 12f, Screen.height - topLeft.y + 16f);
    }

    private void RestoreImeCompositionMode()
    {
        if (!_imeCompositionCaptured)
            return;

        Input.imeCompositionMode = _previousImeCompositionMode;
        _imeCompositionCaptured = false;
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

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            samplingPointSize: 90,
            atlasPadding: 9,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: 1024,
            atlasHeight: 1024,
            atlasPopulationMode: AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true);

        return fontAsset;
    }

    private void ApplyScreenRect(RectTransform rectTransform, Rect screenRect)
    {
        if (rectTransform == null || screenRect.width <= 0f || screenRect.height <= 0f)
            return;

        rectTransform.anchoredPosition = new Vector2(screenRect.xMin, -screenRect.yMin);
        rectTransform.sizeDelta = new Vector2(screenRect.width, Mathf.Max(FieldHeight, screenRect.height));
    }

    private void OnDisable()
    {
        RestoreImeCompositionMode();

        if (_ownsFallbackEventSystem && _fallbackEventSystem != null)
        {
            Destroy(_fallbackEventSystem.gameObject);
            _fallbackEventSystem = null;
            _ownsFallbackEventSystem = false;
        }
    }
}
