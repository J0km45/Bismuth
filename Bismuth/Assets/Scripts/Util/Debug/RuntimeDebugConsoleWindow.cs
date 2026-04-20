using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RuntimeDebugConsoleWindow : MonoBehaviour
{
    private const string PrefKeyPrefix = "DebugConsole.RuntimeWindow";
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

    private bool _showTypeFilterPanel;

    private readonly DebugConsoleFocusState _focusState = new();
    private readonly List<DebugEntry> _visibleEntriesCache = new();
    private int _cachedManagerChangeVersion = -1;
    private string _cachedHierarchySearch = string.Empty;
    private string _cachedLogSearch = string.Empty;
    private int _cachedFocusedGameObjectId = -1;
    private int _cachedFocusedComponentId = -1;
    private bool _viewStateDirty;

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
        LoadViewState();
    }

    private void OnDisable()
    {
        FlushViewStateIfDirty(force: true);
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void LoadViewState()
    {
        _visible = DebugConsolePreferenceStore.GetBool($"{PrefKeyPrefix}.Visible", _visible);
        _autoScroll = DebugConsolePreferenceStore.GetBool($"{PrefKeyPrefix}.AutoScroll", _autoScroll);
        _hideTransform = DebugConsolePreferenceStore.GetBool($"{PrefKeyPrefix}.HideTransform", _hideTransform);
        _collapsePreviousOnSelection = DebugConsolePreferenceStore.GetBool($"{PrefKeyPrefix}.CollapsePrev", _collapsePreviousOnSelection);
        _showTypeFilterPanel = DebugConsolePreferenceStore.GetBool($"{PrefKeyPrefix}.ShowTypeFilterPanel", _showTypeFilterPanel);
        _hierarchyPanelWidth = DebugConsolePreferenceStore.GetFloat($"{PrefKeyPrefix}.HierarchyPanelWidth", _hierarchyPanelWidth);
        _windowRect = DebugConsolePreferenceStore.GetRect($"{PrefKeyPrefix}.WindowRect", _windowRect);
    }

    private void SaveViewState()
    {
        DebugConsolePreferenceStore.SetBool($"{PrefKeyPrefix}.Visible", _visible);
        DebugConsolePreferenceStore.SetBool($"{PrefKeyPrefix}.AutoScroll", _autoScroll);
        DebugConsolePreferenceStore.SetBool($"{PrefKeyPrefix}.HideTransform", _hideTransform);
        DebugConsolePreferenceStore.SetBool($"{PrefKeyPrefix}.CollapsePrev", _collapsePreviousOnSelection);
        DebugConsolePreferenceStore.SetBool($"{PrefKeyPrefix}.ShowTypeFilterPanel", _showTypeFilterPanel);
        DebugConsolePreferenceStore.SetFloat($"{PrefKeyPrefix}.HierarchyPanelWidth", _hierarchyPanelWidth);
        DebugConsolePreferenceStore.SetRect($"{PrefKeyPrefix}.WindowRect", _windowRect);
        _viewStateDirty = false;
    }

    private void MarkViewStateDirty()
    {
        _viewStateDirty = true;
    }

    private void FlushViewStateIfDirty(bool force = false)
    {
        if (!_viewStateDirty && !force)
            return;

        SaveViewState();
    }

    private void InvalidateVisibleEntriesCache()
    {
        _cachedManagerChangeVersion = -1;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _stylesDirty = true;
        _titleStyle = null;
        _expandedComponents.Clear();
        _expandedChildren.Clear();
        _selectedLogIndex = -1;
        _hierarchyScroll = Vector2.zero;
        ClearFocus();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            _visible = !_visible;
            if (_visible)
                MarkViewStateDirty();
            else
                SaveViewState();
        }
    }

    private void OnGUI()
    {
        if (!_visible)
            return;

        InitStyles();
        Rect previousRect = _windowRect;
        _windowRect = GUI.Window(91357, _windowRect, DrawWindow, "Runtime Debug Console");
        if (previousRect != _windowRect)
            MarkViewStateDirty();
    }

    private void InitStyles()
    {
        if (!_stylesDirty && _titleStyle != null)
            return;

        _stylesDirty = false;

        DebugConsoleStyleSet styles = DebugConsoleStyleFactory.Create(
            GUI.skin.label,
            GUI.skin.label,
            GUI.skin.box,
            GUI.skin.textField,
            GUI.skin.button,
            HierarchyRowHeight,
            HierarchyFoldoutSize,
            _solidTexture);

        _titleStyle = styles.TitleStyle;
        _boxStyle = styles.BoxStyle;
        _richLabelStyle = styles.RichLabelStyle;
        _dimLabelStyle = styles.DimLabelStyle;
        _searchTextFieldStyle = styles.SearchTextFieldStyle;
        _linkButtonStyle = styles.LinkButtonStyle;
        _disabledButtonStyle = styles.DisabledButtonStyle;
        _objectSelectedButtonStyle = styles.ObjectSelectedButtonStyle;
        _componentSelectedButtonStyle = styles.ComponentSelectedButtonStyle;
        _parentSelectedButtonStyle = styles.ParentSelectedButtonStyle;
        _foldoutButtonStyle = styles.FoldoutButtonStyle;
        _toolbarButtonStyle = styles.ToolbarButtonStyle;
        _toolbarInfoLabelStyle = styles.ToolbarInfoLabelStyle;
        _toolbarInfoRightLabelStyle = styles.ToolbarInfoRightLabelStyle;
        _footerLeftLabelStyle = styles.FooterLeftLabelStyle;
        _footerRightLabelStyle = styles.FooterRightLabelStyle;
        _objectFocusedRowStyle = styles.ObjectFocusedRowStyle;
        _objectParentFocusedRowStyle = styles.ObjectParentFocusedRowStyle;
        _componentFocusedRowStyle = styles.ComponentFocusedRowStyle;
        _solidTexture = styles.SolidTexture;
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
        FlushViewStateIfDirty();

        Rect previousRect = _windowRect;
        GUI.DragWindow(new Rect(0, 0, 10000, 24));
        if (previousRect.position != _windowRect.position || previousRect.size != _windowRect.size)
            MarkViewStateDirty();
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
                {
                    manager.SetTypeEnabled(type, next);
                    MarkViewStateDirty();
                }
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
        DebugConsoleHierarchyRenderContext context = new DebugConsoleHierarchyRenderContext
        {
            Manager = manager,
            PanelWidth = panelWidth,
            Scroll = _hierarchyScroll,
            HierarchySearch = _hierarchySearch,
            TitleStyle = _titleStyle,
            BoxStyle = _boxStyle,
            LinkButtonStyle = _linkButtonStyle,
            FoldoutButtonStyle = _foldoutButtonStyle,
            FooterLeftLabelStyle = _footerLeftLabelStyle,
            FooterRightLabelStyle = _footerRightLabelStyle,
            HierarchyRowHeight = HierarchyRowHeight,
            HierarchyToggleSize = HierarchyToggleSize,
            HierarchyFoldoutSize = HierarchyFoldoutSize,
            HierarchyRowContentRightReserve = HierarchyRowContentRightReserve,
            MaxHierarchyIndentPenalty = MaxHierarchyIndentPenalty,
            ExpandedComponents = _expandedComponents,
            ExpandedChildren = _expandedChildren,
            GetVisibleEntryCount = () => GetVisibleEntryCount(manager),
            GetFocusLabel = GetFocusLabel,
            GetFooterFocusLabel = GetFooterFocusLabel,
            ShouldShowGameObject = ShouldShowGameObject,
            HasVisibleComponents = HasVisibleComponents,
            HasVisibleChildren = HasVisibleChildren,
            HasMatchingComponent = HasMatchingComponent,
            ShouldShowComponent = ShouldShowComponent,
            IsObjectFocused = IsObjectFocused,
            IsFocusedObjectParent = IsFocusedObjectParent,
            IsComponentFocused = IsComponentFocused,
            GetHierarchyRowStyle = GetHierarchyRowStyle,
            GetObjectButtonStyle = GetObjectButtonStyle,
            GetComponentButtonStyle = GetComponentButtonStyle,
            GetDisplayName = GetDisplayName,
            ToggleGameObjectFocus = ToggleGameObjectFocus,
            ToggleComponentFocus = ToggleComponentFocus,
            ToggleExpandedSet = ToggleExpandedSet,
            SaveState = MarkViewStateDirty
        };

        DebugConsoleHierarchyRenderer.Draw(context);
        _hierarchyScroll = context.Scroll;
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
        {
            manager.SetGameObjectEnabled(go, nextObjectEnabled);
            MarkViewStateDirty();
        }

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
                {
                    manager.SetComponentEnabled(component, nextComponentEnabled);
                    MarkViewStateDirty();
                }

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
        DebugConsoleLogRenderContext context = new DebugConsoleLogRenderContext
        {
            PanelWidth = panelWidth,
            Scroll = _logScroll,
            AutoScroll = _autoScroll,
            LastLogContentHeight = _lastLogContentHeight,
            LastLogViewportHeight = _lastLogViewportHeight,
            LastMaxLogScrollY = _lastMaxLogScrollY,
            SelectedLogIndex = _selectedLogIndex,
            Entries = GetVisibleEntries(manager),
            TitleStyle = _titleStyle,
            BoxStyle = _boxStyle,
            RichLabelStyle = _richLabelStyle,
            GetFocusSuffix = GetFocusSuffix,
            ShouldDisplayEntry = _ => true,
            FocusEntry = FocusEntry,
            OpenEntryScript = OpenEntryScript
        };

        DebugConsoleLogRenderer.Draw(context);
        _logScroll = context.Scroll;
        _lastLogContentHeight = context.LastLogContentHeight;
        _lastLogViewportHeight = context.LastLogViewportHeight;
        _lastMaxLogScrollY = context.LastMaxLogScrollY;
        _selectedLogIndex = context.SelectedLogIndex;
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
            _focusState.FocusGameObject(go.GetInstanceID(), go.name);
            PrepareSelectionExpansion(go.transform, true);
            InvalidateVisibleEntriesCache();
        }
        else if (entry.Context is Component component)
        {
            targetGameObject = component.gameObject;
            _focusState.FocusComponent(component.gameObject.GetInstanceID(), component.GetInstanceID(), component.gameObject.name, component.GetType().Name);
            PrepareSelectionExpansion(component.transform, true);
            InvalidateVisibleEntriesCache();
        }

        if (targetGameObject == null)
            return;

        SyncUnityHierarchySelection(targetGameObject);
    }

    private void SyncUnityHierarchySelection(GameObject targetGameObject)
    {
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

        if (_focusState.HasComponentFocus)
        {
            if (entry.ComponentId != _focusState.FocusedComponentId)
                return false;
        }
        else if (_focusState.HasGameObjectFocus)
        {
            if (entry.GameObjectId != _focusState.FocusedGameObjectId)
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

        if (_focusState.IsObjectFocused(id))
        {
            ClearFocus();
            return;
        }

        _focusState.FocusGameObject(id, go.name);

        PrepareSelectionExpansion(go.transform, true);
        InvalidateVisibleEntriesCache();
        SyncUnityHierarchySelection(go);
    }

    private void ToggleComponentFocus(Component component)
    {
        if (component == null)
            return;

        int componentId = component.GetInstanceID();

        if (_focusState.IsComponentFocused(componentId))
        {
            ClearFocus();
            return;
        }

        _focusState.FocusComponent(component.gameObject.GetInstanceID(), componentId, component.gameObject.name, component.GetType().Name);

        PrepareSelectionExpansion(component.transform, true);
        InvalidateVisibleEntriesCache();
        SyncUnityHierarchySelection(component.gameObject);
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
        _focusState.Clear();
        InvalidateVisibleEntriesCache();
    }

    private string GetFocusLabel()
    {
        return _focusState.GetLabel();
    }

    private string GetFooterFocusLabel()
    {
        return _focusState.GetFooterLabel(FooterFocusSegmentMaxLength);
    }

    private string GetFocusSuffix()
    {
        return _focusState.GetSuffix();
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
        return _focusState.IsFocusedObjectParent(gameObjectId);
    }

    private bool IsObjectFocused(int gameObjectId)
    {
        return _focusState.IsObjectFocused(gameObjectId);
    }

    private bool IsComponentFocused(int componentId)
    {
        return _focusState.IsComponentFocused(componentId);
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
        {
            manager.GlobalEnabled = global;
            InvalidateVisibleEntriesCache();
            MarkViewStateDirty();
        }

        bool mirror = GUILayout.Toggle(manager.MirrorToUnityConsole, "Mirror Unity", GUILayout.Width(110f));
        if (mirror != manager.MirrorToUnityConsole)
        {
            manager.MirrorToUnityConsole = mirror;
            MarkViewStateDirty();
        }

        bool autoScroll = GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100f));
        if (autoScroll != _autoScroll)
        {
            _autoScroll = autoScroll;
            MarkViewStateDirty();
        }

        bool hideTransform = GUILayout.Toggle(_hideTransform, "Hide Transform", GUILayout.Width(120f));
        if (hideTransform != _hideTransform)
        {
            _hideTransform = hideTransform;
            MarkViewStateDirty();
        }

        bool collapsePrevious = GUILayout.Toggle(_collapsePreviousOnSelection, "Collapse Prev", GUILayout.Width(120f));
        if (collapsePrevious != _collapsePreviousOnSelection)
        {
            _collapsePreviousOnSelection = collapsePrevious;
            MarkViewStateDirty();
        }
    }

    private void DrawToolbarActionGroup(DebugConsoleManager manager, string typeButtonLabel)
    {
        if (GUILayout.Button(typeButtonLabel, _toolbarButtonStyle, GUILayout.Width(160f)))
        {
            _showTypeFilterPanel = !_showTypeFilterPanel;
            MarkViewStateDirty();
        }

        if (GUILayout.Button("All Types On", GUILayout.Width(100f)))
        {
            manager.SetAllTypes(true);
            InvalidateVisibleEntriesCache();
            MarkViewStateDirty();
        }

        if (GUILayout.Button("All Types Off", GUILayout.Width(100f)))
        {
            manager.SetAllTypes(false);
            InvalidateVisibleEntriesCache();
            MarkViewStateDirty();
        }

        if (GUILayout.Button("Reset Filters", GUILayout.Width(110f)))
            ResetFilterState(manager);

        if (GUILayout.Button("Clear Logs", GUILayout.Width(100f)))
        {
            manager.ClearLogs();
            InvalidateVisibleEntriesCache();
        }

        if (GUILayout.Button("Clear Focus", GUILayout.Width(100f)))
            ClearFocus();
    }

    private void ResetFilterState(DebugConsoleManager manager)
    {
        if (manager == null)
            return;

        manager.ResetAllFiltersToDefault();
        _hierarchySearch = string.Empty;
        _logSearch = string.Empty;
        _selectedLogIndex = -1;
        _expandedComponents.Clear();
        _expandedChildren.Clear();
        ClearFocus();
        GUI.FocusControl(null);
        InvalidateVisibleEntriesCache();
        MarkViewStateDirty();
    }

    private void DrawToolbarInfoGroup(DebugConsoleManager manager, bool expanded)
    {
        GUILayout.Label(GetFocusLabel(), _toolbarInfoLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(expanded ? 34f : 18f));
        GUILayout.Space(8f);
        GUILayout.Label($"Count : {GetVisibleEntryCount(manager)}", _toolbarInfoLabelStyle, GUILayout.Width(expanded ? 120f : 110f), GUILayout.MinHeight(expanded ? 34f : 18f));
    }

    private void DrawHierarchySearchField(float fieldWidth, float labelWidth)
    {
        GUILayout.Label("Hierarchy Search", GUILayout.Width(labelWidth));
        string nextSearch = GUILayout.TextField(_hierarchySearch, _searchTextFieldStyle, GUILayout.Width(fieldWidth));
        if (!string.Equals(nextSearch, _hierarchySearch, StringComparison.Ordinal))
        {
            _hierarchySearch = nextSearch;
            InvalidateVisibleEntriesCache();
        }
    }

    private void DrawLogSearchField(float labelWidth)
    {
        GUILayout.Label("Log Search", GUILayout.Width(labelWidth));
        _logSearch = GUILayout.TextField(_logSearch, _searchTextFieldStyle, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("Clear Search", GUILayout.Width(100f)))
        {
            _hierarchySearch = string.Empty;
            _logSearch = string.Empty;
            GUI.FocusControl(null);
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
        return GetVisibleEntries(manager).Count;
    }

    private IReadOnlyList<DebugEntry> GetVisibleEntries(DebugConsoleManager manager)
    {
        if (manager == null)
            return Array.Empty<DebugEntry>();

        if (_cachedManagerChangeVersion == manager.ChangeVersion &&
            string.Equals(_cachedHierarchySearch, _hierarchySearch, StringComparison.Ordinal) &&
            string.Equals(_cachedLogSearch, _logSearch, StringComparison.Ordinal) &&
            _cachedFocusedGameObjectId == _focusState.FocusedGameObjectId &&
            _cachedFocusedComponentId == _focusState.FocusedComponentId)
        {
            return _visibleEntriesCache;
        }

        _visibleEntriesCache.Clear();
        IReadOnlyList<DebugEntry> entries = manager.Entries;

        for (int i = 0; i < entries.Count; i++)
        {
            DebugEntry entry = entries[i];
            if (ShouldDisplayEntry(manager, entry))
                _visibleEntriesCache.Add(entry);
        }

        _cachedManagerChangeVersion = manager.ChangeVersion;
        _cachedHierarchySearch = _hierarchySearch;
        _cachedLogSearch = _logSearch;
        _cachedFocusedGameObjectId = _focusState.FocusedGameObjectId;
        _cachedFocusedComponentId = _focusState.FocusedComponentId;
        return _visibleEntriesCache;
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
