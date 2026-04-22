#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugConsoleEditorWindow : EditorWindow
{
    private Vector2 _hierarchyScroll;
    private Vector2 _logScroll;
    private Vector2 _typeFilterScroll;
    private Vector2 _detailScroll;

    private string _hierarchySearch = string.Empty;
    private string _logSearch = string.Empty;
    private string _pendingHierarchySearch = string.Empty;
    private string _pendingLogSearch = string.Empty;
    private double _hierarchySearchApplyTime;
    private double _logSearchApplyTime;

    private bool _autoScroll = true;
    private bool _hideTransform = true;
    private bool _collapsePreviousOnSelection = true;
    private bool _collapseLogs = true;
    private bool _showTypeFilterPanel;
    private bool _showLogDetails = true;
    private bool _stackTraceFoldout = true;

    private int _focusedGameObjectId;
    private int _focusedComponentId;
    private string _focusedObjectName = string.Empty;
    private string _focusedComponentName = string.Empty;
    private string _focusedSnapshotGameObjectKey = string.Empty;
    private string _focusedSnapshotComponentKey = string.Empty;

    private SearchField _hierarchySearchFieldControl;
    private SearchField _logSearchFieldControl;

    private const float SearchLabelWidth = 48f;
    private const float SearchFieldFixedWidth = 160f;
    private const float SearchClearButtonWidth = 56f;
    private const double SearchDebounceDelay = 0.2d;
    private const bool UseCompactLogRows = true;
    private const float CompactLogRowHeight = 34f;

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
    private const string HierarchyPanelWidthPrefKey = "DebugConsoleEditorWindow.HierarchyPanelWidth";
    private const float DefaultLogDetailPanelHeight = 220f;
    private const float MinLogDetailPanelHeight = 140f;
    private const float MaxLogDetailPanelHeight = 340f;

    private float _hierarchyPanelWidth = 420f;
    private bool _isDraggingPanelSplitter;

    private float _lastLogContentHeight;
    private float _lastLogViewportHeight;
    private float _lastMaxLogScrollY;
    private float _logDetailPanelHeight = 220f;
    private readonly Dictionary<string, float> _liveLogHeightCache = new();
    private readonly Dictionary<string, float> _snapshotLogHeightCache = new();
    private List<LiveLogGroup> _cachedLiveLogGroups = new();
    private List<float> _cachedLiveRowHeights = new();
    private int _cachedLiveLogChangeVersion = -1;
    private string _cachedLiveLogSignature = string.Empty;
    private float _cachedLiveLogWidth = -1f;


    private readonly HashSet<int> _expandedComponents = new();
    private readonly HashSet<int> _expandedChildren = new();

    private int _selectedLogIndex = -1;

    private DebugConsoleEditorSnapshot _lastSnapshot;
    private double _nextSnapshotCaptureTime;
    private int _lastCapturedManagerChangeVersion = -1;

    private readonly HashSet<string> _expandedSnapshotDetails = new();
    private readonly HashSet<string> _expandedSnapshotChildren = new();

    private const string SnapshotDirectoryPath = "Library/DebugConsole";
    private const string SnapshotFileName = "DebugConsoleEditorSnapshot.json";
    private const int CurrentSnapshotVersion = 3;
    private const int MaxSnapshotHierarchyDepth = 4;
    private const string EditorStatePrefKey = "DebugConsoleEditorWindow.State";
    private const string ManagerPrefKeyPrefix = "DebugConsole.Manager";
    private const string GlobalEnabledPrefKey = ManagerPrefKeyPrefix + ".GlobalEnabled";
    private const string MirrorToUnityPrefKey = ManagerPrefKeyPrefix + ".MirrorToUnity";
    private const string LogLevelLogPrefKey = ManagerPrefKeyPrefix + ".Level.Log";
    private const string LogLevelWarningPrefKey = ManagerPrefKeyPrefix + ".Level.Warning";
    private const string LogLevelErrorPrefKey = ManagerPrefKeyPrefix + ".Level.Error";
    private const string TypePrefKeyPrefix = ManagerPrefKeyPrefix + ".Type.";
    private const string GameObjectPrefKeyPrefix = ManagerPrefKeyPrefix + ".GameObject.";
    private const string ComponentPrefKeyPrefix = ManagerPrefKeyPrefix + ".Component.";
    private const string GameObjectRegistryPrefKey = ManagerPrefKeyPrefix + ".Registry.GameObject";
    private const string ComponentRegistryPrefKey = ManagerPrefKeyPrefix + ".Registry.Component";

    [Serializable]
    private sealed class DebugConsoleEditorSnapshot
    {
        public int SnapshotVersion;
        public string SceneName;
        public string CapturedAt;
        public bool GlobalEnabled;
        public bool MirrorToUnityConsole;
        public bool ShowLogLevelLog = true;
        public bool ShowLogLevelWarning = true;
        public bool ShowLogLevelError = true;
        public bool[] TypeFilters;
        public List<SnapshotGameObjectNode> Roots = new();
        public List<SnapshotLogEntry> Entries = new();

        public bool HasData => (Roots != null && Roots.Count > 0) || (Entries != null && Entries.Count > 0);
    }

    [Serializable]
    private sealed class SnapshotGameObjectNode
    {
        public string Name;
        public string PathKey;
        public bool Enabled;
        public List<SnapshotComponentNode> Components = new();
        public List<SnapshotGameObjectNode> Children = new();
    }

    [Serializable]
    private sealed class SnapshotComponentNode
    {
        public string Name;
        public string Key;
        public bool Enabled;
    }

    [Serializable]
    private sealed class SnapshotLogEntry
    {
        public string Time;
        public string Message;
        public string SourceName;
        public string MemberName;
        public int LineNumber;
        public DebugType Type;
        public DebugLogLevel Level;
        public int GameObjectId;
        public int ComponentId;
        public string GameObjectKey;
        public string ComponentKey;
        public string GameObjectName;
        public string ComponentName;
        public string SceneKey;
        public string HierarchyPath;
        public string ComponentTypeName;
        public long SequenceId;
        public int FrameCount;
        public string CapturedAtIsoUtc;
        public string ColorHex;
        public string CallerFilePath;
        public int CallerColumn = 1;
        public string StackTrace;
        public bool WasVisibleAtCapture;

        public string RichText =>
            $"<color={ColorHex}>[{Time}] [{Type}] {Message}</color>\n" +
            $"<color=#daa520>출처 : [{SourceName}.{MemberName} : {LineNumber}]</color>";
    }


    [Serializable]
    private sealed class DebugConsoleEditorUiState
    {
        public bool AutoScroll = true;
        public bool HideTransform = true;
        public bool CollapsePreviousOnSelection = true;
        public bool CollapseLogs = true;
        public bool ShowTypeFilterPanel;
        public bool ShowLogDetails = true;
        public bool StackTraceFoldout = true;
        public float HierarchyPanelWidth = 420f;
        public float LogDetailPanelHeight = DefaultLogDetailPanelHeight;
        public Vector2 HierarchyScroll;
        public Vector2 LogScroll;
        public Vector2 TypeFilterScroll;
        public Vector2 DetailScroll;
        public int SelectedLogIndex = -1;
        public string FocusedSnapshotGameObjectKey = string.Empty;
        public string FocusedSnapshotComponentKey = string.Empty;
        public string FocusedObjectName = string.Empty;
        public string FocusedComponentName = string.Empty;
        public List<string> ExpandedSnapshotDetails = new();
        public List<string> ExpandedSnapshotChildren = new();
    }

    private sealed class LiveLogGroup
    {
        public DebugEntry Entry;
        public int Count;
        public int LastSourceIndex;
    }

    private sealed class SnapshotLogGroup
    {
        public SnapshotLogEntry Entry;
        public int Count;
        public int LastSourceIndex;
    }


[Serializable]
private sealed class DebugConsoleStoredBoolValue
{
    public string Key;
    public bool Value;
}

[Serializable]
private sealed class DebugConsoleStoredManagerSettings
{
    public bool GlobalEnabled = true;
    public bool MirrorToUnityConsole;
    public bool ShowLogLevelLog = true;
    public bool ShowLogLevelWarning = true;
    public bool ShowLogLevelError = true;
    public bool[] TypeFilters;
    public List<DebugConsoleStoredBoolValue> GameObjectFilterValues = new();
    public List<DebugConsoleStoredBoolValue> ComponentFilterValues = new();
}

[Serializable]
private sealed class DebugConsoleEditorBackupData
{
    public DebugConsoleStoredManagerSettings ManagerSettings = new();
    public DebugConsoleEditorUiState EditorState = new();
}


    [MenuItem("Tools/Debug/Runtime Debug Console Window")]
    public static void Open()
    {
        DebugConsoleEditorWindow window = GetWindow<DebugConsoleEditorWindow>();
        window.titleContent = new GUIContent("Debug Console");
        window.minSize = new Vector2(1000f, 650f);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += HandleEditorUpdate;
        EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        _hierarchyPanelWidth = DebugConsolePreferenceStore.GetFloat(HierarchyPanelWidthPrefKey, _hierarchyPanelWidth);
        _hierarchySearchFieldControl ??= new SearchField();
        _logSearchFieldControl ??= new SearchField();
        _pendingHierarchySearch = _hierarchySearch;
        _pendingLogSearch = _logSearch;
        LoadEditorUiState();
        _pendingHierarchySearch = _hierarchySearch;
        _pendingLogSearch = _logSearch;
        LoadSnapshotFromDisk();
        ApplyStoredPreferencesToSnapshot(_lastSnapshot);
    }

    private void OnDisable()
    {
        EditorApplication.update -= HandleEditorUpdate;
        EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SaveEditorUiState();
        DebugConsolePreferenceStore.SetFloat(HierarchyPanelWidthPrefKey, _hierarchyPanelWidth);
    }

    private void HandleEditorUpdate()
    {
        if (ProcessSearchDebounce())
            Repaint();

        if (EditorApplication.isPlaying)
        {
            TryCaptureLiveSnapshot(false);
            Repaint();
        }
    }

    private void HandlePlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _selectedLogIndex = -1;
            _hierarchyScroll = Vector2.zero;
            _logScroll = Vector2.zero;
            ClearFocus();
            _lastCapturedManagerChangeVersion = -1;
            _nextSnapshotCaptureTime = 0d;
            SaveEditorUiState();
            Repaint();
            return;
        }

        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            SaveEditorUiState();
            TryCaptureLiveSnapshot(true);
            Repaint();
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            LoadEditorUiState();
            LoadSnapshotFromDisk();
            ApplyStoredPreferencesToSnapshot(_lastSnapshot);
            Repaint();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _stylesDirty = true;
        _titleStyle = null;
        _expandedComponents.Clear();
        _expandedChildren.Clear();
        _expandedSnapshotDetails.Clear();
        _expandedSnapshotChildren.Clear();
        _selectedLogIndex = -1;
        _hierarchyScroll = Vector2.zero;
        ClearFocus();
        Repaint();
    }

    private void OnGUI()
    {
        InitStyles();

        if (!EditorApplication.isPlaying)
        {
            DrawSnapshotOrIdleView();
            return;
        }

        DebugConsoleManager manager = DebugConsoleManager.Instance;
        if (manager == null)
        {
            EditorGUILayout.HelpBox("DebugConsoleManager를 찾지 못했습니다. 플레이 시작 후 한 프레임 뒤에 다시 확인해보세요.", MessageType.Warning);
            return;
        }

        TryCaptureLiveSnapshot(false);
        DrawToolbar(manager);
        DrawTypeFilterPanel(manager);

        DrawResizablePanels(manager);
    }


    private void DrawSnapshotOrIdleView()
    {
        if (_lastSnapshot == null || !_lastSnapshot.HasData)
        {
            EditorGUILayout.HelpBox("플레이 모드에서 Runtime Debug Console 데이터를 표시합니다.", MessageType.Info);
            if (GUILayout.Button("Play"))
                EditorApplication.isPlaying = true;
            return;
        }

        ApplyStoredPreferencesToSnapshot(_lastSnapshot);

        string sceneName = string.IsNullOrWhiteSpace(_lastSnapshot.SceneName) ? "(Unknown)" : _lastSnapshot.SceneName;
        string capturedAt = string.IsNullOrWhiteSpace(_lastSnapshot.CapturedAt) ? "-" : _lastSnapshot.CapturedAt;

        EditorGUILayout.HelpBox($"마지막 플레이 스냅샷을 표시합니다. Scene : {sceneName} / Captured : {capturedAt}", MessageType.Info);

        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
        GUILayout.Label("Snapshot Mode", _titleStyle, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("Clear Snapshot", GUILayout.Width(120f)))
        {
            ClearSavedSnapshot();
            return;
        }

        if (GUILayout.Button("Play", GUILayout.Width(100f)))
            EditorApplication.isPlaying = true;

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4f);

        DrawSnapshotToolbar(_lastSnapshot);
        DrawSnapshotTypeFilterPanel(_lastSnapshot);
        DrawSnapshotResizablePanels(_lastSnapshot);
    }

    private void DrawSnapshotResizablePanels(DebugConsoleEditorSnapshot snapshot)
    {
        float contentWidth = Mathf.Max(620f, position.width - 24f);
        float maxHierarchyPanelWidth = Mathf.Max(MinHierarchyPanelWidth, contentWidth - MinLogPanelWidth - PanelSplitterWidth);

        if (_hierarchyPanelWidth <= 0f)
            _hierarchyPanelWidth = contentWidth * 0.42f;

        _hierarchyPanelWidth = Mathf.Clamp(_hierarchyPanelWidth, MinHierarchyPanelWidth, maxHierarchyPanelWidth);
        float logPanelWidth = Mathf.Max(MinLogPanelWidth, contentWidth - _hierarchyPanelWidth - PanelSplitterWidth);

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawSnapshotHierarchyPanel(snapshot, _hierarchyPanelWidth);
        DrawPanelSplitter(contentWidth);
        DrawSnapshotLogPanel(snapshot, logPanelWidth);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSnapshotHierarchyPanel(DebugConsoleEditorSnapshot snapshot, float panelWidth)
    {
        GUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));
        GUILayout.BeginHorizontal();
        GUILayout.Label("Scene Objects / Components", _titleStyle, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
        DrawHierarchySearchField(panelWidth - 20f);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4f);

        _hierarchyScroll = GUILayout.BeginScrollView(_hierarchyScroll);

        if (snapshot?.Roots != null)
        {
            for (int i = 0; i < snapshot.Roots.Count; i++)
                DrawSnapshotGameObjectNode(snapshot.Roots[i], 0, panelWidth);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(4f);
        string footerCountText = $"Count : {GetVisibleSnapshotEntryCount(snapshot)}";

        GUILayout.BeginHorizontal(_boxStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(28f));
        GUILayout.Space(10f);
        GUILayout.Label(new GUIContent(footerCountText, footerCountText), _footerLeftLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(20f));
        GUILayout.Space(10f);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void DrawSnapshotGameObjectNode(SnapshotGameObjectNode node, int depth, float panelWidth)
    {
        if (node == null)
            return;

        if (!ShouldShowSnapshotGameObject(node))
            return;

        bool hasVisibleComponents = HasVisibleSnapshotComponents(node.Components, node.Name);
        bool hasVisibleChildren = HasVisibleSnapshotChildren(node.Children);
        bool hasDetails = hasVisibleComponents || hasVisibleChildren;

        bool detailsExpanded = !string.IsNullOrWhiteSpace(node.PathKey) && _expandedSnapshotDetails.Contains(node.PathKey);
        bool childrenExpanded = !string.IsNullOrWhiteSpace(node.PathKey) && _expandedSnapshotChildren.Contains(node.PathKey);

        bool searchActive = !string.IsNullOrWhiteSpace(_hierarchySearch);
        bool forceOpenDetails = searchActive && (HasMatchingSnapshotComponent(node, _hierarchySearch) || hasVisibleChildren);
        bool forceOpenChildren = searchActive && hasVisibleChildren;

        bool isObjectFocused = IsSnapshotObjectFocused(node.PathKey);
        bool isComponentParentFocused = IsSnapshotFocusedObjectParent(node.PathKey);
        bool showDetails = hasDetails && (detailsExpanded || forceOpenDetails);
        bool canShowChildControls = hasVisibleChildren && isObjectFocused;
        bool showChildren = hasVisibleChildren && (forceOpenChildren || (childrenExpanded && canShowChildControls));

        float objectLeadingSpace = depth * 18f;
        float rowContentWidth = GetHierarchyRowContentWidth(panelWidth);

        GUILayout.BeginVertical(GetHierarchyRowStyle(isObjectFocused, isComponentParentFocused, false), GUILayout.Width(rowContentWidth));
        GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
        GUILayout.Space(objectLeadingSpace);

        bool nextObjectEnabled = GUILayout.Toggle(node.Enabled, GUIContent.none, GUILayout.Width(HierarchyToggleSize), GUILayout.Height(HierarchyRowHeight));
        if (nextObjectEnabled != node.Enabled)
            SetSnapshotGameObjectEnabled(node, nextObjectEnabled);

        GUIStyle objectStyle = GetObjectButtonStyle(node.Enabled, isObjectFocused, isComponentParentFocused);
        GUIContent objectContent = new GUIContent(GetDisplayName(node.Name), node.Name);
        float objectButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, objectLeadingSpace, true, true);
        if (GUILayout.Button(objectContent, objectStyle, GUILayout.Width(objectButtonWidth), GUILayout.Height(HierarchyRowHeight)))
            ToggleSnapshotGameObjectFocus(node);

        if (hasDetails)
        {
            string foldoutLabel = showDetails ? "▾" : "▸";
            if (GUILayout.Button(foldoutLabel, _foldoutButtonStyle, GUILayout.Width(HierarchyFoldoutSize), GUILayout.Height(HierarchyRowHeight)))
                ToggleSnapshotExpandedDetails(node.PathKey);
        }
        else
        {
            GUILayout.Space(HierarchyFoldoutSize);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (showDetails && hasVisibleComponents)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = node.Enabled;

            for (int i = 0; i < node.Components.Count; i++)
            {
                SnapshotComponentNode component = node.Components[i];
                if (!ShouldShowSnapshotComponent(component, node.Name))
                    continue;

                bool isComponentFocused = IsSnapshotComponentFocused(component.Key);
                float componentLeadingSpace = (depth + 1) * 18f + HierarchyToggleSize + 8f;
                rowContentWidth = GetHierarchyRowContentWidth(panelWidth);

                GUILayout.BeginVertical(GetHierarchyRowStyle(false, false, isComponentFocused), GUILayout.Width(rowContentWidth));
                GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
                GUILayout.Space(componentLeadingSpace);

                bool nextComponentEnabled = GUILayout.Toggle(component.Enabled, GUIContent.none, GUILayout.Width(HierarchyToggleSize), GUILayout.Height(HierarchyRowHeight));
                if (nextComponentEnabled != component.Enabled)
                    SetSnapshotComponentEnabled(component, nextComponentEnabled);

                GUIStyle componentStyle = GetComponentButtonStyle(node.Enabled, isComponentFocused);
                GUIContent componentContent = new GUIContent(GetDisplayName(component.Name), component.Name);
                float componentButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, componentLeadingSpace, true, true);
                if (GUILayout.Button(componentContent, componentStyle, GUILayout.Width(componentButtonWidth), GUILayout.Height(HierarchyRowHeight)))
                    ToggleSnapshotComponentFocus(node, component);

                GUILayout.Space(HierarchyFoldoutSize);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUI.enabled = previousEnabled;
        }

        if (!hasVisibleChildren)
            return;

        if (canShowChildControls)
        {
            float childLeadingSpace = (depth + 1) * 18f + HierarchyToggleSize + 8f + HierarchyToggleSize;

            GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
            GUILayout.Space((depth + 1) * 18f + HierarchyToggleSize + 8f);
            GUILayout.Space(HierarchyToggleSize);

            float childButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, childLeadingSpace, true, true);
            if (GUILayout.Button(new GUIContent("하위 오브젝트", "하위 오브젝트"), _linkButtonStyle, GUILayout.Width(childButtonWidth), GUILayout.Height(HierarchyRowHeight)))
                ToggleSnapshotExpandedChildren(node.PathKey);

            string childFoldoutLabel = showChildren ? "▾" : "▸";
            if (GUILayout.Button(childFoldoutLabel, _foldoutButtonStyle, GUILayout.Width(HierarchyFoldoutSize), GUILayout.Height(HierarchyRowHeight)))
                ToggleSnapshotExpandedChildren(node.PathKey);

            GUILayout.EndHorizontal();
        }

        if (!showChildren)
            return;

        for (int i = 0; i < node.Children.Count; i++)
            DrawSnapshotGameObjectNode(node.Children[i], depth + 1, panelWidth);
    }


    private List<SnapshotLogGroup> BuildVisibleSnapshotLogGroups(DebugConsoleEditorSnapshot snapshot)
    {
        List<SnapshotLogGroup> groups = new List<SnapshotLogGroup>();
        if (snapshot?.Entries == null)
            return groups;

        for (int i = 0; i < snapshot.Entries.Count; i++)
        {
            SnapshotLogEntry entry = snapshot.Entries[i];
            if (!ShouldDisplaySnapshotEntry(entry))
                continue;

            if (_collapseLogs && groups.Count > 0)
            {
                SnapshotLogGroup lastGroup = groups[groups.Count - 1];
                if (CanCollapseSnapshotEntries(lastGroup.Entry, entry))
                {
                    lastGroup.Entry = entry;
                    lastGroup.Count++;
                    lastGroup.LastSourceIndex = i;
                    continue;
                }
            }

            groups.Add(new SnapshotLogGroup
            {
                Entry = entry,
                Count = 1,
                LastSourceIndex = i
            });
        }

        return groups;
    }


    private string BuildLiveLogCacheSignature(DebugConsoleManager manager)
    {
        return string.Join("|",
            manager != null ? manager.ChangeVersion : -1,
            _collapseLogs,
            UseCompactLogRows,
            _logSearch ?? string.Empty,
            _focusedGameObjectId,
            _focusedComponentId,
            _focusedSnapshotGameObjectKey ?? string.Empty,
            _focusedSnapshotComponentKey ?? string.Empty);
    }

    private void GetVisibleLiveLogGroupsAndHeights(DebugConsoleManager manager, float width, out List<LiveLogGroup> groups, out List<float> rowHeights)
    {
        if (manager == null)
        {
            groups = new List<LiveLogGroup>();
            rowHeights = new List<float>();
            return;
        }

        string signature = BuildLiveLogCacheSignature(manager);
        bool requiresRefresh =
            _cachedLiveLogChangeVersion != manager.ChangeVersion ||
            !string.Equals(_cachedLiveLogSignature, signature, StringComparison.Ordinal) ||
            Mathf.Abs(_cachedLiveLogWidth - width) > 0.5f;

        if (requiresRefresh)
        {
            bool canRefreshNow = Event.current == null || Event.current.type == EventType.Layout || _cachedLiveLogGroups == null || _cachedLiveRowHeights == null;
            if (canRefreshNow)
            {
                _cachedLiveLogGroups = BuildVisibleLiveLogGroups(manager);
                _cachedLiveRowHeights = BuildLiveRowHeights(_cachedLiveLogGroups, width);
                _cachedLiveLogChangeVersion = manager.ChangeVersion;
                _cachedLiveLogSignature = signature;
                _cachedLiveLogWidth = width;
            }
        }

        _cachedLiveLogGroups ??= new List<LiveLogGroup>();
        _cachedLiveRowHeights ??= new List<float>();

        groups = _cachedLiveLogGroups;
        rowHeights = _cachedLiveRowHeights;
    }

    private List<LiveLogGroup> BuildVisibleLiveLogGroups(DebugConsoleManager manager)
    {
        List<LiveLogGroup> groups = new List<LiveLogGroup>();
        if (manager == null)
            return groups;

        IReadOnlyList<DebugEntry> entries = manager.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            DebugEntry entry = entries[i];
            if (!ShouldDisplayEntry(manager, entry))
                continue;

            int repeatCount = Mathf.Max(1, entry.RepeatCount);

            if (_collapseLogs && groups.Count > 0)
            {
                LiveLogGroup lastGroup = groups[groups.Count - 1];
                if (CanCollapseLiveEntries(lastGroup.Entry, entry))
                {
                    lastGroup.Entry = entry;
                    lastGroup.Count += repeatCount;
                    lastGroup.LastSourceIndex = i;
                    continue;
                }
            }

            groups.Add(new LiveLogGroup
            {
                Entry = entry,
                Count = repeatCount,
                LastSourceIndex = i
            });
        }

        return groups;
    }

    private bool CanCollapseLiveEntries(DebugEntry left, DebugEntry right)
    {
        if (left == null || right == null)
            return false;

        return string.Equals(left.CollapseKey, right.CollapseKey, StringComparison.Ordinal);
    }

    private bool CanCollapseSnapshotEntries(SnapshotLogEntry left, SnapshotLogEntry right)
    {
        if (left == null || right == null)
            return false;

        return string.Equals(left.Message, right.Message, StringComparison.Ordinal) &&
               string.Equals(left.SourceName, right.SourceName, StringComparison.Ordinal) &&
               string.Equals(left.MemberName, right.MemberName, StringComparison.Ordinal) &&
               string.Equals(left.ColorHex, right.ColorHex, StringComparison.Ordinal) &&
               string.Equals(left.CallerFilePath, right.CallerFilePath, StringComparison.Ordinal) &&
               string.Equals(left.GameObjectKey, right.GameObjectKey, StringComparison.Ordinal) &&
               string.Equals(left.ComponentKey, right.ComponentKey, StringComparison.Ordinal) &&
               left.LineNumber == right.LineNumber &&
               left.CallerColumn == right.CallerColumn &&
               left.Type == right.Type &&
               left.Level == right.Level &&
               left.GameObjectId == right.GameObjectId &&
               left.ComponentId == right.ComponentId;
    }

    private GUIContent BuildCollapsedLogContent(string richText, int repeatCount)
    {
        if (repeatCount <= 1 || string.IsNullOrWhiteSpace(richText))
            return new GUIContent(richText ?? string.Empty);

        string suffix = $" <color=#f1c232>(x{repeatCount})</color>";
        int newLineIndex = richText.IndexOf('\n');
        string collapsedText = newLineIndex >= 0
            ? richText.Insert(newLineIndex, suffix)
            : richText + suffix;

        return new GUIContent(collapsedText);
    }


