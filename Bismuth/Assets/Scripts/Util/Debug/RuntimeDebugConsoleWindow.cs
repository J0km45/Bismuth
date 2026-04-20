using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RuntimeDebugConsoleWindow : MonoBehaviour
{
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
    [SerializeField] private bool _visible = false;
    [SerializeField] private Rect _windowRect = new Rect(20f, 20f, 1450f, 850f);
    [SerializeField] private bool _autoScroll = true;
    [SerializeField] private bool _hideTransform = true;
    [SerializeField] private bool _collapsePreviousOnSelection = true;

    private Vector2 _hierarchyScroll;
    private Vector2 _logScroll;
    private Vector2 _typeFilterScroll;

    private string _hierarchySearch = string.Empty;
    private string _logSearch = string.Empty;

    private const string HierarchySearchControlName = "DebugConsole_HierarchySearch";
    private const string LogSearchControlName = "DebugConsole_LogSearch";
    private IMECompositionMode _previousImeCompositionMode = IMECompositionMode.Auto;
    private bool _imeCompositionCaptured;
    private bool _searchFieldFocusedThisFrame;
    private Rect _lastFocusedSearchFieldRect;

    private enum SearchFieldFocus
    {
        None,
        Hierarchy,
        Log
    }

    private SearchFieldFocus _activeSearchField = SearchFieldFocus.None;
    private GUIStyle _searchFieldContentStyle;

    private RuntimeDebugConsoleSearchOverlay _searchOverlay;

    private bool _showTypeFilterPanel;

    private int _focusedGameObjectId;
    private int _focusedComponentId;
    private string _focusedObjectName = string.Empty;
    private string _focusedComponentName = string.Empty;

    private GUIStyle _titleStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _richLabelStyle;
    private GUIStyle _dimLabelStyle;
    private GUIStyle _searchTextFieldStyle;
    private GUIStyle _linkButtonStyle;
    private GUIStyle _disabledButtonStyle;
    private GUIStyle _objectSelectedButtonStyle;
    private GUIStyle _componentSelectedButtonStyle;
    private GUIStyle _parentSelectedButtonStyle;
    private GUIStyle _foldoutButtonStyle;
    private GUIStyle _toolbarButtonStyle;
    private GUIStyle _toolbarInfoLabelStyle;
    private GUIStyle _toolbarInfoRightLabelStyle;
    private GUIStyle _footerLeftLabelStyle;
    private GUIStyle _footerRightLabelStyle;
    private GUIStyle _objectFocusedRowStyle;
    private GUIStyle _objectParentFocusedRowStyle;
    private GUIStyle _componentFocusedRowStyle;
    private Texture2D _solidTexture;
    private bool _stylesDirty = true;

    private readonly Color _selectedObjectBg = new Color(0.98f, 0.80f, 0.18f, 1f);
    private readonly Color _selectedComponentBg = new Color(0.84f, 0.64f, 0.14f, 1f);
    private readonly Color _selectedParentBg = new Color(0.50f, 0.38f, 0.08f, 1f);
    private readonly Color _objectFocusedRowBg = new Color(0.98f, 0.80f, 0.18f, 0.32f);
    private readonly Color _parentFocusedRowBg = new Color(0.76f, 0.58f, 0.12f, 0.22f);
    private readonly Color _componentFocusedRowBg = new Color(0.84f, 0.64f, 0.14f, 0.36f);
    private readonly Color _selectedText = new Color(0.18f, 0.11f, 0.00f, 1f);
    private readonly Color _selectedParentText = new Color(1.00f, 0.95f, 0.78f, 1f);
    private readonly Color _toolbarInfoText = new Color(1.00f, 0.89f, 0.34f, 1f);
    private readonly Color _footerInfoTextColor = new Color(0.96f, 0.84f, 0.22f, 1f);

    private const int MaxDisplayNameLength = 15;
    private const int FooterFocusSegmentMaxLength = 16;
    private const float HierarchyRowHeight = 22f;
    private const float HierarchyToggleSize = 18f;
    private const float HierarchyFoldoutSize = 18f;
    private const float PanelSplitterWidth = 6f;
    private const float MaxHierarchyIndentPenalty = 24f;
    private const float MinHierarchyPanelWidth = 220f;
    private const float MinLogPanelWidth = 220f;
    private const float HierarchyRowContentRightReserve = 18f;

    [SerializeField] private float _hierarchyPanelWidth = 480f;
    private bool _isDraggingPanelSplitter;

    private float _lastLogContentHeight;
    private float _lastLogViewportHeight;
    private float _lastMaxLogScrollY;

    private readonly HashSet<int> _expandedComponents = new();
    private readonly HashSet<int> _expandedChildren = new();

    private int _selectedLogIndex = -1;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        _stylesDirty = true;
        _titleStyle = null;
        HideSearchOverlay();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        HideSearchOverlay();

        if (_imeCompositionCaptured)
        {
            Input.imeCompositionMode = _previousImeCompositionMode;
            _imeCompositionCaptured = false;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _stylesDirty = true;
        _titleStyle = null;
        _expandedComponents.Clear();
        _expandedChildren.Clear();
        _selectedLogIndex = -1;
        _hierarchyScroll = Vector2.zero;
        _activeSearchField = SearchFieldFocus.None;
        ClearFocus();

        HideSearchOverlay();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            _visible = !_visible;

            if (!_visible)
            {
                HideSearchOverlay();
                _activeSearchField = SearchFieldFocus.None;
            }
        }
    }

    private void OnGUI()
    {
        HideSearchOverlay();

        if (!_visible)
        {
            EndSearchInputFrame();
            return;
        }

        BeginSearchInputFrame();
        InitStyles();
        _windowRect = GUI.Window(91357, _windowRect, DrawWindow, "Runtime Debug Console");
        EndSearchInputFrame();
    }

    

    

    private void HideSearchOverlay()
    {
        if (_searchOverlay == null)
            _searchOverlay = GetComponentInChildren<RuntimeDebugConsoleSearchOverlay>(true);

        if (_searchOverlay != null)
            _searchOverlay.SetVisible(false);
    }

    private void BeginSearchInputFrame()
    {
        _searchFieldFocusedThisFrame = false;
        _lastFocusedSearchFieldRect = Rect.zero;
    }

    private void EndSearchInputFrame()
    {
        if (_searchFieldFocusedThisFrame)
        {
            if (!_imeCompositionCaptured)
            {
                _previousImeCompositionMode = Input.imeCompositionMode;
                _imeCompositionCaptured = true;
            }

            Input.imeCompositionMode = IMECompositionMode.On;
            Vector2 screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(_lastFocusedSearchFieldRect.x + 6f, _lastFocusedSearchFieldRect.y + _lastFocusedSearchFieldRect.height - 6f));
            Input.compositionCursorPos = screenPoint;
            return;
        }

        if (_imeCompositionCaptured)
        {
            Input.imeCompositionMode = _previousImeCompositionMode;
            _imeCompositionCaptured = false;
        }
    }

    private void InitStyles()
    {
        if (!_stylesDirty && _titleStyle != null)
            return;

        _stylesDirty = false;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13,
            wordWrap = false,
            clipping = TextClipping.Clip
        };

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(8, 8, 8, 8)
        };

        _richLabelStyle = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            wordWrap = true,
            fontSize = 12
        };

        _dimLabelStyle = new GUIStyle(GUI.skin.label);
        _dimLabelStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);

        _searchTextFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 12
        };

        _linkButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(6, 6, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            fixedHeight = HierarchyRowHeight,
            fontStyle = FontStyle.Normal,
            wordWrap = false,
            clipping = TextClipping.Clip
        };
        Color normalButtonText = new Color(0.84f, 0.96f, 0.92f, 1f);
        _linkButtonStyle.normal.textColor = normalButtonText;
        _linkButtonStyle.hover.textColor = normalButtonText;
        _linkButtonStyle.active.textColor = normalButtonText;
        _linkButtonStyle.focused.textColor = normalButtonText;
        _linkButtonStyle.onNormal.textColor = normalButtonText;
        _linkButtonStyle.onHover.textColor = normalButtonText;
        _linkButtonStyle.onActive.textColor = normalButtonText;
        _linkButtonStyle.onFocused.textColor = normalButtonText;

        _disabledButtonStyle = new GUIStyle(_linkButtonStyle);
        _disabledButtonStyle.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
        _disabledButtonStyle.hover.textColor = _disabledButtonStyle.normal.textColor;
        _disabledButtonStyle.active.textColor = _disabledButtonStyle.normal.textColor;

        _foldoutButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            fixedWidth = HierarchyFoldoutSize,
            fixedHeight = HierarchyRowHeight,
            fontStyle = FontStyle.Bold
        };

        if (_solidTexture == null)
        {
            _solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _solidTexture.SetPixel(0, 0, Color.white);
            _solidTexture.Apply();
        }

        _objectFocusedRowStyle = CreateRowStyle(_objectFocusedRowBg);
        _objectParentFocusedRowStyle = CreateRowStyle(_parentFocusedRowBg);
        _componentFocusedRowStyle = CreateRowStyle(_componentFocusedRowBg);

        _objectSelectedButtonStyle = CreateButtonStyle(_selectedObjectBg, _selectedText, true, TextAnchor.MiddleLeft);
        _parentSelectedButtonStyle = CreateButtonStyle(_selectedParentBg, _selectedParentText, true, TextAnchor.MiddleLeft);
        _componentSelectedButtonStyle = CreateButtonStyle(_selectedComponentBg, _selectedText, true, TextAnchor.MiddleLeft);

        _toolbarButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter
        };

        _toolbarInfoLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            richText = false,
            fontStyle = FontStyle.Bold
        };
        _toolbarInfoLabelStyle.normal.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.hover.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.active.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.focused.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.onNormal.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.onHover.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.onActive.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.onFocused.textColor = _toolbarInfoText;

        _toolbarInfoRightLabelStyle = new GUIStyle(_toolbarInfoLabelStyle)
        {
            alignment = TextAnchor.MiddleRight
        };

        _footerLeftLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            richText = false,
            fontStyle = FontStyle.Bold
        };
        ApplyLabelTextColor(_footerLeftLabelStyle, _footerInfoTextColor);

        _footerRightLabelStyle = new GUIStyle(_footerLeftLabelStyle)
        {
            alignment = TextAnchor.MiddleRight
        };
    }

    private void ApplyLabelTextColor(GUIStyle style, Color color)
    {
        style.normal.textColor = color;
        style.hover.textColor = color;
        style.active.textColor = color;
        style.focused.textColor = color;
        style.onNormal.textColor = color;
        style.onHover.textColor = color;
        style.onActive.textColor = color;
        style.onFocused.textColor = color;
    }

    private void DrawWindow(int windowId)
    {
        DebugConsoleManager manager = DebugConsoleManager.Instance;
        if (manager == null)
        {
            GUILayout.Label("DebugConsoleManager가 없습니다.");
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
            return;
        }

        DrawToolbar(manager);
        DrawSearchBar();
        DrawTypeFilterPanel(manager);

        DrawResizablePanels(manager);

        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private void DrawToolbar(DebugConsoleManager manager)
    {
        float availableWidth = GetTopAreaWidth();
        int layoutLevel = GetTopLayoutLevel(availableWidth);

        int enabledCount = GetEnabledTypeCount(manager);
        int totalCount = Enum.GetValues(typeof(DebugType)).Length;
        string typeButtonLabel = _showTypeFilterPanel
            ? $"Type Filter ▲ ({enabledCount}/{totalCount})"
            : $"Type Filter ▼ ({enabledCount}/{totalCount})";

        if (layoutLevel == 1)
        {
            GUILayout.BeginHorizontal(GUILayout.MinHeight(28f));
            DrawToolbarToggleGroup(manager);
            GUILayout.Space(8f);
            DrawToolbarActionGroup(manager, typeButtonLabel);
            GUILayout.EndHorizontal();
            return;
        }

        GUILayout.BeginHorizontal(GUILayout.MinHeight(28f));
        DrawToolbarToggleGroup(manager);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(GUILayout.MinHeight(28f));
        DrawToolbarActionGroup(manager, typeButtonLabel);
        GUILayout.EndHorizontal();
    }

    private void DrawSearchBar()
    {
        float availableWidth = GetTopAreaWidth();

        if (availableWidth >= 760f)
        {
            GUILayout.BeginHorizontal(GUILayout.MinHeight(26f));
            float hierarchyWidth = Mathf.Clamp((availableWidth - 330f) * 0.42f, 160f, 320f);
            DrawHierarchySearchField(hierarchyWidth, 105f);
            GUILayout.Space(12f);
            DrawLogSearchField(75f);
            GUILayout.EndHorizontal();
            return;
        }

        GUILayout.BeginHorizontal(GUILayout.MinHeight(26f));
        DrawHierarchySearchField(Mathf.Max(160f, availableWidth - 130f), 105f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(GUILayout.MinHeight(26f));
        DrawLogSearchField(75f);
        GUILayout.EndHorizontal();
    }

    private void DrawTypeFilterPanel(DebugConsoleManager manager)
    {
        if (!_showTypeFilterPanel)
            return;

        GUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("DebugType Filter", _titleStyle);

        _typeFilterScroll = GUILayout.BeginScrollView(_typeFilterScroll, GUILayout.Height(88f));

        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        const int columns = 4;

        for (int row = 0; row < types.Length; row += columns)
        {
            GUILayout.BeginHorizontal();

            for (int col = 0; col < columns; col++)
            {
                int index = row + col;
                if (index >= types.Length)
                {
                    GUILayout.FlexibleSpace();
                    continue;
                }

                DebugType type = types[index];
                bool current = manager.GetTypeEnabled(type);
                bool next = GUILayout.Toggle(current, type.ToString(), GUILayout.Width(140f));

                if (next != current)
                    manager.SetTypeEnabled(type, next);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawResizablePanels(DebugConsoleManager manager)
    {
        float contentWidth = Mathf.Max(620f, _windowRect.width - 24f);
        float maxHierarchyPanelWidth = Mathf.Max(MinHierarchyPanelWidth, contentWidth - MinLogPanelWidth - PanelSplitterWidth);

        if (_hierarchyPanelWidth <= 0f)
            _hierarchyPanelWidth = contentWidth * 0.42f;

        _hierarchyPanelWidth = Mathf.Clamp(_hierarchyPanelWidth, MinHierarchyPanelWidth, maxHierarchyPanelWidth);
        float logPanelWidth = Mathf.Max(MinLogPanelWidth, contentWidth - _hierarchyPanelWidth - PanelSplitterWidth);

        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawHierarchyPanel(manager, _hierarchyPanelWidth);
        DrawPanelSplitter(contentWidth);
        DrawLogPanel(manager, logPanelWidth);
        GUILayout.EndHorizontal();
    }

    private void DrawPanelSplitter(float contentWidth)
    {
        Rect splitterRect = GUILayoutUtility.GetRect(PanelSplitterWidth, 10f, GUILayout.Width(PanelSplitterWidth), GUILayout.ExpandHeight(true));
        Event current = Event.current;
        bool hovered = splitterRect.Contains(current.mousePosition);

        if (current.type == EventType.MouseDown && current.button == 0 && hovered)
        {
            _isDraggingPanelSplitter = true;
            current.Use();
        }

        if (_isDraggingPanelSplitter && current.type == EventType.MouseDrag)
        {
            float maxHierarchyPanelWidth = Mathf.Max(MinHierarchyPanelWidth, contentWidth - MinLogPanelWidth - PanelSplitterWidth);
            _hierarchyPanelWidth = Mathf.Clamp(_hierarchyPanelWidth + current.delta.x, MinHierarchyPanelWidth, maxHierarchyPanelWidth);
            current.Use();
        }

        if (_isDraggingPanelSplitter && (current.type == EventType.MouseUp || current.rawType == EventType.MouseUp))
        {
            _isDraggingPanelSplitter = false;
            current.Use();
        }

        Color previousColor = GUI.color;
        if (_isDraggingPanelSplitter)
            GUI.color = new Color(1.00f, 0.86f, 0.32f, 0.95f);
        else if (hovered)
            GUI.color = new Color(0.95f, 0.92f, 0.72f, 0.55f);
        else
            GUI.color = new Color(0.80f, 0.80f, 0.80f, 0.20f);

        GUI.Box(splitterRect, GUIContent.none);
        GUI.color = previousColor;
    }

    private void DrawHierarchyPanel(DebugConsoleManager manager, float panelWidth)
    {
        GUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));
        GUILayout.BeginHorizontal();
        GUILayout.Label("Scene Objects / Components", _titleStyle, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        _hierarchyScroll = GUILayout.BeginScrollView(_hierarchyScroll);

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
            DrawGameObjectNode(manager, roots[i], 0, panelWidth);

        GUILayout.EndScrollView();

        GUILayout.Space(4f);
        string footerFocusFullText = GetFocusLabel();
        string footerFocusDisplayText = GetFooterFocusLabel();
        string footerCountText = $"Count : {GetVisibleEntryCount(manager)}";
        float footerHorizontalPadding = 10f;
        float footerGap = 12f;
        float footerFocusRequiredWidth = _footerLeftLabelStyle.CalcSize(new GUIContent(footerFocusDisplayText)).x;
        float footerCountRequiredWidth = _footerRightLabelStyle.CalcSize(new GUIContent(footerCountText)).x;
        bool useTwoLineFooter = panelWidth < footerFocusRequiredWidth + footerCountRequiredWidth + (footerHorizontalPadding * 2f) + footerGap;

        if (useTwoLineFooter)
        {
            GUILayout.BeginVertical(_boxStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(52f));

            GUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
            GUILayout.Space(footerHorizontalPadding);
            GUILayout.Label(new GUIContent(footerFocusDisplayText, footerFocusFullText), _footerLeftLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(22f));
            GUILayout.Space(footerHorizontalPadding);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
            GUILayout.Space(footerHorizontalPadding);
            GUILayout.Label(new GUIContent(footerCountText, footerCountText), _footerLeftLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(22f));
            GUILayout.Space(footerHorizontalPadding);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        else
        {
            GUILayout.BeginHorizontal(_boxStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(30f));
            GUILayout.Space(footerHorizontalPadding);
            float footerCountWidth = Mathf.Ceil(footerCountRequiredWidth) + 4f;
            float footerLeftWidth = Mathf.Max(60f, panelWidth - footerCountWidth - (footerHorizontalPadding * 2f) - footerGap);
            GUILayout.Label(new GUIContent(footerFocusDisplayText, footerFocusFullText), _footerLeftLabelStyle, GUILayout.Width(footerLeftWidth), GUILayout.MinHeight(22f));
            GUILayout.Space(footerGap);
            GUILayout.Label(new GUIContent(footerCountText, footerCountText), _footerRightLabelStyle, GUILayout.Width(footerCountWidth), GUILayout.MinHeight(22f));
            GUILayout.Space(footerHorizontalPadding);
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }

    private float GetHierarchyRowContentWidth(float panelWidth)
    {
        float width = panelWidth;
        width -= _boxStyle.padding.left + _boxStyle.padding.right;
        width -= HierarchyRowContentRightReserve;
        return Mathf.Max(140f, width);
    }

    private float GetHierarchyTextButtonWidth(float rowContentWidth, float leadingSpace, bool reserveToggle, bool reserveFoldout)
    {
        float width = rowContentWidth;
        width -= Mathf.Min(leadingSpace, MaxHierarchyIndentPenalty);

        if (reserveToggle)
            width -= HierarchyToggleSize + 4f;

        if (reserveFoldout)
            width -= HierarchyFoldoutSize + 4f;

        return Mathf.Max(92f, width);
    }

    private float GetLogContentWidth(float panelWidth)
    {
        return Mathf.Max(140f, panelWidth - _boxStyle.padding.left - _boxStyle.padding.right - 58f);
    }

    private void DrawGameObjectNode(DebugConsoleManager manager, GameObject go, int depth, float panelWidth)
    {
        if (go == null)
            return;

        if (!ShouldShowGameObject(go))
            return;

        int id = go.GetInstanceID();
        bool objectEnabled = manager.GetGameObjectEnabled(go);

        Component[] components = go.GetComponents<Component>();
        bool hasVisibleComponents = HasVisibleComponents(components);
        bool hasVisibleChildren = HasVisibleChildren(go);
        bool hasDetails = hasVisibleComponents || hasVisibleChildren;

        bool detailsExpanded = _expandedComponents.Contains(id);
        bool childrenExpanded = _expandedChildren.Contains(id);

        bool searchActive = !string.IsNullOrWhiteSpace(_hierarchySearch);
        bool forceOpenDetails = searchActive && (HasMatchingComponent(go, _hierarchySearch) || HasVisibleChildren(go));
        bool forceOpenChildren = searchActive && HasVisibleChildren(go);

        bool isObjectFocused = IsObjectFocused(id);
        bool isComponentParentFocused = IsFocusedObjectParent(id);
        bool showDetails = hasDetails && (detailsExpanded || forceOpenDetails);
        bool showChildren = hasVisibleChildren && (childrenExpanded || forceOpenChildren);

        float objectLeadingSpace = depth * 18f;
        float rowContentWidth = GetHierarchyRowContentWidth(panelWidth);

        GUILayout.BeginVertical(GetHierarchyRowStyle(isObjectFocused, isComponentParentFocused, false), GUILayout.Width(rowContentWidth));
        GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
        GUILayout.Space(objectLeadingSpace);

        bool nextObjectEnabled = GUILayout.Toggle(objectEnabled, GUIContent.none, GUILayout.Width(HierarchyToggleSize), GUILayout.Height(HierarchyRowHeight));
        if (nextObjectEnabled != objectEnabled)
            manager.SetGameObjectEnabled(go, nextObjectEnabled);

        GUIStyle objectStyle = GetObjectButtonStyle(objectEnabled, isObjectFocused, isComponentParentFocused);
        GUIContent objectContent = new GUIContent(GetDisplayName(go.name), go.name);
        float objectButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, objectLeadingSpace, true, true);
        if (GUILayout.Button(objectContent, objectStyle, GUILayout.Width(objectButtonWidth), GUILayout.Height(HierarchyRowHeight)))
            ToggleGameObjectFocus(go);

        if (hasDetails)
        {
            string foldoutLabel = showDetails ? "▾" : "▸";
            if (GUILayout.Button(foldoutLabel, _foldoutButtonStyle, GUILayout.Width(HierarchyFoldoutSize), GUILayout.Height(HierarchyRowHeight)))
                ToggleExpandedSet(_expandedComponents, id);
        }
        else
        {
            GUILayout.Space(HierarchyFoldoutSize);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (!showDetails)
            return;

        if (hasVisibleComponents)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = objectEnabled;

            float componentLeadingSpace = (depth + 1) * 18f + HierarchyToggleSize + 8f;

            foreach (Component component in components)
            {
                if (!ShouldShowComponent(component, go.name))
                    continue;

                bool isComponentFocused = IsComponentFocused(component.GetInstanceID());

                GUILayout.BeginVertical(GetHierarchyRowStyle(false, false, isComponentFocused), GUILayout.Width(rowContentWidth));
                GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
                GUILayout.Space(componentLeadingSpace);

                bool componentEnabled = manager.GetComponentEnabled(component);
                bool nextComponentEnabled = GUILayout.Toggle(componentEnabled, GUIContent.none, GUILayout.Width(HierarchyToggleSize), GUILayout.Height(HierarchyRowHeight));
                if (nextComponentEnabled != componentEnabled)
                    manager.SetComponentEnabled(component, nextComponentEnabled);

                GUIStyle componentStyle = GetComponentButtonStyle(objectEnabled, isComponentFocused);
                GUIContent componentContent = new GUIContent(GetDisplayName(component.GetType().Name), component.GetType().Name);
                float componentButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, componentLeadingSpace, true, true);
                if (GUILayout.Button(componentContent, componentStyle, GUILayout.Width(componentButtonWidth), GUILayout.Height(HierarchyRowHeight)))
                    ToggleComponentFocus(component);

                GUILayout.Space(HierarchyFoldoutSize);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUI.enabled = previousEnabled;
        }

        if (hasVisibleChildren)
        {
            float childLeadingSpace = (depth + 1) * 18f + HierarchyToggleSize + 8f + HierarchyToggleSize;

            GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
            GUILayout.Space((depth + 1) * 18f + HierarchyToggleSize + 8f);
            GUILayout.Space(HierarchyToggleSize);

            float childButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, childLeadingSpace, true, true);
            if (GUILayout.Button(new GUIContent("하위 오브젝트", "하위 오브젝트"), _linkButtonStyle, GUILayout.Width(childButtonWidth), GUILayout.Height(HierarchyRowHeight)))
                ToggleExpandedSet(_expandedChildren, id);

            string childFoldoutLabel = showChildren ? "▾" : "▸";
            if (GUILayout.Button(childFoldoutLabel, _foldoutButtonStyle, GUILayout.Width(HierarchyFoldoutSize), GUILayout.Height(HierarchyRowHeight)))
                ToggleExpandedSet(_expandedChildren, id);

            GUILayout.EndHorizontal();

            if (showChildren)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                    DrawGameObjectNode(manager, go.transform.GetChild(i).gameObject, depth + 1, panelWidth);
            }
        }
    }

    private void DrawLogPanel(DebugConsoleManager manager, float panelWidth)
    {
        GUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));
        GUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent($"Logs {GetFocusSuffix()}", $"Logs {GetFocusSuffix()}"), _titleStyle, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        bool wasNearBottom = IsNearBottom(_lastMaxLogScrollY);
        float contentHeight = 0f;
        float logContentWidth = GetLogContentWidth(panelWidth);

        _logScroll = GUILayout.BeginScrollView(_logScroll);

        IReadOnlyList<DebugEntry> entries = manager.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            DebugEntry entry = entries[i];

            if (!ShouldDisplayEntry(manager, entry))
                continue;

            float drawnHeight = DrawLogEntry(entry, i, logContentWidth);
            contentHeight += drawnHeight + 4f;
            GUILayout.Space(4f);
        }

        GUILayout.EndScrollView();

        Rect scrollRect = GUILayoutUtility.GetLastRect();
        _lastLogViewportHeight = scrollRect.height;
        _lastLogContentHeight = contentHeight + 8f;
        _lastMaxLogScrollY = Mathf.Max(0f, _lastLogContentHeight - _lastLogViewportHeight);

        if (Event.current.type == EventType.Repaint && (_autoScroll || wasNearBottom || IsNearBottom(_lastMaxLogScrollY)))
            _logScroll.y = _lastMaxLogScrollY + 4f;

        GUILayout.EndVertical();
    }

    private float DrawLogEntry(DebugEntry entry, int index, float contentWidth)
    {
        GUIContent content = new GUIContent(entry.RichText);
        float estimatedWidth = Mathf.Max(140f, contentWidth);
        float height = _richLabelStyle.CalcHeight(content, estimatedWidth);

        Rect rect = GUILayoutUtility.GetRect(0f, height + 14f, GUILayout.ExpandWidth(true));

        Color previousColor = GUI.color;
        if (index == _selectedLogIndex)
            GUI.color = new Color(0.75f, 0.85f, 1f, 1f);

        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;

        Rect labelRect = new Rect(rect.x + 6f, rect.y + 6f, Mathf.Max(0f, rect.width - 12f), rect.height - 12f);
        GUI.Label(labelRect, content, _richLabelStyle);

        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 0 &&
            rect.Contains(Event.current.mousePosition))
        {
            _selectedLogIndex = index;
            FocusEntry(entry);

            if (Event.current.clickCount >= 2)
                OpenEntryScript(entry);

            Event.current.Use();
        }

        return rect.height;
    }

    private void OpenEntryScript(DebugEntry entry)
    {
#if UNITY_EDITOR
        if (!TryGetEntryScriptLocation(entry, out UnityEditor.MonoScript script, out int lineNumber, out int columnNumber))
            return;

        UnityEditor.AssetDatabase.OpenAsset(script, Mathf.Max(1, lineNumber), Mathf.Max(1, columnNumber));
#endif
    }

