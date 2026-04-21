using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class DebugConsoleManager : MonoBehaviour
{
    private sealed class DebugEntryRingBuffer : IReadOnlyList<DebugEntry>
    {
        private DebugEntry[] _buffer;
        private int _start;
        private int _count;

        public DebugEntryRingBuffer(int capacity)
        {
            _buffer = new DebugEntry[Mathf.Max(1, capacity)];
            _start = 0;
            _count = 0;
        }

        public int Count => _count;
        public int Capacity => _buffer.Length;

        public DebugEntry this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _buffer[(_start + index) % _buffer.Length];
            }
        }

        public void Add(DebugEntry entry)
        {
            if (_count < _buffer.Length)
            {
                _buffer[(_start + _count) % _buffer.Length] = entry;
                _count++;
                return;
            }

            _buffer[_start] = entry;
            _start = (_start + 1) % _buffer.Length;
        }

        public void Clear()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _start = 0;
            _count = 0;
        }

        public void SetCapacity(int capacity)
        {
            capacity = Mathf.Max(1, capacity);
            if (capacity == _buffer.Length)
                return;

            DebugEntry[] newBuffer = new DebugEntry[capacity];
            int newCount = Mathf.Min(_count, capacity);
            int sourceStartIndex = Mathf.Max(0, _count - newCount);

            for (int i = 0; i < newCount; i++)
                newBuffer[i] = this[sourceStartIndex + i];

            _buffer = newBuffer;
            _start = 0;
            _count = newCount;
        }

        public IEnumerator<DebugEntry> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static DebugConsoleManager Instance { get; private set; }

    [SerializeField] private int _maxEntries = 2000;
    [SerializeField] private bool _globalEnabled = true;
    [SerializeField] private bool _mirrorToUnityConsole = false;
    [SerializeField] private bool _showLogLevelLog = true;
    [SerializeField] private bool _showLogLevelWarning = true;
    [SerializeField] private bool _showLogLevelError = true;

    public const string PreferencePrefix = "DebugConsole.Manager";
    private const string PrefKeyPrefix = PreferencePrefix;
    private const string MaxEntriesPrefKey = PrefKeyPrefix + ".MaxEntries";

    private DebugEntryRingBuffer _entries;
    private bool[] _typeFilters;
    private readonly Dictionary<int, bool> _gameObjectFilters = new();
    private readonly Dictionary<int, bool> _componentFilters = new();
    private readonly Dictionary<int, string> _gameObjectFilterKeys = new();
    private readonly Dictionary<int, string> _componentFilterKeys = new();
    private readonly HashSet<string> _gameObjectPrefKeyRegistry = new();
    private readonly HashSet<string> _componentPrefKeyRegistry = new();

    private const string GameObjectRegistryPrefKey = PrefKeyPrefix + ".Registry.GameObject";
    private const string ComponentRegistryPrefKey = PrefKeyPrefix + ".Registry.Component";

    private int _changeVersion;
    public int ChangeVersion => _changeVersion;

    public IReadOnlyList<DebugEntry> Entries => _entries;

    public int MaxEntries
    {
        get => _maxEntries;
        set
        {
            int nextValue = Mathf.Max(100, value);
            if (_maxEntries == nextValue)
                return;

            _maxEntries = nextValue;
            _entries ??= new DebugEntryRingBuffer(_maxEntries);
            _entries.SetCapacity(_maxEntries);
            DebugConsolePreferenceStore.SetInt(MaxEntriesPrefKey, _maxEntries);
            MarkChanged();
        }
    }

    public bool GlobalEnabled
    {
        get => _globalEnabled;
        set
        {
            if (_globalEnabled == value)
                return;

            _globalEnabled = value;
            SaveGlobalSettings();
            MarkChanged();
        }
    }

    public bool MirrorToUnityConsole
    {
        get => _mirrorToUnityConsole;
        set
        {
            if (_mirrorToUnityConsole == value)
                return;

            _mirrorToUnityConsole = value;
            SaveGlobalSettings();
            MarkChanged();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("[RuntimeDebugConsole]");
        DontDestroyOnLoad(go);

        go.AddComponent<DebugConsoleManager>();
        go.AddComponent<RuntimeDebugConsoleWindow>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _maxEntries = Mathf.Max(100, DebugConsolePreferenceStore.GetInt(MaxEntriesPrefKey, _maxEntries));
        _entries = new DebugEntryRingBuffer(_maxEntries);

        InitializeFilters();
        LoadGlobalSettings();
        LoadTypeFilters();
        LoadLevelFilters();
        LoadRegistries();

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _gameObjectFilters.Clear();
        _componentFilters.Clear();
        _gameObjectFilterKeys.Clear();
        _componentFilterKeys.Clear();
        MarkChanged();
    }

    public void AddEntry(DebugEntry entry)
    {
        if (entry == null)
            return;

        _entries.Add(entry);
        MarkChanged();
    }

    public void ClearLogs()
    {
        _entries.Clear();
        MarkChanged();
    }

    public bool GetTypeEnabled(DebugType type)
    {
        return _typeFilters[(int)type];
    }

    public void SetTypeEnabled(DebugType type, bool value)
    {
        if (_typeFilters[(int)type] == value)
            return;

        _typeFilters[(int)type] = value;
        SaveTypeFilter(type, value);
        MarkChanged();
    }

    public void SetAllTypes(bool value)
    {
        bool changed = false;
        for (int i = 0; i < _typeFilters.Length; i++)
        {
            if (_typeFilters[i] == value)
                continue;

            _typeFilters[i] = value;
            SaveTypeFilter((DebugType)i, value);
            changed = true;
        }

        if (changed)
            MarkChanged();
    }

    public bool GetLevelEnabled(DebugLogLevel level)
    {
        return level switch
        {
            DebugLogLevel.Warning => _showLogLevelWarning,
            DebugLogLevel.Error => _showLogLevelError,
            _ => _showLogLevelLog
        };
    }

    public void SetLevelEnabled(DebugLogLevel level, bool value)
    {
        bool changed = false;

        switch (level)
        {
            case DebugLogLevel.Warning:
                if (_showLogLevelWarning != value)
                {
                    _showLogLevelWarning = value;
                    changed = true;
                }
                break;

            case DebugLogLevel.Error:
                if (_showLogLevelError != value)
                {
                    _showLogLevelError = value;
                    changed = true;
                }
                break;

            default:
                if (_showLogLevelLog != value)
                {
                    _showLogLevelLog = value;
                    changed = true;
                }
                break;
        }

        if (!changed)
            return;

        SaveLevelFilter(level, value);
        MarkChanged();
    }

    public void SetAllLevels(bool value)
    {
        _showLogLevelLog = value;
        _showLogLevelWarning = value;
        _showLogLevelError = value;

        SaveAllLevelFilters();
        MarkChanged();
    }

    public void SetWarningAndErrorOnly()
    {
        _showLogLevelLog = false;
        _showLogLevelWarning = true;
        _showLogLevelError = true;

        SaveAllLevelFilters();
        MarkChanged();
    }

    public void SetErrorOnly()
    {
        _showLogLevelLog = false;
        _showLogLevelWarning = false;
        _showLogLevelError = true;

        SaveAllLevelFilters();
        MarkChanged();
    }

    public bool GetGameObjectEnabled(GameObject go)
    {
        if (go == null)
            return true;

        int instanceId = go.GetInstanceID();
        if (_gameObjectFilters.TryGetValue(instanceId, out bool cachedValue))
            return cachedValue;

        string filterKey = GetOrCacheGameObjectFilterKey(go);
        string prefKey = GetGameObjectPrefKey(filterKey);
        RegisterFilterPrefKey(_gameObjectPrefKeyRegistry, GameObjectRegistryPrefKey, prefKey);
        bool value = DebugConsolePreferenceStore.GetBool(prefKey, true);
        _gameObjectFilters[instanceId] = value;
        return value;
    }

    public bool GetGameObjectEnabled(int instanceId)
    {
        return !_gameObjectFilters.TryGetValue(instanceId, out bool value) || value;
    }

    public void SetGameObjectEnabled(GameObject go, bool value)
    {
        if (go == null)
            return;

        int instanceId = go.GetInstanceID();
        _gameObjectFilters[instanceId] = value;

        string filterKey = GetOrCacheGameObjectFilterKey(go);
        string prefKey = GetGameObjectPrefKey(filterKey);
        RegisterFilterPrefKey(_gameObjectPrefKeyRegistry, GameObjectRegistryPrefKey, prefKey);
        DebugConsolePreferenceStore.SetBool(prefKey, value);
        MarkChanged();
    }

    public bool GetComponentEnabled(Component component)
    {
        if (component == null)
            return true;

        int instanceId = component.GetInstanceID();
        if (_componentFilters.TryGetValue(instanceId, out bool cachedValue))
            return cachedValue;

        string filterKey = GetOrCacheComponentFilterKey(component);
        string prefKey = GetComponentPrefKey(filterKey);
        RegisterFilterPrefKey(_componentPrefKeyRegistry, ComponentRegistryPrefKey, prefKey);
        bool value = DebugConsolePreferenceStore.GetBool(prefKey, true);
        _componentFilters[instanceId] = value;
        return value;
    }

    public bool GetComponentEnabled(int instanceId)
    {
        return !_componentFilters.TryGetValue(instanceId, out bool value) || value;
    }

    public void SetComponentEnabled(Component component, bool value)
    {
        if (component == null)
            return;

        int instanceId = component.GetInstanceID();
        _componentFilters[instanceId] = value;

        string filterKey = GetOrCacheComponentFilterKey(component);
        string prefKey = GetComponentPrefKey(filterKey);
        RegisterFilterPrefKey(_componentPrefKeyRegistry, ComponentRegistryPrefKey, prefKey);
        DebugConsolePreferenceStore.SetBool(prefKey, value);
        MarkChanged();
    }

    public void ResetAllFiltersToDefault()
    {
        _globalEnabled = true;
        _mirrorToUnityConsole = false;
        SaveGlobalSettings();

        for (int i = 0; i < _typeFilters.Length; i++)
        {
            _typeFilters[i] = true;
            SaveTypeFilter((DebugType)i, true);
        }

        _showLogLevelLog = true;
        _showLogLevelWarning = true;
        _showLogLevelError = true;
        SaveAllLevelFilters();

        foreach (string prefKey in _gameObjectPrefKeyRegistry)
            DebugConsolePreferenceStore.DeleteKey(prefKey);

        foreach (string prefKey in _componentPrefKeyRegistry)
            DebugConsolePreferenceStore.DeleteKey(prefKey);

        _gameObjectPrefKeyRegistry.Clear();
        _componentPrefKeyRegistry.Clear();
        SaveRegistry(GameObjectRegistryPrefKey, _gameObjectPrefKeyRegistry);
        SaveRegistry(ComponentRegistryPrefKey, _componentPrefKeyRegistry);

        _gameObjectFilters.Clear();
        _componentFilters.Clear();
        _gameObjectFilterKeys.Clear();
        _componentFilterKeys.Clear();
        MarkChanged();
    }


[Serializable]
private sealed class DebugConsoleStoredBoolValue
{
    public string Key;
    public bool Value;
}

[Serializable]
private sealed class DebugConsoleSettingsData
{
    public int MaxEntries = 2000;
    public bool GlobalEnabled = true;
    public bool MirrorToUnityConsole;
    public bool ShowLogLevelLog = true;
    public bool ShowLogLevelWarning = true;
    public bool ShowLogLevelError = true;
    public bool[] TypeFilters;
    public List<DebugConsoleStoredBoolValue> GameObjectFilterValues = new();
    public List<DebugConsoleStoredBoolValue> ComponentFilterValues = new();
}

public string ExportSettingsJson()
{
    DebugConsoleSettingsData data = new DebugConsoleSettingsData
    {
        MaxEntries = _maxEntries,
        GlobalEnabled = _globalEnabled,
        MirrorToUnityConsole = _mirrorToUnityConsole,
        ShowLogLevelLog = _showLogLevelLog,
        ShowLogLevelWarning = _showLogLevelWarning,
        ShowLogLevelError = _showLogLevelError,
        TypeFilters = (bool[])_typeFilters.Clone(),
        GameObjectFilterValues = BuildStoredFilterValues(_gameObjectPrefKeyRegistry),
        ComponentFilterValues = BuildStoredFilterValues(_componentPrefKeyRegistry)
    };

    return JsonUtility.ToJson(data, true);
}

public bool ImportSettingsJson(string json)
{
    if (string.IsNullOrWhiteSpace(json))
        return false;

    DebugConsoleSettingsData data = JsonUtility.FromJson<DebugConsoleSettingsData>(json);
    if (data == null)
        return false;

    _maxEntries = Mathf.Max(100, data.MaxEntries);
    DebugConsolePreferenceStore.SetInt(MaxEntriesPrefKey, _maxEntries);
    _entries ??= new DebugEntryRingBuffer(_maxEntries);
    _entries.SetCapacity(_maxEntries);

    _globalEnabled = data.GlobalEnabled;
    _mirrorToUnityConsole = data.MirrorToUnityConsole;
    _showLogLevelLog = data.ShowLogLevelLog;
    _showLogLevelWarning = data.ShowLogLevelWarning;
    _showLogLevelError = data.ShowLogLevelError;
    SaveGlobalSettings();
    SaveAllLevelFilters();

    for (int i = 0; i < _typeFilters.Length; i++)
    {
        bool value = data.TypeFilters == null || i >= data.TypeFilters.Length || data.TypeFilters[i];
        _typeFilters[i] = value;
        SaveTypeFilter((DebugType)i, value);
    }

    ClearStoredFilterRegistry(_gameObjectPrefKeyRegistry, GameObjectRegistryPrefKey);
    ClearStoredFilterRegistry(_componentPrefKeyRegistry, ComponentRegistryPrefKey);
    ApplyStoredFilterValues(data.GameObjectFilterValues, _gameObjectPrefKeyRegistry, GameObjectRegistryPrefKey);
    ApplyStoredFilterValues(data.ComponentFilterValues, _componentPrefKeyRegistry, ComponentRegistryPrefKey);

    _gameObjectFilters.Clear();
    _componentFilters.Clear();
    _gameObjectFilterKeys.Clear();
    _componentFilterKeys.Clear();
    MarkChanged();
    return true;
}

private List<DebugConsoleStoredBoolValue> BuildStoredFilterValues(HashSet<string> registry)
{
    List<DebugConsoleStoredBoolValue> result = new List<DebugConsoleStoredBoolValue>();
    foreach (string prefKey in registry)
    {
        if (string.IsNullOrWhiteSpace(prefKey))
            continue;

        result.Add(new DebugConsoleStoredBoolValue
        {
            Key = prefKey,
            Value = DebugConsolePreferenceStore.GetBool(prefKey, true)
        });
    }

    return result;
}

private void ApplyStoredFilterValues(List<DebugConsoleStoredBoolValue> values, HashSet<string> registry, string registryPrefKey)
{
    registry.Clear();

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

    SaveRegistry(registryPrefKey, registry);
}

private void ClearStoredFilterRegistry(HashSet<string> registry, string registryPrefKey)
{
    foreach (string prefKey in registry)
        DebugConsolePreferenceStore.DeleteKey(prefKey);

    registry.Clear();
    SaveRegistry(registryPrefKey, registry);
}

    private void MarkChanged()
    {
        _changeVersion++;
    }

    private void LoadRegistries()
    {
        LoadRegistry(GameObjectRegistryPrefKey, _gameObjectPrefKeyRegistry);
        LoadRegistry(ComponentRegistryPrefKey, _componentPrefKeyRegistry);
    }

    private void LoadRegistry(string registryPrefKey, HashSet<string> target)
    {
        target.Clear();

        string raw = DebugConsolePreferenceStore.GetString(registryPrefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return;

        string[] parts = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            target.Add(parts[i]);
    }

    private void SaveRegistry(string registryPrefKey, HashSet<string> source)
    {
        if (source == null || source.Count == 0)
        {
            DebugConsolePreferenceStore.DeleteKey(registryPrefKey);
            return;
        }

        DebugConsolePreferenceStore.SetString(registryPrefKey, string.Join("\n", source));
    }

    private void RegisterFilterPrefKey(HashSet<string> registry, string registryPrefKey, string prefKey)
    {
        if (string.IsNullOrWhiteSpace(prefKey))
            return;

        if (!registry.Add(prefKey))
            return;

        SaveRegistry(registryPrefKey, registry);
    }

    private void InitializeFilters()
    {
        _typeFilters = new bool[Enum.GetValues(typeof(DebugType)).Length];
        for (int i = 0; i < _typeFilters.Length; i++)
            _typeFilters[i] = true;
    }

    private void LoadGlobalSettings()
    {
        _globalEnabled = DebugConsolePreferenceStore.GetBool($"{PrefKeyPrefix}.GlobalEnabled", _globalEnabled);
        _mirrorToUnityConsole = DebugConsolePreferenceStore.GetBool($"{PrefKeyPrefix}.MirrorToUnity", _mirrorToUnityConsole);
    }

    private void SaveGlobalSettings()
    {
        DebugConsolePreferenceStore.SetBool($"{PrefKeyPrefix}.GlobalEnabled", _globalEnabled);
        DebugConsolePreferenceStore.SetBool($"{PrefKeyPrefix}.MirrorToUnity", _mirrorToUnityConsole);
    }

    private void LoadTypeFilters()
    {
        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        for (int i = 0; i < types.Length; i++)
        {
            DebugType type = types[i];
            _typeFilters[(int)type] = DebugConsolePreferenceStore.GetBool(GetTypePrefKey(type), true);
        }
    }

    private void SaveTypeFilter(DebugType type, bool value)
    {
        DebugConsolePreferenceStore.SetBool(GetTypePrefKey(type), value);
    }

    private void LoadLevelFilters()
    {
        _showLogLevelLog = DebugConsolePreferenceStore.GetBool(GetLevelPrefKey(DebugLogLevel.Log), _showLogLevelLog);
        _showLogLevelWarning = DebugConsolePreferenceStore.GetBool(GetLevelPrefKey(DebugLogLevel.Warning), _showLogLevelWarning);
        _showLogLevelError = DebugConsolePreferenceStore.GetBool(GetLevelPrefKey(DebugLogLevel.Error), _showLogLevelError);
    }

    private void SaveLevelFilter(DebugLogLevel level, bool value)
    {
        DebugConsolePreferenceStore.SetBool(GetLevelPrefKey(level), value);
    }

    private void SaveAllLevelFilters()
    {
        SaveLevelFilter(DebugLogLevel.Log, _showLogLevelLog);
        SaveLevelFilter(DebugLogLevel.Warning, _showLogLevelWarning);
        SaveLevelFilter(DebugLogLevel.Error, _showLogLevelError);
    }

    private string GetOrCacheGameObjectFilterKey(GameObject go)
    {
        int instanceId = go.GetInstanceID();
        if (_gameObjectFilterKeys.TryGetValue(instanceId, out string cachedKey))
            return cachedKey;

        string filterKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(go);
        _gameObjectFilterKeys[instanceId] = filterKey;
        return filterKey;
    }

    private string GetOrCacheComponentFilterKey(Component component)
    {
        int instanceId = component.GetInstanceID();
        if (_componentFilterKeys.TryGetValue(instanceId, out string cachedKey))
            return cachedKey;

        string filterKey = DebugConsoleFilterKeyUtility.GetComponentKey(component);
        _componentFilterKeys[instanceId] = filterKey;
        return filterKey;
    }

    private string GetTypePrefKey(DebugType type)
    {
        return $"{PrefKeyPrefix}.Type.{type}";
    }

    private string GetLevelPrefKey(DebugLogLevel level)
    {
        return $"{PrefKeyPrefix}.Level.{level}";
    }

    private string GetGameObjectPrefKey(string filterKey)
    {
        return $"{PrefKeyPrefix}.GameObject.{filterKey}";
    }

    private string GetComponentPrefKey(string filterKey)
    {
        return $"{PrefKeyPrefix}.Component.{filterKey}";
    }

    public bool IsAllowed(DebugType type, int gameObjectId, int componentId)
    {
        if (!_globalEnabled)
            return false;

        if (!_typeFilters[(int)type])
            return false;

        if (gameObjectId != 0 && !GetGameObjectEnabled(gameObjectId))
            return false;

        if (componentId != 0 && !GetComponentEnabled(componentId))
            return false;

        return true;
    }

    public bool IsAllowed(DebugType type, Object context)
    {
        if (!_globalEnabled)
            return false;

        if (!_typeFilters[(int)type])
            return false;

        if (context is GameObject go)
            return GetGameObjectEnabled(go);

        if (context is Component component)
            return GetGameObjectEnabled(component.gameObject) && GetComponentEnabled(component);

        return true;
    }
}