private void DrawSnapshotLogPanel(DebugConsoleEditorSnapshot snapshot, float panelWidth)
{
    EditorGUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));
    EditorGUILayout.BeginHorizontal();
    GUILayout.Label("Logs", _titleStyle, GUILayout.ExpandWidth(true));
    bool nextShowLogDetails = GUILayout.Toggle(_showLogDetails, "Details", GUILayout.Width(70f));
    if (nextShowLogDetails != _showLogDetails)
    {
        _showLogDetails = nextShowLogDetails;
        SaveEditorUiState();
    }
    EditorGUILayout.EndHorizontal();
    GUILayout.Space(4f);
    EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
    DrawLogSearchField(panelWidth - 20f);
    EditorGUILayout.EndHorizontal();
    GUILayout.Space(4f);

    float width = Mathf.Max(GetLogContentWidth(panelWidth), 180f);
    float listViewportHeight = Mathf.Max(120f, position.height - (_showLogDetails ? _logDetailPanelHeight + 240f : 190f));

    List<SnapshotLogGroup> groups = BuildVisibleSnapshotLogGroups(snapshot);
    List<float> rowHeights = BuildSnapshotRowHeights(groups, width);
    CalculateVisibleRange(rowHeights, _logScroll.y, listViewportHeight, out int startIndex, out int endIndex, out float topPadding, out float visibleHeight, out float totalHeight);

    _logScroll = GUILayout.BeginScrollView(_logScroll, false, !_autoScroll, GUIStyle.none, GetLogVerticalScrollbarStyle(), GUILayout.MinHeight(listViewportHeight), GUILayout.ExpandHeight(true));

    if (topPadding > 0f)
        GUILayout.Space(topPadding);

    for (int i = startIndex; i < endIndex; i++)
    {
        SnapshotLogGroup group = groups[i];
        DrawSnapshotLogEntry(group.Entry, group.LastSourceIndex, width, group.Count);
        GUILayout.Space(4f);
    }

    float bottomPadding = Mathf.Max(0f, totalHeight - topPadding - visibleHeight);
    if (bottomPadding > 0f)
        GUILayout.Space(bottomPadding);

    GUILayout.EndScrollView();

    Rect scrollRect = GUILayoutUtility.GetLastRect();
    _lastLogViewportHeight = scrollRect.height;
    _lastLogContentHeight = totalHeight;
    _lastMaxLogScrollY = Mathf.Max(0f, _lastLogContentHeight - _lastLogViewportHeight);

    if (Event.current.type == EventType.Repaint && _autoScroll)
    {
        Vector2 nextScroll = _logScroll;
        nextScroll.y = _lastMaxLogScrollY + 4f;
        _logScroll = nextScroll;
    }

    if (_showLogDetails)
    {
        GUILayout.Space(4f);
        SnapshotLogEntry selectedEntry = GetSelectedSnapshotEntry(snapshot);
        DrawSnapshotLogDetailPanel(selectedEntry, panelWidth);
    }

    EditorGUILayout.EndVertical();
}

