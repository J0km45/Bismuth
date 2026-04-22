// ------------------------------------------------------------------------------
// 디버그 로그 수집, 필터 상태, 스냅샷, 오브젝트별 표시 상태를 총괄 관리하는 핵심 매니저 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// 로그 저장과 필터 상태, 스냅샷 상태를 총괄하는 중심 매니저 클래스이다.
/// </summary>
public class DebugConsoleManager : MonoBehaviour
{
    /// <summary>
    /// DebugEntryRingBuffer 관련 역할을 담당하는 class이다.
    /// </summary>
    private sealed class DebugEntryRingBuffer : IReadOnlyList<DebugEntry>
    {
        // buffer 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        private DebugEntry[] _buffer;
        // start 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        private int _start;
        // 개수 값을 저장한다. 개수 또는 표시할 항목 수를 저장한다.
        private int _count;

        /// <summary>
        /// 이 생성자는 debug 엔트리 ring buffer 인스턴스를 만들 때 필요한 기본 상태를 준비한다.
        /// </summary>
        public DebugEntryRingBuffer(int capacity)
        {
            _buffer = new DebugEntry[Mathf.Max(1, capacity)];
            _start = 0;
            _count = 0;
        }

        /// <summary>
        /// 개수 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
        /// </summary>
        public int Count => _count;
        /// <summary>
        /// capacity 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
        /// </summary>
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

        /// <summary>
        /// get last 처리를 시도한다. 성공 여부를 bool로 반환하고 실패 시 안전하게 빠져나간다.
        /// </summary>
        public bool TryGetLast(out DebugEntry entry)
        {
            if (_count <= 0)
            {
                entry = null;
                return false;
            }

            entry = this[_count - 1];
            return entry != null;
        }

        /// <summary>
        /// 관련 작업 항목을 추가한다.
        /// </summary>
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

        /// <summary>
        /// 관련 작업를 비우거나 초기화한다. 이전 상태를 제거하고 다음 작업을 준비하는 데 사용한다.
        /// </summary>
        public void Clear()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _start = 0;
            _count = 0;
        }

        /// <summary>
    /// set capacity 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
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

        /// <summary>
        /// enumerator 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
        /// </summary>
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

    /// <summary>
    /// instance 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public static DebugConsoleManager Instance { get; private set; }

    // 최대 엔트리 목록 값을 저장한다. 현재 처리하거나 렌더링할 로그 엔트리 목록을 저장한다.
    [SerializeField] private int _maxEntries = 2000;
    // 전체 활성화 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    [SerializeField] private bool _globalEnabled = true;
    // 미러 to 유니티 console 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    [SerializeField] private bool _mirrorToUnityConsole = false;
    // show 로그 레벨 로그 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    [SerializeField] private bool _showLogLevelLog = true;
    // show 로그 레벨 경고 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    [SerializeField] private bool _showLogLevelWarning = true;
    // show 로그 레벨 오류 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    [SerializeField] private bool _showLogLevelError = true;
    // coalesce duplicate 로그 목록 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    [SerializeField] private bool _coalesceDuplicateLogs = true;
    // 최대 로그 목록 per 프레임 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    [SerializeField] private int _maxLogsPerFrame = 40;

    // 환경설정 prefix 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public const string PreferencePrefix = "DebugConsole.Manager";
    // pref 식별 키 prefix 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string PrefKeyPrefix = PreferencePrefix;
    // 최대 엔트리 목록 pref 식별 키 값을 저장한다. 현재 처리하거나 렌더링할 로그 엔트리 목록을 저장한다.
    private const string MaxEntriesPrefKey = PrefKeyPrefix + ".MaxEntries";

    // 엔트리 목록 값을 저장한다. 현재 처리하거나 렌더링할 로그 엔트리 목록을 저장한다.
    private DebugEntryRingBuffer _entries;
    // 타입 filters 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private bool[] _typeFilters;
    // 오브젝트 filters 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Dictionary<int, bool> _gameObjectFilters = new();
    // 컴포넌트 filters 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Dictionary<int, bool> _componentFilters = new();
    // 오브젝트 필터 식별 키 목록 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private readonly Dictionary<int, string> _gameObjectFilterKeys = new();
    // 컴포넌트 필터 식별 키 목록 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private readonly Dictionary<int, string> _componentFilterKeys = new();
    // 오브젝트 pref 식별 키 registry 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private readonly HashSet<string> _gameObjectPrefKeyRegistry = new();
    // 컴포넌트 pref 식별 키 registry 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private readonly HashSet<string> _componentPrefKeyRegistry = new();

