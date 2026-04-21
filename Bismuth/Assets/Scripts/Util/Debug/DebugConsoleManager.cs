using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class DebugConsoleManager : MonoBehaviour
{
    public static DebugConsoleManager Instance { get; private set; }

    [SerializeField] private int _maxEntries = 1000;

    private const string PreferencePrefix = "DebugConsole.Manager";
    private const string RegistryGameObjectKey = "Registry.GameObject";
    private const string RegistryComponentKey = "Registry.Component";
    private const string GlobalEnabledKey = "GlobalEnabled";
    private const string MirrorToUnityKey = "MirrorToUnity";
    private const string TypePrefixKey = "Type";
    private const string GameObjectPrefixKey = "GameObject";
    private const string ComponentPrefixKey = "Component";

    private readonly List<DebugEntry> _entries = new();
    private readonly Dictionary<int, bool> _gameObjectFilters = new();
    private readonly Dictionary<int, bool> _componentFilters = new();
    private readonly Dictionary<int, string> _gameObjectFilterKeys = new();
    private readonly Dictionary<int, string> _componentFilterKeys = new();
    private readonly HashSet<string> _gameObjectPrefKeyRegistry = new();
    private readonly HashSet<string> _componentPrefKeyRegistry = new();

    private bool[] _typeFilters;
    private DebugConsoleFilterState _filterState = new DebugConsoleFilterState();
    private DebugConsolePreferenceRepository _repository;
    private int _changeVersion;

    public int ChangeVersion => _changeVersion;
    public IReadOnlyList<DebugEntry> Entries => _entries;

    public bool GlobalEnabled
    {
        get => _filterState.GlobalEnabled;
        set
        {
            if (_filterState.GlobalEnabled == value)
                return;

            _filterState.GlobalEnabled = value;
            SaveGlobalSettings();
            MarkChanged();
        }
    }

    public bool MirrorToUnityConsole
    {
        get => _filterState.MirrorToUnityConsole;
        set
        {
            if (_filterState.MirrorToUnityConsole == value)
                return;

            _filterState.MirrorToUnityConsole = value;
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

        _repository = new DebugConsolePreferenceRepository(PreferencePrefix);

        InitializeFilters();
        LoadGlobalSettings();
        LoadTypeFilters();
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

        if (_entries.Count > _maxEntries)
            _entries.RemoveAt(0);

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

    public bool GetGameObjectEnabled(GameObject go)
    {
        if (go == null)
            return true;

        int instanceId = go.GetInstanceID();
        if (_gameObjectFilters.TryGetValue(instanceId, out bool cachedValue))
            return cachedValue;

        string filterKey = GetOrCacheGameObjectFilterKey(go);
        string prefKey = BuildGameObjectFilterKey(filterKey);
        RegisterFilterPrefKey(_gameObjectPrefKeyRegistry, RegistryGameObjectKey, prefKey);
        bool value = _repository.GetBool(prefKey, true);
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
        string prefKey = BuildGameObjectFilterKey(filterKey);
        RegisterFilterPrefKey(_gameObjectPrefKeyRegistry, RegistryGameObjectKey, prefKey);
        _repository.SetBool(prefKey, value);
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
        string prefKey = BuildComponentFilterKey(filterKey);
        RegisterFilterPrefKey(_componentPrefKeyRegistry, RegistryComponentKey, prefKey);
        bool value = _repository.GetBool(prefKey, true);
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
        string prefKey = BuildComponentFilterKey(filterKey);
        RegisterFilterPrefKey(_componentPrefKeyRegistry, RegistryComponentKey, prefKey);
        _repository.SetBool(prefKey, value);
        MarkChanged();
    }

    public void ResetAllFiltersToDefault()
    {
        _filterState.GlobalEnabled = true;
        _filterState.MirrorToUnityConsole = false;
        SaveGlobalSettings();

        for (int i = 0; i < _typeFilters.Length; i++)
        {
            _typeFilters[i] = true;
            SaveTypeFilter((DebugType)i, true);
        }

        foreach (string prefKey in _gameObjectPrefKeyRegistry)
            _repository.DeleteKey(prefKey);

        foreach (string prefKey in _componentPrefKeyRegistry)
            _repository.DeleteKey(prefKey);

        _gameObjectPrefKeyRegistry.Clear();
        _componentPrefKeyRegistry.Clear();
        _repository.DeleteKey(RegistryGameObjectKey);
        _repository.DeleteKey(RegistryComponentKey);

        _gameObjectFilters.Clear();
        _componentFilters.Clear();
        _gameObjectFilterKeys.Clear();
        _componentFilterKeys.Clear();
        MarkChanged();
    }

    public bool IsAllowed(DebugType type, int gameObjectId, int componentId)
    {
        return DebugConsoleFilterService.IsAllowed(_filterState, GetTypeEnabled, GetGameObjectEnabled, GetComponentEnabled, type, gameObjectId, componentId);
    }

    public bool IsAllowed(DebugType type, Object context)
    {
        return DebugConsoleFilterService.IsAllowed(_filterState, GetTypeEnabled, GetGameObjectEnabled, GetComponentEnabled, type, context);
    }

    private void MarkChanged()
    {
        _changeVersion++;
    }

    private void InitializeFilters()
    {
        _typeFilters = new bool[Enum.GetValues(typeof(DebugType)).Length];
        for (int i = 0; i < _typeFilters.Length; i++)
            _typeFilters[i] = true;
    }

    private void LoadGlobalSettings()
    {
        _filterState.GlobalEnabled = _repository.GetBool(GlobalEnabledKey, _filterState.GlobalEnabled);
        _filterState.MirrorToUnityConsole = _repository.GetBool(MirrorToUnityKey, _filterState.MirrorToUnityConsole);
    }

    private void SaveGlobalSettings()
    {
        _repository.SetBool(GlobalEnabledKey, _filterState.GlobalEnabled);
        _repository.SetBool(MirrorToUnityKey, _filterState.MirrorToUnityConsole);
    }

    private void LoadTypeFilters()
    {
        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        for (int i = 0; i < types.Length; i++)
        {
            DebugType type = types[i];
            _typeFilters[(int)type] = _repository.GetBool(BuildTypeFilterKey(type), true);
        }
    }

    private void SaveTypeFilter(DebugType type, bool value)
    {
        _repository.SetBool(BuildTypeFilterKey(type), value);
    }

    private void LoadRegistries()
    {
        _gameObjectPrefKeyRegistry.Clear();
        foreach (string key in _repository.GetStringSet(RegistryGameObjectKey))
            _gameObjectPrefKeyRegistry.Add(key);

        _componentPrefKeyRegistry.Clear();
        foreach (string key in _repository.GetStringSet(RegistryComponentKey))
            _componentPrefKeyRegistry.Add(key);
    }

    private void RegisterFilterPrefKey(HashSet<string> registry, string registryKey, string prefKey)
    {
        if (string.IsNullOrWhiteSpace(prefKey))
            return;

        if (!registry.Add(prefKey))
            return;

        _repository.SetStringSet(registryKey, registry);
    }

    private string GetOrCacheGameObjectFilterKey(GameObject go)
    {
        int instanceId = go.GetInstanceID();
        if (_gameObjectFilterKeys.TryGetValue(instanceId, out string cachedKey))
            return cachedKey;

        string filterKey = DebugTargetKeyBuilder.BuildGameObjectKey(go);
        _gameObjectFilterKeys[instanceId] = filterKey;
        return filterKey;
    }

    private string GetOrCacheComponentFilterKey(Component component)
    {
        int instanceId = component.GetInstanceID();
        if (_componentFilterKeys.TryGetValue(instanceId, out string cachedKey))
            return cachedKey;

        string filterKey = DebugTargetKeyBuilder.BuildComponentKey(component);
        _componentFilterKeys[instanceId] = filterKey;
        return filterKey;
    }

    private static string BuildTypeFilterKey(DebugType type)
    {
        return $"{TypePrefixKey}.{type}";
    }

    private static string BuildGameObjectFilterKey(string filterKey)
    {
        return $"{GameObjectPrefixKey}.{filterKey}";
    }

    private static string BuildComponentFilterKey(string filterKey)
    {
        return $"{ComponentPrefixKey}.{filterKey}";
    }
}