private float DrawSnapshotLogEntry(SnapshotLogEntry entry, int sourceIndex, float width, int repeatCount)
    {
        GUIContent content = UseCompactLogRows
            ? BuildCollapsedLogContent(BuildSnapshotCompactRichText(entry), repeatCount)
            : BuildCollapsedLogContent(entry.RichText, repeatCount);
        float rowHeight = UseCompactLogRows ? CompactLogRowHeight : _richLabelStyle.CalcHeight(content, width) + 12f;

        Rect rect = GUILayoutUtility.GetRect(10f, rowHeight, GUILayout.ExpandWidth(true));

        Color previousColor = GUI.color;
        if (sourceIndex == _selectedLogIndex)
            GUI.color = new Color(0.75f, 0.85f, 1f, 1f);

        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;

        Rect labelRect = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f);
        GUI.Label(labelRect, content, _richLabelStyle);

        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 0 &&
            rect.Contains(Event.current.mousePosition))
        {
            _selectedLogIndex = sourceIndex;
            FocusSnapshotEntry(entry);
            SaveEditorUiState();

            if (Event.current.clickCount >= 2)
                OpenEntryScript(entry);

            Event.current.Use();
        }

        return rect.height;
    }

    private bool ShouldShowSnapshotGameObject(SnapshotGameObjectNode node)
    {
        if (node == null)
            return false;

        if (string.IsNullOrWhiteSpace(_hierarchySearch))
            return true;

        if (ContainsIgnoreCase(node.Name, _hierarchySearch))
            return true;

        if (HasMatchingSnapshotComponent(node, _hierarchySearch))
            return true;

        return HasVisibleSnapshotChildren(node.Children);
    }

    private bool HasMatchingSnapshotComponent(SnapshotGameObjectNode node, string keyword)
    {
        if (node?.Components == null)
            return false;

        for (int i = 0; i < node.Components.Count; i++)
        {
            SnapshotComponentNode component = node.Components[i];
            if (ShouldShowSnapshotComponent(component, node.Name) && ContainsIgnoreCase(component.Name, keyword))
                return true;
        }

        return false;
    }

    private bool HasVisibleSnapshotComponents(List<SnapshotComponentNode> components, string ownerName)
    {
        if (components == null || components.Count == 0)
            return false;

        for (int i = 0; i < components.Count; i++)
        {
            if (ShouldShowSnapshotComponent(components[i], ownerName))
                return true;
        }

        return false;
    }

    private bool HasVisibleSnapshotChildren(List<SnapshotGameObjectNode> children)
    {
        if (children == null || children.Count == 0)
            return false;

        for (int i = 0; i < children.Count; i++)
        {
            if (ShouldShowSnapshotGameObject(children[i]))
                return true;
        }

        return false;
    }

    private bool ShouldShowSnapshotComponent(SnapshotComponentNode component, string ownerName)
    {
        if (component == null)
            return false;

        if (_hideTransform && string.Equals(component.Name, nameof(Transform), StringComparison.Ordinal))
            return false;

        if (string.IsNullOrWhiteSpace(_hierarchySearch))
            return true;

        if (ContainsIgnoreCase(ownerName, _hierarchySearch))
            return true;

        return ContainsIgnoreCase(component.Name, _hierarchySearch);
    }

    private bool ShouldDisplaySnapshotEntry(SnapshotLogEntry entry)
    {
        if (entry == null || _lastSnapshot == null)
            return false;

        if (!_lastSnapshot.GlobalEnabled)
            return false;

        int typeIndex = (int)entry.Type;
        if (_lastSnapshot.TypeFilters != null && typeIndex >= 0 && typeIndex < _lastSnapshot.TypeFilters.Length && !_lastSnapshot.TypeFilters[typeIndex])
            return false;

        if (!IsSnapshotLevelEnabled(_lastSnapshot, entry.Level))
            return false;

        if (!string.IsNullOrWhiteSpace(entry.GameObjectKey) && !GetStoredGameObjectEnabled(entry.GameObjectKey, true))
            return false;

        if (!string.IsNullOrWhiteSpace(entry.ComponentKey) && !GetStoredComponentEnabled(entry.ComponentKey, true))
            return false;

        if (!string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey))
        {
            if (!string.Equals(entry.ComponentKey, _focusedSnapshotComponentKey, StringComparison.Ordinal))
                return false;
        }
        else if (!string.IsNullOrWhiteSpace(_focusedSnapshotGameObjectKey))
        {
            if (!string.Equals(entry.GameObjectKey, _focusedSnapshotGameObjectKey, StringComparison.Ordinal))
                return false;
        }
        else if (_focusedComponentId != 0)
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

        string searchPool = $"{entry.Message} {entry.SourceName} {entry.MemberName} {entry.Type} {entry.Time} {entry.Level}";
        return ContainsIgnoreCase(searchPool, _logSearch);
    }

    private int GetVisibleSnapshotEntryCount(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot?.Entries == null)
            return 0;

        int count = 0;
        for (int i = 0; i < snapshot.Entries.Count; i++)
        {
            if (ShouldDisplaySnapshotEntry(snapshot.Entries[i]))
                count++;
        }

        return count;
    }

    private void TryCaptureLiveSnapshot(bool force)
    {
        if (!EditorApplication.isPlaying)
            return;

        DebugConsoleManager manager = DebugConsoleManager.Instance;
        if (manager == null)
            return;

        double now = EditorApplication.timeSinceStartup;
        bool captureByInterval = now >= _nextSnapshotCaptureTime;
        bool captureByChange = manager.ChangeVersion != _lastCapturedManagerChangeVersion;

        if (!force && !captureByInterval && !captureByChange)
            return;

        _lastSnapshot = CaptureSnapshot(manager);
        SaveEditorUiState();
        SaveSnapshotToDisk(_lastSnapshot);
        _lastCapturedManagerChangeVersion = manager.ChangeVersion;
        _nextSnapshotCaptureTime = now + 0.75d;
    }

    private DebugConsoleEditorSnapshot CaptureSnapshot(DebugConsoleManager manager)
    {
        DebugConsoleEditorSnapshot snapshot = new DebugConsoleEditorSnapshot
        {
            SnapshotVersion = CurrentSnapshotVersion,
            SceneName = SceneManager.GetActiveScene().name,
            CapturedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            GlobalEnabled = manager.GlobalEnabled,
            MirrorToUnityConsole = manager.MirrorToUnityConsole,
            ShowLogLevelLog = manager.GetLevelEnabled(DebugLogLevel.Log),
            ShowLogLevelWarning = manager.GetLevelEnabled(DebugLogLevel.Warning),
            ShowLogLevelError = manager.GetLevelEnabled(DebugLogLevel.Error),
            TypeFilters = CaptureTypeFilters(manager),
            Roots = new List<SnapshotGameObjectNode>(),
            Entries = new List<SnapshotLogEntry>()
        };

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            GameObject[] roots = activeScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                snapshot.Roots.Add(CaptureSnapshotNode(manager, roots[i], 0));
        }

        IReadOnlyList<DebugEntry> entries = manager.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            DebugEntry entry = entries[i];
            if (entry == null)
                continue;

            CaptureSnapshotEntryTargets(entry, out string gameObjectKey, out string componentKey, out string gameObjectName, out string componentName);

            snapshot.Entries.Add(new SnapshotLogEntry
            {
                Time = entry.Time,
                Message = entry.Message,
                SourceName = entry.SourceName,
                MemberName = entry.MemberName,
                LineNumber = entry.LineNumber,
                Type = entry.Type,
                Level = entry.Level,
                GameObjectId = entry.GameObjectId,
                ComponentId = entry.ComponentId,
                GameObjectKey = gameObjectKey,
                ComponentKey = componentKey,
                GameObjectName = gameObjectName,
                ComponentName = componentName,
                SceneKey = entry.SceneKey,
                HierarchyPath = entry.HierarchyPath,
                ComponentTypeName = entry.ComponentTypeName,
                SequenceId = entry.SequenceId,
                FrameCount = entry.FrameCount,
                CapturedAtIsoUtc = entry.CapturedAtIsoUtc,
                ColorHex = entry.ColorHex,
                CallerFilePath = entry.CallerFilePath,
                CallerColumn = entry.CallerColumn,
                StackTrace = entry.StackTrace,
                WasVisibleAtCapture = ShouldDisplayEntry(manager, entry)
            });
        }

        return snapshot;
    }

    private SnapshotGameObjectNode CaptureSnapshotNode(DebugConsoleManager manager, GameObject go, int depth)
    {
        string objectKey = go != null ? DebugConsoleFilterKeyUtility.GetGameObjectKey(go) : string.Empty;
        SnapshotGameObjectNode node = new SnapshotGameObjectNode
        {
            Name = go != null ? go.name : "(Null)",
            PathKey = objectKey,
            Enabled = go != null && manager.GetGameObjectEnabled(go),
            Components = new List<SnapshotComponentNode>(),
            Children = new List<SnapshotGameObjectNode>()
        };

        if (go == null)
            return node;

        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            string componentName = component != null ? component.GetType().Name : "Missing Script";
            string componentKey = component != null
                ? DebugConsoleFilterKeyUtility.GetComponentKey(component)
                : $"{objectKey}|Missing Script#{i}";

            node.Components.Add(new SnapshotComponentNode
            {
                Name = componentName,
                Key = componentKey,
                Enabled = component == null || manager.GetComponentEnabled(component)
            });
        }

        for (int i = 0; i < go.transform.childCount; i++)
        {
            Transform child = go.transform.GetChild(i);
            if (child == null)
                continue;

            if (depth + 1 < MaxSnapshotHierarchyDepth)
                node.Children.Add(CaptureSnapshotNode(manager, child.gameObject, depth + 1));
        }

        return node;
    }

    private void CaptureSnapshotEntryTargets(DebugEntry entry, out string gameObjectKey, out string componentKey, out string gameObjectName, out string componentName)
    {
        gameObjectKey = entry?.GameObjectKey ?? string.Empty;
        componentKey = entry?.ComponentKey ?? string.Empty;
        gameObjectName = entry?.GameObjectName ?? string.Empty;
        componentName = entry?.ComponentName ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(gameObjectKey))
            return;

        try
        {
            if (entry?.Context is GameObject go)
            {
                if (go == null)
                    return;

                gameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(go);
                gameObjectName = go.name;
                return;
            }

            if (entry?.Context is Component component)
            {
                if (component == null)
                    return;

                GameObject owner = component.gameObject;
                if (owner == null)
                    return;

                gameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(owner);
                componentKey = DebugConsoleFilterKeyUtility.GetComponentKey(component);
                gameObjectName = owner.name;
                componentName = component.GetType().Name;
            }
        }
        catch (MissingReferenceException)
        {
        }
    }

    private bool[] CaptureTypeFilters(DebugConsoleManager manager)
    {
        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        bool[] filters = new bool[types.Length];

        for (int i = 0; i < types.Length; i++)
            filters[i] = manager.GetTypeEnabled(types[i]);

        return filters;
    }

    private string GetHierarchyPath(Transform target)
    {
        if (target == null)
            return string.Empty;

        Stack<string> stack = new Stack<string>();
        Transform current = target;

        while (current != null)
        {
            stack.Push($"{current.name}[{current.GetSiblingIndex()}]");
            current = current.parent;
        }

        return string.Join("/", stack);
    }

    private string GetSnapshotFilePath()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), SnapshotDirectoryPath, SnapshotFileName);
    }

    private void SaveSnapshotToDisk(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        try
        {
            snapshot.SnapshotVersion = CurrentSnapshotVersion;

            string filePath = GetSnapshotFilePath();
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllText(filePath, JsonUtility.ToJson(snapshot, true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"DebugConsoleEditorWindow snapshot save failed: {exception.Message}");
        }
    }

    private void LoadSnapshotFromDisk()
    {
        try
        {
            string filePath = GetSnapshotFilePath();
            if (!File.Exists(filePath))
            {
                _lastSnapshot = null;
                return;
            }

            string json = File.ReadAllText(filePath);
            _lastSnapshot = JsonUtility.FromJson<DebugConsoleEditorSnapshot>(json);
            if (!TryUpgradeSnapshotToCurrentVersion(_lastSnapshot))
            {
                _lastSnapshot = null;
                return;
            }

            ApplyStoredPreferencesToSnapshot(_lastSnapshot);
        }
        catch (Exception exception)
        {
            _lastSnapshot = null;
            Debug.LogWarning($"DebugConsoleEditorWindow snapshot load failed: {exception.Message}");
        }
    }


    private bool TryUpgradeSnapshotToCurrentVersion(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return false;

        if (snapshot.SnapshotVersion > CurrentSnapshotVersion)
            return false;

        NormalizeSnapshot(snapshot);
        snapshot.SnapshotVersion = CurrentSnapshotVersion;
        return true;
    }

    private void NormalizeSnapshot(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        snapshot.Roots ??= new List<SnapshotGameObjectNode>();
        snapshot.Entries ??= new List<SnapshotLogEntry>();

        if (!snapshot.ShowLogLevelLog && !snapshot.ShowLogLevelWarning && !snapshot.ShowLogLevelError)
        {
            snapshot.ShowLogLevelLog = true;
            snapshot.ShowLogLevelWarning = true;
            snapshot.ShowLogLevelError = true;
        }

        for (int i = 0; i < snapshot.Roots.Count; i++)
            NormalizeSnapshotNode(snapshot.Roots[i]);

        for (int i = 0; i < snapshot.Entries.Count; i++)
        {
            SnapshotLogEntry entry = snapshot.Entries[i];
            if (entry == null)
                continue;

            entry.GameObjectKey ??= string.Empty;
            entry.ComponentKey ??= string.Empty;
            entry.GameObjectName ??= string.Empty;
            entry.ComponentName ??= string.Empty;
            entry.SceneKey ??= string.Empty;
            entry.HierarchyPath ??= string.Empty;
            entry.ComponentTypeName ??= string.Empty;
            entry.CapturedAtIsoUtc ??= string.Empty;
            entry.ColorHex ??= "#ffffff";
            entry.CallerFilePath ??= string.Empty;
            entry.SourceName ??= string.Empty;
            entry.MemberName ??= string.Empty;
            entry.Message ??= string.Empty;
            entry.Time ??= string.Empty;
        }
    }

    private void NormalizeSnapshotNode(SnapshotGameObjectNode node)
    {
        if (node == null)
            return;

        node.Name ??= string.Empty;
        node.PathKey ??= string.Empty;
        node.Components ??= new List<SnapshotComponentNode>();
        node.Children ??= new List<SnapshotGameObjectNode>();

        for (int i = 0; i < node.Components.Count; i++)
        {
            SnapshotComponentNode component = node.Components[i];
            if (component == null)
                continue;

            component.Name ??= string.Empty;
            component.Key ??= string.Empty;
        }

        for (int i = 0; i < node.Children.Count; i++)
            NormalizeSnapshotNode(node.Children[i]);
    }


    private void LoadEditorUiState()
    {
        string raw = DebugConsolePreferenceStore.GetString(EditorStatePrefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return;

        DebugConsoleEditorUiState state = JsonUtility.FromJson<DebugConsoleEditorUiState>(raw);
        if (state == null)
            return;

        _autoScroll = state.AutoScroll;
        _hideTransform = state.HideTransform;
        _collapsePreviousOnSelection = state.CollapsePreviousOnSelection;
        _collapseLogs = state.CollapseLogs;
        _showTypeFilterPanel = state.ShowTypeFilterPanel;
        _showLogDetails = state.ShowLogDetails;
        _stackTraceFoldout = state.StackTraceFoldout;
        _hierarchyPanelWidth = state.HierarchyPanelWidth > 0f ? state.HierarchyPanelWidth : _hierarchyPanelWidth;
        _logDetailPanelHeight = Mathf.Clamp(state.LogDetailPanelHeight > 0f ? state.LogDetailPanelHeight : DefaultLogDetailPanelHeight, MinLogDetailPanelHeight, MaxLogDetailPanelHeight);
        _hierarchyScroll = state.HierarchyScroll;
        _logScroll = state.LogScroll;
        _typeFilterScroll = state.TypeFilterScroll;
        _detailScroll = state.DetailScroll;
        _selectedLogIndex = state.SelectedLogIndex;
        _focusedSnapshotGameObjectKey = state.FocusedSnapshotGameObjectKey ?? string.Empty;
        _focusedSnapshotComponentKey = state.FocusedSnapshotComponentKey ?? string.Empty;
        _focusedObjectName = state.FocusedObjectName ?? string.Empty;
        _focusedComponentName = state.FocusedComponentName ?? string.Empty;

        _expandedSnapshotDetails.Clear();
        if (state.ExpandedSnapshotDetails != null)
        {
            for (int i = 0; i < state.ExpandedSnapshotDetails.Count; i++)
            {
                string key = state.ExpandedSnapshotDetails[i];
                if (!string.IsNullOrWhiteSpace(key))
                    _expandedSnapshotDetails.Add(key);
            }
        }

        _expandedSnapshotChildren.Clear();
        if (state.ExpandedSnapshotChildren != null)
        {
            for (int i = 0; i < state.ExpandedSnapshotChildren.Count; i++)
            {
                string key = state.ExpandedSnapshotChildren[i];
                if (!string.IsNullOrWhiteSpace(key))
                    _expandedSnapshotChildren.Add(key);
            }
        }
    }

    private void SaveEditorUiState()
    {
        DebugConsoleEditorUiState state = new DebugConsoleEditorUiState
        {
            AutoScroll = _autoScroll,
            HideTransform = _hideTransform,
            CollapsePreviousOnSelection = _collapsePreviousOnSelection,
            CollapseLogs = _collapseLogs,
            ShowTypeFilterPanel = _showTypeFilterPanel,
            ShowLogDetails = _showLogDetails,
            StackTraceFoldout = _stackTraceFoldout,
            HierarchyPanelWidth = _hierarchyPanelWidth,
            LogDetailPanelHeight = _logDetailPanelHeight,
            HierarchyScroll = _hierarchyScroll,
            LogScroll = _logScroll,
            TypeFilterScroll = _typeFilterScroll,
            DetailScroll = _detailScroll,
            SelectedLogIndex = _selectedLogIndex,
            FocusedSnapshotGameObjectKey = _focusedSnapshotGameObjectKey ?? string.Empty,
            FocusedSnapshotComponentKey = _focusedSnapshotComponentKey ?? string.Empty,
            FocusedObjectName = _focusedObjectName ?? string.Empty,
            FocusedComponentName = _focusedComponentName ?? string.Empty,
            ExpandedSnapshotDetails = new List<string>(_expandedSnapshotDetails),
            ExpandedSnapshotChildren = new List<string>(_expandedSnapshotChildren)
        };

        DebugConsolePreferenceStore.SetString(EditorStatePrefKey, JsonUtility.ToJson(state));
    }

    private void ClearSavedSnapshot()
    {
        _lastSnapshot = null;
        _selectedLogIndex = -1;
        _hierarchyScroll = Vector2.zero;
        _logScroll = Vector2.zero;
        _expandedSnapshotDetails.Clear();
        _expandedSnapshotChildren.Clear();
        ClearFocus();
        SaveEditorUiState();

        try
        {
            string filePath = GetSnapshotFilePath();
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"DebugConsoleEditorWindow snapshot delete failed: {exception.Message}");
        }
    }

    private void ApplyStoredPreferencesToSnapshot(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        snapshot.GlobalEnabled = DebugConsolePreferenceStore.GetBool(GlobalEnabledPrefKey, snapshot.GlobalEnabled);
        snapshot.MirrorToUnityConsole = DebugConsolePreferenceStore.GetBool(MirrorToUnityPrefKey, snapshot.MirrorToUnityConsole);
        snapshot.ShowLogLevelLog = DebugConsolePreferenceStore.GetBool(LogLevelLogPrefKey, snapshot.ShowLogLevelLog);
        snapshot.ShowLogLevelWarning = DebugConsolePreferenceStore.GetBool(LogLevelWarningPrefKey, snapshot.ShowLogLevelWarning);
        snapshot.ShowLogLevelError = DebugConsolePreferenceStore.GetBool(LogLevelErrorPrefKey, snapshot.ShowLogLevelError);

        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        snapshot.TypeFilters ??= new bool[types.Length];
        if (snapshot.TypeFilters.Length != types.Length)
            Array.Resize(ref snapshot.TypeFilters, types.Length);

        for (int i = 0; i < types.Length; i++)
            snapshot.TypeFilters[i] = DebugConsolePreferenceStore.GetBool(GetTypePrefKey(types[i]), snapshot.TypeFilters[i]);

        if (snapshot.Roots == null)
            return;

        for (int i = 0; i < snapshot.Roots.Count; i++)
            ApplyStoredPreferencesToSnapshotNode(snapshot.Roots[i]);
    }

    private void ApplyStoredPreferencesToSnapshotNode(SnapshotGameObjectNode node)
    {
        if (node == null)
            return;

        if (!string.IsNullOrWhiteSpace(node.PathKey))
            node.Enabled = GetStoredGameObjectEnabled(node.PathKey, node.Enabled);

        if (node.Components != null)
        {
            for (int i = 0; i < node.Components.Count; i++)
            {
                SnapshotComponentNode component = node.Components[i];
                if (component == null || string.IsNullOrWhiteSpace(component.Key))
                    continue;

                component.Enabled = GetStoredComponentEnabled(component.Key, component.Enabled);
            }
        }

        if (node.Children == null)
            return;

        for (int i = 0; i < node.Children.Count; i++)
            ApplyStoredPreferencesToSnapshotNode(node.Children[i]);
    }

    private void DrawSnapshotToolbar(DebugConsoleEditorSnapshot snapshot)
    {
        float availableWidth = GetTopAreaWidth();

        int enabledCount = GetEnabledTypeCount(snapshot);
        int totalCount = Enum.GetValues(typeof(DebugType)).Length;
        string typeButtonLabel = _showTypeFilterPanel
            ? $"Type Filter ▲ ({enabledCount}/{totalCount})"
            : $"Type Filter ▼ ({enabledCount}/{totalCount})";

        if (availableWidth >= 1500f)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawSnapshotToolbarToggleGroup(snapshot);
            GUILayout.Space(8f);
            DrawSnapshotToolbarActionGroup(snapshot, typeButtonLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
            return;
        }

        DrawSnapshotToolbarToggleGroupWrapped(snapshot, availableWidth);
        DrawSnapshotToolbarActionGroupWrapped(snapshot, typeButtonLabel, availableWidth);
        GUILayout.Space(4f);
    }

    private void DrawSnapshotTypeFilterPanel(DebugConsoleEditorSnapshot snapshot)
    {
        if (!_showTypeFilterPanel || snapshot == null)
            return;

        EditorGUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("DebugType Filter", _titleStyle);

        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));

        const float minItemWidth = 120f;
        const float itemSpacing = 12f;
        float availableWidth = Mathf.Max(220f, position.width - 44f);
        int columns = Mathf.Clamp(Mathf.FloorToInt((availableWidth + itemSpacing) / (minItemWidth + itemSpacing)), 1, types.Length);
        float itemWidth = Mathf.Floor((availableWidth - itemSpacing * (columns - 1)) / columns);
        itemWidth = Mathf.Max(minItemWidth, itemWidth);

        int rows = Mathf.CeilToInt(types.Length / (float)columns);
        float viewHeight = Mathf.Min(120f, rows * 22f + Mathf.Max(0, rows - 1) * 4f + 6f);

        _typeFilterScroll = EditorGUILayout.BeginScrollView(_typeFilterScroll, GUILayout.Height(viewHeight));

        for (int row = 0; row < types.Length; row += columns)
        {
            EditorGUILayout.BeginHorizontal();

            for (int col = 0; col < columns; col++)
            {
                int index = row + col;
                if (index >= types.Length)
                    break;

                DebugType type = types[index];
                bool current = GetSnapshotTypeEnabled(snapshot, type);
                bool next = GUILayout.Toggle(current, type.ToString(), GUILayout.Width(itemWidth));

                if (next != current)
                    SetSnapshotTypeEnabled(snapshot, type, next);

                if (col < columns - 1)
                    GUILayout.Space(itemSpacing);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        GUILayout.Space(4f);
    }

    private int GetEnabledTypeCount(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot?.TypeFilters == null)
            return 0;

        int count = 0;
        for (int i = 0; i < snapshot.TypeFilters.Length; i++)
        {
            if (snapshot.TypeFilters[i])
                count++;
        }

        return count;
    }

    private bool GetSnapshotTypeEnabled(DebugConsoleEditorSnapshot snapshot, DebugType type)
    {
        if (snapshot?.TypeFilters == null)
            return true;

        int index = (int)type;
        if (index < 0 || index >= snapshot.TypeFilters.Length)
            return true;

        return snapshot.TypeFilters[index];
    }


    private bool IsSnapshotLevelEnabled(DebugConsoleEditorSnapshot snapshot, DebugLogLevel level)
    {
        if (snapshot == null)
            return true;

        return level switch
        {
            DebugLogLevel.Warning => snapshot.ShowLogLevelWarning,
            DebugLogLevel.Error => snapshot.ShowLogLevelError,
            _ => snapshot.ShowLogLevelLog
        };
    }

    private void SetSnapshotLevelEnabled(DebugConsoleEditorSnapshot snapshot, DebugLogLevel level, bool value)
    {
        if (snapshot == null)
            return;

        switch (level)
        {
            case DebugLogLevel.Warning:
                snapshot.ShowLogLevelWarning = value;
                DebugConsolePreferenceStore.SetBool(LogLevelWarningPrefKey, value);
                break;

            case DebugLogLevel.Error:
                snapshot.ShowLogLevelError = value;
                DebugConsolePreferenceStore.SetBool(LogLevelErrorPrefKey, value);
                break;

            default:
                snapshot.ShowLogLevelLog = value;
                DebugConsolePreferenceStore.SetBool(LogLevelLogPrefKey, value);
                break;
        }

        SaveSnapshotIfAvailable();
    }

    private void SetAllSnapshotLevels(DebugConsoleEditorSnapshot snapshot, bool value)
    {
        if (snapshot == null)
            return;

        snapshot.ShowLogLevelLog = value;
        snapshot.ShowLogLevelWarning = value;
        snapshot.ShowLogLevelError = value;
        DebugConsolePreferenceStore.SetBool(LogLevelLogPrefKey, value);
        DebugConsolePreferenceStore.SetBool(LogLevelWarningPrefKey, value);
        DebugConsolePreferenceStore.SetBool(LogLevelErrorPrefKey, value);
        SaveSnapshotIfAvailable();
    }

    private void SetSnapshotWarningAndErrorOnly(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        snapshot.ShowLogLevelLog = false;
        snapshot.ShowLogLevelWarning = true;
        snapshot.ShowLogLevelError = true;
        DebugConsolePreferenceStore.SetBool(LogLevelLogPrefKey, false);
        DebugConsolePreferenceStore.SetBool(LogLevelWarningPrefKey, true);
        DebugConsolePreferenceStore.SetBool(LogLevelErrorPrefKey, true);
        SaveSnapshotIfAvailable();
    }

    private void SetSnapshotErrorOnly(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        snapshot.ShowLogLevelLog = false;
        snapshot.ShowLogLevelWarning = false;
        snapshot.ShowLogLevelError = true;
        DebugConsolePreferenceStore.SetBool(LogLevelLogPrefKey, false);
        DebugConsolePreferenceStore.SetBool(LogLevelWarningPrefKey, false);
        DebugConsolePreferenceStore.SetBool(LogLevelErrorPrefKey, true);
        SaveSnapshotIfAvailable();
    }
    private void SetSnapshotTypeEnabled(DebugConsoleEditorSnapshot snapshot, DebugType type, bool value)
    {
        if (snapshot == null)
            return;

        int index = (int)type;
        if (snapshot.TypeFilters == null || index < 0)
            return;

        if (index >= snapshot.TypeFilters.Length)
            Array.Resize(ref snapshot.TypeFilters, index + 1);

        snapshot.TypeFilters[index] = value;
        DebugConsolePreferenceStore.SetBool(GetTypePrefKey(type), value);
        SaveSnapshotIfAvailable();
    }

    private void SetAllSnapshotTypes(DebugConsoleEditorSnapshot snapshot, bool value)
    {
        if (snapshot == null)
            return;

        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        snapshot.TypeFilters ??= new bool[types.Length];
        if (snapshot.TypeFilters.Length != types.Length)
            Array.Resize(ref snapshot.TypeFilters, types.Length);

        for (int i = 0; i < types.Length; i++)
        {
            snapshot.TypeFilters[i] = value;
            DebugConsolePreferenceStore.SetBool(GetTypePrefKey(types[i]), value);
        }

        SaveSnapshotIfAvailable();
    }

    private void SetSnapshotGameObjectEnabled(SnapshotGameObjectNode node, bool value)
    {
        if (node == null)
            return;

        node.Enabled = value;
        SetStoredGameObjectEnabled(node.PathKey, value);
        SaveSnapshotIfAvailable();
    }

    private void SetSnapshotComponentEnabled(SnapshotComponentNode component, bool value)
    {
        if (component == null)
            return;

        component.Enabled = value;
        SetStoredComponentEnabled(component.Key, value);
        SaveSnapshotIfAvailable();
    }

    private void FocusSnapshotEntry(SnapshotLogEntry entry)
    {
        if (entry == null)
            return;

        if (!string.IsNullOrWhiteSpace(entry.ComponentKey))
        {
            _focusedGameObjectId = 0;
            _focusedComponentId = 0;
            _focusedSnapshotGameObjectKey = entry.GameObjectKey ?? string.Empty;
            _focusedSnapshotComponentKey = entry.ComponentKey ?? string.Empty;
            _focusedObjectName = !string.IsNullOrWhiteSpace(entry.GameObjectName) ? entry.GameObjectName : entry.SourceName ?? string.Empty;
            _focusedComponentName = !string.IsNullOrWhiteSpace(entry.ComponentName) ? entry.ComponentName : string.Empty;
            PrepareSnapshotSelectionExpansion(_focusedSnapshotGameObjectKey, true);
            SaveEditorUiState();
            return;
        }

        if (!string.IsNullOrWhiteSpace(entry.GameObjectKey))
        {
            _focusedGameObjectId = 0;
            _focusedComponentId = 0;
            _focusedSnapshotGameObjectKey = entry.GameObjectKey ?? string.Empty;
            _focusedSnapshotComponentKey = string.Empty;
            _focusedObjectName = !string.IsNullOrWhiteSpace(entry.GameObjectName) ? entry.GameObjectName : entry.SourceName ?? string.Empty;
            _focusedComponentName = string.Empty;
            PrepareSnapshotSelectionExpansion(_focusedSnapshotGameObjectKey, true);
            SaveEditorUiState();
        }
    }

    private void ToggleSnapshotGameObjectFocus(SnapshotGameObjectNode node)
    {
        if (node == null || string.IsNullOrWhiteSpace(node.PathKey))
            return;

        if (string.Equals(_focusedSnapshotGameObjectKey, node.PathKey, StringComparison.Ordinal) && string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey))
        {
            ClearFocus();
            return;
        }

        _focusedGameObjectId = 0;
        _focusedComponentId = 0;
        _focusedSnapshotGameObjectKey = node.PathKey;
        _focusedSnapshotComponentKey = string.Empty;
        _focusedObjectName = node.Name ?? string.Empty;
        _focusedComponentName = string.Empty;
        PrepareSnapshotSelectionExpansion(node.PathKey, true);
        SaveEditorUiState();
    }

    private void ToggleSnapshotComponentFocus(SnapshotGameObjectNode node, SnapshotComponentNode component)
    {
        if (node == null || component == null || string.IsNullOrWhiteSpace(component.Key))
            return;

        if (string.Equals(_focusedSnapshotComponentKey, component.Key, StringComparison.Ordinal))
        {
            ClearFocus();
            return;
        }

        _focusedGameObjectId = 0;
        _focusedComponentId = 0;
        _focusedSnapshotGameObjectKey = node.PathKey ?? string.Empty;
        _focusedSnapshotComponentKey = component.Key;
        _focusedObjectName = node.Name ?? string.Empty;
        _focusedComponentName = component.Name ?? string.Empty;
        PrepareSnapshotSelectionExpansion(_focusedSnapshotGameObjectKey, true);
        SaveEditorUiState();
    }

    private void PrepareSnapshotSelectionExpansion(string objectKey, bool includeDetails)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return;

        if (_collapsePreviousOnSelection)
            PreserveSnapshotExpansionWithinTopLevelRoot(objectKey);

        ExpandSnapshotSelectionPath(objectKey, includeDetails);
    }

    private void ExpandSnapshotSelectionPath(string objectKey, bool includeDetails)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return;

        if (includeDetails)
            _expandedSnapshotDetails.Add(objectKey);

        string current = objectKey;
        while (!string.IsNullOrWhiteSpace(current))
        {
            string parentKey = GetSnapshotParentObjectKey(current);
            if (string.IsNullOrWhiteSpace(parentKey))
                break;

            _expandedSnapshotDetails.Add(parentKey);
            _expandedSnapshotChildren.Add(parentKey);
            current = parentKey;
        }
    }

    private void PreserveSnapshotExpansionWithinTopLevelRoot(string objectKey)
    {
        string topLevelRootKey = GetSnapshotTopLevelRootKey(objectKey);
        if (string.IsNullOrWhiteSpace(topLevelRootKey))
        {
            _expandedSnapshotDetails.Clear();
            _expandedSnapshotChildren.Clear();
            return;
        }

        _expandedSnapshotDetails.RemoveWhere(key => !IsSameOrChildSnapshotObjectKey(key, topLevelRootKey));
        _expandedSnapshotChildren.RemoveWhere(key => !IsSameOrChildSnapshotObjectKey(key, topLevelRootKey));
    }

    private bool IsSameOrChildSnapshotObjectKey(string candidateKey, string ancestorKey)
    {
        if (string.IsNullOrWhiteSpace(candidateKey) || string.IsNullOrWhiteSpace(ancestorKey))
            return false;

        if (string.Equals(candidateKey, ancestorKey, StringComparison.Ordinal))
            return true;

        return candidateKey.StartsWith(ancestorKey + "/", StringComparison.Ordinal);
    }

    private string GetSnapshotTopLevelRootKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return string.Empty;

        int separatorIndex = objectKey.IndexOf('|');
        if (separatorIndex < 0)
            return objectKey;

        string sceneKey = objectKey.Substring(0, separatorIndex);
        string hierarchyPath = objectKey.Substring(separatorIndex + 1);
        if (string.IsNullOrWhiteSpace(hierarchyPath))
            return objectKey;

        int slashIndex = hierarchyPath.IndexOf('/');
        string topLevelSegment = slashIndex >= 0 ? hierarchyPath.Substring(0, slashIndex) : hierarchyPath;
        return $"{sceneKey}|{topLevelSegment}";
    }

    private string GetSnapshotParentObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return string.Empty;

        int separatorIndex = objectKey.IndexOf('|');
        if (separatorIndex < 0)
            return string.Empty;

        string sceneKey = objectKey.Substring(0, separatorIndex);
        string hierarchyPath = objectKey.Substring(separatorIndex + 1);
        if (string.IsNullOrWhiteSpace(hierarchyPath))
            return string.Empty;

        int lastSlashIndex = hierarchyPath.LastIndexOf('/');
        if (lastSlashIndex < 0)
            return string.Empty;

        return $"{sceneKey}|{hierarchyPath.Substring(0, lastSlashIndex)}";
    }

    private bool IsSnapshotObjectFocused(string objectKey)
    {
        return !string.IsNullOrWhiteSpace(objectKey) &&
               string.Equals(_focusedSnapshotGameObjectKey, objectKey, StringComparison.Ordinal) &&
               string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey);
    }

    private bool IsSnapshotComponentFocused(string componentKey)
    {
        return !string.IsNullOrWhiteSpace(componentKey) &&
               string.Equals(_focusedSnapshotComponentKey, componentKey, StringComparison.Ordinal);
    }

    private bool IsSnapshotFocusedObjectParent(string objectKey)
    {
        return !string.IsNullOrWhiteSpace(objectKey) &&
               !string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey) &&
               string.Equals(_focusedSnapshotGameObjectKey, objectKey, StringComparison.Ordinal);
    }

    private bool GetStoredGameObjectEnabled(string filterKey, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
            return defaultValue;

        return DebugConsolePreferenceStore.GetBool(GetGameObjectPrefKey(filterKey), defaultValue);
    }

    private bool GetStoredComponentEnabled(string filterKey, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
            return defaultValue;

        return DebugConsolePreferenceStore.GetBool(GetComponentPrefKey(filterKey), defaultValue);
    }

    private void SetStoredGameObjectEnabled(string filterKey, bool value)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
            return;

        string prefKey = GetGameObjectPrefKey(filterKey);
        RegisterStoredPrefKey(GameObjectRegistryPrefKey, prefKey);
        DebugConsolePreferenceStore.SetBool(prefKey, value);
    }

    private void SetStoredComponentEnabled(string filterKey, bool value)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
            return;

        string prefKey = GetComponentPrefKey(filterKey);
        RegisterStoredPrefKey(ComponentRegistryPrefKey, prefKey);
        DebugConsolePreferenceStore.SetBool(prefKey, value);
    }

    private void RegisterStoredPrefKey(string registryPrefKey, string prefKey)
    {
        if (string.IsNullOrWhiteSpace(registryPrefKey) || string.IsNullOrWhiteSpace(prefKey))
            return;

        HashSet<string> registry = LoadStoredRegistry(registryPrefKey);
        if (!registry.Add(prefKey))
            return;

        SaveStoredRegistry(registryPrefKey, registry);
    }

    private HashSet<string> LoadStoredRegistry(string registryPrefKey)
    {
        HashSet<string> result = new HashSet<string>();
        string raw = DebugConsolePreferenceStore.GetString(registryPrefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        string[] parts = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            result.Add(parts[i]);

        return result;
    }

    private void SaveStoredRegistry(string registryPrefKey, HashSet<string> registry)
    {
        if (registry == null || registry.Count == 0)
        {
            DebugConsolePreferenceStore.DeleteKey(registryPrefKey);
            return;
        }

        DebugConsolePreferenceStore.SetString(registryPrefKey, string.Join("\n", registry));
    }

    private string GetTypePrefKey(DebugType type)
    {
        return TypePrefKeyPrefix + type;
    }

    private string GetGameObjectPrefKey(string filterKey)
    {
        return GameObjectPrefKeyPrefix + filterKey;
    }

    private string GetComponentPrefKey(string filterKey)
    {
        return ComponentPrefKeyPrefix + filterKey;
    }

    private void SaveSnapshotIfAvailable()
    {
        SaveEditorUiState();

        if (_lastSnapshot != null)
            SaveSnapshotToDisk(_lastSnapshot);
    }

    private void DrawSnapshotToolbarToggleGroup(DebugConsoleEditorSnapshot snapshot)
    {
        bool global = GUILayout.Toggle(snapshot.GlobalEnabled, "Global", GUILayout.Width(80f));
        if (global != snapshot.GlobalEnabled)
        {
            snapshot.GlobalEnabled = global;
            DebugConsolePreferenceStore.SetBool(GlobalEnabledPrefKey, global);
            SaveSnapshotIfAvailable();
        }

        bool mirror = GUILayout.Toggle(snapshot.MirrorToUnityConsole, "Mirror Unity", GUILayout.Width(110f));
        if (mirror != snapshot.MirrorToUnityConsole)
        {
            snapshot.MirrorToUnityConsole = mirror;
            DebugConsolePreferenceStore.SetBool(MirrorToUnityPrefKey, mirror);
            SaveSnapshotIfAvailable();
        }

        bool autoScroll = GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100f));
        if (autoScroll != _autoScroll)
        {
            _autoScroll = autoScroll;
            SaveEditorUiState();
        }

        bool hideTransform = GUILayout.Toggle(_hideTransform, "Hide Transform", GUILayout.Width(120f));
        if (hideTransform != _hideTransform)
        {
            _hideTransform = hideTransform;
            SaveEditorUiState();
        }

        bool collapsePrevious = GUILayout.Toggle(_collapsePreviousOnSelection, "Collapse Prev", GUILayout.Width(120f));
        if (collapsePrevious != _collapsePreviousOnSelection)
        {
            _collapsePreviousOnSelection = collapsePrevious;
            SaveEditorUiState();
        }

        bool collapseLogs = GUILayout.Toggle(_collapseLogs, "Collapse Logs", GUILayout.Width(120f));
        if (collapseLogs != _collapseLogs)
        {
            _collapseLogs = collapseLogs;
            SaveEditorUiState();
        }

        bool showLogs = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Log), "Log", GUILayout.Width(70f));
        if (showLogs != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Log))
            SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Log, showLogs);

        bool showWarnings = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning), "Warn", GUILayout.Width(75f));
        if (showWarnings != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning))
            SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning, showWarnings);

        bool showErrors = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Error), "Error", GUILayout.Width(75f));
        if (showErrors != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Error))
            SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Error, showErrors);
    }

    private void DrawSnapshotToolbarToggleGroupWrapped(DebugConsoleEditorSnapshot snapshot, float availableWidth)
    {
        for (int index = 0; index < _toolbarToggleItems.Length;)
        {
            int endIndex = GetWrappedEndIndex(_toolbarToggleItems, index, availableWidth);
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawSnapshotToolbarToggleItems(snapshot, index, endIndex);
            EditorGUILayout.EndHorizontal();
            index = endIndex;
        }
    }

    private void DrawSnapshotToolbarToggleItems(DebugConsoleEditorSnapshot snapshot, int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            switch (_toolbarToggleItems[i].label)
            {
                case "Global":
                {
                    bool value = GUILayout.Toggle(snapshot.GlobalEnabled, "Global", GUILayout.Width(80f));
                    if (value != snapshot.GlobalEnabled)
                    {
                        snapshot.GlobalEnabled = value;
                        DebugConsolePreferenceStore.SetBool(GlobalEnabledPrefKey, value);
                        SaveSnapshotIfAvailable();
                    }
                    break;
                }

                case "Mirror Unity":
                {
                    bool value = GUILayout.Toggle(snapshot.MirrorToUnityConsole, "Mirror Unity", GUILayout.Width(110f));
                    if (value != snapshot.MirrorToUnityConsole)
                    {
                        snapshot.MirrorToUnityConsole = value;
                        DebugConsolePreferenceStore.SetBool(MirrorToUnityPrefKey, value);
                        SaveSnapshotIfAvailable();
                    }
                    break;
                }

                case "Auto Scroll":
                {
                    bool value = GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100f));
                    if (value != _autoScroll)
                    {
                        _autoScroll = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Hide Transform":
                {
                    bool value = GUILayout.Toggle(_hideTransform, "Hide Transform", GUILayout.Width(120f));
                    if (value != _hideTransform)
                    {
                        _hideTransform = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Collapse Prev":
                {
                    bool value = GUILayout.Toggle(_collapsePreviousOnSelection, "Collapse Prev", GUILayout.Width(120f));
                    if (value != _collapsePreviousOnSelection)
                    {
                        _collapsePreviousOnSelection = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Collapse Logs":
                {
                    bool value = GUILayout.Toggle(_collapseLogs, "Collapse Logs", GUILayout.Width(120f));
                    if (value != _collapseLogs)
                    {
                        _collapseLogs = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Log":
                {
                    bool value = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Log), "Log", GUILayout.Width(70f));
                    if (value != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Log))
                        SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Log, value);
                    break;
                }

                case "Warn":
                {
                    bool value = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning), "Warn", GUILayout.Width(75f));
                    if (value != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning))
                        SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning, value);
                    break;
                }

                case "Error":
                {
                    bool value = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Error), "Error", GUILayout.Width(75f));
                    if (value != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Error))
                        SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Error, value);
                    break;
                }
            }

            if (i < endIndex - 1)
                GUILayout.Space(8f);
        }
    }

