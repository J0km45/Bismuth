// ------------------------------------------------------------------------------
// 런타임 검색 입력창을 오버레이 UI로 생성하고 관리하는 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Text = UnityEngine.UI.Text;

/// <summary>
/// 런타임 검색 입력 UI를 동적으로 구성하는 MonoBehaviour 클래스이다.
/// </summary>
public class RuntimeDebugConsoleSearchOverlay : MonoBehaviour
{
    // 입력 필드 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const float FieldHeight = 24f;
    // 폰트 크기 값을 저장한다. 텍스트 렌더링에 사용할 폰트 참조를 저장한다.
    private const int FontSize = 15;

    // 캔버스 값을 저장한다. 오버레이 UI를 구성하는 캔버스 참조를 저장한다.
    private Canvas _canvas;
    // 캔버스 영역 값을 저장한다. 오버레이 UI를 구성하는 캔버스 참조를 저장한다.
    private RectTransform _canvasRect;
    // 계층 입력 값을 저장한다. 입력 필드나 입력 상태 참조를 저장한다.
    private InputField _hierarchyInput;
    // 로그 입력 값을 저장한다. 입력 필드나 입력 상태 참조를 저장한다.
    private InputField _logInput;
    // 계층 영역 transform 값을 저장한다. 영역의 좌표와 크기를 저장한다.
    private RectTransform _hierarchyRectTransform;
    // 로그 영역 transform 값을 저장한다. 영역의 좌표와 크기를 저장한다.
    private RectTransform _logRectTransform;
    // dynamic 폰트 값을 저장한다. 텍스트 렌더링에 사용할 폰트 참조를 저장한다.
    private Font _dynamicFont;
    // 대기 계층 포커스 값을 저장한다. 지금 즉시 적용하지 않고 다음 단계에서 반영할 임시 상태를 저장한다.
    private bool _pendingHierarchyFocus;
    // 대기 로그 포커스 값을 저장한다. 지금 즉시 적용하지 않고 다음 단계에서 반영할 임시 상태를 저장한다.
    private bool _pendingLogFocus;

    /// <summary>
    /// 계층 텍스트 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public string HierarchyText => _hierarchyInput != null ? _hierarchyInput.text : string.Empty;
    /// <summary>
    /// 로그 텍스트 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public string LogText => _logInput != null ? _logInput.text : string.Empty;
    /// <summary>
    /// is 계층 focused 여부를 계산해 반환한다. UI 표시나 분기 조건에서 바로 사용할 수 있다.
    /// </summary>
    public bool IsHierarchyFocused => _hierarchyInput != null && _hierarchyInput.isFocused;
    /// <summary>
    /// is 로그 focused 여부를 계산해 반환한다. UI 표시나 분기 조건에서 바로 사용할 수 있다.
    /// </summary>
    public bool IsLogFocused => _logInput != null && _logInput.isFocused;

    /// <summary>
    /// initialize 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
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

    /// <summary>
    /// set 표시 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_canvas == null)
            return;

        _canvas.enabled = visible;

        if (!visible)
        {
            _pendingHierarchyFocus = false;
            _pendingLogFocus = false;

            if (_hierarchyInput != null)
                _hierarchyInput.gameObject.SetActive(false);

            if (_logInput != null)
                _logInput.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 계층를 현재 포커스 대상으로 설정한다. 관련 선택 상태도 함께 갱신한다.
    /// </summary>
    public void FocusHierarchy()
    {
        if (_hierarchyInput == null)
            return;

        EnsureEventSystemExists();
        _canvas.enabled = true;
        _hierarchyInput.gameObject.SetActive(true);

        if (_logInput != null)
            _logInput.gameObject.SetActive(false);

        _pendingHierarchyFocus = true;
        _pendingLogFocus = false;
    }

    /// <summary>
    /// 로그를 현재 포커스 대상으로 설정한다. 관련 선택 상태도 함께 갱신한다.
    /// </summary>
    public void FocusLog()
    {
        if (_logInput == null)
            return;

        EnsureEventSystemExists();
        _canvas.enabled = true;
        _logInput.gameObject.SetActive(true);

        if (_hierarchyInput != null)
            _hierarchyInput.gameObject.SetActive(false);

        _pendingLogFocus = true;
        _pendingHierarchyFocus = false;
    }

    /// <summary>
    /// set 텍스트 목록 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetTexts(string hierarchyText, string logText)
    {
        hierarchyText ??= string.Empty;
        logText ??= string.Empty;

        if (_hierarchyInput != null && !_hierarchyInput.isFocused && !_pendingHierarchyFocus && _hierarchyInput.text != hierarchyText)
            SetInputText(_hierarchyInput, hierarchyText);

        if (_logInput != null && !_logInput.isFocused && !_pendingLogFocus && _logInput.text != logText)
            SetInputText(_logInput, logText);
    }

    /// <summary>
    /// 텍스트 목록를 비우거나 초기화한다. 이전 상태를 제거하고 다음 작업을 준비하는 데 사용한다.
    /// </summary>
    public void ClearTexts()
    {
        if (_hierarchyInput != null)
            SetInputText(_hierarchyInput, string.Empty);

        if (_logInput != null)
            SetInputText(_logInput, string.Empty);
    }

    /// <summary>
    /// set 계층 영역 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetHierarchyRect(Rect screenRect)
    {
        ApplyScreenRect(_hierarchyRectTransform, screenRect);
    }

    /// <summary>
    /// set 로그 영역 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetLogRect(Rect screenRect)
    {
        ApplyScreenRect(_logRectTransform, screenRect);
    }

    /// <summary>
    /// 입력 입력 필드 인스턴스나 데이터를 생성한다. 필요한 기본값을 함께 채워 즉시 사용할 수 있게 만든다.
    /// </summary>
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
        background.color = new Color(0.96f, 0.97f, 0.99f, 0.92f);
        background.raycastTarget = true;

        InputField inputField = root.GetComponent<InputField>();
        inputField.targetGraphic = background;
        inputField.lineType = InputField.LineType.SingleLine;
        inputField.contentType = InputField.ContentType.Standard;
        inputField.shouldHideMobileInput = false;
        inputField.caretWidth = 2;
        inputField.customCaretColor = true;
        inputField.caretColor = Color.black;
        inputField.selectionColor = new Color(0.48f, 0.70f, 1.00f, 0.85f);

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(root.transform, false);

        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(8f, 3f);
        textAreaRect.offsetMax = new Vector2(-8f, -3f);

        Text placeholder = CreateTextChild(textArea.transform, "Placeholder", new Color(0.38f, 0.46f, 0.48f, 0.95f));
        placeholder.text = string.Empty;

        Text text = CreateTextChild(textArea.transform, "Text", Color.white);

        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        ApplyInputVisuals(inputField);

        return inputField;
    }


