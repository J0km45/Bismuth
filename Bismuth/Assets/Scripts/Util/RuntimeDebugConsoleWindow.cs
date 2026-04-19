using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RuntimeDebugConsoleWindow : MonoBehaviour
{
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
    [SerializeField] private bool _visible = false;
    [SerializeField] private Rect _windowRect = new Rect(20f, 20f, 1450f, 850f);
    [SerializeField] private bool _autoScroll = true;
    [SerializeField] private bool _hideTransform = true;

    private Vector2 _hierarchyScroll;
    private Vector2 _logScroll;
    private Vector2 _typeFilterScroll;

    private string _hierarchySearch = string.Empty;
    private string _logSearch = string.Empty;

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
    private GUIStyle _selectedLinkButtonStyle;
    private GUIStyle _toolbarButtonStyle;
    private GUIStyle _toolbarInfoLabelStyle;
    private GUIStyle _objectFocusedRowStyle;
    private GUIStyle _objectParentFocusedRowStyle;
    private GUIStyle _componentFocusedRowStyle;
    private Texture2D _solidTexture;

    private const int MaxDisplayNameLength = 15;

    private float _hierarchyActionButtonWidth = 104f;
    private float _lastLogContentHeight;
    private float _lastLogViewportHeight;
    private float _lastMaxLogScrollY;

    private readonly HashSet<int> _expandedComponents = new();
    private readonly HashSet<int> _expandedChildren = new();

    private int _selectedLogIndex = -1;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _expandedComponents.Clear();
        _expandedChildren.Clear();
        _selectedLogIndex = -1;
        _hierarchyScroll = Vector2.zero;
        ClearFocus();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            _visible = !_visible;
    }

    private void OnGUI()
    {
        if (!_visible)
            return;

        InitStyles();
        _windowRect = GUI.Window(91357, _windowRect, DrawWindow, "Runtime Debug Console");
    }

    private void InitStyles()
    {
        if (_titleStyle != null)
            return;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13
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
            padding = new RectOffset(2, 2, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };

        _selectedLinkButtonStyle = new GUIStyle(_linkButtonStyle);
        _selectedLinkButtonStyle.fontStyle = FontStyle.Bold;
        _selectedLinkButtonStyle.normal.textColor = new Color(0.4f, 0.9f, 1f);

        if (_solidTexture == null)
        {
            _solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _solidTexture.SetPixel(0, 0, Color.white);
            _solidTexture.Apply();
        }

        _objectFocusedRowStyle = CreateRowStyle(new Color(0.20f, 0.72f, 0.95f, 0.22f));
        _objectParentFocusedRowStyle = CreateRowStyle(new Color(0.20f, 0.72f, 0.95f, 0.10f));
        _componentFocusedRowStyle = CreateRowStyle(new Color(0.22f, 0.52f, 0.95f, 0.32f));

        _toolbarButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter
        };

        _toolbarInfoLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
            richText = false
        };
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

        GUILayout.BeginHorizontal();
        DrawHierarchyPanel(manager);
        DrawLogPanel(manager);
        GUILayout.EndHorizontal();

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
            GUILayout.Space(12f);
            DrawToolbarInfoGroup(manager, false);
            GUILayout.EndHorizontal();
            return;
        }

        if (layoutLevel == 2)
        {
            GUILayout.BeginHorizontal(GUILayout.MinHeight(28f));
            DrawToolbarToggleGroup(manager);
            GUILayout.Space(8f);
            DrawToolbarActionGroup(manager, typeButtonLabel);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.MinHeight(32f));
            DrawToolbarInfoGroup(manager, true);
            GUILayout.EndHorizontal();
            return;
        }

        GUILayout.BeginHorizontal(GUILayout.MinHeight(28f));
        DrawToolbarToggleGroup(manager);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(GUILayout.MinHeight(28f));
        DrawToolbarActionGroup(manager, typeButtonLabel);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(GUILayout.MinHeight(40f));
        DrawToolbarInfoGroup(manager, true);
        GUILayout.EndHorizontal();
    }

    private void DrawSearchBar()
    {
        float availableWidth = GetTopAreaWidth();

        if (availableWidth >= 980f)
        {
            GUILayout.BeginHorizontal(GUILayout.MinHeight(26f));
            DrawHierarchySearchField(GetSearchFieldWidth(availableWidth, false));
            GUILayout.Space(12f);
            DrawLogSearchField(true);
            GUILayout.EndHorizontal();
            return;
        }

        GUILayout.BeginHorizontal(GUILayout.MinHeight(26f));
        DrawHierarchySearchField(GetSearchFieldWidth(availableWidth, true));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(GUILayout.MinHeight(26f));
        DrawLogSearchField(false);
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

    private void DrawHierarchyPanel(DebugConsoleManager manager)
    {
        UpdateHierarchyButtonWidths();

        GUILayout.BeginVertical(_boxStyle, GUILayout.Width(_windowRect.width * 0.42f), GUILayout.ExpandHeight(true));
        GUILayout.Label("Scene Objects / Components", _titleStyle);

        _hierarchyScroll = GUILayout.BeginScrollView(_hierarchyScroll);

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
            DrawGameObjectNode(manager, roots[i], 0);

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawGameObjectNode(DebugConsoleManager manager, GameObject go, int depth)
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

        bool componentsExpanded = _expandedComponents.Contains(id);
        bool childrenExpanded = _expandedChildren.Contains(id);

        bool searchActive = !string.IsNullOrWhiteSpace(_hierarchySearch);
        bool forceOpenComponents = searchActive && HasMatchingComponent(go, _hierarchySearch);
        bool forceOpenChildren = searchActive && HasVisibleChildren(go);

        bool isObjectFocused = IsObjectFocused(id);
        bool isComponentParentFocused = IsFocusedObjectParent(id);

        GUILayout.BeginVertical(GetHierarchyRowStyle(isObjectFocused, isComponentParentFocused, false));

        GUILayout.BeginHorizontal(GUILayout.Height(24f));
        GUILayout.Space(depth * 18f);

        bool nextObjectEnabled = GUILayout.Toggle(objectEnabled, "", GUILayout.Width(20));
        if (nextObjectEnabled != objectEnabled)
            manager.SetGameObjectEnabled(go, nextObjectEnabled);

        string objectLabel = isObjectFocused
            ? $"▶ {GetDisplayName(go.name)}"
            : (isComponentParentFocused ? $"▸ {GetDisplayName(go.name)}" : GetDisplayName(go.name));
        GUIStyle objectStyle = (isObjectFocused || isComponentParentFocused)
            ? _selectedLinkButtonStyle
            : (objectEnabled ? _linkButtonStyle : _dimLabelStyle);

        GUIContent objectContent = new GUIContent(objectLabel, go.name);
        if (GUILayout.Button(objectContent, objectStyle, GUILayout.ExpandWidth(true)))
            ToggleGameObjectFocus(go);

        GUILayout.EndHorizontal();

        if (hasVisibleChildren || hasVisibleComponents)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(22f));
            GUILayout.Space(depth * 18f + 24f);

            if (hasVisibleChildren)
            {
                if (GUILayout.Button((childrenExpanded || forceOpenChildren) ? "하위 ▼" : "하위 ▶", GUILayout.Width(_hierarchyActionButtonWidth)))
                {
                    if (childrenExpanded)
                        _expandedChildren.Remove(id);
                    else
                        _expandedChildren.Add(id);
                }
            }

            if (hasVisibleComponents)
            {
                if (hasVisibleChildren)
                    GUILayout.Space(4f);

                if (GUILayout.Button((componentsExpanded || forceOpenComponents) ? "컴포넌트 ▼" : "컴포넌트 ▶", GUILayout.Width(_hierarchyActionButtonWidth)))
                {
                    if (componentsExpanded)
                        _expandedComponents.Remove(id);
                    else
                        _expandedComponents.Add(id);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();

        bool showComponents = hasVisibleComponents && (componentsExpanded || forceOpenComponents);
        bool showChildren = hasVisibleChildren && (childrenExpanded || forceOpenChildren);

        if (showComponents)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = objectEnabled;

            foreach (Component component in components)
            {
                if (!ShouldShowComponent(component, go.name))
                    continue;

                bool isComponentFocused = IsComponentFocused(component.GetInstanceID());

                GUILayout.BeginHorizontal(GetHierarchyRowStyle(false, false, isComponentFocused), GUILayout.Height(22f));
                GUILayout.Space((depth + 1) * 18f + 24f);

                bool componentEnabled = manager.GetComponentEnabled(component);
                bool nextComponentEnabled = GUILayout.Toggle(componentEnabled, "", GUILayout.Width(20));
                if (nextComponentEnabled != componentEnabled)
                    manager.SetComponentEnabled(component, nextComponentEnabled);

                string componentName = GetDisplayName(component.GetType().Name);
                string componentLabel = isComponentFocused ? $"▶ {componentName}" : componentName;
                GUIStyle componentStyle = isComponentFocused
                    ? _selectedLinkButtonStyle
                    : (objectEnabled ? _linkButtonStyle : _dimLabelStyle);

                GUIContent componentContent = new GUIContent(componentLabel, component.GetType().Name);
                if (GUILayout.Button(componentContent, componentStyle, GUILayout.ExpandWidth(true)))
                    ToggleComponentFocus(component);

                GUILayout.EndHorizontal();
            }

            GUI.enabled = previousEnabled;
        }

        if (showChildren)
        {
            for (int i = 0; i < go.transform.childCount; i++)
                DrawGameObjectNode(manager, go.transform.GetChild(i).gameObject, depth + 1);
        }
    }

    private void DrawLogPanel(DebugConsoleManager manager)
    {
        GUILayout.BeginVertical(_boxStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.Label($"Logs {GetFocusSuffix()}", _titleStyle);

        bool wasNearBottom = IsNearBottom(_lastMaxLogScrollY);
        float contentHeight = 0f;

        _logScroll = GUILayout.BeginScrollView(_logScroll);

        IReadOnlyList<DebugEntry> entries = manager.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            DebugEntry entry = entries[i];

            if (!ShouldDisplayEntry(manager, entry))
                continue;

            float drawnHeight = DrawLogEntry(entry, i);
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

    private float DrawLogEntry(DebugEntry entry, int index)
    {
        GUIContent content = new GUIContent(entry.RichText);
        float estimatedWidth = Mathf.Max(200f, (_windowRect.width * 0.52f) - 30f);
        float height = _richLabelStyle.CalcHeight(content, estimatedWidth);

        Rect rect = GUILayoutUtility.GetRect(10f, height + 12f, GUILayout.ExpandWidth(true));

        Color previousColor = GUI.color;
        if (index == _selectedLogIndex)
            GUI.color = new Color(0.75f, 0.85f, 1f, 1f);

        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;

        Rect labelRect = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f);
        GUI.Label(labelRect, content, _richLabelStyle);

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            _selectedLogIndex = index;
            FocusEntry(entry);
            Event.current.Use();
        }

        return rect.height;
    }

    private void FocusEntry(DebugEntry entry)
    {
        if (entry == null)
            return;

        GameObject targetGameObject = null;

        if (entry.Context is GameObject go)
        {
            targetGameObject = go;
        }
        else if (entry.Context is Component component)
        {
            targetGameObject = component.gameObject;
            _expandedComponents.Add(targetGameObject.GetInstanceID());
        }

        if (targetGameObject == null)
            return;

        ExpandParents(targetGameObject.transform);

#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = targetGameObject;
        UnityEditor.EditorGUIUtility.PingObject(targetGameObject);
#endif
    }

    private void ExpandParents(Transform target)
    {
        Transform current = target;

        while (current.parent != null)
        {
            _expandedChildren.Add(current.parent.gameObject.GetInstanceID());
            current = current.parent;
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

        _expandedComponents.Add(id);
        ExpandParents(go.transform);
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

        _expandedComponents.Add(_focusedGameObjectId);
        ExpandParents(component.transform);
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
            padding = new RectOffset(4, 4, 1, 1),
            alignment = TextAnchor.MiddleLeft
        };
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

    private void DrawHierarchySearchField(float fieldWidth)
    {
        GUILayout.Label("Hierarchy Search", GUILayout.Width(105f));
        _hierarchySearch = GUILayout.TextField(_hierarchySearch, _searchTextFieldStyle, GUILayout.Width(fieldWidth));
    }

    private void DrawLogSearchField(bool singleRow)
    {
        GUILayout.Label("Log Search", GUILayout.Width(75f));
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

    private void UpdateHierarchyButtonWidths()
    {
        float childWidth = _linkButtonStyle.CalcSize(new GUIContent("하위 ▼")).x + 12f;
        float componentWidth = _linkButtonStyle.CalcSize(new GUIContent("컴포넌트 ▼")).x + 12f;
        _hierarchyActionButtonWidth = Mathf.Ceil(Mathf.Max(childWidth, componentWidth, 76f));
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