    // 오브젝트 registry pref 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string GameObjectRegistryPrefKey = PrefKeyPrefix + ".Registry.GameObject";
    // 컴포넌트 registry pref 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string ComponentRegistryPrefKey = PrefKeyPrefix + ".Registry.Component";

    // change version 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private int _changeVersion;
    /// <summary>
    /// change version 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public int ChangeVersion => _changeVersion;

    // current 프레임 number 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private int _currentFrameNumber = -1;
    // accepted 로그 개수 this 프레임 값을 저장한다. 개수 또는 표시할 항목 수를 저장한다.
    private int _acceptedLogCountThisFrame;
    // dropped 로그 개수 this 프레임 값을 저장한다. 개수 또는 표시할 항목 수를 저장한다.
    private int _droppedLogCountThisFrame;
    // 대기 dropped 요약 개수 값을 저장한다. 지금 즉시 적용하지 않고 다음 단계에서 반영할 임시 상태를 저장한다.
    private int _pendingDroppedSummaryCount;

    /// <summary>
    /// 엔트리 목록 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
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
    /// <summary>
    /// 자동 create 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private static void AutoCreate()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("[RuntimeDebugConsole]");
        DontDestroyOnLoad(go);

        go.AddComponent<DebugConsoleManager>();
        go.AddComponent<RuntimeDebugConsoleWindow>();
    }

    /// <summary>
    /// 오브젝트가 생성될 때 한 번 호출되며, 필요한 참조와 초기 상태를 준비한다.
    /// </summary>
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

    /// <summary>
    /// 객체가 파괴될 때 호출되며, 남아 있는 참조와 자원을 정리한다.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    /// <summary>
    /// 씬 loaded와 관련된 입력이나 이벤트를 처리한다.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _gameObjectFilters.Clear();
        _componentFilters.Clear();
        _gameObjectFilterKeys.Clear();
        _componentFilterKeys.Clear();
        MarkChanged();
    }

    /// <summary>
    /// 엔트리 항목을 추가한다.
    /// </summary>
    public void AddEntry(DebugEntry entry)
    {
        if (entry == null)
            return;

        InitializeFrameStateIfNeeded();
        FlushPendingDroppedSummaryIfNeeded(entry.FrameCount);

        if (_maxLogsPerFrame > 0 && _acceptedLogCountThisFrame >= _maxLogsPerFrame)
        {
            _droppedLogCountThisFrame++;
            _pendingDroppedSummaryCount++;
            return;
        }

        _acceptedLogCountThisFrame++;
        AddEntryInternal(entry);
    }

    /// <summary>
    /// 프레임 마지막 단계에서 호출되며, 앞선 갱신 결과를 바탕으로 보정 작업을 수행한다.
    /// </summary>
    private void LateUpdate()
    {
        if (_pendingDroppedSummaryCount > 0)
            FlushPendingDroppedSummaryIfNeeded(Time.frameCount + 1);
    }

    /// <summary>
    /// initialize 프레임 상태 if needed 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void InitializeFrameStateIfNeeded()
    {
        int frameNumber = Time.frameCount;
        if (_currentFrameNumber == frameNumber)
            return;

        if (_pendingDroppedSummaryCount > 0)
            FlushPendingDroppedSummaryIfNeeded(frameNumber);

        _currentFrameNumber = frameNumber;
        _acceptedLogCountThisFrame = 0;
        _droppedLogCountThisFrame = 0;
    }

    /// <summary>
    /// flush 대기 dropped 요약 if needed 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void FlushPendingDroppedSummaryIfNeeded(int frameNumber)
    {
        if (_pendingDroppedSummaryCount <= 0)
            return;

        DebugEntry summaryEntry = BuildDroppedSummaryEntry(_pendingDroppedSummaryCount, frameNumber);
        _pendingDroppedSummaryCount = 0;
        _droppedLogCountThisFrame = 0;
        AddEntryInternal(summaryEntry);
    }

    /// <summary>
    /// 엔트리 internal 항목을 추가한다.
    /// </summary>
    private void AddEntryInternal(DebugEntry entry)
    {
        entry.RepeatCount = Mathf.Max(1, entry.RepeatCount);
        entry.RefreshDerivedFields();

        if (_coalesceDuplicateLogs && _entries.TryGetLast(out DebugEntry lastEntry) && CanCoalesce(lastEntry, entry))
        {
            lastEntry.Time = entry.Time;
            lastEntry.FrameCount = entry.FrameCount;
            lastEntry.CapturedAtIsoUtc = entry.CapturedAtIsoUtc;
            lastEntry.RepeatCount += entry.RepeatCount;
            lastEntry.RefreshDerivedFields();
            MarkChanged();
            return;
        }

        _entries.Add(entry);
        MarkChanged();
    }

    /// <summary>
    /// 현재 상태에서 coalesce가 가능한지 검사한다.
    /// </summary>
    private bool CanCoalesce(DebugEntry left, DebugEntry right)
    {
        if (left == null || right == null)
            return false;

        return string.Equals(left.CollapseKey, right.CollapseKey, StringComparison.Ordinal);
    }

    /// <summary>
    /// dropped 요약 엔트리 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private DebugEntry BuildDroppedSummaryEntry(int droppedCount, int frameNumber)
    {
        DebugEntry entry = new DebugEntry
        {
            Time = DateTime.Now.ToString("HH:mm:ss.fff"),
            Message = $"동일 프레임에서 로그 {droppedCount}개가 생략되었습니다.",
            SourceName = nameof(DebugConsoleManager),
            MemberName = nameof(AddEntry),
            LineNumber = 0,
            Type = DebugType.Default,
            Level = DebugLogLevel.Warning,
            Context = this,
            GameObjectId = gameObject != null ? gameObject.GetInstanceID() : 0,
            ComponentId = GetInstanceID(),
            ColorHex = "#ffcc00",
            CallerFilePath = string.Empty,
            CallerColumn = 1,
            StackTrace = string.Empty,
            SequenceId = DateTime.UtcNow.Ticks,
            FrameCount = frameNumber,
            CapturedAtIsoUtc = DateTime.UtcNow.ToString("O"),
            SceneKey = gameObject != null && gameObject.scene.IsValid() ? gameObject.scene.name : string.Empty,
            HierarchyPath = gameObject != null ? gameObject.name : string.Empty,
            GameObjectKey = gameObject != null ? gameObject.scene.name + "/" + gameObject.name : string.Empty,
            ComponentKey = nameof(DebugConsoleManager),
            GameObjectName = gameObject != null ? gameObject.name : nameof(DebugConsoleManager),
            ComponentName = nameof(DebugConsoleManager),
            ComponentTypeName = GetType().FullName,
            RepeatCount = 1
        };

        entry.RefreshDerivedFields();
        return entry;
    }


    /// <summary>
    /// 로그 목록를 비우거나 초기화한다. 이전 상태를 제거하고 다음 작업을 준비하는 데 사용한다.
    /// </summary>
    public void ClearLogs()
    {
        _entries.Clear();
        MarkChanged();
    }

    /// <summary>
    /// 타입 활성화 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public bool GetTypeEnabled(DebugType type)
    {
        return _typeFilters[(int)type];
    }

    /// <summary>
    /// set 타입 활성화 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetTypeEnabled(DebugType type, bool value)
    {
        if (_typeFilters[(int)type] == value)
            return;

        _typeFilters[(int)type] = value;
        SaveTypeFilter(type, value);
        MarkChanged();
    }

    /// <summary>
    /// set all types 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
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

    /// <summary>
    /// 레벨 활성화 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public bool GetLevelEnabled(DebugLogLevel level)
    {
        return level switch
        {
            DebugLogLevel.Warning => _showLogLevelWarning,
            DebugLogLevel.Error => _showLogLevelError,
            _ => _showLogLevelLog
        };
    }

    /// <summary>
    /// set 레벨 활성화 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
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

    /// <summary>
    /// set all levels 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetAllLevels(bool value)
    {
        _showLogLevelLog = value;
        _showLogLevelWarning = value;
        _showLogLevelError = value;

        SaveAllLevelFilters();
        MarkChanged();
    }

    /// <summary>
    /// set 경고 and 오류 only 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetWarningAndErrorOnly()
    {
        _showLogLevelLog = false;
        _showLogLevelWarning = true;
        _showLogLevelError = true;

        SaveAllLevelFilters();
        MarkChanged();
    }

    /// <summary>
    /// set 오류 only 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    public void SetErrorOnly()
    {
        _showLogLevelLog = false;
        _showLogLevelWarning = false;
        _showLogLevelError = true;

        SaveAllLevelFilters();
        MarkChanged();
    }

    /// <summary>
    /// 오브젝트 활성화 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
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

    /// <summary>
    /// 오브젝트 활성화 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public bool GetGameObjectEnabled(int instanceId)
    {
        return !_gameObjectFilters.TryGetValue(instanceId, out bool value) || value;
    }

    /// <summary>
    /// set 오브젝트 활성화 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
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

    /// <summary>
    /// 컴포넌트 활성화 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
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

    /// <summary>
    /// 컴포넌트 활성화 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    public bool GetComponentEnabled(int instanceId)
    {
        return !_componentFilters.TryGetValue(instanceId, out bool value) || value;
    }

    /// <summary>
    /// set 컴포넌트 활성화 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
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

    /// <summary>
    /// all filters to 기본를 기본 상태로 되돌린다. 사용자가 변경한 임시 상태를 초기 기준값으로 복원한다.
    /// </summary>
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
/// <summary>
/// DebugConsoleStoredBoolValue 관련 역할을 담당하는 class이다.
/// </summary>
private sealed class DebugConsoleStoredBoolValue
{
    // 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    public string Key;
    // value 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool Value;
}