#if UNITY_EDITOR
    private bool TryGetEntryScriptLocation(DebugEntry entry, out UnityEditor.MonoScript script, out int lineNumber, out int columnNumber)
    {
        script = null;
        lineNumber = 1;
        columnNumber = 1;

        if (entry == null || string.IsNullOrWhiteSpace(entry.CallerFilePath))
            return false;

        lineNumber = Mathf.Max(1, entry.LineNumber);
        columnNumber = Mathf.Max(1, entry.CallerColumn);

        if (TryConvertCallerPathToAssetPath(entry.CallerFilePath, out string assetPath))
        {
            script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(assetPath);
            if (script != null)
                return true;
        }

        return TryFindScriptByFileName(entry.CallerFilePath, out script);
    }

    private bool TryConvertCallerPathToAssetPath(string callerFilePath, out string assetPath)
    {
        assetPath = string.Empty;

        if (string.IsNullOrWhiteSpace(callerFilePath))
            return false;

        string normalizedPath = callerFilePath.Replace('\\', '/');

        int assetsIndex = normalizedPath.LastIndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
        if (assetsIndex >= 0)
        {
            assetPath = normalizedPath.Substring(assetsIndex + 1);
            return true;
        }

        int packagesIndex = normalizedPath.LastIndexOf("/Packages/", StringComparison.OrdinalIgnoreCase);
        if (packagesIndex >= 0)
        {
            assetPath = normalizedPath.Substring(packagesIndex + 1);
            return true;
        }

        string projectAssetsPath = Application.dataPath.Replace('\\', '/');
        if (normalizedPath.StartsWith(projectAssetsPath, StringComparison.OrdinalIgnoreCase))
        {
            assetPath = "Assets" + normalizedPath.Substring(projectAssetsPath.Length);
            return true;
        }

        return false;
    }

    private bool TryFindScriptByFileName(string callerFilePath, out UnityEditor.MonoScript script)
    {
        script = null;

        string fileName = Path.GetFileNameWithoutExtension(callerFilePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        string[] guids = UnityEditor.AssetDatabase.FindAssets($"{fileName} t:MonoScript");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.Equals(Path.GetFileNameWithoutExtension(assetPath), fileName, StringComparison.Ordinal))
                continue;

            UnityEditor.MonoScript found = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(assetPath);
            if (found == null)
                continue;

            script = found;
            return true;
        }

        return false;
    }