private void DrawSnapshotToolbarActionGroup(DebugConsoleEditorSnapshot snapshot, string typeButtonLabel)
{
    if (GUILayout.Button(typeButtonLabel, GUILayout.Width(160f)))
    {
        _showTypeFilterPanel = !_showTypeFilterPanel;
        SaveEditorUiState();
    }

    if (GUILayout.Button("All Types On", GUILayout.Width(100f)))
        SetAllSnapshotTypes(snapshot, true);

    if (GUILayout.Button("All Types Off", GUILayout.Width(100f)))
        SetAllSnapshotTypes(snapshot, false);

    if (GUILayout.Button("All Levels", GUILayout.Width(100f)))
        SetAllSnapshotLevels(snapshot, true);

    if (GUILayout.Button("Warn+", GUILayout.Width(80f)))
        SetSnapshotWarningAndErrorOnly(snapshot);

    if (GUILayout.Button("Error Only", GUILayout.Width(100f)))
        SetSnapshotErrorOnly(snapshot);

    if (GUILayout.Button("Clear Logs", GUILayout.Width(100f)))
    {
        snapshot.Entries?.Clear();
        _selectedLogIndex = -1;
        SaveSnapshotIfAvailable();
    }

    if (GUILayout.Button("Clear Focus", GUILayout.Width(100f)))
        ClearFocus();

    if (GUILayout.Button("Reset Filters", GUILayout.Width(110f)))
        ResetStoredManagerPreferencesToDefault(snapshot);

    if (GUILayout.Button("Reset Layout", GUILayout.Width(110f)))
        ResetEditorLayoutToDefault();

    if (GUILayout.Button("Export Settings", GUILayout.Width(120f)))
        ExportEditorSettingsToFile();

    if (GUILayout.Button("Import Settings", GUILayout.Width(120f)))
        ImportEditorSettingsFromFile(snapshot);
}