[Serializable]
/// <summary>
/// DebugConsoleSettingsData 관련 역할을 담당하는 class이다.
/// </summary>
private sealed class DebugConsoleSettingsData
{
    // 최대 엔트리 목록 값을 저장한다. 현재 처리하거나 렌더링할 로그 엔트리 목록을 저장한다.
    public int MaxEntries = 2000;
    // 전체 활성화 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool GlobalEnabled = true;
    // 미러 to 유니티 console 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool MirrorToUnityConsole;
    // show 로그 레벨 로그 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool ShowLogLevelLog = true;
    // show 로그 레벨 경고 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool ShowLogLevelWarning = true;
    // show 로그 레벨 오류 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool ShowLogLevelError = true;
    // 타입 filters 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool[] TypeFilters;
    // 오브젝트 필터 values 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public List<DebugConsoleStoredBoolValue> GameObjectFilterValues = new();
    // 컴포넌트 필터 values 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public List<DebugConsoleStoredBoolValue> ComponentFilterValues = new();
}

/// <summary>
/// settings json를 외부 파일이나 문자열 형태로 내보낸다.
/// </summary>
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

/// <summary>
/// 외부에서 읽은 settings json를 현재 상태에 반영한다.
/// </summary>
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

/// <summary>
/// stored 필터 values 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
/// </summary>
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