#endif

    private void FocusEntry(DebugEntry entry)
    {
        if (entry == null)
            return;

        GameObject targetGameObject = null;

        if (entry.Context is GameObject go)
        {
            targetGameObject = go;
            _focusedGameObjectId = go.GetInstanceID();
            _focusedComponentId = 0;
            _focusedObjectName = go.name;
            _focusedComponentName = string.Empty;
            PrepareSelectionExpansion(go.transform, true);
        }
        else if (entry.Context is Component component)
        {
            targetGameObject = component.gameObject;
            _focusedGameObjectId = component.gameObject.GetInstanceID();
            _focusedComponentId = component.GetInstanceID();
            _focusedObjectName = component.gameObject.name;
            _focusedComponentName = component.GetType().Name;
            PrepareSelectionExpansion(component.transform, true);
        }

        if (targetGameObject == null)
            return;

#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = targetGameObject;
        UnityEditor.EditorGUIUtility.PingObject(targetGameObject);
#endif
    }

    private void ExpandSelectionPath(Transform target, bool includeTargetDetails)
    {
        Transform current = target;

        if (current == null)
            return;

        if (includeTargetDetails)
            _expandedComponents.Add(current.gameObject.GetInstanceID());

        while (current.parent != null)
        {
            Transform parent = current.parent;
            int parentId = parent.gameObject.GetInstanceID();
            _expandedComponents.Add(parentId);
            _expandedChildren.Add(parentId);
            current = parent;
        }
    }

    private bool ShouldDisplayEntry(DebugConsoleManager manager, DebugEntry entry)
    {
        if (entry == null)
            return false;

        if (!manager.IsAllowed(entry.Type, entry.GameObjectId, entry.ComponentId))
            return false;

        if (_focusedComponentId != 0)
        {
            if (entry.ComponentId != _focusedComponentId)
                return false;
        }
        else if (_focusedGameObjectId != 0)
        {
            if (entry.GameObjectId != _focusedGameObjectId)
                return false;
        }

        if (string.IsNullOrWhiteSpace(_logSearch))
            return true;

        string searchPool = $"{entry.Message} {entry.SourceName} {entry.MemberName} {entry.Type} {entry.Time}";
        return ContainsIgnoreCase(searchPool, _logSearch);
    }

    private void ToggleGameObjectFocus(GameObject go)
    {
        if (go == null)
            return;

        int id = go.GetInstanceID();

        if (_focusedGameObjectId == id && _focusedComponentId == 0)
        {
            ClearFocus();
            return;
        }

        _focusedGameObjectId = id;
        _focusedComponentId = 0;
        _focusedObjectName = go.name;
        _focusedComponentName = string.Empty;

        PrepareSelectionExpansion(go.transform, true);
    }

    private void ToggleComponentFocus(Component component)
    {
        if (component == null)
            return;

        int componentId = component.GetInstanceID();

        if (_focusedComponentId == componentId)
        {
            ClearFocus();
            return;
        }

        _focusedGameObjectId = component.gameObject.GetInstanceID();
        _focusedComponentId = componentId;
        _focusedObjectName = component.gameObject.name;
        _focusedComponentName = component.GetType().Name;

        PrepareSelectionExpansion(component.transform, true);
    }

    private void PrepareSelectionExpansion(Transform target, bool includeDetails)
    {
        if (target == null)
            return;

        if (_collapsePreviousOnSelection)
            PreserveExpansionWithinTopLevelRoot(target);

        ExpandSelectionPath(target, includeDetails);
    }

    private void PreserveExpansionWithinTopLevelRoot(Transform target)
    {
        Transform topLevelRoot = GetTopLevelRoot(target);

        if (topLevelRoot == null)
        {
            _expandedComponents.Clear();
            _expandedChildren.Clear();
            return;
        }

        HashSet<int> allowedIds = new HashSet<int>();
        CollectSubtreeIds(topLevelRoot, allowedIds);

        _expandedComponents.RemoveWhere(id => !allowedIds.Contains(id));
        _expandedChildren.RemoveWhere(id => !allowedIds.Contains(id));
    }

    private Transform GetTopLevelRoot(Transform target)
    {
        if (target == null)
            return null;

        Transform current = target;
        while (current.parent != null)
            current = current.parent;

        return current;
    }

    private void CollectSubtreeIds(Transform node, HashSet<int> ids)
    {
        if (node == null || ids == null)
            return;

        ids.Add(node.gameObject.GetInstanceID());

        for (int i = 0; i < node.childCount; i++)
            CollectSubtreeIds(node.GetChild(i), ids);
    }

    private void ClearFocus()
    {
        _focusedGameObjectId = 0;
        _focusedComponentId = 0;
        _focusedObjectName = string.Empty;
        _focusedComponentName = string.Empty;
    }

    private string GetFocusLabel()
    {
        if (_focusedComponentId != 0)
            return $"Focus : {_focusedObjectName}/{_focusedComponentName}";

        if (_focusedGameObjectId != 0)
            return $"Focus : {_focusedObjectName} (All Components)";

        return "Focus : All";
    }

    private string GetFooterFocusLabel()
    {
        if (_focusedComponentId != 0)
        {
            string objectName = TrimFooterFocusSegment(_focusedObjectName);
            string componentName = TrimFooterFocusSegment(_focusedComponentName);

            if (string.Equals(_focusedObjectName, _focusedComponentName, StringComparison.Ordinal))
                return $"Focus : {objectName}";

            return $"Focus : {objectName} / {componentName}";
        }

        if (_focusedGameObjectId != 0)
            return $"Focus : {TrimFooterFocusSegment(_focusedObjectName)}";

        return "Focus : All";
    }

    private string TrimFooterFocusSegment(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length > FooterFocusSegmentMaxLength
            ? value.Substring(0, FooterFocusSegmentMaxLength) + "..."
            : value;
    }

    private string GetFocusSuffix()
    {
        if (_focusedComponentId != 0)
            return $"({_focusedObjectName}/{_focusedComponentName})";

        if (_focusedGameObjectId != 0)
            return $"({_focusedObjectName})";

        return string.Empty;
    }

    private GUIStyle CreateRowStyle(Color backgroundColor)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, backgroundColor);
        texture.Apply();

        return new GUIStyle(GUI.skin.box)
        {
            normal = { background = texture },
            border = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 1, 1),
            padding = new RectOffset(3, 3, 1, 1),
            alignment = TextAnchor.MiddleLeft
        };
    }

    private GUIStyle CreateButtonStyle(Color backgroundColor, Color textColor, bool bold, TextAnchor alignment)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, backgroundColor);
        texture.Apply();

        GUIStyle style = new GUIStyle(_linkButtonStyle)
        {
            alignment = alignment,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            fixedHeight = HierarchyRowHeight
        };

        style.normal.background = texture;
        style.hover.background = texture;
        style.active.background = texture;
        style.focused.background = texture;
        style.onNormal.background = texture;
        style.onHover.background = texture;
        style.onActive.background = texture;
        style.onFocused.background = texture;
        style.normal.textColor = textColor;
        style.hover.textColor = textColor;
        style.active.textColor = textColor;
        style.focused.textColor = textColor;
        style.onNormal.textColor = textColor;
        style.onHover.textColor = textColor;
        style.onActive.textColor = textColor;
        style.onFocused.textColor = textColor;
        return style;
    }

    private GUIStyle GetHierarchyRowStyle(bool isObjectFocused, bool isComponentParentFocused, bool isComponentFocused)
    {
        if (isComponentFocused)
            return _componentFocusedRowStyle;

        if (isObjectFocused)
            return _objectFocusedRowStyle;

        if (isComponentParentFocused)
            return _objectParentFocusedRowStyle;

        return GUIStyle.none;
    }

    private bool IsFocusedObjectParent(int gameObjectId)
    {
        return _focusedGameObjectId == gameObjectId && _focusedComponentId != 0;
    }

    private bool IsObjectFocused(int gameObjectId)
    {
        return _focusedGameObjectId == gameObjectId && _focusedComponentId == 0;
    }

    private bool IsComponentFocused(int componentId)
    {
        return _focusedComponentId == componentId;
    }

    private GUIStyle GetObjectButtonStyle(bool objectEnabled, bool isObjectFocused, bool isComponentParentFocused)
    {
        if (isObjectFocused)
            return _objectSelectedButtonStyle;

        if (isComponentParentFocused)
            return _parentSelectedButtonStyle;

        return objectEnabled ? _linkButtonStyle : _disabledButtonStyle;
    }

    private GUIStyle GetComponentButtonStyle(bool objectEnabled, bool isComponentFocused)
    {
        if (isComponentFocused)
            return _componentSelectedButtonStyle;

        return objectEnabled ? _linkButtonStyle : _disabledButtonStyle;
    }

    private void ToggleExpandedSet(HashSet<int> set, int id)
    {
        if (set.Contains(id))
            set.Remove(id);
        else
            set.Add(id);
    }

    private int GetEnabledTypeCount(DebugConsoleManager manager)
    {
        int count = 0;
        foreach (DebugType type in Enum.GetValues(typeof(DebugType)))
        {
            if (manager.GetTypeEnabled(type))
                count++;
        }

        return count;
    }

    private bool ShouldShowGameObject(GameObject go)
    {
        if (go == null)
            return false;

        if (string.IsNullOrWhiteSpace(_hierarchySearch))
            return true;

        if (ContainsIgnoreCase(go.name, _hierarchySearch))
            return true;

        if (HasMatchingComponent(go, _hierarchySearch))
            return true;

        for (int i = 0; i < go.transform.childCount; i++)
        {
            if (ShouldShowGameObject(go.transform.GetChild(i).gameObject))
                return true;
        }

        return false;
    }

    private bool HasVisibleChildren(GameObject go)
    {
        for (int i = 0; i < go.transform.childCount; i++)
        {
            if (ShouldShowGameObject(go.transform.GetChild(i).gameObject))
                return true;
        }

        return false;
    }

    private bool HasVisibleComponents(Component[] components)
    {
        if (components == null || components.Length == 0)
            return false;

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            if (_hideTransform && component is Transform)
                continue;

            return true;
        }

        return false;
    }

    private bool HasMatchingComponent(GameObject go, string query)
    {
        Component[] components = go.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            if (_hideTransform && component is Transform)
                continue;

            if (ContainsIgnoreCase(component.GetType().Name, query))
                return true;
        }

        return false;
    }

    private bool ShouldShowComponent(Component component, string ownerName)
    {
        if (component == null)
            return false;

        if (_hideTransform && component is Transform)
            return false;

        if (string.IsNullOrWhiteSpace(_hierarchySearch))
            return true;

        if (ContainsIgnoreCase(ownerName, _hierarchySearch))
            return true;

        return ContainsIgnoreCase(component.GetType().Name, _hierarchySearch);
    }


    private void DrawToolbarToggleGroup(DebugConsoleManager manager)
    {
        bool global = GUILayout.Toggle(manager.GlobalEnabled, "Global", GUILayout.Width(80f));
        if (global != manager.GlobalEnabled)
            manager.GlobalEnabled = global;

        bool mirror = GUILayout.Toggle(manager.MirrorToUnityConsole, "Mirror Unity", GUILayout.Width(110f));
        if (mirror != manager.MirrorToUnityConsole)
            manager.MirrorToUnityConsole = mirror;

        bool autoScroll = GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100f));
        if (autoScroll != _autoScroll)
            _autoScroll = autoScroll;

        bool hideTransform = GUILayout.Toggle(_hideTransform, "Hide Transform", GUILayout.Width(120f));
        if (hideTransform != _hideTransform)
            _hideTransform = hideTransform;

        bool collapsePrevious = GUILayout.Toggle(_collapsePreviousOnSelection, "Collapse Prev", GUILayout.Width(120f));
        if (collapsePrevious != _collapsePreviousOnSelection)
            _collapsePreviousOnSelection = collapsePrevious;
    }

    private void DrawToolbarActionGroup(DebugConsoleManager manager, string typeButtonLabel)
    {
        if (GUILayout.Button(typeButtonLabel, _toolbarButtonStyle, GUILayout.Width(160f)))
            _showTypeFilterPanel = !_showTypeFilterPanel;

        if (GUILayout.Button("All Types On", GUILayout.Width(100f)))
            manager.SetAllTypes(true);

        if (GUILayout.Button("All Types Off", GUILayout.Width(100f)))
            manager.SetAllTypes(false);

        if (GUILayout.Button("Clear Logs", GUILayout.Width(100f)))
            manager.ClearLogs();

        if (GUILayout.Button("Clear Focus", GUILayout.Width(100f)))
            ClearFocus();
    }

    private void DrawToolbarInfoGroup(DebugConsoleManager manager, bool expanded)
    {
        GUILayout.Label(GetFocusLabel(), _toolbarInfoLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(expanded ? 34f : 18f));
        GUILayout.Space(8f);
        GUILayout.Label($"Count : {manager.Entries.Count}", _toolbarInfoLabelStyle, GUILayout.Width(expanded ? 120f : 110f), GUILayout.MinHeight(expanded ? 34f : 18f));
    }

    private void DrawHierarchySearchField(float fieldWidth, float labelWidth)
    {
        GUILayout.Label("Hierarchy Search", GUILayout.Width(labelWidth));

        GUI.SetNextControlName(HierarchySearchControlName);
        string nextValue = GUILayout.TextField(_hierarchySearch ?? string.Empty, _searchTextFieldStyle, GUILayout.Width(fieldWidth), GUILayout.Height(24f));
        if (!string.Equals(nextValue, _hierarchySearch, StringComparison.Ordinal))
            _hierarchySearch = nextValue;

        Rect fieldRect = GUILayoutUtility.GetLastRect();
        HandleSearchFieldFocus(HierarchySearchControlName, SearchFieldFocus.Hierarchy, fieldRect);
    }

    private void DrawLogSearchField(float labelWidth)
    {
        GUILayout.Label("Log Search", GUILayout.Width(labelWidth));

        GUI.SetNextControlName(LogSearchControlName);
        string nextValue = GUILayout.TextField(_logSearch ?? string.Empty, _searchTextFieldStyle, GUILayout.ExpandWidth(true), GUILayout.Height(24f));
        if (!string.Equals(nextValue, _logSearch, StringComparison.Ordinal))
            _logSearch = nextValue;

        Rect fieldRect = GUILayoutUtility.GetLastRect();
        HandleSearchFieldFocus(LogSearchControlName, SearchFieldFocus.Log, fieldRect);

        if (GUILayout.Button("Clear Search", GUILayout.Width(100f)))
        {
            _hierarchySearch = string.Empty;
            _logSearch = string.Empty;
            GUI.FocusControl(string.Empty);
            _activeSearchField = SearchFieldFocus.None;
        }
    }

    private void HandleSearchFieldFocus(string controlName, SearchFieldFocus focusField, Rect fieldRect)
    {
        Event current = Event.current;

        if (current.type == EventType.MouseDown && fieldRect.Contains(current.mousePosition))
            _activeSearchField = focusField;

        string focusedControl = GUI.GetNameOfFocusedControl();
        bool isFocused = string.Equals(focusedControl, controlName, StringComparison.Ordinal);

        if (isFocused)
        {
            _searchFieldFocusedThisFrame = true;
            _lastFocusedSearchFieldRect = fieldRect;
            _activeSearchField = focusField;
            return;
        }

        if (_activeSearchField == focusField && current.type == EventType.Repaint)
        {
            GUI.FocusControl(controlName);
            _searchFieldFocusedThisFrame = true;
            _lastFocusedSearchFieldRect = fieldRect;
        }
    }

    

    

    

    

    

    

    private float GetTopAreaWidth()
    {
        return Mathf.Max(320f, _windowRect.width - 36f);
    }

    private int GetTopLayoutLevel(float availableWidth)
    {
        if (availableWidth >= 1500f)
            return 1;

        if (availableWidth >= 980f)
            return 2;

        return 3;
    }

    private float GetSearchFieldWidth(float availableWidth, bool stacked)
    {
        if (stacked)
            return Mathf.Max(180f, availableWidth - 130f);

        return Mathf.Clamp((availableWidth * 0.28f), 180f, 280f);
    }

    private int GetVisibleEntryCount(DebugConsoleManager manager)
    {
        int count = 0;
        IReadOnlyList<DebugEntry> entries = manager.Entries;

        for (int i = 0; i < entries.Count; i++)
        {
            if (ShouldDisplayEntry(manager, entries[i]))
                count++;
        }

        return count;
    }

    private void UpdateHierarchyButtonWidths()
    {
    }

    private void CollectHierarchyButtonWidths(GameObject go, ref float maxNameWidth)
    {
        if (go == null || !ShouldShowGameObject(go))
            return;

        maxNameWidth = Mathf.Max(maxNameWidth, _linkButtonStyle.CalcSize(new GUIContent($"▶ {GetDisplayName(go.name)}")).x);

        Component[] components = go.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (!ShouldShowComponent(component, go.name))
                continue;

            maxNameWidth = Mathf.Max(maxNameWidth, _linkButtonStyle.CalcSize(new GUIContent($"▶ {GetDisplayName(component.GetType().Name)}")).x);
        }

        for (int i = 0; i < go.transform.childCount; i++)
            CollectHierarchyButtonWidths(go.transform.GetChild(i).gameObject, ref maxNameWidth);
    }

    private string GetDisplayName(string source)
    {
        if (string.IsNullOrEmpty(source))
            return "(Null)";

        return source.Length > MaxDisplayNameLength ? source.Substring(0, MaxDisplayNameLength) + "..." : source;
    }

    private bool IsNearBottom(float maxScrollY)
    {
        if (maxScrollY <= 0f)
            return true;

        float remaining = maxScrollY - _logScroll.y;
        return remaining <= Mathf.Max(maxScrollY * 0.05f, 32f);
    }

    private bool ContainsIgnoreCase(string source, string keyword)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(keyword))
            return false;

        return source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