private void DrawSnapshotToolbarActionGroupWrapped(DebugConsoleEditorSnapshot snapshot, string typeButtonLabel, float availableWidth)
    {
        for (int index = 0; index < _toolbarActionItems.Length;)
        {
            int endIndex = GetWrappedEndIndex(_toolbarActionItems, index, availableWidth);
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawSnapshotToolbarActionItems(snapshot, typeButtonLabel, index, endIndex);
            EditorGUILayout.EndHorizontal();
            index = endIndex;
        }
    }

    private void DrawSnapshotToolbarActionItems(DebugConsoleEditorSnapshot snapshot, string typeButtonLabel, int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            string key = _toolbarActionItems[i].label;
            float width = _toolbarActionItems[i].width;

            switch (key)
            {
                case "TypeFilter":
                    if (GUILayout.Button(typeButtonLabel, GUILayout.Width(width)))
                    {
                        _showTypeFilterPanel = !_showTypeFilterPanel;
                        SaveEditorUiState();
                    }
                    break;

                case "All Types On":
                    if (GUILayout.Button("All Types On", GUILayout.Width(width)))
                        SetAllSnapshotTypes(snapshot, true);
                    break;

                case "All Types Off":
                    if (GUILayout.Button("All Types Off", GUILayout.Width(width)))
                        SetAllSnapshotTypes(snapshot, false);
                    break;

                case "All Levels":
                    if (GUILayout.Button("All Levels", GUILayout.Width(width)))
                        SetAllSnapshotLevels(snapshot, true);
                    break;

                case "Warn+":
                    if (GUILayout.Button("Warn+", GUILayout.Width(width)))
                        SetSnapshotWarningAndErrorOnly(snapshot);
                    break;

                case "Error Only":
                    if (GUILayout.Button("Error Only", GUILayout.Width(width)))
                        SetSnapshotErrorOnly(snapshot);
                    break;

                case "Clear Logs":
                    if (GUILayout.Button("Clear Logs", GUILayout.Width(width)))
                    {
                        snapshot.Entries?.Clear();
                        _selectedLogIndex = -1;
                        SaveSnapshotIfAvailable();
                    }
                    break;

                case "Clear Focus":
                    if (GUILayout.Button("Clear Focus", GUILayout.Width(width)))
                        ClearFocus();
                    break;

                case "Reset Filters":
                    if (GUILayout.Button("Reset Filters", GUILayout.Width(width)))
                        ResetStoredManagerPreferencesToDefault(null);
                    break;

                case "Reset Layout":
                    if (GUILayout.Button("Reset Layout", GUILayout.Width(width)))
                        ResetEditorLayoutToDefault();
                    break;

                case "Export Settings":
                    if (GUILayout.Button("Export Settings", GUILayout.Width(width)))
                        ExportEditorSettingsToFile();
                    break;

                case "Import Settings":
                    if (GUILayout.Button("Import Settings", GUILayout.Width(width)))
                        ImportEditorSettingsFromFile(_lastSnapshot);
                    break;
            }

            if (i < endIndex - 1)
                GUILayout.Space(8f);
        }
    }

    private void InitStyles()
    {
        if (!_stylesDirty && _titleStyle != null)
            return;

        _stylesDirty = false;

        _titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            wordWrap = false,
            clipping = TextClipping.Clip
        };

        _boxStyle = new GUIStyle("box")
        {
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(8, 8, 8, 8)
        };

        _richLabelStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            wordWrap = true,
            fontSize = 12
        };

        _dimLabelStyle = new GUIStyle(EditorStyles.label);
        _dimLabelStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);

        _searchTextFieldStyle = new GUIStyle(EditorStyles.textField)
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

        _toolbarInfoLabelStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
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

        _footerLeftLabelStyle = new GUIStyle(EditorStyles.label)
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

    private void DrawToolbar(DebugConsoleManager manager)
    {
        float availableWidth = GetTopAreaWidth();

        int enabledCount = GetEnabledTypeCount(manager);
        int totalCount = Enum.GetValues(typeof(DebugType)).Length;
        string typeButtonLabel = _showTypeFilterPanel
            ? $"Type Filter ▲ ({enabledCount}/{totalCount})"
            : $"Type Filter ▼ ({enabledCount}/{totalCount})";

        if (availableWidth >= 1500f)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawToolbarToggleGroup(manager);
            GUILayout.Space(8f);
            DrawToolbarActionGroup(manager, typeButtonLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
            return;
        }

        DrawToolbarToggleGroupWrapped(manager, availableWidth);
        DrawToolbarActionGroupWrapped(manager, typeButtonLabel, availableWidth);
        GUILayout.Space(4f);
    }

    private void DrawSearchBar()
    {
        float availableWidth = GetTopAreaWidth();

        if (availableWidth >= 760f)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
            float hierarchyWidth = Mathf.Clamp((availableWidth - 120f) * 0.38f, 160f, 260f);
            float logWidth = Mathf.Clamp((availableWidth - hierarchyWidth) - 24f, 220f, 320f);
            DrawHierarchySearchField(hierarchyWidth);
            GUILayout.Space(12f);
            DrawLogSearchField(logWidth);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
            return;
        }

        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
        DrawHierarchySearchField(availableWidth);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
        DrawLogSearchField(availableWidth);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4f);
    }

    private void DrawTypeFilterPanel(DebugConsoleManager manager)
    {
        if (!_showTypeFilterPanel)
            return;

        EditorGUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("DebugType Filter", _titleStyle);

        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));

        const float minItemWidth = 120f;
        const float itemSpacing = 12f;
        float availableWidth = Mathf.Max(220f, position.width - 44f);
        int columns = Mathf.Clamp(Mathf.FloorToInt((availableWidth + itemSpacing) / (minItemWidth + itemSpacing)), 1, types.Length);
        float itemWidth = Mathf.Floor((availableWidth - itemSpacing * (columns - 1)) / columns);
        itemWidth = Mathf.Max(minItemWidth, itemWidth);

        int rows = Mathf.CeilToInt(types.Length / (float)columns);
        float viewHeight = Mathf.Min(120f, rows * 22f + Mathf.Max(0, rows - 1) * 4f + 6f);

        _typeFilterScroll = EditorGUILayout.BeginScrollView(_typeFilterScroll, GUILayout.Height(viewHeight));

        for (int row = 0; row < types.Length; row += columns)
        {
            EditorGUILayout.BeginHorizontal();

            for (int col = 0; col < columns; col++)
            {
                int index = row + col;
                if (index >= types.Length)
                    break;

                DebugType type = types[index];
                bool current = manager.GetTypeEnabled(type);
                bool next = GUILayout.Toggle(current, type.ToString(), GUILayout.Width(itemWidth));

                if (next != current)
                    manager.SetTypeEnabled(type, next);

                if (col < columns - 1)
                    GUILayout.Space(itemSpacing);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        GUILayout.Space(4f);
    }

    private void DrawResizablePanels(DebugConsoleManager manager)
    {
        float contentWidth = Mathf.Max(620f, position.width - 24f);
        float maxHierarchyPanelWidth = Mathf.Max(MinHierarchyPanelWidth, contentWidth - MinLogPanelWidth - PanelSplitterWidth);

        if (_hierarchyPanelWidth <= 0f)
            _hierarchyPanelWidth = contentWidth * 0.42f;

        _hierarchyPanelWidth = Mathf.Clamp(_hierarchyPanelWidth, MinHierarchyPanelWidth, maxHierarchyPanelWidth);
        float logPanelWidth = Mathf.Max(MinLogPanelWidth, contentWidth - _hierarchyPanelWidth - PanelSplitterWidth);

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawHierarchyPanel(manager, _hierarchyPanelWidth);
        DrawPanelSplitter(contentWidth);
        DrawLogPanel(manager, logPanelWidth);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPanelSplitter(float contentWidth)
    {
        Rect splitterRect = GUILayoutUtility.GetRect(PanelSplitterWidth, 10f, GUILayout.Width(PanelSplitterWidth), GUILayout.ExpandHeight(true));
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

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
            Repaint();
        }

        if (_isDraggingPanelSplitter && (current.type == EventType.MouseUp || current.rawType == EventType.MouseUp))
        {
            _isDraggingPanelSplitter = false;
            DebugConsolePreferenceStore.SetFloat(HierarchyPanelWidthPrefKey, _hierarchyPanelWidth);
            SaveEditorUiState();
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
        GUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
        DrawHierarchySearchField(panelWidth - 20f);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4f);

        _hierarchyScroll = GUILayout.BeginScrollView(_hierarchyScroll);

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
            DrawGameObjectNode(manager, roots[i], 0, panelWidth);

        GUILayout.EndScrollView();

        GUILayout.Space(4f);
        string footerCountText = $"Count : {GetVisibleEntryCount(manager)}";
        GUILayout.BeginHorizontal(_boxStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(28f));
        GUILayout.Space(10f);
        GUILayout.Label(new GUIContent(footerCountText, footerCountText), _footerLeftLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(20f));
        GUILayout.Space(10f);
        GUILayout.EndHorizontal();
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
        float scrollbarReserve = _autoScroll ? 34f : 58f;
        return Mathf.Max(140f, panelWidth - _boxStyle.padding.left - _boxStyle.padding.right - scrollbarReserve);
    }

    private GUIStyle GetLogVerticalScrollbarStyle()
    {
        return _autoScroll ? GUIStyle.none : GUI.skin.verticalScrollbar;
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
                ToggleLiveExpandedDetails(go);
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
                ToggleLiveExpandedChildren(go);

            string childFoldoutLabel = showChildren ? "▾" : "▸";
            if (GUILayout.Button(childFoldoutLabel, _foldoutButtonStyle, GUILayout.Width(HierarchyFoldoutSize), GUILayout.Height(HierarchyRowHeight)))
                ToggleLiveExpandedChildren(go);

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
    EditorGUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));
    EditorGUILayout.BeginHorizontal();
    GUILayout.Label("Logs", _titleStyle, GUILayout.ExpandWidth(true));
    bool nextShowLogDetails = GUILayout.Toggle(_showLogDetails, "Details", GUILayout.Width(70f));
    if (nextShowLogDetails != _showLogDetails)
    {
        _showLogDetails = nextShowLogDetails;
        SaveEditorUiState();
    }
    EditorGUILayout.EndHorizontal();
    GUILayout.Space(4f);
    EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
    DrawLogSearchField(panelWidth - 20f);
    EditorGUILayout.EndHorizontal();
    GUILayout.Space(4f);

    float width = Mathf.Max(GetLogContentWidth(panelWidth), 180f);
    float listViewportHeight = Mathf.Max(120f, position.height - (_showLogDetails ? _logDetailPanelHeight + 240f : 190f));

    GetVisibleLiveLogGroupsAndHeights(manager, width, out List<LiveLogGroup> groups, out List<float> rowHeights);
    CalculateVisibleRange(rowHeights, _logScroll.y, listViewportHeight, out int startIndex, out int endIndex, out float topPadding, out float visibleHeight, out float totalHeight);

    _logScroll = GUILayout.BeginScrollView(_logScroll, false, !_autoScroll, GUIStyle.none, GetLogVerticalScrollbarStyle(), GUILayout.MinHeight(listViewportHeight), GUILayout.ExpandHeight(true));

    if (topPadding > 0f)
        GUILayout.Space(topPadding);

    for (int i = startIndex; i < endIndex; i++)
    {
        LiveLogGroup group = groups[i];
        DrawLogEntry(group.Entry, group.LastSourceIndex, width, group.Count);
        GUILayout.Space(4f);
    }

    float bottomPadding = Mathf.Max(0f, totalHeight - topPadding - visibleHeight);
    if (bottomPadding > 0f)
        GUILayout.Space(bottomPadding);

    GUILayout.EndScrollView();

    Rect scrollRect = GUILayoutUtility.GetLastRect();
    _lastLogViewportHeight = scrollRect.height;
    _lastLogContentHeight = totalHeight;
    _lastMaxLogScrollY = Mathf.Max(0f, _lastLogContentHeight - _lastLogViewportHeight);

    if (Event.current.type == EventType.Repaint && _autoScroll)
        _logScroll.y = _lastMaxLogScrollY + 4f;

    if (_showLogDetails)
    {
        GUILayout.Space(4f);
        DebugEntry selectedEntry = GetSelectedLiveEntry(manager);
        DrawLiveLogDetailPanel(selectedEntry, panelWidth);
    }

    EditorGUILayout.EndVertical();
}