/// <summary>
/// 준비된 stored 필터 values 값을 실제 상태에 반영한다.
/// </summary>
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

/// <summary>
/// stored 필터 registry를 비우거나 초기화한다. 이전 상태를 제거하고 다음 작업을 준비하는 데 사용한다.
/// </summary>
private void ClearStoredFilterRegistry(HashSet<string> registry, string registryPrefKey)
{
    foreach (string prefKey in registry)
        DebugConsolePreferenceStore.DeleteKey(prefKey);

    registry.Clear();
    SaveRegistry(registryPrefKey, registry);
}

    /// <summary>
    /// mark changed 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void MarkChanged()
    {
        _changeVersion++;
    }

    /// <summary>
    /// registries를 저장소에서 불러온다. 이전에 저장한 상태를 다시 적용하는 데 사용한다.
    /// </summary>
    private void LoadRegistries()
    {
        LoadRegistry(GameObjectRegistryPrefKey, _gameObjectPrefKeyRegistry);
        LoadRegistry(ComponentRegistryPrefKey, _componentPrefKeyRegistry);
    }

    /// <summary>
    /// registry를 저장소에서 불러온다. 이전에 저장한 상태를 다시 적용하는 데 사용한다.
    /// </summary>
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

    /// <summary>
    /// registry를 저장소에 기록한다. 다음 실행에서도 같은 상태를 복원하기 위해 사용한다.
    /// </summary>
    private void SaveRegistry(string registryPrefKey, HashSet<string> source)
    {
        if (source == null || source.Count == 0)
        {
            DebugConsolePreferenceStore.DeleteKey(registryPrefKey);
            return;
        }

        DebugConsolePreferenceStore.SetString(registryPrefKey, string.Join("\n", source));
    }

    /// <summary>
    /// register 필터 pref 식별 키 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void RegisterFilterPrefKey(HashSet<string> registry, string registryPrefKey, string prefKey)
    {
        if (string.IsNullOrWhiteSpace(prefKey))
            return;

        if (!registry.Add(prefKey))
            return;

        SaveRegistry(registryPrefKey, registry);
    }

    /// <summary>
    /// initialize filters 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void InitializeFilters()
    {
        _typeFilters = new bool[Enum.GetValues(typeof(DebugType)).Length];
        for (int i = 0; i < _typeFilters.Length; i++)
            _typeFilters[i] = true;
    }

    /// <summary>
    /// 전체 settings를 저장소에서 불러온다. 이전에 저장한 상태를 다시 적용하는 데 사용한다.
    /// </summary>
    private void LoadGlobalSettings()
    {
        _globalEnabled = DebugConsolePreferenceStore.GetBool($"{PrefKeyPrefix}.GlobalEnabled", _globalEnabled);
        _mirrorToUnityConsole = DebugConsolePreferenceStore.GetBool($"{PrefKeyPrefix}.MirrorToUnity", _mirrorToUnityConsole);
    }

    /// <summary>
    /// 전체 settings를 저장소에 기록한다. 다음 실행에서도 같은 상태를 복원하기 위해 사용한다.
    /// </summary>
    private void SaveGlobalSettings()
    {
        DebugConsolePreferenceStore.SetBool($"{PrefKeyPrefix}.GlobalEnabled", _globalEnabled);
        DebugConsolePreferenceStore.SetBool($"{PrefKeyPrefix}.MirrorToUnity", _mirrorToUnityConsole);
    }

    /// <summary>
    /// 타입 filters를 저장소에서 불러온다. 이전에 저장한 상태를 다시 적용하는 데 사용한다.
    /// </summary>
    private void LoadTypeFilters()
    {
        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        for (int i = 0; i < types.Length; i++)
        {
            DebugType type = types[i];
            _typeFilters[(int)type] = DebugConsolePreferenceStore.GetBool(GetTypePrefKey(type), true);
        }
    }

    /// <summary>
    /// 타입 필터를 저장소에 기록한다. 다음 실행에서도 같은 상태를 복원하기 위해 사용한다.
    /// </summary>
    private void SaveTypeFilter(DebugType type, bool value)
    {
        DebugConsolePreferenceStore.SetBool(GetTypePrefKey(type), value);
    }

    /// <summary>
    /// 레벨 filters를 저장소에서 불러온다. 이전에 저장한 상태를 다시 적용하는 데 사용한다.
    /// </summary>
    private void LoadLevelFilters()
    {
        _showLogLevelLog = DebugConsolePreferenceStore.GetBool(GetLevelPrefKey(DebugLogLevel.Log), _showLogLevelLog);
        _showLogLevelWarning = DebugConsolePreferenceStore.GetBool(GetLevelPrefKey(DebugLogLevel.Warning), _showLogLevelWarning);
        _showLogLevelError = DebugConsolePreferenceStore.GetBool(GetLevelPrefKey(DebugLogLevel.Error), _showLogLevelError);
    }

    /// <summary>
    /// 레벨 필터를 저장소에 기록한다. 다음 실행에서도 같은 상태를 복원하기 위해 사용한다.
    /// </summary>
    private void SaveLevelFilter(DebugLogLevel level, bool value)
    {
        DebugConsolePreferenceStore.SetBool(GetLevelPrefKey(level), value);
    }

    /// <summary>
    /// all 레벨 filters를 저장소에 기록한다. 다음 실행에서도 같은 상태를 복원하기 위해 사용한다.
    /// </summary>
    private void SaveAllLevelFilters()
    {
        SaveLevelFilter(DebugLogLevel.Log, _showLogLevelLog);
        SaveLevelFilter(DebugLogLevel.Warning, _showLogLevelWarning);
        SaveLevelFilter(DebugLogLevel.Error, _showLogLevelError);
    }

    /// <summary>
    /// or cache 오브젝트 필터 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetOrCacheGameObjectFilterKey(GameObject go)
    {
        int instanceId = go.GetInstanceID();
        if (_gameObjectFilterKeys.TryGetValue(instanceId, out string cachedKey))
            return cachedKey;

        string filterKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(go);
        _gameObjectFilterKeys[instanceId] = filterKey;
        return filterKey;
    }

    /// <summary>
    /// or cache 컴포넌트 필터 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetOrCacheComponentFilterKey(Component component)
    {
        int instanceId = component.GetInstanceID();
        if (_componentFilterKeys.TryGetValue(instanceId, out string cachedKey))
            return cachedKey;

        string filterKey = DebugConsoleFilterKeyUtility.GetComponentKey(component);
        _componentFilterKeys[instanceId] = filterKey;
        return filterKey;
    }

    /// <summary>
    /// 타입 pref 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetTypePrefKey(DebugType type)
    {
        return $"{PrefKeyPrefix}.Type.{type}";
    }

    /// <summary>
    /// 레벨 pref 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetLevelPrefKey(DebugLogLevel level)
    {
        return $"{PrefKeyPrefix}.Level.{level}";
    }

    /// <summary>
    /// 오브젝트 pref 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetGameObjectPrefKey(string filterKey)
    {
        return $"{PrefKeyPrefix}.GameObject.{filterKey}";
    }

    /// <summary>
    /// 컴포넌트 pref 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetComponentPrefKey(string filterKey)
    {
        return $"{PrefKeyPrefix}.Component.{filterKey}";
    }

    /// <summary>
    /// allowed 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
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

    /// <summary>
    /// allowed 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
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
