
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Text = UnityEngine.UI.Text;

public class RuntimeDebugConsoleWindow : MonoBehaviour
{
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
    [SerializeField] private bool _visible;
    [SerializeField] private bool _autoScroll = true;
    [SerializeField] private bool _hideTransform = true;
    [SerializeField] private bool _collapsePreviousOnSelection = true;
    [SerializeField] private bool _blockInput = true;

    public static RuntimeDebugConsoleWindow Instance { get; private set; }
    public static bool IsRuntimeConsoleVisible { get; private set; }
    public static bool ShouldBlockGameplayInput => Instance != null && Instance._visible && Instance._blockInput;

    private const int CanvasSortOrder = 32000;
    private const float RebuildInterval = 0.15f;

    private Canvas _canvas;
    private CanvasScaler _canvasScaler;
    private GraphicRaycaster _graphicRaycaster;
    private CanvasGroup _canvasGroup;
    private RectTransform _windowRoot;
    private Image _screenBlocker;

    private RectTransform _titleRect;
    private RectTransform _toggleRowRect;
    private RectTransform _buttonRowRect;
    private RectTransform _searchRowRect;
    private RectTransform _typeFilterPanelRect;
    private RectTransform _contentRowRect;
    private RectTransform _footerRect;
    private RectTransform _leftPanelRect;
    private RectTransform _rightPanelRect;
    private RectTransform _dividerRect;
    private RectTransform _hierarchyScrollRoot;
    private RectTransform _logScrollRoot;

    private Toggle _globalToggle;
    private Toggle _mirrorToggle;
    private Toggle _autoScrollToggle;
    private Toggle _hideTransformToggle;
    private Toggle _collapsePrevToggle;
    private Toggle _blockInputToggle;

    private Button _typeFilterButton;
    private TextMeshProUGUI _typeFilterButtonLabel;

    private InputField _hierarchySearchInput;
    private InputField _logSearchInput;

    private ScrollRect _hierarchyScrollRect;
    private RectTransform _hierarchyContent;
    private ScrollRect _logScrollRect;
    private RectTransform _logContent;

    private TextMeshProUGUI _focusLabel;
    private TextMeshProUGUI _countLabel;

    private readonly Dictionary<DebugType, Toggle> _typeToggles = new();
    private readonly Dictionary<int, bool> _expandedObjects = new();
    private readonly DebugConsoleFocusState _focusState = new();
    private readonly List<Behaviour> _blockedInputBehaviours = new();
    private EventSystem _ownedEventSystem;

    private TMP_FontAsset _fontAsset;
    private Font _inputFont;
    private bool _showTypeFilterPanel = true;
    private float _nextRebuildTime;
    private int _lastManagerVersion = -1;
    private bool _pendingAutoScroll;

    private string _hierarchySearch = string.Empty;
    private string _logSearch = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        BuildUi();
        ApplyFixedLayout();
        EnsureEventSystemExists();
        SetVisible(_visible);

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        RestoreBlockedGameplayInput();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (_windowRoot != null)
            ApplyFixedLayout();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _expandedObjects.Clear();
        _focusState.Clear();
        _lastManagerVersion = -1;
        _nextRebuildTime = 0f;

        if (_visible)
            RequestRebuild(true);
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

        bool shouldRebuild = false;

        if (_lastManagerVersion != manager.ChangeVersion)
        {
            _lastManagerVersion = manager.ChangeVersion;
            SyncManagerStateToUi(manager);
            shouldRebuild = true;
            _pendingAutoScroll = _autoScroll;
        }

        if (Time.unscaledTime >= _nextRebuildTime)
        {
            shouldRebuild = true;
            _nextRebuildTime = Time.unscaledTime + RebuildInterval;
        }