private float DrawLogEntry(DebugEntry entry, int sourceIndex, float width, int repeatCount)
    {
        GUIContent content = BuildCollapsedLogContent(UseCompactLogRows ? entry.SummaryRichText : entry.RichText, repeatCount);
        float rowHeight = UseCompactLogRows ? CompactLogRowHeight : _richLabelStyle.CalcHeight(content, width) + 12f;

        Rect rect = GUILayoutUtility.GetRect(10f, rowHeight, GUILayout.ExpandWidth(true));

        Color previousColor = GUI.color;
        if (sourceIndex == _selectedLogIndex)
            GUI.color = new Color(0.75f, 0.85f, 1f, 1f);

        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;

        Rect labelRect = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f);
        GUI.Label(labelRect, content, _richLabelStyle);

        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 0 &&
            rect.Contains(Event.current.mousePosition))
        {
            _selectedLogIndex = sourceIndex;
            FocusEntry(entry);
            SaveEditorUiState();

            if (Event.current.clickCount >= 2)
                OpenEntryScript(entry);

            Event.current.Use();
        }

        return rect.height;
    }

    private string BuildSnapshotCompactRichText(SnapshotLogEntry entry)
    {
        if (entry == null)
            return string.Empty;

        return $"<color={entry.ColorHex}>[{entry.Time}] [{entry.Type}] {entry.Message}</color> <color=#daa520>| [{entry.SourceName}.{entry.MemberName} : {entry.LineNumber}]</color>";
    }

private List<float> BuildSnapshotRowHeights(List<SnapshotLogGroup> groups, float width)
{
    List<float> heights = new List<float>(groups.Count);
    float rowHeight = UseCompactLogRows ? CompactLogRowHeight : 0f;

    for (int i = 0; i < groups.Count; i++)
    {
        if (UseCompactLogRows)
        {
            heights.Add(rowHeight);
            continue;
        }

        SnapshotLogGroup group = groups[i];
        string key = $"{group.LastSourceIndex}:{group.Count}:{Mathf.RoundToInt(width)}";
        if (!_snapshotLogHeightCache.TryGetValue(key, out float height))
        {
            GUIContent content = BuildCollapsedLogContent(group.Entry.RichText, group.Count);
            height = _richLabelStyle.CalcHeight(content, width) + 16f;
            _snapshotLogHeightCache[key] = height;
        }

        heights.Add(height);
    }

    return heights;
}

private List<float> BuildLiveRowHeights(List<LiveLogGroup> groups, float width)
{
    List<float> heights = new List<float>(groups.Count);
    float rowHeight = UseCompactLogRows ? CompactLogRowHeight : 0f;

    for (int i = 0; i < groups.Count; i++)
    {
        if (UseCompactLogRows)
        {
            heights.Add(rowHeight);
            continue;
        }

        LiveLogGroup group = groups[i];
        long sequence = group.Entry != null ? group.Entry.SequenceId : i;
        string key = $"{sequence}:{group.Count}:{Mathf.RoundToInt(width)}";
        if (!_liveLogHeightCache.TryGetValue(key, out float height))
        {
            GUIContent content = BuildCollapsedLogContent(group.Entry.RichText, group.Count);
            height = _richLabelStyle.CalcHeight(content, width) + 16f;
            _liveLogHeightCache[key] = height;
        }

        heights.Add(height);
    }

    return heights;
}

private void CalculateVisibleRange(List<float> rowHeights, float scrollY, float viewportHeight, out int startIndex, out int endIndex, out float topPadding, out float visibleHeight, out float totalHeight)
{
    startIndex = 0;
    endIndex = rowHeights != null ? rowHeights.Count : 0;
    topPadding = 0f;
    visibleHeight = 0f;
    totalHeight = 0f;

    if (rowHeights == null || rowHeights.Count == 0)
        return;

    const float overscan = 240f;
    float minY = Mathf.Max(0f, scrollY - overscan);
    float maxY = scrollY + Mathf.Max(0f, viewportHeight) + overscan;

    float cumulative = 0f;
    bool started = false;

    for (int i = 0; i < rowHeights.Count; i++)
    {
        float rowHeight = rowHeights[i];
        float rowStart = cumulative;
        float rowEnd = cumulative + rowHeight;
        totalHeight = rowEnd;

        if (!started && rowEnd >= minY)
        {
            startIndex = i;
            topPadding = rowStart;
            started = true;
        }

        if (started)
        {
            visibleHeight += rowHeight;
            endIndex = i + 1;
            if (rowStart > maxY)
                break;
        }

        cumulative = rowEnd;
    }

    if (!started)
    {
        startIndex = 0;
        endIndex = rowHeights.Count;
        topPadding = 0f;
        visibleHeight = totalHeight;
    }
}

private SnapshotLogEntry GetSelectedSnapshotEntry(DebugConsoleEditorSnapshot snapshot)
{
    if (snapshot?.Entries == null || _selectedLogIndex < 0 || _selectedLogIndex >= snapshot.Entries.Count)
        return null;

    return snapshot.Entries[_selectedLogIndex];
}

private DebugEntry GetSelectedLiveEntry(DebugConsoleManager manager)
{
    if (manager == null)
        return null;

    IReadOnlyList<DebugEntry> entries = manager.Entries;
    if (_selectedLogIndex < 0 || _selectedLogIndex >= entries.Count)
        return null;

    return entries[_selectedLogIndex];
}

private void DrawSnapshotLogDetailPanel(SnapshotLogEntry entry, float panelWidth)
{
    EditorGUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.Height(_logDetailPanelHeight));
    GUILayout.Label("Log Detail", _titleStyle);

    if (entry == null)
    {
        EditorGUILayout.HelpBox("로그를 선택하면 상세 정보가 표시됩니다.", MessageType.Info);
        EditorGUILayout.EndVertical();
        return;
    }

    _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.Height(_logDetailPanelHeight - 28f));
    DrawDetailHeader(entry.Time, entry.Type.ToString(), entry.Level.ToString(), entry.SourceName, entry.MemberName, entry.LineNumber, entry.SceneKey, entry.HierarchyPath, entry.GameObjectName, entry.ComponentName, entry.ComponentTypeName, entry.FrameCount, entry.CapturedAtIsoUtc);
    DrawDetailBody(entry.Message, entry.CallerFilePath, entry.StackTrace);
    EditorGUILayout.EndScrollView();
    EditorGUILayout.EndVertical();
}

private void DrawLiveLogDetailPanel(DebugEntry entry, float panelWidth)
{
    EditorGUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.Height(_logDetailPanelHeight));
    GUILayout.Label("Log Detail", _titleStyle);

    if (entry == null)
    {
        EditorGUILayout.HelpBox("로그를 선택하면 상세 정보가 표시됩니다.", MessageType.Info);
        EditorGUILayout.EndVertical();
        return;
    }

    _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.Height(_logDetailPanelHeight - 28f));
    DrawDetailHeader(entry.Time, entry.Type.ToString(), entry.Level.ToString(), entry.SourceName, entry.MemberName, entry.LineNumber, entry.SceneKey, entry.HierarchyPath, entry.GameObjectName, entry.ComponentName, entry.ComponentTypeName, entry.FrameCount, entry.CapturedAtIsoUtc);
    DrawDetailBody(entry.Message, entry.CallerFilePath, entry.StackTrace);
    EditorGUILayout.EndScrollView();
    EditorGUILayout.EndVertical();
}

private void DrawDetailHeader(string time, string type, string level, string sourceName, string memberName, int lineNumber, string sceneKey, string hierarchyPath, string gameObjectName, string componentName, string componentTypeName, int frameCount, string capturedAtIsoUtc)
{
    EditorGUILayout.LabelField("Time", string.IsNullOrWhiteSpace(time) ? "-" : time);
    EditorGUILayout.LabelField("Type", $"{type} / {level}");
    EditorGUILayout.LabelField("Source", string.IsNullOrWhiteSpace(sourceName) ? "-" : sourceName);
    EditorGUILayout.LabelField("Member", $"{memberName} : {Mathf.Max(1, lineNumber)}");
    EditorGUILayout.LabelField("Scene", string.IsNullOrWhiteSpace(sceneKey) ? "-" : sceneKey);
    EditorGUILayout.LabelField("Path", string.IsNullOrWhiteSpace(hierarchyPath) ? "-" : hierarchyPath);
    EditorGUILayout.LabelField("GameObject", string.IsNullOrWhiteSpace(gameObjectName) ? "-" : gameObjectName);
    EditorGUILayout.LabelField("Component", string.IsNullOrWhiteSpace(componentName) ? "-" : componentName);
    if (!string.IsNullOrWhiteSpace(componentTypeName))
        EditorGUILayout.LabelField("Component Type", componentTypeName);
    EditorGUILayout.LabelField("Frame", frameCount.ToString());
    EditorGUILayout.LabelField("Captured", string.IsNullOrWhiteSpace(capturedAtIsoUtc) ? "-" : capturedAtIsoUtc);
    GUILayout.Space(4f);
}

private void DrawDetailBody(string message, string callerFilePath, string stackTrace)
{
    GUILayout.Label("Message");
    bool previousEnabled = GUI.enabled;
    GUI.enabled = false;
    EditorGUILayout.TextArea(message ?? string.Empty, GUILayout.MinHeight(68f));
    GUI.enabled = previousEnabled;

    if (!string.IsNullOrWhiteSpace(callerFilePath))
        EditorGUILayout.LabelField("Caller File", callerFilePath);

    bool nextFoldout = EditorGUILayout.Foldout(_stackTraceFoldout, "Stack Trace", true);
    if (nextFoldout != _stackTraceFoldout)
    {
        _stackTraceFoldout = nextFoldout;
        SaveEditorUiState();
    }

    if (_stackTraceFoldout)
    {
        GUI.enabled = false;
        EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(stackTrace) ? "(No Stack Trace)" : stackTrace, GUILayout.MinHeight(88f));
        GUI.enabled = previousEnabled;
    }
}

private void ResetEditorLayoutToDefault()
{
    _autoScroll = true;
    _hideTransform = true;
    _collapsePreviousOnSelection = true;
    _collapseLogs = true;
    _showTypeFilterPanel = false;
    _showLogDetails = true;
    _stackTraceFoldout = true;
    _hierarchyPanelWidth = 420f;
    _logDetailPanelHeight = DefaultLogDetailPanelHeight;
    _hierarchyScroll = Vector2.zero;
    _logScroll = Vector2.zero;
    _typeFilterScroll = Vector2.zero;
    _detailScroll = Vector2.zero;
    SaveEditorUiState();
    Repaint();
}

private void ResetStoredManagerPreferencesToDefault(DebugConsoleEditorSnapshot snapshot)
{
    DebugConsolePreferenceStore.DeleteKey(GlobalEnabledPrefKey);
    DebugConsolePreferenceStore.DeleteKey(MirrorToUnityPrefKey);
    DebugConsolePreferenceStore.DeleteKey(LogLevelLogPrefKey);
    DebugConsolePreferenceStore.DeleteKey(LogLevelWarningPrefKey);
    DebugConsolePreferenceStore.DeleteKey(LogLevelErrorPrefKey);

    DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
    for (int i = 0; i < types.Length; i++)
        DebugConsolePreferenceStore.DeleteKey(GetTypePrefKey(types[i]));

    DeleteRegistryPrefValues(GameObjectRegistryPrefKey);
    DeleteRegistryPrefValues(ComponentRegistryPrefKey);

    DebugConsolePreferenceStore.DeleteKey(GameObjectRegistryPrefKey);
    DebugConsolePreferenceStore.DeleteKey(ComponentRegistryPrefKey);

    if (snapshot != null)
    {
        snapshot.GlobalEnabled = true;
        snapshot.MirrorToUnityConsole = false;
        snapshot.ShowLogLevelLog = true;
        snapshot.ShowLogLevelWarning = true;
        snapshot.ShowLogLevelError = true;

        if (snapshot.TypeFilters != null)
        {
            for (int i = 0; i < snapshot.TypeFilters.Length; i++)
                snapshot.TypeFilters[i] = true;
        }

        SaveSnapshotIfAvailable();
    }

    SaveEditorUiState();
}

