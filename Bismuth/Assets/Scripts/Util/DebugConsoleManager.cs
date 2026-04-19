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

    private readonly List<DebugEntry> _entries = new();
    private bool[] _typeFilters;
    private readonly Dictionary<int, bool> _gameObjectFilters = new();
    private readonly Dictionary<int, bool> _componentFilters = new();

    public IReadOnlyList<DebugEntry> Entries => _entries;

    public bool GlobalEnabled
    {
        get => _globalEnabled;
        set => _globalEnabled = value;
    }

    public bool MirrorToUnityConsole
    {
        get => _mirrorToUnityConsole;
        set => _mirrorToUnityConsole = value;
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

        _typeFilters = new bool[Enum.GetValues(typeof(DebugType)).Length];
        for (int i = 0; i < _typeFilters.Length; i++)
            _typeFilters[i] = true;

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
        _typeFilters[(int)type] = value;
    }

    public void SetAllTypes(bool value)
    {
        for (int i = 0; i < _typeFilters.Length; i++)
            _typeFilters[i] = value;
    }

    public bool GetGameObjectEnabled(GameObject go)
    {
        if (go == null)
            return true;

        return GetGameObjectEnabled(go.GetInstanceID());
    }

    public bool GetGameObjectEnabled(int instanceId)
    {
        return !_gameObjectFilters.TryGetValue(instanceId, out bool value) || value;
    }

    public void SetGameObjectEnabled(GameObject go, bool value)
    {
        if (go == null)
            return;

        _gameObjectFilters[go.GetInstanceID()] = value;
    }

    public bool GetComponentEnabled(Component component)
    {
        if (component == null)
            return true;

        return GetComponentEnabled(component.GetInstanceID());
    }

    public bool GetComponentEnabled(int instanceId)
    {
        return !_componentFilters.TryGetValue(instanceId, out bool value) || value;
    }

    public void SetComponentEnabled(Component component, bool value)
    {
        if (component == null)
            return;

        _componentFilters[component.GetInstanceID()] = value;
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
        int gameObjectId = 0;
        int componentId = 0;

        if (context is GameObject go)
        {
            gameObjectId = go.GetInstanceID();
        }
        else if (context is Component component)
        {
            gameObjectId = component.gameObject.GetInstanceID();
            componentId = component.GetInstanceID();
        }

        return IsAllowed(type, gameObjectId, componentId);
    }
}