    /// <summary>
    /// set 입력 텍스트 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetInputText(InputField inputField, string value)
    {
        if (inputField == null)
            return;

        inputField.text = value ?? string.Empty;
        ApplyInputVisuals(inputField);
        inputField.ForceLabelUpdate();
    }

    /// <summary>
    /// 프레임 마지막 단계에서 호출되며, 앞선 갱신 결과를 바탕으로 보정 작업을 수행한다.
    /// </summary>
    private void LateUpdate()
    {
        if (_pendingHierarchyFocus)
        {
            _pendingHierarchyFocus = false;
            ActivateInput(_hierarchyInput);
        }

        if (_pendingLogFocus)
        {
            _pendingLogFocus = false;
            ActivateInput(_logInput);
        }

        SafeClampSelection(_hierarchyInput);
        SafeClampSelection(_logInput);

        ApplyInputVisuals(_hierarchyInput);
        ApplyInputVisuals(_logInput);
    }

    /// <summary>
    /// activate 입력 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void ActivateInput(InputField inputField)
    {
        if (inputField == null)
            return;

        inputField.gameObject.SetActive(true);
        inputField.Select();
        inputField.ActivateInputField();

        string currentText = inputField.text ?? string.Empty;
        int safeLength = currentText.Length;
        inputField.caretPosition = safeLength;
        inputField.selectionAnchorPosition = safeLength;
        inputField.selectionFocusPosition = safeLength;
        inputField.ForceLabelUpdate();
    }


    /// <summary>
    /// safe clamp 선택 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SafeClampSelection(InputField inputField)
    {
        if (inputField == null || !inputField.gameObject.activeSelf)
            return;

        string currentText = inputField.text ?? string.Empty;
        int safeLength = currentText.Length;

        if (inputField.caretPosition > safeLength)
            inputField.caretPosition = safeLength;

        if (inputField.selectionAnchorPosition > safeLength)
            inputField.selectionAnchorPosition = safeLength;

        if (inputField.selectionFocusPosition > safeLength)
            inputField.selectionFocusPosition = safeLength;
    }

    /// <summary>
    /// 준비된 입력 visuals 값을 실제 상태에 반영한다.
    /// </summary>
    private void ApplyInputVisuals(InputField inputField)
    {
        if (inputField == null)
            return;

        if (inputField.textComponent != null)
        {
            inputField.textComponent.color = new Color(0.05f, 0.05f, 0.05f, 1f);
            inputField.textComponent.fontStyle = FontStyle.Bold;
            inputField.textComponent.fontSize = FontSize;
            inputField.textComponent.material = null;
        }

        Text placeholderText = inputField.placeholder as Text;
        if (placeholderText != null)
        {
            placeholderText.color = new Color(0.42f, 0.45f, 0.50f, 0.95f);
            placeholderText.fontStyle = FontStyle.Normal;
            placeholderText.fontSize = FontSize;
            placeholderText.material = null;
        }

        inputField.caretColor = Color.black;
    }
    /// <summary>
    /// 텍스트 하위 인스턴스나 데이터를 생성한다. 필요한 기본값을 함께 채워 즉시 사용할 수 있게 만든다.
    /// </summary>
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

    /// <summary>
    /// dynamic ui 폰트 인스턴스나 데이터를 생성한다. 필요한 기본값을 함께 채워 즉시 사용할 수 있게 만든다.
    /// </summary>
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

    /// <summary>
    /// 준비된 screen 영역 값을 실제 상태에 반영한다.
    /// </summary>
    private void ApplyScreenRect(RectTransform rectTransform, Rect screenRect)
    {
        if (_canvasRect == null || rectTransform == null || screenRect.width <= 0f || screenRect.height <= 0f)
            return;

        Vector2 localTopLeft;
        Vector2 screenTopLeft = new Vector2(screenRect.xMin, screenRect.yMin);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenTopLeft, null, out localTopLeft))
            return;

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = localTopLeft;
        rectTransform.sizeDelta = new Vector2(screenRect.width, Mathf.Max(FieldHeight, screenRect.height));
    }

    /// <summary>
    /// event system exists가 준비되어 있는지 확인하고, 없으면 생성하거나 복구한다.
    /// </summary>
    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }
}