private void DeleteRegistryPrefValues(string registryPrefKey)
{
    string raw = DebugConsolePreferenceStore.GetString(registryPrefKey, string.Empty);
    if (string.IsNullOrWhiteSpace(raw))
        return;

    string[] parts = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < parts.Length; i++)
        DebugConsolePreferenceStore.DeleteKey(parts[i]);
}

private DebugConsoleStoredManagerSettings CaptureStoredManagerSettings()
{
    DebugConsoleStoredManagerSettings settings = new DebugConsoleStoredManagerSettings
    {
        GlobalEnabled = DebugConsolePreferenceStore.GetBool(GlobalEnabledPrefKey, true),
        MirrorToUnityConsole = DebugConsolePreferenceStore.GetBool(MirrorToUnityPrefKey, false),
        ShowLogLevelLog = DebugConsolePreferenceStore.GetBool(LogLevelLogPrefKey, true),
        ShowLogLevelWarning = DebugConsolePreferenceStore.GetBool(LogLevelWarningPrefKey, true),
        ShowLogLevelError = DebugConsolePreferenceStore.GetBool(LogLevelErrorPrefKey, true)
    };

    DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
    settings.TypeFilters = new bool[types.Length];
    for (int i = 0; i < types.Length; i++)
        settings.TypeFilters[i] = DebugConsolePreferenceStore.GetBool(GetTypePrefKey(types[i]), true);

    settings.GameObjectFilterValues = CaptureStoredRegistryValues(GameObjectRegistryPrefKey);
    settings.ComponentFilterValues = CaptureStoredRegistryValues(ComponentRegistryPrefKey);
    return settings;
}

private List<DebugConsoleStoredBoolValue> CaptureStoredRegistryValues(string registryPrefKey)
{
    List<DebugConsoleStoredBoolValue> values = new List<DebugConsoleStoredBoolValue>();
    string raw = DebugConsolePreferenceStore.GetString(registryPrefKey, string.Empty);
    if (string.IsNullOrWhiteSpace(raw))
        return values;

    string[] parts = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < parts.Length; i++)
    {
        string key = parts[i];
        if (string.IsNullOrWhiteSpace(key))
            continue;

        values.Add(new DebugConsoleStoredBoolValue
        {
            Key = key,
            Value = DebugConsolePreferenceStore.GetBool(key, true)
        });
    }

    return values;
}

private void ApplyStoredManagerSettings(DebugConsoleStoredManagerSettings settings, DebugConsoleEditorSnapshot snapshot)
{
    if (settings == null)
        return;

    ResetStoredManagerPreferencesToDefault(snapshot);

    DebugConsolePreferenceStore.SetBool(GlobalEnabledPrefKey, settings.GlobalEnabled);
    DebugConsolePreferenceStore.SetBool(MirrorToUnityPrefKey, settings.MirrorToUnityConsole);
    DebugConsolePreferenceStore.SetBool(LogLevelLogPrefKey, settings.ShowLogLevelLog);
    DebugConsolePreferenceStore.SetBool(LogLevelWarningPrefKey, settings.ShowLogLevelWarning);
    DebugConsolePreferenceStore.SetBool(LogLevelErrorPrefKey, settings.ShowLogLevelError);

    DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
    if (settings.TypeFilters != null)
    {
        for (int i = 0; i < types.Length && i < settings.TypeFilters.Length; i++)
            DebugConsolePreferenceStore.SetBool(GetTypePrefKey(types[i]), settings.TypeFilters[i]);
    }

    ApplyStoredRegistryValues(GameObjectRegistryPrefKey, settings.GameObjectFilterValues);
    ApplyStoredRegistryValues(ComponentRegistryPrefKey, settings.ComponentFilterValues);

    if (snapshot != null)
        ApplyStoredPreferencesToSnapshot(snapshot);
}

private void ApplyStoredRegistryValues(string registryPrefKey, List<DebugConsoleStoredBoolValue> values)
{
    List<string> registry = new List<string>();

    if (values != null)
    {
        for (int i = 0; i < values.Count; i++)
        {
            DebugConsoleStoredBoolValue value = values[i];
            if (value == null || string.IsNullOrWhiteSpace(value.Key))
                continue;

            registry.Add(value.Key);
            DebugConsolePreferenceStore.SetBool(value.Key, value.Value);
        }
    }

    DebugConsolePreferenceStore.SetString(registryPrefKey, string.Join("\n", registry));
}

private void ExportEditorSettingsToFile()
{
    string path = EditorUtility.SaveFilePanel("Export Debug Console Settings", Application.dataPath, "debug_console_settings", "json");
    if (string.IsNullOrWhiteSpace(path))
        return;

    DebugConsoleEditorBackupData data = new DebugConsoleEditorBackupData
    {
        ManagerSettings = CaptureStoredManagerSettings(),
        EditorState = BuildCurrentUiState()
    };

    File.WriteAllText(path, JsonUtility.ToJson(data, true));
}

private void ImportEditorSettingsFromFile(DebugConsoleEditorSnapshot snapshot)
{
    string path = EditorUtility.OpenFilePanel("Import Debug Console Settings", Application.dataPath, "json");
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        return;

    DebugConsoleEditorBackupData data = JsonUtility.FromJson<DebugConsoleEditorBackupData>(File.ReadAllText(path));
    if (data == null)
        return;

    ApplyStoredManagerSettings(data.ManagerSettings, snapshot);
    ApplyEditorUiState(data.EditorState);
    SaveEditorUiState();
    Repaint();
}

private DebugConsoleEditorUiState BuildCurrentUiState()
{
    return new DebugConsoleEditorUiState
    {
        AutoScroll = _autoScroll,
        HideTransform = _hideTransform,
        CollapsePreviousOnSelection = _collapsePreviousOnSelection,
        CollapseLogs = _collapseLogs,
        ShowTypeFilterPanel = _showTypeFilterPanel,
        ShowLogDetails = _showLogDetails,
        StackTraceFoldout = _stackTraceFoldout,
        HierarchyPanelWidth = _hierarchyPanelWidth,
        LogDetailPanelHeight = _logDetailPanelHeight,
        HierarchyScroll = _hierarchyScroll,
        LogScroll = _logScroll,
        TypeFilterScroll = _typeFilterScroll,
        DetailScroll = _detailScroll,
        SelectedLogIndex = _selectedLogIndex,
        FocusedSnapshotGameObjectKey = _focusedSnapshotGameObjectKey ?? string.Empty,
        FocusedSnapshotComponentKey = _focusedSnapshotComponentKey ?? string.Empty,
        FocusedObjectName = _focusedObjectName ?? string.Empty,
        FocusedComponentName = _focusedComponentName ?? string.Empty,
        ExpandedSnapshotDetails = new List<string>(_expandedSnapshotDetails),
        ExpandedSnapshotChildren = new List<string>(_expandedSnapshotChildren)
    };
}

private void ApplyEditorUiState(DebugConsoleEditorUiState state)
{
    if (state == null)
        return;

    _autoScroll = state.AutoScroll;
    _hideTransform = state.HideTransform;
    _collapsePreviousOnSelection = state.CollapsePreviousOnSelection;
    _collapseLogs = state.CollapseLogs;
    _showTypeFilterPanel = state.ShowTypeFilterPanel;
    _showLogDetails = state.ShowLogDetails;
    _stackTraceFoldout = state.StackTraceFoldout;
    _hierarchyPanelWidth = state.HierarchyPanelWidth > 0f ? state.HierarchyPanelWidth : _hierarchyPanelWidth;
    _logDetailPanelHeight = Mathf.Clamp(state.LogDetailPanelHeight > 0f ? state.LogDetailPanelHeight : DefaultLogDetailPanelHeight, MinLogDetailPanelHeight, MaxLogDetailPanelHeight);
    _hierarchyScroll = state.HierarchyScroll;
    _logScroll = state.LogScroll;
    _typeFilterScroll = state.TypeFilterScroll;
    _detailScroll = state.DetailScroll;
    _selectedLogIndex = state.SelectedLogIndex;
    _focusedSnapshotGameObjectKey = state.FocusedSnapshotGameObjectKey ?? string.Empty;
    _focusedSnapshotComponentKey = state.FocusedSnapshotComponentKey ?? string.Empty;
    _focusedObjectName = state.FocusedObjectName ?? string.Empty;
    _focusedComponentName = state.FocusedComponentName ?? string.Empty;

    _expandedSnapshotDetails.Clear();
    if (state.ExpandedSnapshotDetails != null)
    {
        for (int i = 0; i < state.ExpandedSnapshotDetails.Count; i++)
        {
            string key = state.ExpandedSnapshotDetails[i];
            if (!string.IsNullOrWhiteSpace(key))
                _expandedSnapshotDetails.Add(key);
        }
    }

    _expandedSnapshotChildren.Clear();
    if (state.ExpandedSnapshotChildren != null)
    {
        for (int i = 0; i < state.ExpandedSnapshotChildren.Count; i++)
        {
            string key = state.ExpandedSnapshotChildren[i];
            if (!string.IsNullOrWhiteSpace(key))
                _expandedSnapshotChildren.Add(key);
        }
    }
}

    private void OpenEntryScript(DebugEntry entry)
    {
#if UNITY_EDITOR
        if (!TryGetEntryScriptLocation(entry, out MonoScript script, out int lineNumber, out int columnNumber))
            return;

        AssetDatabase.OpenAsset(script, Mathf.Max(1, lineNumber), Mathf.Max(1, columnNumber));
#endif
    }

    private void OpenEntryScript(SnapshotLogEntry entry)
    {
#if UNITY_EDITOR
        if (entry == null)
            return;

        if (!TryGetEntryScriptLocation(entry.CallerFilePath, entry.LineNumber, entry.CallerColumn, out MonoScript script, out int lineNumber, out int columnNumber))
            return;

        AssetDatabase.OpenAsset(script, Mathf.Max(1, lineNumber), Mathf.Max(1, columnNumber));
#endif
    }

