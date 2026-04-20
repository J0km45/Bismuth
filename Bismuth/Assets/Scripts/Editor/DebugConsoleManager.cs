using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class DebugConsoleManager : MonoBehaviour
{
    public static DebugConsoleManager Instance { get; private set; }

    [SerializeField] private int _maxEntries = 1000;
    [SerializeField] private bool _globalEnabled = true;
    [SerializeField] private bool _mirrorToUnityConsole = false;

    private const string PrefKeyPrefix = "DebugConsole.Manager";

    private readonly List<DebugEntry> _entries = new();
    private bool[] _typeFilters;
    private readonly Dictionary<int, bool> _gameObjectFilters = new();
    private readonly Dictionary<int, bool> _componentFilters = new();
    private readonly Dictionary<int, string> _gameObjectFilterKeys = new();
    private readonly Dictionary<int, string> _componentFilterKeys = new();
    private readonly HashSet<string> _gameObjectPrefKeyRegistry = new();
    private readonly HashSet<string> _componentPrefKeyRegistry = new();

    private const string GameObjectRegistryPrefKey = PrefKeyPrefix + ".Registry.GameObject";
    private const string ComponentRegistryPrefKey = PrefKeyPrefix + ".Registry.Component";

    public IReadOnlyList<DebugEntry> Entries => _entries;

    public bool GlobalEnabled
    {
        get => _globalEnabled;
        set
        {
            if (_globalEnabled == value)
                return;

            _globalEnabled = value;
            SaveGlobalSettings();
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
    }

    public void AddEntry(DebugEntry entry)
    {
        if (entry == null)
            return;

        _entries.Add(entry);

        if (_entries.Count > _maxEntries)
            _entries.RemoveAt(0);
    }

    public void ClearLogs()
    {
        _entries.Clear();
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
            SaveGlobalSettings();
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
    }


    public void ResetAllFiltersToDefault()
    {
        _globalEnabled = true;
        SaveGlobalSettings();

        for (int i = 0; i < _typeFilters.Length; i++)
        {
            _typeFilters[i] = true;
            SaveTypeFilter((DebugType)i, true);
        }

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
