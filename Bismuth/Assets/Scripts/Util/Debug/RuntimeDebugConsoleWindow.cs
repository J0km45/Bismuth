using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RuntimeDebugConsoleWindow : MonoBehaviour
{
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
    [SerializeField] private bool _visible;
    [SerializeField] private bool _autoScroll = true;
    [SerializeField] private bool _hideTransform = true;
    [SerializeField] private bool _collapsePreviousOnSelection = true;
    [SerializeField] private bool _blockInput = true;

    private const int SortOrder = 32000;
    private const float RebuildInterval = 0.25f;
    private const string FontFallbackName = "Arial";

    public static bool IsRuntimeConsoleVisible { get; private set; }
    public static bool ShouldBlockGameplayInput => Instance != null && Instance._visible && Instance._blockInput;
    public static RuntimeDebugConsoleWindow Instance { get; private set; }

    private Canvas _canvas;
    private CanvasScaler _canvasScaler;
    private GraphicRaycaster _graphicRaycaster;
    private CanvasGroup _canvasGroup;
    private Image _screenBlocker;
    private RectTransform _windowRoot;

    private Toggle _globalToggle;
    private Toggle _mirrorToggle;
    private Toggle _autoScrollToggle;
    private Toggle _hideTransformToggle;
    private Toggle _collapsePrevToggle;
    private Toggle _blockInputToggle;

    private Button _typeFilterToggleButton;
    private RectTransform _typeFilterPanel;
    private TextMeshProUGUI _typeFilterToggleLabel;

    private TMP_InputField _hierarchySearchInput;
    private TMP_InputField _logSearchInput;

    private ScrollRect _hierarchyScrollRect;
    private RectTransform _hierarchyContent;
    private ScrollRect _logScrollRect;
    private RectTransform _logContent;

    private TextMeshProUGUI _focusLabel;
    private TextMeshProUGUI _countLabel;
    private TextMeshProUGUI _titleLabel;
    private Button _clearSearchButton;

    private TMP_FontAsset _runtimeFontAsset;
    private bool _showTypeFilterPanel = true;
    private bool _pendingAutoScroll;
    private float _nextRebuildTime;
    private int _lastManagerVersion = -1;
    private string _hierarchySearch = string.Empty;
    private string _logSearch = string.Empty;

    private readonly DebugConsoleFocusState _focusState = new();
    private readonly Dictionary<int, bool> _expandedObjectState = new();
    private readonly Dictionary<DebugType, Toggle> _typeToggleMap = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        BuildRuntimeUI();
        SetVisible(_visible);
        EnsureEventSystemExists();

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            SetVisible(!_visible);

        if (!_visible)
            return;

        EnsureEventSystemExists();

        DebugConsoleManager manager = DebugConsoleManager.Instance;
        if (manager == null)
            return;

        bool needsRebuild = false;

        if (_lastManagerVersion != manager.ChangeVersion)
        {
            _lastManagerVersion = manager.ChangeVersion;
            SyncManagerStateToUI(manager);
            needsRebuild = true;
            _pendingAutoScroll = _autoScroll;
        }

        if (Time.unscaledTime >= _nextRebuildTime)
        {
            needsRebuild = true;
            _nextRebuildTime = Time.unscaledTime + RebuildInterval;
        }

        if (needsRebuild)
            RebuildUIContents(manager);
    }

    private void LateUpdate()
    {
        if (!_visible || !_pendingAutoScroll || _logScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        _logScrollRect.verticalNormalizedPosition = 0f;
        _pendingAutoScroll = false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _expandedObjectState.Clear();
        _focusState.Clear();
        _nextRebuildTime = 0f;
        _lastManagerVersion = -1;
    }

    private void BuildRuntimeUI()
    {
        _runtimeFontAsset = CreateDynamicFontAsset();

        GameObject canvasObject = new GameObject("RuntimeDebugConsoleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        _canvas = canvasObject.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = SortOrder;

        _canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        _canvasScaler.matchWidthOrHeight = 0.5f;

        _graphicRaycaster = canvasObject.GetComponent<GraphicRaycaster>();
        _canvasGroup = canvasObject.GetComponent<CanvasGroup>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        _screenBlocker = CreateImage("ScreenBlocker", canvasRect, new Color(0f, 0f, 0f, 0.28f));
        RectTransform blockerRect = _screenBlocker.rectTransform;
        Stretch(blockerRect, 0f, 0f, 0f, 0f);

        Image windowBackground = CreateImage("WindowRoot", canvasRect, new Color(0.16f, 0.21f, 0.30f, 0.98f));
        _windowRoot = windowBackground.rectTransform;
        Stretch(_windowRoot, 40f, -40f, 40f, -40f);
        SetImageBorder(windowBackground, new Color(0.74f, 0.76f, 0.81f, 1f));

        VerticalLayoutGroup rootLayout = _windowRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(12, 12, 10, 10);
        rootLayout.spacing = 8f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        ContentSizeFitter rootFitter = _windowRoot.gameObject.AddComponent<ContentSizeFitter>();
        rootFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        _titleLabel = CreateText("Title", _windowRoot, "Runtime Debug Console", 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        LayoutElement titleLayout = _titleLabel.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 34f;

        RectTransform toggleRow = CreateHorizontalRow("ToggleRow", _windowRoot, 8f, 34f);
        _globalToggle = CreateHeaderToggle(toggleRow, "Global", value =>
        {
            DebugConsoleManager manager = DebugConsoleManager.Instance;
            if (manager == null)
                return;

            manager.GlobalEnabled = value;
            RequestRebuild();
        });

        _mirrorToggle = CreateHeaderToggle(toggleRow, "Mirror Unity", value =>
        {
            DebugConsoleManager manager = DebugConsoleManager.Instance;
            if (manager == null)
                return;

            manager.MirrorToUnityConsole = value;
            RequestRebuild();
        });

        _autoScrollToggle = CreateHeaderToggle(toggleRow, "Auto Scroll", value => _autoScroll = value);
        _hideTransformToggle = CreateHeaderToggle(toggleRow, "Hide Transform", value =>
        {
            _hideTransform = value;
            RequestRebuild();
        });

        _collapsePrevToggle = CreateHeaderToggle(toggleRow, "Collapse Prev", value => _collapsePreviousOnSelection = value);
        _blockInputToggle = CreateHeaderToggle(toggleRow, "Block Input", value => ApplyBlockInput(value));

        RectTransform buttonRow = CreateHorizontalRow("ButtonRow", _windowRoot, 8f, 36f);
        _typeFilterToggleButton = CreateButton(buttonRow, "Type Filter ▼", ToggleTypeFilterPanel, out _typeFilterToggleLabel);
        CreateButton(buttonRow, "All Types On", () =>
        {
            DebugConsoleManager manager = DebugConsoleManager.Instance;
            if (manager == null)
                return;

            manager.SetAllTypes(true);
            SyncManagerStateToUI(manager);
            RequestRebuild();
        });

        CreateButton(buttonRow, "All Types Off", () =>
        {
            DebugConsoleManager manager = DebugConsoleManager.Instance;
            if (manager == null)
                return;

            manager.SetAllTypes(false);
            SyncManagerStateToUI(manager);
            RequestRebuild();
        });

        CreateButton(buttonRow, "Clear Logs", () =>
        {
            DebugConsoleManager manager = DebugConsoleManager.Instance;
            if (manager == null)
                return;

            manager.ClearLogs();
            RequestRebuild();
        });

        CreateButton(buttonRow, "Clear Focus", () =>
        {
            _focusState.Clear();
            RequestRebuild();
        });

        RectTransform searchRow = CreateHorizontalRow("SearchRow", _windowRoot, 10f, 34f);
        CreateLabeledInput(searchRow, "Hierarchy Search", out _hierarchySearchInput, value =>
        {
            _hierarchySearch = value ?? string.Empty;
            RequestRebuild();
        }, 0.33f);

        CreateLabeledInput(searchRow, "Log Search", out _logSearchInput, value =>
        {
            _logSearch = value ?? string.Empty;
            RequestRebuild();
        }, 0.55f);

        _clearSearchButton = CreateButton(searchRow, "Clear Search", () =>
        {
            _hierarchySearch = string.Empty;
            _logSearch = string.Empty;
            _hierarchySearchInput.SetTextWithoutNotify(string.Empty);
            _logSearchInput.SetTextWithoutNotify(string.Empty);
            RequestRebuild();
        }, 150f);

        _typeFilterPanel = CreatePanel("TypeFilterPanel", _windowRoot, new Color(0.10f, 0.14f, 0.18f, 0.75f));
        GridLayoutGroup gridLayout = _typeFilterPanel.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(240f, 30f);
        gridLayout.spacing = new Vector2(8f, 8f);
        gridLayout.padding = new RectOffset(12, 12, 12, 12);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        LayoutElement typePanelLayout = _typeFilterPanel.gameObject.AddComponent<LayoutElement>();
        typePanelLayout.preferredHeight = 108f;

        CreateTypeToggles(_typeFilterPanel);

        RectTransform contentRow = CreateHorizontalRow("ContentRow", _windowRoot, 10f, -1f);
        LayoutElement contentLayout = contentRow.gameObject.AddComponent<LayoutElement>();
        contentLayout.flexibleHeight = 1f;
        contentLayout.minHeight = 300f;

        RectTransform hierarchyPanel = CreatePanel("HierarchyPanel", contentRow, new Color(0.10f, 0.14f, 0.18f, 0.78f));
        LayoutElement hierarchyLayout = hierarchyPanel.gameObject.AddComponent<LayoutElement>();
        hierarchyLayout.flexibleWidth = 0.42f;
        hierarchyLayout.flexibleHeight = 1f;

        RectTransform logPanel = CreatePanel("LogPanel", contentRow, new Color(0.10f, 0.14f, 0.18f, 0.78f));
        LayoutElement logLayout = logPanel.gameObject.AddComponent<LayoutElement>();
        logLayout.flexibleWidth = 0.58f;
        logLayout.flexibleHeight = 1f;

        BuildPane(hierarchyPanel, "Scene Objects / Components", out _hierarchyScrollRect, out _hierarchyContent);
        BuildPane(logPanel, "Logs", out _logScrollRect, out _logContent);

        RectTransform footerRow = CreateHorizontalRow("FooterRow", _windowRoot, 8f, 42f);
        Image footerBackground = footerRow.gameObject.AddComponent<Image>();
        footerBackground.color = new Color(0.05f, 0.07f, 0.10f, 0.95f);

        _focusLabel = CreateText("FocusLabel", footerRow, "Focus : All", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement footerLeftLayout = _focusLabel.gameObject.AddComponent<LayoutElement>();
        footerLeftLayout.flexibleWidth = 1f;

        _countLabel = CreateText("CountLabel", footerRow, "Count : 0", 22f, FontStyles.Bold, TextAlignmentOptions.Right);
        LayoutElement footerRightLayout = _countLabel.gameObject.AddComponent<LayoutElement>();
        footerRightLayout.minWidth = 140f;
    }

    private void BuildPane(RectTransform panelRoot, string title, out ScrollRect scrollRect, out RectTransform contentRoot)
    {
        VerticalLayoutGroup layout = panelRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText("Title", panelRoot, title, 26f, FontStyles.Bold, TextAlignmentOptions.Left);

        GameObject scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollObject.transform.SetParent(panelRoot, false);
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        LayoutElement layoutElement = scrollObject.AddComponent<LayoutElement>();
        layoutElement.flexibleHeight = 1f;
        layoutElement.minHeight = 200f;

        Image scrollBackground = scrollObject.GetComponent<Image>();
        scrollBackground.color = new Color(0.14f, 0.18f, 0.25f, 1f);
        Mask mask = scrollObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25f;

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        Stretch(viewportRect, 0f, 0f, 0f, 0f);
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        Mask viewportMask = viewportObject.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        contentRoot = contentObject.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(8, 8, 8, 8);
        contentLayout.spacing = 4f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRoot;
    }

    private void CreateTypeToggles(RectTransform root)
    {
        _typeToggleMap.Clear();
        Array values = Enum.GetValues(typeof(DebugType));
        for (int i = 0; i < values.Length; i++)
        {
            DebugType type = (DebugType)values.GetValue(i);
            Toggle toggle = CreateTypeToggle(root, type.ToString(), isOn =>
            {
                DebugConsoleManager manager = DebugConsoleManager.Instance;
                if (manager == null)
                    return;

                manager.SetTypeEnabled(type, isOn);
                UpdateTypeFilterButtonLabel();
                RequestRebuild();
            });
            _typeToggleMap[type] = toggle;
        }
    }

    private void ToggleTypeFilterPanel()
    {
        _showTypeFilterPanel = !_showTypeFilterPanel;
        _typeFilterPanel.gameObject.SetActive(_showTypeFilterPanel);
        _typeFilterToggleLabel.text = _showTypeFilterPanel ? "Type Filter ▼" : "Type Filter ▶";
        LayoutRebuilder.ForceRebuildLayoutImmediate(_windowRoot);
    }

    private void ApplyBlockInput(bool value)
    {
        _blockInput = value;
        if (_screenBlocker != null)
            _screenBlocker.raycastTarget = _visible && value;
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;
        IsRuntimeConsoleVisible = visible;

        if (_canvas == null)
            return;

        _canvas.enabled = visible;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.blocksRaycasts = visible;
        _canvasGroup.interactable = visible;

        if (_screenBlocker != null)
            _screenBlocker.raycastTarget = visible && _blockInput;

        if (visible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            EnsureEventSystemExists();
            RequestRebuild(true);
        }
    }

    private void RequestRebuild(bool forceImmediate = false)
    {
        _nextRebuildTime = 0f;

        if (!forceImmediate || !_visible || DebugConsoleManager.Instance == null)
            return;

        RebuildUIContents(DebugConsoleManager.Instance);
    }

    private void RebuildUIContents(DebugConsoleManager manager)
    {
        if (manager == null)
            return;

        RebuildHierarchyPane(manager);
        RebuildLogPane(manager);
        UpdateFooterLabels();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_windowRoot);
    }

    private void RebuildHierarchyPane(DebugConsoleManager manager)
    {
        ClearChildren(_hierarchyContent);

        List<GameObject> roots = GetSceneRootObjects();
        for (int i = 0; i < roots.Count; i++)
            AddGameObjectRowRecursive(_hierarchyContent, roots[i], 0, manager);
    }

    private void AddGameObjectRowRecursive(RectTransform parent, GameObject go, int depth, DebugConsoleManager manager)
    {
        if (go == null)
            return;

        if (!ShouldShowGameObject(go))
            return;

        RectTransform row = CreateHorizontalRow($"ObjectRow_{go.GetInstanceID()}", parent, 4f, 30f);
        Image rowBackground = row.gameObject.AddComponent<Image>();
        rowBackground.color = GetObjectRowColor(go.GetInstanceID());

        LayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(6 + depth * 18, 6, 2, 2);

        Toggle objectToggle = CreateInlineToggle(row, manager.GetGameObjectEnabled(go), value =>
        {
            manager.SetGameObjectEnabled(go, value);
            RequestRebuild();
        });

        objectToggle.transform.SetAsFirstSibling();

        bool hasChildren = HasVisibleChildren(go);
        bool hasComponents = HasVisibleComponents(go);
        bool hasExpandTarget = hasChildren || hasComponents;

        Button labelButton = CreateRowButton(row, TrimName(go.name), () =>
        {
            HandleObjectSelected(go);
        }, TextAlignmentOptions.Left, true);

        LayoutElement labelLayout = labelButton.gameObject.GetComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;

        if (hasExpandTarget)
        {
            bool expanded = GetExpanded(go.GetInstanceID());
            CreateRowButton(row, expanded ? "▼" : "▶", () =>
            {
                SetExpanded(go.GetInstanceID(), !GetExpanded(go.GetInstanceID()));
                RequestRebuild();
            }, TextAlignmentOptions.Center, false, 32f);
        }
        else
        {
            LayoutElement spacer = new GameObject("ExpandSpacer", typeof(RectTransform), typeof(LayoutElement)).GetComponent<LayoutElement>();
            spacer.transform.SetParent(row, false);
            spacer.preferredWidth = 32f;
        }

        if (!GetExpanded(go.GetInstanceID()))
            return;

        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            if (_hideTransform && component is Transform)
                continue;

            if (!ShouldShowComponent(component))
                continue;

            AddComponentRow(parent, go, component, depth + 1, manager);
        }

        for (int childIndex = 0; childIndex < go.transform.childCount; childIndex++)
        {
            Transform child = go.transform.GetChild(childIndex);
            if (child == null)
                continue;

            AddGameObjectRowRecursive(parent, child.gameObject, depth + 1, manager);
        }
    }

    private void AddComponentRow(RectTransform parent, GameObject owner, Component component, int depth, DebugConsoleManager manager)
    {
        RectTransform row = CreateHorizontalRow($"ComponentRow_{component.GetInstanceID()}", parent, 4f, 28f);
        Image rowBackground = row.gameObject.AddComponent<Image>();
        rowBackground.color = _focusState.IsComponentFocused(component.GetInstanceID())
            ? new Color(0.73f, 0.58f, 0.15f, 0.72f)
            : new Color(0.32f, 0.24f, 0.08f, 0.30f);

        LayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(18 + depth * 18, 6, 2, 2);

        CreateInlineToggle(row, manager.GetComponentEnabled(component), value =>
        {
            manager.SetComponentEnabled(component, value);
            RequestRebuild();
        });

        Button labelButton = CreateRowButton(row, TrimName(component.GetType().Name), () =>
        {
            HandleComponentSelected(owner, component);
        }, TextAlignmentOptions.Left, true);

        LayoutElement labelLayout = labelButton.gameObject.GetComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;

        LayoutElement spacer = new GameObject("EndSpacer", typeof(RectTransform), typeof(LayoutElement)).GetComponent<LayoutElement>();
        spacer.transform.SetParent(row, false);
        spacer.preferredWidth = 32f;
    }

    private void HandleObjectSelected(GameObject go)
    {
        if (go == null)
            return;

        if (_collapsePreviousOnSelection)
            _expandedObjectState.Clear();

        _focusState.FocusGameObject(go.GetInstanceID(), go.name);
        SetExpanded(go.GetInstanceID(), true);
        RequestRebuild();
    }

    private void HandleComponentSelected(GameObject owner, Component component)
    {
        if (owner == null || component == null)
            return;

        if (_collapsePreviousOnSelection)
            _expandedObjectState.Clear();

        _focusState.FocusComponent(owner.GetInstanceID(), component.GetInstanceID(), owner.name, component.GetType().Name);
        SetExpanded(owner.GetInstanceID(), true);
        RequestRebuild();
    }

    private void RebuildLogPane(DebugConsoleManager manager)
    {
        ClearChildren(_logContent);

        IReadOnlyList<DebugEntry> entries = manager.Entries;
        int visibleCount = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            DebugEntry entry = entries[i];
            if (!ShouldDisplayEntry(manager, entry))
                continue;

            visibleCount++;
            AddLogEntryRow(_logContent, entry);
        }

        _countLabel.text = $"Count : {visibleCount}";
    }

    private void AddLogEntryRow(RectTransform parent, DebugEntry entry)
    {
        RectTransform row = CreateVerticalRow($"LogRow_{parent.childCount}", parent, 2f);
        Image background = row.gameObject.AddComponent<Image>();
        background.color = GetLogRowColor(entry.Level);

        Button button = row.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.12f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0.08f);
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
        button.onClick.AddListener(() => FocusFromEntry(entry));

        string header = $"[{entry.Time}] [{entry.Type}] {entry.SourceName}";
        string footer = string.IsNullOrEmpty(entry.MemberName)
            ? entry.Message
            : $"{entry.Message}\n{entry.MemberName}:{entry.LineNumber}";

        TextMeshProUGUI headerText = CreateText("Header", row, header, 18f, FontStyles.Bold, TextAlignmentOptions.Left);
        headerText.color = ColorFromHex(entry.ColorHex, new Color(0.92f, 0.94f, 0.96f, 1f));
        headerText.enableWordWrapping = true;

        TextMeshProUGUI messageText = CreateText("Message", row, footer, 17f, FontStyles.Normal, TextAlignmentOptions.Left);
        messageText.color = new Color(0.90f, 0.94f, 0.96f, 1f);
        messageText.enableWordWrapping = true;

        LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 54f;
    }

    private void FocusFromEntry(DebugEntry entry)
    {
        if (entry == null)
            return;

        if (entry.ComponentId != 0 && entry.Context is Component component)
        {
            HandleComponentSelected(component.gameObject, component);
            return;
        }

        if (entry.GameObjectId != 0)
        {
            GameObject target = FindGameObjectByInstanceId(entry.GameObjectId);
            if (target != null)
                HandleObjectSelected(target);
        }
    }

    private bool ShouldDisplayEntry(DebugConsoleManager manager, DebugEntry entry)
    {
        if (entry == null)
            return false;

        if (!manager.IsAllowed(entry.Type, entry.GameObjectId, entry.ComponentId))
            return false;

        if (_focusState.HasComponentFocus && entry.ComponentId != _focusState.FocusedComponentId)
            return false;

        if (_focusState.HasGameObjectFocus && !_focusState.HasComponentFocus && entry.GameObjectId != _focusState.FocusedGameObjectId)
            return false;

        if (string.IsNullOrWhiteSpace(_logSearch))
            return true;

        string keyword = _logSearch.Trim();
        return Contains(entry.Message, keyword)
               || Contains(entry.SourceName, keyword)
               || Contains(entry.MemberName, keyword)
               || Contains(entry.Type.ToString(), keyword)
               || Contains(entry.Time, keyword);
    }

    private bool ShouldShowGameObject(GameObject go)
    {
        if (go == null)
            return false;

        if (string.IsNullOrWhiteSpace(_hierarchySearch))
            return true;

        string keyword = _hierarchySearch.Trim();
        if (Contains(go.name, keyword))
            return true;

        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            if (_hideTransform && component is Transform)
                continue;

            if (Contains(component.GetType().Name, keyword))
                return true;
        }

        for (int childIndex = 0; childIndex < go.transform.childCount; childIndex++)
        {
            Transform child = go.transform.GetChild(childIndex);
            if (child != null && ShouldShowGameObject(child.gameObject))
                return true;
        }

        return false;
    }

    private bool ShouldShowComponent(Component component)
    {
        if (component == null)
            return false;

        if (string.IsNullOrWhiteSpace(_hierarchySearch))
            return true;

        string keyword = _hierarchySearch.Trim();
        return Contains(component.GetType().Name, keyword)
               || Contains(component.gameObject.name, keyword);
    }

    private bool HasVisibleChildren(GameObject go)
    {
        for (int i = 0; i < go.transform.childCount; i++)
        {
            Transform child = go.transform.GetChild(i);
            if (child != null && ShouldShowGameObject(child.gameObject))
                return true;
        }

        return false;
    }

    private bool HasVisibleComponents(GameObject go)
    {
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            if (_hideTransform && component is Transform)
                continue;

            if (ShouldShowComponent(component))
                return true;
        }

        return false;
    }

    private bool GetExpanded(int instanceId)
    {
        return _expandedObjectState.TryGetValue(instanceId, out bool expanded) && expanded;
    }

    private void SetExpanded(int instanceId, bool expanded)
    {
        _expandedObjectState[instanceId] = expanded;
    }

    private void SyncManagerStateToUI(DebugConsoleManager manager)
    {
        if (manager == null)
            return;

        SetToggleValueWithoutNotify(_globalToggle, manager.GlobalEnabled);
        SetToggleValueWithoutNotify(_mirrorToggle, manager.MirrorToUnityConsole);
        SetToggleValueWithoutNotify(_autoScrollToggle, _autoScroll);
        SetToggleValueWithoutNotify(_hideTransformToggle, _hideTransform);
        SetToggleValueWithoutNotify(_collapsePrevToggle, _collapsePreviousOnSelection);
        SetToggleValueWithoutNotify(_blockInputToggle, _blockInput);

        foreach (KeyValuePair<DebugType, Toggle> pair in _typeToggleMap)
            SetToggleValueWithoutNotify(pair.Value, manager.GetTypeEnabled(pair.Key));

        UpdateTypeFilterButtonLabel();
    }

    private void UpdateTypeFilterButtonLabel()
    {
        if (_typeFilterToggleLabel == null)
            return;

        int enabledCount = 0;
        foreach (KeyValuePair<DebugType, Toggle> pair in _typeToggleMap)
        {
            if (pair.Value != null && pair.Value.isOn)
                enabledCount++;
        }

        string arrow = _showTypeFilterPanel ? "▼" : "▶";
        _typeFilterToggleLabel.text = $"Type Filter {arrow} ({enabledCount}/{_typeToggleMap.Count})";
    }

    private void UpdateFooterLabels()
    {
        _focusLabel.text = _focusState.GetFooterLabel(20);
    }

    private List<GameObject> GetSceneRootObjects()
    {
        List<GameObject> roots = new List<GameObject>();
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            roots.AddRange(scene.GetRootGameObjects());
        }

        return roots;
    }

    private GameObject FindGameObjectByInstanceId(int instanceId)
    {
        List<GameObject> roots = GetSceneRootObjects();
        for (int i = 0; i < roots.Count; i++)
        {
            GameObject result = FindGameObjectRecursive(roots[i].transform, instanceId);
            if (result != null)
                return result;
        }

        return null;
    }

    private GameObject FindGameObjectRecursive(Transform current, int instanceId)
    {
        if (current == null)
            return null;

        if (current.gameObject.GetInstanceID() == instanceId)
            return current.gameObject;

        for (int i = 0; i < current.childCount; i++)
        {
            GameObject result = FindGameObjectRecursive(current.GetChild(i), instanceId);
            if (result != null)
                return result;
        }

        return null;
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        DontDestroyOnLoad(eventSystemObject);

        Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
            eventSystemObject.AddComponent(inputSystemModuleType);
        else
            eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private TMP_FontAsset CreateDynamicFontAsset()
    {
        string[] fontCandidates =
        {
            "Malgun Gothic",
            "맑은 고딕",
            "Noto Sans CJK KR",
            "Noto Sans KR",
            "Arial Unicode MS",
            FontFallbackName
        };

        Font sourceFont = Font.CreateDynamicFontFromOSFont(fontCandidates, 32);
        if (sourceFont == null)
            sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
    }

    private RectTransform CreateHorizontalRow(string name, Transform parent, float spacing, float preferredHeight)
    {
        GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowObject.transform.SetParent(parent, false);
        RectTransform rect = rowObject.GetComponent<RectTransform>();

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        if (preferredHeight > 0f)
            layoutElement.preferredHeight = preferredHeight;

        return rect;
    }

    private RectTransform CreateVerticalRow(string name, Transform parent, float spacing)
    {
        GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        rowObject.transform.SetParent(parent, false);
        RectTransform rect = rowObject.GetComponent<RectTransform>();

        VerticalLayoutGroup layout = rowObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return rect;
    }

    private RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        return image.rectTransform;
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.font = _runtimeFontAsset;
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = alignment;
        textComponent.color = new Color(0.90f, 0.94f, 0.96f, 1f);
        textComponent.enableWordWrapping = false;
        textComponent.overflowMode = TextOverflowModes.Ellipsis;
        return textComponent;
    }

    private Toggle CreateHeaderToggle(Transform parent, string label, Action<bool> callback)
    {
        Toggle toggle = CreateTypeToggle(parent, label, callback, 170f, 30f);
        LayoutElement layout = toggle.gameObject.GetComponent<LayoutElement>();
        layout.preferredWidth = 170f;
        return toggle;
    }

    private Toggle CreateTypeToggle(Transform parent, string label, Action<bool> callback, float preferredWidth = 200f, float preferredHeight = 30f)
    {
        GameObject root = new GameObject(label + "Toggle", typeof(RectTransform), typeof(LayoutElement), typeof(Toggle));
        root.transform.SetParent(parent, false);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.preferredWidth = preferredWidth;
        layout.preferredHeight = preferredHeight;

        HorizontalLayoutGroup row = root.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 6f;
        row.padding = new RectOffset(2, 2, 2, 2);
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = false;
        row.childControlHeight = false;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        Toggle toggle = root.GetComponent<Toggle>();

        Image background = CreateImage("Background", root.transform, new Color(0.14f, 0.18f, 0.25f, 1f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.sizeDelta = new Vector2(20f, 20f);
        toggle.targetGraphic = background;

        Image checkmark = CreateImage("Checkmark", background.transform, new Color(0.56f, 0.88f, 0.75f, 1f));
        Stretch(checkmark.rectTransform, 3f, -3f, 3f, -3f);
        toggle.graphic = checkmark;

        TextMeshProUGUI labelText = CreateText("Label", root.transform, label, 19f, FontStyles.Normal, TextAlignmentOptions.Left);
        LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = preferredWidth - 28f;
        labelText.enableWordWrapping = false;

        toggle.onValueChanged.AddListener(value => callback?.Invoke(value));
        return toggle;
    }

    private Toggle CreateInlineToggle(Transform parent, bool initialValue, Action<bool> callback)
    {
        GameObject root = new GameObject("InlineToggle", typeof(RectTransform), typeof(LayoutElement), typeof(Toggle));
        root.transform.SetParent(parent, false);
        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.preferredWidth = 22f;
        layout.preferredHeight = 22f;

        Toggle toggle = root.GetComponent<Toggle>();
        Image background = CreateImage("Background", root.transform, new Color(0.15f, 0.19f, 0.24f, 1f));
        Stretch(background.rectTransform, 0f, 0f, 0f, 0f);
        toggle.targetGraphic = background;

        Image checkmark = CreateImage("Checkmark", background.transform, new Color(0.56f, 0.88f, 0.75f, 1f));
        Stretch(checkmark.rectTransform, 4f, -4f, 4f, -4f);
        toggle.graphic = checkmark;

        toggle.SetIsOnWithoutNotify(initialValue);
        toggle.onValueChanged.AddListener(value => callback?.Invoke(value));
        return toggle;
    }

    private Button CreateButton(Transform parent, string text, Action onClick, float width = 170f)
    {
        return CreateButton(parent, text, onClick, out _, width);
    }

    private Button CreateButton(Transform parent, string text, Action onClick, out TextMeshProUGUI label, float width = 170f)
    {
        GameObject buttonObject = new GameObject(text + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 34f;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.28f, 0.37f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.28f, 0.35f, 0.46f, 1f);
        colors.pressedColor = new Color(0.18f, 0.24f, 0.31f, 1f);
        colors.selectedColor = new Color(0.28f, 0.35f, 0.46f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() => onClick?.Invoke());

        label = CreateText("Label", buttonObject.transform, text, 18f, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 6f, -6f, 2f, -2f);
        label.text = text;
        return button;
    }

    private Button CreateRowButton(Transform parent, string text, Action onClick, TextAlignmentOptions alignment, bool flexible, float width = 0f)
    {
        GameObject buttonObject = new GameObject(text + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        if (flexible)
            layout.flexibleWidth = 1f;
        else
            layout.preferredWidth = width;
        layout.preferredHeight = 24f;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.24f, 0.30f, 0.38f, 1f);
        colors.pressedColor = new Color(0.15f, 0.19f, 0.24f, 1f);
        colors.selectedColor = new Color(0.24f, 0.30f, 0.38f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() => onClick?.Invoke());

        TextMeshProUGUI label = CreateText("Label", buttonObject.transform, text, 18f, FontStyles.Normal, alignment);
        Stretch(label.rectTransform, 6f, -6f, 0f, 0f);
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return button;
    }

    private void CreateLabeledInput(Transform parent, string label, out TMP_InputField inputField, Action<string> onValueChanged, float widthWeight)
    {
        GameObject containerObject = new GameObject(label + "Container", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        containerObject.transform.SetParent(parent, false);
        LayoutElement containerLayout = containerObject.GetComponent<LayoutElement>();
        containerLayout.flexibleWidth = widthWeight;
        containerLayout.preferredHeight = 34f;

        HorizontalLayoutGroup row = containerObject.GetComponent<HorizontalLayoutGroup>();
        row.spacing = 6f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = false;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        TextMeshProUGUI labelText = CreateText("Label", containerObject.transform, label, 19f, FontStyles.Normal, TextAlignmentOptions.Left);
        LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 150f;

        inputField = CreateInputField(containerObject.transform, onValueChanged);
        LayoutElement inputLayout = inputField.gameObject.GetComponent<LayoutElement>();
        inputLayout.flexibleWidth = 1f;
    }

    private TMP_InputField CreateInputField(Transform parent, Action<string> onValueChanged)
    {
        GameObject root = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
        root.transform.SetParent(parent, false);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.preferredHeight = 34f;

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.11f, 0.14f, 0.18f, 1f);

        TMP_InputField inputField = root.GetComponent<TMP_InputField>();
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.richText = false;
        inputField.customCaretColor = true;
        inputField.caretColor = new Color(0.83f, 0.96f, 0.92f, 1f);
        inputField.selectionColor = new Color(0.24f, 0.40f, 0.36f, 0.95f);
        inputField.shouldHideMobileInput = false;
        inputField.resetOnDeActivation = false;
        inputField.restoreOriginalTextOnEscape = false;
        inputField.caretWidth = 2;

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(root.transform, false);
        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        Stretch(textAreaRect, 8f, -8f, 5f, -5f);

        TextMeshProUGUI placeholder = CreateText("Placeholder", textArea.transform, string.Empty, 19f, FontStyles.Normal, TextAlignmentOptions.Left);
        Stretch(placeholder.rectTransform, 0f, 0f, 0f, 0f);
        placeholder.color = new Color(0.52f, 0.62f, 0.66f, 0.7f);

        TextMeshProUGUI text = CreateText("Text", textArea.transform, string.Empty, 19f, FontStyles.Normal, TextAlignmentOptions.Left);
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Masking;

        inputField.textViewport = textAreaRect;
        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        inputField.onValueChanged.AddListener(value => onValueChanged?.Invoke(value));
        return inputField;
    }

    private void ClearChildren(RectTransform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private void SetToggleValueWithoutNotify(Toggle toggle, bool value)
    {
        if (toggle == null)
            return;

        toggle.SetIsOnWithoutNotify(value);
    }

    private Color GetObjectRowColor(int instanceId)
    {
        if (_focusState.IsObjectFocused(instanceId))
            return new Color(0.85f, 0.67f, 0.18f, 0.78f);

        if (_focusState.IsFocusedObjectParent(instanceId))
            return new Color(0.58f, 0.46f, 0.13f, 0.66f);

        return new Color(0.18f, 0.22f, 0.28f, 0.60f);
    }

    private Color GetLogRowColor(DebugLogLevel level)
    {
        switch (level)
        {
            case DebugLogLevel.Warning:
                return new Color(0.38f, 0.28f, 0.06f, 0.90f);
            case DebugLogLevel.Error:
                return new Color(0.44f, 0.10f, 0.10f, 0.90f);
            default:
                return new Color(0.17f, 0.20f, 0.25f, 0.90f);
        }
    }

    private bool Contains(string source, string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return true;

        return !string.IsNullOrEmpty(source)
               && source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string TrimName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length > 36 ? value.Substring(0, 36) + "..." : value;
    }

    private Color ColorFromHex(string colorHex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(colorHex))
            return fallback;

        return ColorUtility.TryParseHtmlString(colorHex, out Color parsedColor) ? parsedColor : fallback;
    }

    private void Stretch(RectTransform rectTransform, float left, float right, float top, float bottom)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, top);
        rectTransform.offsetMax = new Vector2(right, bottom);
    }

    private void SetImageBorder(Image image, Color borderColor)
    {
        Outline outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(1f, -1f);
    }
}