#if UNITY_EDITOR
    private bool TryGetEntryScriptLocation(DebugEntry entry, out MonoScript script, out int lineNumber, out int columnNumber)
    {
        script = null;
        lineNumber = 1;
        columnNumber = 1;

        if (entry == null)
            return false;

        return TryGetEntryScriptLocation(entry.CallerFilePath, entry.LineNumber, entry.CallerColumn, out script, out lineNumber, out columnNumber);
    }

    private bool TryGetEntryScriptLocation(string callerFilePath, int sourceLineNumber, int sourceColumnNumber, out MonoScript script, out int lineNumber, out int columnNumber)
    {
        script = null;
        lineNumber = Mathf.Max(1, sourceLineNumber);
        columnNumber = Mathf.Max(1, sourceColumnNumber);

        if (string.IsNullOrWhiteSpace(callerFilePath))
            return false;

        if (TryConvertCallerPathToAssetPath(callerFilePath, out string assetPath))
        {
            script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (script != null)
                return true;
        }

        return TryFindScriptByFileName(callerFilePath, out script);
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

    private bool TryFindScriptByFileName(string callerFilePath, out MonoScript script)
    {
        script = null;

        string fileName = Path.GetFileNameWithoutExtension(callerFilePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        string[] guids = AssetDatabase.FindAssets($"{fileName} t:MonoScript");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.Equals(Path.GetFileNameWithoutExtension(assetPath), fileName, StringComparison.Ordinal))
                continue;

            MonoScript found = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
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

        _focusedSnapshotGameObjectKey = entry.GameObjectKey ?? string.Empty;
        _focusedSnapshotComponentKey = entry.ComponentKey ?? string.Empty;
        _focusedObjectName = entry.GameObjectName ?? string.Empty;
        _focusedComponentName = entry.ComponentName ?? string.Empty;

        GameObject targetGameObject = null;

        try
        {
            if (entry.Context is GameObject go)
            {
                if (go != null)
                {
                    targetGameObject = go;
                    _focusedGameObjectId = go.GetInstanceID();
                    _focusedComponentId = 0;
                    _focusedSnapshotGameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(go);
                    _focusedSnapshotComponentKey = string.Empty;
                    _focusedObjectName = go.name;
                    _focusedComponentName = string.Empty;
                    PrepareSelectionExpansion(go.transform, true);
                }
            }
            else if (entry.Context is Component component)
            {
                if (component != null && component.gameObject != null)
                {
                    targetGameObject = component.gameObject;
                    _focusedGameObjectId = component.gameObject.GetInstanceID();
                    _focusedComponentId = component.GetInstanceID();
                    _focusedSnapshotGameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(component.gameObject);
                    _focusedSnapshotComponentKey = DebugConsoleFilterKeyUtility.GetComponentKey(component);
                    _focusedObjectName = component.gameObject.name;
                    _focusedComponentName = component.GetType().Name;
                    PrepareSelectionExpansion(component.transform, true);
                }
            }
        }
        catch (MissingReferenceException)
        {
            targetGameObject = null;
        }

        if (targetGameObject != null)
        {
            Selection.activeGameObject = targetGameObject;
            EditorGUIUtility.PingObject(targetGameObject);
        }

        SaveEditorUiState();
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


    private void MirrorLiveExpansionToSnapshot(Transform target, bool includeTargetDetails)
    {
        if (target == null)
            return;

        if (includeTargetDetails)
            _expandedSnapshotDetails.Add(DebugConsoleFilterKeyUtility.GetGameObjectKey(target.gameObject));

        Transform current = target;
        while (current.parent != null)
        {
            current = current.parent;
            string key = DebugConsoleFilterKeyUtility.GetGameObjectKey(current.gameObject);
            _expandedSnapshotDetails.Add(key);
            _expandedSnapshotChildren.Add(key);
        }
    }

    private bool ShouldDisplayEntry(DebugConsoleManager manager, DebugEntry entry)
    {
        if (entry == null)
            return false;

        if (!manager.IsAllowed(entry.Type, entry.GameObjectId, entry.ComponentId))
            return false;

        if (!manager.GetLevelEnabled(entry.Level))
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

        return true;
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
        _focusedSnapshotGameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(go);
        _focusedSnapshotComponentKey = string.Empty;
        _focusedObjectName = go.name;
        _focusedComponentName = string.Empty;

        PrepareSelectionExpansion(go.transform, true);
        SaveEditorUiState();
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
        _focusedSnapshotGameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(component.gameObject);
        _focusedSnapshotComponentKey = DebugConsoleFilterKeyUtility.GetComponentKey(component);
        _focusedObjectName = component.gameObject.name;
        _focusedComponentName = component.GetType().Name;

        PrepareSelectionExpansion(component.transform, true);
        SaveEditorUiState();
    }

    private void PrepareSelectionExpansion(Transform target, bool includeDetails)
    {
        if (target == null)
            return;

        if (_collapsePreviousOnSelection)
        {
            PreserveExpansionWithinTopLevelRoot(target);
            PreserveSnapshotExpansionWithinTopLevelRoot(DebugConsoleFilterKeyUtility.GetGameObjectKey(target.gameObject));
        }

        ExpandSelectionPath(target, includeDetails);
        MirrorLiveExpansionToSnapshot(target, includeDetails);
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
        _focusedSnapshotGameObjectKey = string.Empty;
        _focusedSnapshotComponentKey = string.Empty;
        SaveEditorUiState();
    }

    private string GetFocusLabel()
    {
        if (!string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey))
            return $"Focus : {_focusedObjectName}/{_focusedComponentName}";

        if (!string.IsNullOrWhiteSpace(_focusedSnapshotGameObjectKey))
            return $"Focus : {_focusedObjectName} (All Components)";

        if (_focusedComponentId != 0)
            return $"Focus : {_focusedObjectName}/{_focusedComponentName}";

        if (_focusedGameObjectId != 0)
            return $"Focus : {_focusedObjectName} (All Components)";

        return "Focus : All";
    }

    private string GetFooterFocusLabel()
    {
        if (!string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey))
        {
            string objectName = TrimFooterFocusSegment(_focusedObjectName);
            string componentName = TrimFooterFocusSegment(_focusedComponentName);

            if (string.Equals(_focusedObjectName, _focusedComponentName, StringComparison.Ordinal))
                return $"Focus : {objectName}";

            return $"Focus : {objectName} / {componentName}";
        }

        if (!string.IsNullOrWhiteSpace(_focusedSnapshotGameObjectKey))
            return $"Focus : {TrimFooterFocusSegment(_focusedObjectName)}";

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
        if (!string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey))
            return $"({_focusedObjectName}/{_focusedComponentName})";

        if (!string.IsNullOrWhiteSpace(_focusedSnapshotGameObjectKey))
            return $"({_focusedObjectName})";

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

        return new GUIStyle("box")
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

    private void ToggleExpandedSet(HashSet<string> set, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (set.Contains(key))
            set.Remove(key);
        else
            set.Add(key);
    }

    private void ToggleLiveExpandedDetails(GameObject go)
    {
        if (go == null)
            return;

        ToggleExpandedSet(_expandedComponents, go.GetInstanceID());
        ToggleExpandedSet(_expandedSnapshotDetails, DebugConsoleFilterKeyUtility.GetGameObjectKey(go));
        SaveEditorUiState();
    }

    private void ToggleLiveExpandedChildren(GameObject go)
    {
        if (go == null)
            return;

        ToggleExpandedSet(_expandedChildren, go.GetInstanceID());
        ToggleExpandedSet(_expandedSnapshotChildren, DebugConsoleFilterKeyUtility.GetGameObjectKey(go));
        SaveEditorUiState();
    }

    private void ToggleSnapshotExpandedDetails(string key)
    {
        ToggleExpandedSet(_expandedSnapshotDetails, key);
        SaveEditorUiState();
    }

    private void ToggleSnapshotExpandedChildren(string key)
    {
        ToggleExpandedSet(_expandedSnapshotChildren, key);
        SaveEditorUiState();
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


    private readonly (string label, float width)[] _toolbarToggleItems =
    {
        ("Global", 80f),
        ("Mirror Unity", 110f),
        ("Auto Scroll", 100f),
        ("Hide Transform", 110f),
        ("Collapse Prev", 110f),
        ("Collapse Logs", 110f),
        ("Log", 70f),
        ("Warn", 75f),
        ("Error", 75f),
    };

    private readonly (string label, float width)[] _toolbarActionItems =
    {
        ("TypeFilter", 150f),
        ("All Types On", 92f),
        ("All Types Off", 92f),
        ("All Levels", 92f),
        ("Warn+", 72f),
        ("Error Only", 92f),
        ("Clear Logs", 92f),
        ("Clear Focus", 92f),
        ("Reset Filters", 102f),
        ("Reset Layout", 102f),
        ("Export Settings", 112f),
        ("Import Settings", 112f),
    };

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
        {
            _autoScroll = autoScroll;
            SaveEditorUiState();
        }

        bool hideTransform = GUILayout.Toggle(_hideTransform, "Hide Transform", GUILayout.Width(120f));
        if (hideTransform != _hideTransform)
        {
            _hideTransform = hideTransform;
            SaveEditorUiState();
        }

        bool collapsePrevious = GUILayout.Toggle(_collapsePreviousOnSelection, "Collapse Prev", GUILayout.Width(120f));
        if (collapsePrevious != _collapsePreviousOnSelection)
        {
            _collapsePreviousOnSelection = collapsePrevious;
            SaveEditorUiState();
        }

        bool collapseLogs = GUILayout.Toggle(_collapseLogs, "Collapse Logs", GUILayout.Width(120f));
        if (collapseLogs != _collapseLogs)
        {
            _collapseLogs = collapseLogs;
            SaveEditorUiState();
        }

        bool showLogs = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Log), "Log", GUILayout.Width(70f));
        if (showLogs != manager.GetLevelEnabled(DebugLogLevel.Log))
            manager.SetLevelEnabled(DebugLogLevel.Log, showLogs);

        bool showWarnings = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Warning), "Warn", GUILayout.Width(75f));
        if (showWarnings != manager.GetLevelEnabled(DebugLogLevel.Warning))
            manager.SetLevelEnabled(DebugLogLevel.Warning, showWarnings);

        bool showErrors = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Error), "Error", GUILayout.Width(75f));
        if (showErrors != manager.GetLevelEnabled(DebugLogLevel.Error))
            manager.SetLevelEnabled(DebugLogLevel.Error, showErrors);
    }

    private void DrawToolbarToggleGroupWrapped(DebugConsoleManager manager, float availableWidth)
    {
        for (int index = 0; index < _toolbarToggleItems.Length;)
        {
            int endIndex = GetWrappedEndIndex(_toolbarToggleItems, index, availableWidth);
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawToolbarToggleItems(manager, index, endIndex);
            EditorGUILayout.EndHorizontal();
            index = endIndex;
        }
    }

    private void DrawToolbarToggleItems(DebugConsoleManager manager, int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            switch (_toolbarToggleItems[i].label)
            {
                case "Global":
                {
                    bool value = GUILayout.Toggle(manager.GlobalEnabled, "Global", GUILayout.Width(80f));
                    if (value != manager.GlobalEnabled)
                        manager.GlobalEnabled = value;
                    break;
                }

                case "Mirror Unity":
                {
                    bool value = GUILayout.Toggle(manager.MirrorToUnityConsole, "Mirror Unity", GUILayout.Width(110f));
                    if (value != manager.MirrorToUnityConsole)
                        manager.MirrorToUnityConsole = value;
                    break;
                }

                case "Auto Scroll":
                {
                    bool value = GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100f));
                    if (value != _autoScroll)
                    {
                        _autoScroll = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Hide Transform":
                {
                    bool value = GUILayout.Toggle(_hideTransform, "Hide Transform", GUILayout.Width(120f));
                    if (value != _hideTransform)
                    {
                        _hideTransform = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Collapse Prev":
                {
                    bool value = GUILayout.Toggle(_collapsePreviousOnSelection, "Collapse Prev", GUILayout.Width(120f));
                    if (value != _collapsePreviousOnSelection)
                    {
                        _collapsePreviousOnSelection = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Collapse Logs":
                {
                    bool value = GUILayout.Toggle(_collapseLogs, "Collapse Logs", GUILayout.Width(120f));
                    if (value != _collapseLogs)
                    {
                        _collapseLogs = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Log":
                {
                    bool value = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Log), "Log", GUILayout.Width(70f));
                    if (value != manager.GetLevelEnabled(DebugLogLevel.Log))
                        manager.SetLevelEnabled(DebugLogLevel.Log, value);
                    break;
                }

                case "Warn":
                {
                    bool value = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Warning), "Warn", GUILayout.Width(75f));
                    if (value != manager.GetLevelEnabled(DebugLogLevel.Warning))
                        manager.SetLevelEnabled(DebugLogLevel.Warning, value);
                    break;
                }

                case "Error":
                {
                    bool value = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Error), "Error", GUILayout.Width(75f));
                    if (value != manager.GetLevelEnabled(DebugLogLevel.Error))
                        manager.SetLevelEnabled(DebugLogLevel.Error, value);
                    break;
                }
            }

            if (i < endIndex - 1)
                GUILayout.Space(8f);
        }
    }

private void DrawToolbarActionGroup(DebugConsoleManager manager, string typeButtonLabel)
{
    if (GUILayout.Button(typeButtonLabel, GUILayout.Width(160f)))
    {
        _showTypeFilterPanel = !_showTypeFilterPanel;
        SaveEditorUiState();
    }

    if (GUILayout.Button("All Types On", GUILayout.Width(100f)))
        manager.SetAllTypes(true);

    if (GUILayout.Button("All Types Off", GUILayout.Width(100f)))
        manager.SetAllTypes(false);

    if (GUILayout.Button("All Levels", GUILayout.Width(100f)))
        manager.SetAllLevels(true);

    if (GUILayout.Button("Warn+", GUILayout.Width(80f)))
        manager.SetWarningAndErrorOnly();

    if (GUILayout.Button("Error Only", GUILayout.Width(100f)))
        manager.SetErrorOnly();

    if (GUILayout.Button("Clear Logs", GUILayout.Width(100f)))
        manager.ClearLogs();

    if (GUILayout.Button("Clear Focus", GUILayout.Width(100f)))
        ClearFocus();

    if (GUILayout.Button("Reset Filters", GUILayout.Width(110f)))
        manager.ResetAllFiltersToDefault();

    if (GUILayout.Button("Reset Layout", GUILayout.Width(110f)))
        ResetEditorLayoutToDefault();

    if (GUILayout.Button("Export Settings", GUILayout.Width(120f)))
        ExportEditorSettingsToFile();

    if (GUILayout.Button("Import Settings", GUILayout.Width(120f)))
        ImportEditorSettingsFromFile(_lastSnapshot);
}

private void DrawToolbarActionGroupWrapped(DebugConsoleManager manager, string typeButtonLabel, float availableWidth)
    {
        for (int index = 0; index < _toolbarActionItems.Length;)
        {
            int endIndex = GetWrappedEndIndex(_toolbarActionItems, index, availableWidth);
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawToolbarActionItems(manager, typeButtonLabel, index, endIndex);
            EditorGUILayout.EndHorizontal();
            index = endIndex;
        }
    }

private void DrawToolbarActionItems(DebugConsoleManager manager, string typeButtonLabel, int startIndex, int endIndex)
{
    for (int i = startIndex; i < endIndex; i++)
    {
        string key = _toolbarActionItems[i].label;
        float width = _toolbarActionItems[i].width;

        switch (key)
        {
            case "TypeFilter":
                if (GUILayout.Button(typeButtonLabel, GUILayout.Width(width)))
                {
                    _showTypeFilterPanel = !_showTypeFilterPanel;
                    SaveEditorUiState();
                }
                break;

            case "All Types On":
                if (GUILayout.Button("All Types On", GUILayout.Width(width)))
                    manager.SetAllTypes(true);
                break;

            case "All Types Off":
                if (GUILayout.Button("All Types Off", GUILayout.Width(width)))
                    manager.SetAllTypes(false);
                break;

            case "All Levels":
                if (GUILayout.Button("All Levels", GUILayout.Width(width)))
                    manager.SetAllLevels(true);
                break;

            case "Warn+":
                if (GUILayout.Button("Warn+", GUILayout.Width(width)))
                    manager.SetWarningAndErrorOnly();
                break;

            case "Error Only":
                if (GUILayout.Button("Error Only", GUILayout.Width(width)))
                    manager.SetErrorOnly();
                break;

            case "Clear Logs":
                if (GUILayout.Button("Clear Logs", GUILayout.Width(width)))
                    manager.ClearLogs();
                break;

            case "Clear Focus":
                if (GUILayout.Button("Clear Focus", GUILayout.Width(width)))
                    ClearFocus();
                break;

            case "Reset Filters":
                if (GUILayout.Button("Reset Filters", GUILayout.Width(width)))
                    manager.ResetAllFiltersToDefault();
                break;

            case "Reset Layout":
                if (GUILayout.Button("Reset Layout", GUILayout.Width(width)))
                    ResetEditorLayoutToDefault();
                break;

            case "Export Settings":
                if (GUILayout.Button("Export Settings", GUILayout.Width(width)))
                    ExportEditorSettingsToFile();
                break;

            case "Import Settings":
                if (GUILayout.Button("Import Settings", GUILayout.Width(width)))
                    ImportEditorSettingsFromFile(_lastSnapshot);
                break;
        }

        if (i < endIndex - 1)
            GUILayout.Space(8f);
    }
}


private int GetWrappedEndIndex((string label, float width)[] items, int startIndex, float availableWidth)
    {
        float rowWidth = 0f;
        const float spacing = 8f;

        for (int i = startIndex; i < items.Length; i++)
        {
            float nextWidth = items[i].width + (i > startIndex ? spacing : 0f);
            if (rowWidth + nextWidth > availableWidth && i > startIndex)
                return i;

            rowWidth += nextWidth;
        }

        return items.Length;
    }

private int GetWrappedSplitIndex((string label, float width)[] items, float availableWidth)
    {
        float rowWidth = 0f;
        const float spacing = 8f;

        for (int i = 0; i < items.Length; i++)
        {
            float nextWidth = items[i].width + (i > 0 ? spacing : 0f);
            if (rowWidth + nextWidth > availableWidth && i > 0)
                return i;

            rowWidth += nextWidth;
        }

        return items.Length;
    }

    private void DrawToolbarInfoGroup(DebugConsoleManager manager, bool expanded)
    {
        GUILayout.Label(GetFocusLabel(), _toolbarInfoLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(expanded ? 30f : 18f));
        GUILayout.Space(8f);
        GUILayout.Label($"Count : {manager.Entries.Count}", _toolbarInfoLabelStyle, GUILayout.Width(expanded ? 120f : 110f), GUILayout.MinHeight(expanded ? 30f : 18f));
    }

    private float GetSearchFieldWidth(float availableWidth, bool hasClearButton)
    {
        float reserveWidth = SearchLabelWidth + 10f + (hasClearButton ? SearchClearButtonWidth + 8f : 0f);
        float fieldWidth = availableWidth - reserveWidth;
        return Mathf.Clamp(fieldWidth, 96f, SearchFieldFixedWidth);
    }

    private void DrawHierarchySearchField()
    {
        DrawHierarchySearchField(SearchLabelWidth + SearchFieldFixedWidth + 12f);
    }

    private void DrawHierarchySearchField(float availableWidth)
    {
        GUILayout.Label("Search", GUILayout.Width(SearchLabelWidth));
        float fieldWidth = GetSearchFieldWidth(availableWidth, false);
        Rect fieldRect = GUILayoutUtility.GetRect(fieldWidth, 20f, GUILayout.Width(fieldWidth), GUILayout.Height(20f));
        string nextSearch = (_hierarchySearchFieldControl ??= new SearchField()).OnGUI(fieldRect, _pendingHierarchySearch);
        if (!string.Equals(nextSearch, _pendingHierarchySearch, StringComparison.Ordinal))
        {
            _pendingHierarchySearch = nextSearch;
            _hierarchySearchApplyTime = EditorApplication.timeSinceStartup + SearchDebounceDelay;
        }
    }

    private void DrawLogSearchField()
    {
        DrawLogSearchField(SearchLabelWidth + SearchFieldFixedWidth + SearchClearButtonWidth + 20f);
    }

    private void DrawLogSearchField(float availableWidth)
    {
        GUILayout.Label("Search", GUILayout.Width(SearchLabelWidth));
        float fieldWidth = GetSearchFieldWidth(availableWidth, true);
        Rect fieldRect = GUILayoutUtility.GetRect(fieldWidth, 20f, GUILayout.Width(fieldWidth), GUILayout.Height(20f));
        string nextSearch = (_logSearchFieldControl ??= new SearchField()).OnGUI(fieldRect, _pendingLogSearch);
        if (!string.Equals(nextSearch, _pendingLogSearch, StringComparison.Ordinal))
        {
            _pendingLogSearch = nextSearch;
            _logSearchApplyTime = EditorApplication.timeSinceStartup + SearchDebounceDelay;
        }

        if (GUILayout.Button("Clear", GUILayout.Width(SearchClearButtonWidth)))
        {
            _pendingLogSearch = string.Empty;
            _logSearch = string.Empty;
            _logSearchApplyTime = 0d;
            GUI.FocusControl(null);
            SaveEditorUiState();
        }
    }

    private bool ProcessSearchDebounce()
    {
        bool changed = false;
        double now = EditorApplication.timeSinceStartup;

        if (!string.Equals(_hierarchySearch, _pendingHierarchySearch, StringComparison.Ordinal) &&
            now >= _hierarchySearchApplyTime)
        {
            _hierarchySearch = _pendingHierarchySearch;
            _hierarchyScroll = Vector2.zero;
            changed = true;
        }

        if (!string.Equals(_logSearch, _pendingLogSearch, StringComparison.Ordinal) &&
            now >= _logSearchApplyTime)
        {
            _logSearch = _pendingLogSearch;
            _logScroll = Vector2.zero;
            changed = true;
        }

        return changed;
    }

    private float GetTopAreaWidth()
    {
        return Mathf.Max(320f, position.width - 32f);
    }

    private int GetTopLayoutLevel(float availableWidth)
    {
        if (availableWidth >= 1500f)
            return 1;

        if (availableWidth >= 980f)
            return 2;

        return 3;
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
#endif