        if (shouldRebuild)
            RebuildContents(manager);
    }

    private void LateUpdate()
    {
        if (!_visible || !_pendingAutoScroll || _logScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        _logScrollRect.verticalNormalizedPosition = 0f;
        _pendingAutoScroll = false;
    }

    private void BuildUi()
    {
        _fontAsset = CreateDynamicFontAsset();
        _inputFont = CreateDynamicUiFont();

        GameObject canvasObject = new GameObject("RuntimeDebugConsoleCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect, 0f, 0f, 0f, 0f);

        _canvas = canvasObject.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = CanvasSortOrder;

        _canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        _canvasScaler.matchWidthOrHeight = 0.5f;

        _graphicRaycaster = canvasObject.GetComponent<GraphicRaycaster>();
        _canvasGroup = canvasObject.GetComponent<CanvasGroup>();

        _screenBlocker = CreateImage(canvasRect, "ScreenBlocker", new Color(0f, 0f, 0f, 0.22f));
        Stretch(_screenBlocker.rectTransform, 0f, 0f, 0f, 0f);

        Image windowImage = CreateImage(canvasRect, "WindowRoot", new Color(0.20f, 0.25f, 0.35f, 0.98f));
        _windowRoot = windowImage.rectTransform;
        Stretch(_windowRoot, 18f, 18f, 18f, 18f);
        AddOutline(windowImage.gameObject, new Color(0.80f, 0.82f, 0.88f, 1f));

        _titleRect = CreateRect(_windowRoot, "TitleRow");
        CreateText(_titleRect, "Runtime Debug Console", 34f, FontStyles.Bold, TextAlignmentOptions.Center);

        _toggleRowRect = CreateRect(_windowRoot, "ToggleRow");
        HorizontalLayoutGroup toggleLayout = _toggleRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        toggleLayout.spacing = 24f;
        toggleLayout.padding = new RectOffset(4, 4, 0, 0);
        toggleLayout.childAlignment = TextAnchor.MiddleLeft;
        toggleLayout.childControlWidth = false;
        toggleLayout.childControlHeight = true;
        toggleLayout.childForceExpandWidth = false;
        toggleLayout.childForceExpandHeight = false;
        _globalToggle = CreateLabeledToggle(_toggleRowRect, "Global", v =>
        {
            DebugConsoleManager manager = DebugConsoleManager.Instance;
            if (manager == null)
                return;
            manager.GlobalEnabled = v;
            RequestRebuild();
        }, 82f, 13f);

        _mirrorToggle = CreateLabeledToggle(_toggleRowRect, "Mirror Unity", v =>
        {
            DebugConsoleManager manager = DebugConsoleManager.Instance;
            if (manager == null)
                return;
            manager.MirrorToUnityConsole = v;
            RequestRebuild();
        }, 110f, 13f);

        _autoScrollToggle = CreateLabeledToggle(_toggleRowRect, "Auto Scroll", v => _autoScroll = v, 102f, 13f);
        _hideTransformToggle = CreateLabeledToggle(_toggleRowRect, "Hide Transform", v =>
        {
            _hideTransform = v;
            RequestRebuild();
        }, 118f, 13f);
        _collapsePrevToggle = CreateLabeledToggle(_toggleRowRect, "Collapse Prev", v => _collapsePreviousOnSelection = v, 190f);
        _blockInputToggle = CreateLabeledToggle(_toggleRowRect, "Block Input", v => ApplyBlockInput(v), 102f, 13f);

        _buttonRowRect = CreateRect(_windowRoot, "ButtonRow");
        HorizontalLayoutGroup buttonLayout = _buttonRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 20f;
        buttonLayout.padding = new RectOffset(0, 0, 0, 0);
        buttonLayout.childAlignment = TextAnchor.MiddleLeft;
        buttonLayout.childControlWidth = false;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;

        _typeFilterButton = CreateButton(_buttonRowRect, "Type Filter v (0/0)", ToggleTypeFilterPanel, out _typeFilterButtonLabel, 150f);
        CreateButton(_buttonRowRect, "All Types On", () =>
        {
            DebugConsoleManager manager = DebugConsoleManager.Instance;
            if (manager == null)
                return;
            manager.SetAllTypes(true);
            SyncManagerStateToUi(manager);
            RequestRebuild();
        }, out _, 120f);

        CreateButton(_buttonRowRect, "All Types Off", () =>
        {
            DebugConsoleManager manager = DebugConsoleManager.Instance;
            if (manager == null)
                return;
            manager.SetAllTypes(false);
            SyncManagerStateToUi(manager);
            RequestRebuild();
        }, out _, 120f);

        CreateButton(_buttonRowRect, "Clear Logs", () =>
        {
            DebugConsoleManager manager = DebugConsoleManager.Instance;
            if (manager == null)
                return;
            manager.ClearLogs();
            RequestRebuild();
        }, out _, 120f);

        CreateButton(_buttonRowRect, "Clear Focus", () =>
        {
            _focusState.Clear();
            RequestRebuild();
        }, out _, 120f);

        _searchRowRect = CreateRect(_windowRoot, "SearchRow");
        CreateSearchArea();

        _typeFilterPanelRect = CreatePanelRect(_windowRoot, "TypeFilterPanel", new Color(0.12f, 0.15f, 0.21f, 0.92f));
        AddOutline(_typeFilterPanelRect.gameObject, new Color(0.28f, 0.33f, 0.43f, 1f));
        GridLayoutGroup grid = _typeFilterPanelRect.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(92f, 28f);
        grid.spacing = new Vector2(8f, 8f);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        CreateTypeToggleGrid();

        _contentRowRect = CreateRect(_windowRoot, "ContentRow");

        _leftPanelRect = CreatePanelRect(_contentRowRect, "HierarchyPanel", new Color(0.11f, 0.15f, 0.20f, 0.95f));
        AddOutline(_leftPanelRect.gameObject, new Color(0.25f, 0.30f, 0.40f, 1f));
        BuildPanel(_leftPanelRect, "Scene Objects / Components", out _hierarchyScrollRect, out _hierarchyContent, out _hierarchyScrollRoot);

        _dividerRect = CreatePanelRect(_contentRowRect, "Divider", new Color(0.23f, 0.27f, 0.35f, 1f));

        _rightPanelRect = CreatePanelRect(_contentRowRect, "LogPanel", new Color(0.11f, 0.15f, 0.20f, 0.95f));
        AddOutline(_rightPanelRect.gameObject, new Color(0.25f, 0.30f, 0.40f, 1f));
        BuildPanel(_rightPanelRect, "Logs", out _logScrollRect, out _logContent, out _logScrollRoot);

        _footerRect = CreatePanelRect(_leftPanelRect, "Footer", new Color(0.05f, 0.08f, 0.12f, 0.98f));
        AddOutline(_footerRect.gameObject, new Color(0.20f, 0.24f, 0.32f, 1f));
        CreateFooter();
    }

    private void CreateSearchArea()
    {
        RectTransform hierarchyLabelRect = CreateRect(_searchRowRect, "HierarchyLabel");
        SetRect(hierarchyLabelRect, 0f, 0f, 0f, 1f, 0f, 0f, 140f, 0f);
        TextMeshProUGUI hierarchyLabel = CreateText(hierarchyLabelRect, "Hierarchy Search", 20f, FontStyles.Normal, TextAlignmentOptions.Left);
        Stretch(hierarchyLabel.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform hierarchyInputRect = CreateRect(_searchRowRect, "HierarchyInputRect");
        SetRect(hierarchyInputRect, 0f, 0f, 0.42f, 1f, 150f, 0f, -8f, 0f);
        _hierarchySearchInput = CreateInputField(hierarchyInputRect, value =>
        {
            _hierarchySearch = value ?? string.Empty;
            RequestRebuild();
        });
        Stretch(_hierarchySearchInput.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        RectTransform logLabelRect = CreateRect(_searchRowRect, "LogLabel");
        SetRect(logLabelRect, 0.42f, 0f, 0.42f, 1f, 8f, 0f, 100f, 0f);
        TextMeshProUGUI logLabel = CreateText(logLabelRect, "Log Search", 20f, FontStyles.Normal, TextAlignmentOptions.Left);
        Stretch(logLabel.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform logInputRect = CreateRect(_searchRowRect, "LogInputRect");
        SetRect(logInputRect, 0.42f, 0f, 0.92f, 1f, 118f, 0f, -8f, 0f);
        _logSearchInput = CreateInputField(logInputRect, value =>
        {
            _logSearch = value ?? string.Empty;
            RequestRebuild();
        });
        Stretch(_logSearchInput.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        RectTransform clearRect = CreateRect(_searchRowRect, "ClearSearchRect");
        SetRect(clearRect, 0.92f, 0f, 1f, 1f, 8f, 0f, 0f, 0f);
        CreateButton(clearRect, "Clear Search", () =>
        {
            _hierarchySearch = string.Empty;
            _logSearch = string.Empty;
            _hierarchySearchInput.SetTextWithoutNotify(string.Empty);
            _logSearchInput.SetTextWithoutNotify(string.Empty);
            RequestRebuild();
        }, out _, 140f);
        Stretch(clearRect.GetChild(0).GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
    }

    private void CreateTypeToggleGrid()
    {
        _typeToggles.Clear();
        Array values = Enum.GetValues(typeof(DebugType));
        for (int i = 0; i < values.Length; i++)
        {
            DebugType debugType = (DebugType)values.GetValue(i);
            Toggle toggle = CreateLabeledToggle(_typeFilterPanelRect, debugType.ToString(), value =>
            {
                DebugConsoleManager manager = DebugConsoleManager.Instance;
                if (manager == null)
                    return;

                manager.SetTypeEnabled(debugType, value);
                UpdateTypeFilterButtonLabel();
                RequestRebuild();
            }, 92f, 12f);

            _typeToggles[debugType] = toggle;
        }
    }

    private void CreateFooter()
    {
        _focusLabel = CreateText(_footerRect, "Focus : All", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        _focusLabel.color = new Color(0.95f, 0.84f, 0.24f, 1f);

        _countLabel = CreateText(_footerRect, "Count : 0", 22f, FontStyles.Bold, TextAlignmentOptions.Right);
        _countLabel.color = new Color(0.95f, 0.84f, 0.24f, 1f);

        UpdateFooterLayout();
    }

    private void BuildPanel(RectTransform panelRoot, string title, out ScrollRect scrollRect, out RectTransform contentRoot, out RectTransform scrollRoot)
    {
        RectTransform titleRect = CreateRect(panelRoot, "Title");
        SetTopRect(titleRect, 10f, 28f, 12f, 12f);
        CreateText(titleRect, title, 26f, FontStyles.Bold, TextAlignmentOptions.Center);

        scrollRoot = CreatePanelRect(panelRoot, "ScrollView", new Color(0.14f, 0.18f, 0.25f, 1f));
        Stretch(scrollRoot, 12f, 12f, 44f, 12f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollRoot, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, 0f, 0f, 0f, 0f);

        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);

        Mask viewportMask = viewport.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);

        contentRoot = content.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRoot;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;
    }

    private void ApplyFixedLayout()
    {
        if (_windowRoot == null)
            return;

        SetTopRect(_titleRect, 10f, 38f, 12f, 12f);
        SetTopRect(_toggleRowRect, 56f, 26f, 12f, 12f);
        SetTopRect(_buttonRowRect, 90f, 34f, 12f, 12f);
        SetTopRect(_searchRowRect, 132f, 34f, 12f, 12f);

        float bodyTop;
        if (_showTypeFilterPanel)
        {
            _typeFilterPanelRect.gameObject.SetActive(true);
            SetTopRect(_typeFilterPanelRect, 174f, 120f, 12f, 12f);
            bodyTop = 304f;
        }
        else
        {
            _typeFilterPanelRect.gameObject.SetActive(false);
            bodyTop = 174f;
        }

        Stretch(_contentRowRect, 12f, 12f, bodyTop, 12f);

        SetRect(_leftPanelRect, 0f, 0f, 0.36f, 1f, 0f, 0f, -6f, 0f);
        SetRect(_dividerRect, 0.36f, 0f, 0.36f, 1f, 0f, 0f, 4f, 0f);
        _dividerRect.sizeDelta = new Vector2(4f, 0f);
        SetRect(_rightPanelRect, 0.36f, 0f, 1f, 1f, 8f, 0f, 0f, 0f);

        if (_footerRect != null)
            SetBottomRect(_footerRect, 12f, GetFooterHeight(), 12f, 12f);

        if (_hierarchyScrollRoot != null)
            Stretch(_hierarchyScrollRoot, 12f, 12f, 44f, GetFooterHeight() + 18f);

        if (_logScrollRoot != null)
            Stretch(_logScrollRoot, 12f, 12f, 44f, 12f);

        UpdateFooterLayout();
        UpdateTypeFilterButtonLabel();
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
            ApplyBlockedGameplayInput();
            RequestRebuild(true);
        }
        else
        {
            RestoreBlockedGameplayInput();
        }
    }

    private void ApplyBlockInput(bool value)
    {
        _blockInput = value;

        if (_screenBlocker != null)
            _screenBlocker.raycastTarget = _visible && value;

        if (_visible)
        {
            if (value)
                ApplyBlockedGameplayInput();
            else
                RestoreBlockedGameplayInput();
        }
    }

    private void ApplyBlockedGameplayInput()
    {
        RestoreBlockedGameplayInput();

        if (!_blockInput)
            return;

        Type playerInputType = Type.GetType("UnityEngine.InputSystem.PlayerInput, Unity.InputSystem");
        if (playerInputType == null)
            return;

        UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(playerInputType);
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] is not Behaviour behaviour)
                continue;

            if (!behaviour.gameObject.scene.IsValid())
                continue;

            if (!behaviour.enabled)
                continue;

            behaviour.enabled = false;
            _blockedInputBehaviours.Add(behaviour);
        }
    }

    private void RestoreBlockedGameplayInput()
    {
        for (int i = 0; i < _blockedInputBehaviours.Count; i++)
        {
            Behaviour behaviour = _blockedInputBehaviours[i];
            if (behaviour != null)
                behaviour.enabled = true;
        }

        _blockedInputBehaviours.Clear();
    }

    private void RequestRebuild(bool immediate = false)
    {
        _nextRebuildTime = 0f;
        if (!immediate || !_visible || DebugConsoleManager.Instance == null)
            return;

        RebuildContents(DebugConsoleManager.Instance);
    }

    private void RebuildContents(DebugConsoleManager manager)
    {
        if (manager == null)
            return;

        RebuildHierarchyContent(manager);
        RebuildLogContent(manager);
        UpdateFooter();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_hierarchyContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_logContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_leftPanelRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rightPanelRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_windowRoot);
    }

    private void RebuildHierarchyContent(DebugConsoleManager manager)
    {
        ClearChildren(_hierarchyContent);

        List<GameObject> roots = GetSceneRootObjects();
        for (int i = 0; i < roots.Count; i++)
            AddGameObjectRecursive(_hierarchyContent, roots[i], 0, manager);
    }

    private void AddGameObjectRecursive(RectTransform parent, GameObject gameObject, int depth, DebugConsoleManager manager)
    {
        if (gameObject == null || !ShouldShowGameObject(gameObject))
            return;

        RectTransform row = CreateRect(parent, $"GameObjectRow_{gameObject.GetInstanceID()}");
        HorizontalLayoutGroup layout = AddHorizontalLayout(row, 4f, 6 + depth * 18, 6, 2, 2);
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 30f;

        Image rowBackground = row.gameObject.AddComponent<Image>();
        rowBackground.color = GetGameObjectRowColor(gameObject.GetInstanceID());

        CreateInlineToggle(row, manager.GetGameObjectEnabled(gameObject), value =>
        {
            manager.SetGameObjectEnabled(gameObject, value);
            RequestRebuild();
        });

        Button selectButton = CreateRowButton(row, TrimDisplayName(gameObject.name), () =>
        {
            SelectGameObject(gameObject);
        }, true, 0f);
        LayoutElement labelLayout = selectButton.gameObject.GetComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;

        bool hasVisibleComponents = HasVisibleComponents(gameObject);
        bool hasVisibleChildren = HasVisibleChildren(gameObject);

        if (hasVisibleComponents || hasVisibleChildren)
        {
            bool expanded = GetExpanded(gameObject.GetInstanceID());
            CreateRowButton(row, expanded ? "-" : "+", () =>
            {
                SetExpanded(gameObject.GetInstanceID(), !expanded);
                RequestRebuild();
            }, false, 32f);
        }
        else
        {
            CreateSpacer(row, 32f);
        }

        if (!GetExpanded(gameObject.GetInstanceID()))
            return;

        Component[] components = gameObject.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            if (_hideTransform && component is Transform)
                continue;

            if (!ShouldShowComponent(component))
                continue;

            AddComponentRow(parent, gameObject, component, depth + 1, manager);
        }

        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            Transform child = gameObject.transform.GetChild(i);
            if (child == null)
                continue;

            AddGameObjectRecursive(parent, child.gameObject, depth + 1, manager);
        }
    }

    private void AddComponentRow(RectTransform parent, GameObject owner, Component component, int depth, DebugConsoleManager manager)
    {
        RectTransform row = CreateRect(parent, $"ComponentRow_{component.GetInstanceID()}");
        HorizontalLayoutGroup layout = AddHorizontalLayout(row, 4f, 18 + depth * 18, 6, 2, 2);
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 28f;

        Image rowBackground = row.gameObject.AddComponent<Image>();
        rowBackground.color = _focusState.IsComponentFocused(component.GetInstanceID())
            ? new Color(0.72f, 0.55f, 0.16f, 0.82f)
            : new Color(0.45f, 0.33f, 0.08f, 0.28f);

        CreateInlineToggle(row, manager.GetComponentEnabled(component), value =>
        {
            manager.SetComponentEnabled(component, value);
            RequestRebuild();
        });

        Button selectButton = CreateRowButton(row, TrimDisplayName(component.GetType().Name), () =>
        {
            SelectComponent(owner, component);
        }, true, 0f);
        LayoutElement labelLayout = selectButton.gameObject.GetComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;

        CreateSpacer(row, 32f);
    }

    private void RebuildLogContent(DebugConsoleManager manager)
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
            AddLogEntryRow(entry);
        }

        _countLabel.text = $"Count : {visibleCount}";
    }

    private void AddLogEntryRow(DebugEntry entry)
    {
        RectTransform row = CreateRect(_logContent, $"LogRow_{_logContent.childCount}");
        VerticalLayoutGroup layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.minHeight = 56f;

        Image background = row.gameObject.AddComponent<Image>();
        background.color = GetLogBackgroundColor(entry.Level);

        Button button = row.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.12f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0.08f);
        button.colors = colors;
        button.onClick.AddListener(() => FocusFromEntry(entry));

        string header = $"[{entry.Time}] [{entry.Type}] {entry.SourceName}";
        string detail = string.IsNullOrEmpty(entry.MemberName)
            ? entry.Message
            : $"{entry.Message}\n출처 : {entry.MemberName} : {entry.LineNumber}";

        TextMeshProUGUI headerText = CreateText(row, header, 18f, FontStyles.Bold, TextAlignmentOptions.Left);
        headerText.enableWordWrapping = true;
        headerText.color = ParseColor(entry.ColorHex, new Color(0.92f, 0.95f, 0.98f, 1f));

        TextMeshProUGUI detailText = CreateText(row, detail, 17f, FontStyles.Normal, TextAlignmentOptions.Left);
        detailText.enableWordWrapping = true;
        detailText.color = new Color(0.90f, 0.94f, 0.97f, 1f);
    }

    private void FocusFromEntry(DebugEntry entry)
    {
        if (entry == null)
            return;

        if (entry.Context is Component component)
        {
            SelectComponent(component.gameObject, component);
            return;
        }

        if (entry.Context is GameObject gameObject)
        {
            SelectGameObject(gameObject);
            return;
        }

        if (entry.GameObjectId != 0)
        {
            GameObject target = FindGameObjectByInstanceId(entry.GameObjectId);
            if (target != null)
                SelectGameObject(target);
        }
    }

    private void SelectGameObject(GameObject gameObject)
    {
        if (gameObject == null)
            return;

        if (_collapsePreviousOnSelection)
            _expandedObjects.Clear();

        _focusState.FocusGameObject(gameObject.GetInstanceID(), gameObject.name);
        SetExpanded(gameObject.GetInstanceID(), true);
        RequestRebuild();
    }

    private void SelectComponent(GameObject owner, Component component)
    {
        if (owner == null || component == null)
            return;

        if (_collapsePreviousOnSelection)
            _expandedObjects.Clear();

        _focusState.FocusComponent(owner.GetInstanceID(), component.GetInstanceID(), owner.name, component.GetType().Name);
        SetExpanded(owner.GetInstanceID(), true);
        RequestRebuild();
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

        if (!string.IsNullOrWhiteSpace(_logSearch))
        {
            string keyword = _logSearch.Trim();
            if (!ContainsText(entry.Message, keyword)
                && !ContainsText(entry.SourceName, keyword)
                && !ContainsText(entry.MemberName, keyword)
                && !ContainsText(entry.Type.ToString(), keyword)
                && !ContainsText(entry.Time, keyword))
                return false;
        }

        return true;
    }

    private bool ShouldShowGameObject(GameObject gameObject)
    {
        if (gameObject == null)
            return false;

        if (string.IsNullOrWhiteSpace(_hierarchySearch))
            return true;

        string keyword = _hierarchySearch.Trim();
        if (ContainsText(gameObject.name, keyword))
            return true;

        Component[] components = gameObject.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            if (_hideTransform && component is Transform)
                continue;

            if (ContainsText(component.GetType().Name, keyword))
                return true;
        }

        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            Transform child = gameObject.transform.GetChild(i);
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
        return ContainsText(component.GetType().Name, keyword)
               || ContainsText(component.gameObject.name, keyword);
    }

    private bool HasVisibleChildren(GameObject gameObject)
    {
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            Transform child = gameObject.transform.GetChild(i);
            if (child != null && ShouldShowGameObject(child.gameObject))
                return true;
        }

        return false;
    }

    private bool HasVisibleComponents(GameObject gameObject)
    {
        Component[] components = gameObject.GetComponents<Component>();
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
        return _expandedObjects.TryGetValue(instanceId, out bool expanded) && expanded;
    }

    private void SetExpanded(int instanceId, bool expanded)
    {
        _expandedObjects[instanceId] = expanded;
    }

    private void SyncManagerStateToUi(DebugConsoleManager manager)
    {
        if (manager == null)
            return;

        SetToggleValue(_globalToggle, manager.GlobalEnabled);
        SetToggleValue(_mirrorToggle, manager.MirrorToUnityConsole);
        SetToggleValue(_autoScrollToggle, _autoScroll);
        SetToggleValue(_hideTransformToggle, _hideTransform);
        SetToggleValue(_collapsePrevToggle, _collapsePreviousOnSelection);
        SetToggleValue(_blockInputToggle, _blockInput);

        foreach (KeyValuePair<DebugType, Toggle> pair in _typeToggles)
            SetToggleValue(pair.Value, manager.GetTypeEnabled(pair.Key));

        UpdateTypeFilterButtonLabel();
    }

    private void UpdateFooter()
    {
        _focusLabel.text = _focusState.GetFooterLabel(24);
        UpdateFooterLayout();
    }

    private float GetFooterHeight()
    {
        return _leftPanelRect != null && _leftPanelRect.rect.width < 420f ? 74f : 42f;
    }

    private void UpdateFooterLayout()
    {
        if (_footerRect == null || _focusLabel == null || _countLabel == null)
            return;

        bool stacked = _leftPanelRect != null && _leftPanelRect.rect.width < 420f;

        if (stacked)
        {
            SetRect(_focusLabel.rectTransform, 0f, 0.5f, 1f, 1f, 12f, 0f, -12f, 0f);
            SetRect(_countLabel.rectTransform, 0f, 0f, 1f, 0.5f, 12f, 0f, -12f, 0f);
            _focusLabel.alignment = TextAlignmentOptions.MidlineLeft;
            _countLabel.alignment = TextAlignmentOptions.MidlineLeft;
        }
        else
        {
            SetRect(_focusLabel.rectTransform, 0f, 0f, 0.7f, 1f, 12f, 0f, -8f, 0f);
            SetRect(_countLabel.rectTransform, 0.7f, 0f, 1f, 1f, 8f, 0f, -12f, 0f);
            _focusLabel.alignment = TextAlignmentOptions.MidlineLeft;
            _countLabel.alignment = TextAlignmentOptions.MidlineRight;
        }
    }

    private void ToggleTypeFilterPanel()
    {
        _showTypeFilterPanel = !_showTypeFilterPanel;
        ApplyFixedLayout();
    }

    private void UpdateTypeFilterButtonLabel()
    {
        if (_typeFilterButtonLabel == null)
            return;

        int enabledCount = 0;
        foreach (KeyValuePair<DebugType, Toggle> pair in _typeToggles)
        {
            if (pair.Value != null && pair.Value.isOn)
                enabledCount++;
        }

        string arrow = _showTypeFilterPanel ? "v" : ">";
        _typeFilterButtonLabel.text = $"Type Filter {arrow} ({enabledCount}/{_typeToggles.Count})";
    }

    private void EnsureEventSystemExists()
    {
        EventSystem[] sceneSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem externalSystem = null;
        for (int i = 0; i < sceneSystems.Length; i++)
        {
            EventSystem system = sceneSystems[i];
            if (system == null)
                continue;

            if (_ownedEventSystem != null && system == _ownedEventSystem)
                continue;

            externalSystem = system;
            break;
        }

        if (externalSystem != null)
        {
            if (_ownedEventSystem != null)
            {
                Destroy(_ownedEventSystem.gameObject);
                _ownedEventSystem = null;
            }

            return;
        }

        if (_ownedEventSystem != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        DontDestroyOnLoad(eventSystemObject);

        Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
            eventSystemObject.AddComponent(inputSystemModuleType);
        else
            eventSystemObject.AddComponent<StandaloneInputModule>();

        _ownedEventSystem = eventSystemObject.GetComponent<EventSystem>();
    }

    private List<GameObject> GetSceneRootObjects()
    {
        List<GameObject> roots = new();
        HashSet<int> seen = new();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            GameObject[] sceneRoots = scene.GetRootGameObjects();
            for (int j = 0; j < sceneRoots.Length; j++)
            {
                GameObject gameObject = sceneRoots[j];
                if (gameObject == null)
                    continue;

                if (!seen.Add(gameObject.GetInstanceID()))
                    continue;

                roots.Add(gameObject);
            }
        }

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject gameObject = allObjects[i];
            if (gameObject == null)
                continue;

            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
                continue;

            if (gameObject.transform.parent != null)
                continue;

            if (!seen.Add(gameObject.GetInstanceID()))
                continue;

            roots.Add(gameObject);
        }

        roots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return roots;
    }

    private GameObject FindGameObjectByInstanceId(int instanceId)
    {
        List<GameObject> roots = GetSceneRootObjects();
        for (int i = 0; i < roots.Count; i++)
        {
            GameObject found = FindGameObjectRecursive(roots[i].transform, instanceId);
            if (found != null)
                return found;
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
            GameObject found = FindGameObjectRecursive(current.GetChild(i), instanceId);
            if (found != null)
                return found;
        }

        return null;
    }

    private RectTransform CreateRect(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private RectTransform CreatePanelRect(Transform parent, string name, Color color)
    {
        Image image = CreateImage(parent, name, color);
        return image.rectTransform;
    }

    private Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI textComponent = obj.GetComponent<TextMeshProUGUI>();
        textComponent.font = _fontAsset;
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = alignment;
        textComponent.color = new Color(0.92f, 0.95f, 0.98f, 1f);
        textComponent.enableWordWrapping = false;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        return textComponent;
    }

    private HorizontalLayoutGroup AddHorizontalLayout(RectTransform target, float spacing, int left, int right, int top, int bottom)
    {
        HorizontalLayoutGroup layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(left, right, top, bottom);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private Toggle CreateLabeledToggle(Transform parent, string label, Action<bool> callback, float width, float fontSize = 14f)
    {
        RectTransform root = CreateRect(parent, $"{label}_Toggle");
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        width = Mathf.Max(width, 34f + label.Length * 9f);
        layout.preferredWidth = width;
        layout.preferredHeight = 24f;

        Toggle toggle = root.gameObject.AddComponent<Toggle>();

        RectTransform boxRect = CreateRect(root, "Box");
        boxRect.anchorMin = new Vector2(0f, 0.5f);
        boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.sizeDelta = new Vector2(18f, 18f);
        boxRect.anchoredPosition = new Vector2(0f, 0f);

        Image box = boxRect.gameObject.AddComponent<Image>();
        box.color = new Color(0.16f, 0.20f, 0.28f, 1f);
        toggle.targetGraphic = box;

        RectTransform checkRect = CreateRect(boxRect, "Checkmark");
        Stretch(checkRect, 3f, 3f, 3f, 3f);
        Image check = checkRect.gameObject.AddComponent<Image>();
        check.color = new Color(0.56f, 0.88f, 0.75f, 1f);
        toggle.graphic = check;

        RectTransform labelRect = CreateRect(root, "LabelRect");
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(28f, 0f);
        labelRect.offsetMax = new Vector2(0f, 0f);

        TextMeshProUGUI labelText = CreateText(labelRect, label, fontSize, FontStyles.Normal, TextAlignmentOptions.Left);
        Stretch(labelText.rectTransform, 0f, 0f, 0f, 0f);
        labelText.overflowMode = TextOverflowModes.Overflow;

        toggle.onValueChanged.AddListener(value => callback?.Invoke(value));
        return toggle;
    }

    private Toggle CreateInlineToggle(Transform parent, bool initialValue, Action<bool> callback)
    {
        RectTransform root = CreateRect(parent, "InlineToggle");
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 22f;
        layout.preferredHeight = 22f;

        Toggle toggle = root.gameObject.AddComponent<Toggle>();

        Image box = root.gameObject.AddComponent<Image>();
        box.color = new Color(0.16f, 0.20f, 0.28f, 1f);
        toggle.targetGraphic = box;

        RectTransform checkRect = CreateRect(root, "Checkmark");
        Stretch(checkRect, 4f, 4f, 4f, 4f);
        Image check = checkRect.gameObject.AddComponent<Image>();
        check.color = new Color(0.56f, 0.88f, 0.75f, 1f);
        toggle.graphic = check;

        toggle.SetIsOnWithoutNotify(initialValue);
        toggle.onValueChanged.AddListener(value => callback?.Invoke(value));
        return toggle;
    }

    private Button CreateButton(Transform parent, string text, Action onClick, out TextMeshProUGUI label, float width)
    {
        RectTransform root = CreateRect(parent, $"{text}_Button");
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 34f;

        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.18f, 0.23f, 0.31f, 1f);

        Button button = root.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background.color;
        colors.highlightedColor = new Color(0.25f, 0.31f, 0.41f, 1f);
        colors.pressedColor = new Color(0.15f, 0.19f, 0.26f, 1f);
        colors.selectedColor = new Color(0.25f, 0.31f, 0.41f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() => onClick?.Invoke());

        label = CreateText(root, text, 14f, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 6f, 6f, 2f, 2f);
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;

        return button;
    }

    private Button CreateRowButton(Transform parent, string text, Action onClick, bool flexible, float width)
    {
        RectTransform root = CreateRect(parent, $"{text}_RowButton");
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 24f;
        if (flexible)
            layout.flexibleWidth = 1f;
        else
            layout.preferredWidth = width;

        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.18f, 0.22f, 0.28f, 0.96f);

        Button button = root.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background.color;
        colors.highlightedColor = new Color(0.24f, 0.29f, 0.38f, 1f);
        colors.pressedColor = new Color(0.15f, 0.18f, 0.24f, 1f);
        colors.selectedColor = new Color(0.24f, 0.29f, 0.38f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() => onClick?.Invoke());

        TextMeshProUGUI label = CreateText(root, text, 18f, FontStyles.Normal, TextAlignmentOptions.Left);
        Stretch(label.rectTransform, 6f, 6f, 0f, 0f);
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;

        return button;
    }

    private InputField CreateInputField(Transform parent, Action<string> onValueChanged)
    {
        RectTransform root = CreateRect(parent, "InputField");
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.09f, 0.12f, 0.17f, 1f);
        AddOutline(root.gameObject, new Color(0.28f, 0.34f, 0.44f, 1f));

        InputField inputField = root.gameObject.AddComponent<InputField>();
        inputField.lineType = InputField.LineType.SingleLine;
        inputField.contentType = InputField.ContentType.Standard;
        inputField.shouldHideMobileInput = false;
        inputField.caretWidth = 2;
        inputField.customCaretColor = true;
        inputField.caretColor = new Color(0.84f, 0.96f, 0.92f, 1f);
        inputField.selectionColor = new Color(0.24f, 0.40f, 0.36f, 0.95f);

        RectTransform textArea = CreateRect(root, "TextArea");
        Stretch(textArea, 10f, 10f, 5f, 5f);
        textArea.gameObject.AddComponent<RectMask2D>();

        Text text = CreateInputText(textArea, string.Empty, new Color(0.92f, 0.95f, 0.98f, 1f));
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.supportRichText = false;

        Text placeholder = CreateInputText(textArea, string.Empty, new Color(0.52f, 0.62f, 0.66f, 0.72f));
        Stretch(placeholder.rectTransform, 0f, 0f, 0f, 0f);
        placeholder.text = string.Empty;
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.horizontalOverflow = HorizontalWrapMode.Overflow;
        placeholder.verticalOverflow = VerticalWrapMode.Truncate;
        placeholder.supportRichText = false;

        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        inputField.onValueChanged.AddListener(value => onValueChanged?.Invoke(value));
        return inputField;
    }

    private Text CreateInputText(Transform parent, string value, Color color)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);

        Text text = obj.GetComponent<Text>();
        text.font = _inputFont;
        text.text = value;
        text.fontSize = 18;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        return text;
    }


    private void CreateSpacer(Transform parent, float width)
    {
        RectTransform spacer = CreateRect(parent, "Spacer");
        LayoutElement layout = spacer.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 1f;
    }

    private void SetToggleValue(Toggle toggle, bool value)
    {
        if (toggle != null)
            toggle.SetIsOnWithoutNotify(value);
    }

    private void ClearChildren(RectTransform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private bool ContainsText(string source, string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return true;

        return !string.IsNullOrEmpty(source) &&
               source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string TrimDisplayName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length > 34 ? value.Substring(0, 34) + "..." : value;
    }

    private Color ParseColor(string html, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(html))
            return fallback;

        return ColorUtility.TryParseHtmlString(html, out Color parsed) ? parsed : fallback;
    }

    private Color GetGameObjectRowColor(int instanceId)
    {
        if (_focusState.IsObjectFocused(instanceId))
            return new Color(0.85f, 0.67f, 0.18f, 0.82f);

        if (_focusState.IsFocusedObjectParent(instanceId))
            return new Color(0.57f, 0.44f, 0.13f, 0.68f);

        return new Color(0.18f, 0.22f, 0.28f, 0.62f);
    }

    private Color GetLogBackgroundColor(DebugLogLevel level)
    {
        return level switch
        {
            DebugLogLevel.Warning => new Color(0.36f, 0.27f, 0.08f, 0.92f),
            DebugLogLevel.Error => new Color(0.42f, 0.11f, 0.11f, 0.92f),
            _ => new Color(0.17f, 0.21f, 0.27f, 0.92f)
        };
    }

    private Font CreateDynamicUiFont()
    {
        string[] candidates =
        {
            "Malgun Gothic",
            "맑은 고딕",
            "Noto Sans CJK KR",
            "Noto Sans KR",
            "Arial Unicode MS",
            "Arial"
        };

        Font sourceFont = Font.CreateDynamicFontFromOSFont(candidates, 18);
        if (sourceFont == null)
            sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return sourceFont;
    }

    private TMP_FontAsset CreateDynamicFontAsset()
    {
        string[] candidates =
        {
            "Malgun Gothic",
            "맑은 고딕",
            "Noto Sans CJK KR",
            "Noto Sans KR",
            "Arial Unicode MS",
            "Arial"
        };

        Font sourceFont = Font.CreateDynamicFontFromOSFont(candidates, 32);
        if (sourceFont == null)
            sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);
    }

    private void Stretch(RectTransform rectTransform, float left, float right, float top, float bottom)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private void SetTopRect(RectTransform rectTransform, float top, float height, float left, float right)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.offsetMin = new Vector2(left, -(top + height));
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private void SetBottomRect(RectTransform rectTransform, float bottom, float height, float left, float right)
    {
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, bottom + height);
    }

    private void SetRect(RectTransform rectTransform, float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY,
        float offsetLeft, float offsetBottom, float offsetRight, float offsetTop)
    {
        rectTransform.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rectTransform.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rectTransform.offsetMin = new Vector2(offsetLeft, offsetBottom);
        rectTransform.offsetMax = new Vector2(offsetRight, offsetTop);
    }

    private void AddOutline(GameObject gameObject, Color color)
    {
        Outline outline = gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1f, -1f);
    }
}
