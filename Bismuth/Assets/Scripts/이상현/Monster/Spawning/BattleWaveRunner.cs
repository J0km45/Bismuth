using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 전투 설정에 들어 있는 웨이브들을 순서대로 실행
/// 스포너는 웨이브 1개 실행만 맡고, 이 스크립트가 다음 웨이브로 넘길 타이밍을 관리
/// </summary>
public class BattleWaveRunner : MonoBehaviour
{
    [Header("====참조====")]
    [Tooltip("현재 씬이 사용할 맵 전투 설정")]
    [SerializeField] private MapBattleConfigSO _mapBattleConfig;

    [Tooltip("웨이브 1개 실행을 담당하는 스포너")]
    [SerializeField] private MonsterSpawner _monsterSpawner;

    [Tooltip("전투 시작 시 기지 시작 체력 계산에 사용할 난이도 보정 테이블")]
    [SerializeField] private DifficultyModifierTableSO _difficultyModifierTable;

    [Tooltip("전투 시작 시 1회 초기화할 기지 체력 컴포넌트")]
    [SerializeField] private HomeBaseHealth _homeBaseHealth;

    [Space(5)]
    [Header("====실행 설정====")]
    [Tooltip("플레이 시작 시 첫 웨이브를 자동으로 시작할지 여부")]
    [SerializeField] private bool _runOnStart = true;

    [Space(5)]
    [Header("====런타임 확인값====")]
    [Tooltip("현재 진행 중인 웨이브 인덱스(0부터 시작)")]
    [SerializeField, Min(0)] private int _currentWaveIndex;

    [Tooltip("현재 필드에 남아 있는 몬스터 수")]
    [SerializeField, Min(0)] private int _aliveMonsterCount;

    [Tooltip("현재 웨이브에서 소환될 전체 몬스터 수")]
    [SerializeField, Min(0)] private int _currentWaveTotalCount;

    [Tooltip("현재 웨이브에서 소환된 몬스터 수")]
    [SerializeField, Min(0)] private int _currentWaveSpawnedCount;

    [Tooltip("현재 웨이브의 추가 소환이 끝난 상태인지 여부")]
    [SerializeField] private bool _isWaitingForWaveEnd;

    [Tooltip("전투 웨이브 진행이 시작된 상태인지 여부")]
    [SerializeField] private bool _isRunning;

    [Header("====정비 관련====")]
    [Tooltip("웨이브 종료 후 다음 웨이브 시작 전 정비시간(초)")]
    [SerializeField, Min(0f)] private float _intermissionDuration = 30f;

    [Tooltip("현재 정비시간 진행 중인지 여부")]
    [SerializeField] private bool _isIntermissionActive;

    [Tooltip("현재 남아 있는 정비시간(초)")]
    [SerializeField, Min(0f)] private float _intermissionRemainingTime;

    [Tooltip("다음 웨이브 시작 입력을 기다리는 상태인지 여부")]
    [SerializeField] private bool _isWaitingForNextWaveStart;


    public int CurrentWaveIndex => _currentWaveIndex;
    public int AliveMonsterCount => _aliveMonsterCount;
    public int CurrentWaveTotalCount => _currentWaveTotalCount;
    public int CurrentWaveSpawnedCount => _currentWaveSpawnedCount;
    public int CurrentWaveRemainingCount => Mathf.Max(0, _currentWaveTotalCount - _currentWaveSpawnedCount);
    public bool IsRunning => _isRunning;
    public bool IsWaitingForNextWaveStart => _isWaitingForNextWaveStart;
    public bool IsIntermissionActive => _isIntermissionActive;
    public float IntermissionRemainingTime => _intermissionRemainingTime;


    public event Action<WaveDataSO> NextWaveReady;
    public event Action<WaveDataSO> WaveStarted;
    public event Action<WaveDataSO> WaveCleared;
    public event Action<int> AliveMonsterCountChanged;
    public event Action<int, int> WaveSpawnProgressChanged;// <남은 몬스터 수, 전체 몬스터 수>
    public event Action BattleCompleted;
    public event Action BattleFailed;
    public event Action<MonsterController, int> KillRewardReady;
    public event Action<WaveDataSO, int> WaveClearRewardReady;
    public event Action<WaveDataSO, float> IntermissionStarted;
    public event Action<float> IntermissionTimeChanged;
    public event Action IntermissionEnded;


    private readonly HashSet<MonsterController> _trackedMonsters = new();
    private Coroutine _intermissionRoutine;

    private void Reset()
    {
        _monsterSpawner = GetComponent<MonsterSpawner>();
    }

    private void OnEnable()
    {
        if (_monsterSpawner == null) return;
        if (_homeBaseHealth == null) return;

        _monsterSpawner.MonsterSpawned += HandleMonsterSpawned;
        _monsterSpawner.WaveSpawnCompleted += HandleWaveSpawnCompleted;
        _homeBaseHealth.Died += HandleBaseDied;
    }

    private void Start()
    {
        if (_runOnStart == false)
            return;

        StartBattle();
    }

    private void OnDisable()
    {
        if (_monsterSpawner != null)
        {
            _monsterSpawner.MonsterSpawned -= HandleMonsterSpawned;
            _monsterSpawner.WaveSpawnCompleted -= HandleWaveSpawnCompleted;
        }

        if (_homeBaseHealth != null)
        {
            _homeBaseHealth.Died -= HandleBaseDied;
        }

        StopIntermission(false);
        ClearTrackedMonsters();

        _currentWaveTotalCount = 0;
        _currentWaveSpawnedCount = 0;
        _isWaitingForWaveEnd = false;
        _isWaitingForNextWaveStart = false;
        _isRunning = false;
    }

    [ContextMenu("전투 시작")]
    public void StartBattle()
    {
        if (ValidateSettings() == false)
            return;

        if (_isRunning)
        {
            DebugTool.Error("이미 전투 진행 중입니다.", DebugType.Enemy, this);
            return;
        }

        ClearTrackedMonsters();
        // 전투 시작할 때 값 초기화
        _currentWaveIndex = 0;
        _currentWaveTotalCount = 0;
        _currentWaveSpawnedCount = 0;
        _isWaitingForWaveEnd = false;
        _isWaitingForNextWaveStart = false;
        _isIntermissionActive = false;
        _intermissionRemainingTime = 0f;

        _isRunning = true;

        if (InitializeBattleBaseHealth() == false)
        {
            _isRunning = false;
            return;
        }

        StartCurrentWave();
    }

    private bool ValidateSettings()
    {
        if (_mapBattleConfig == null)
        {
            DebugTool.Error("맵 전투 설정이 비어 있습니다.", DebugType.Enemy, this);
            return false;
        }

        if (_monsterSpawner == null)
        {
            DebugTool.Error("몬스터 스포너 참조가 비어 있습니다.", DebugType.Enemy, this);
            return false;
        }

        if (_mapBattleConfig.WaveCount <= 0)
        {
            DebugTool.Error("맵 전투 설정에 웨이브가 없습니다.", DebugType.Enemy, this);
            return false;
        }

        if (_difficultyModifierTable == null)
        {
            DebugTool.Error("난이도 보정 테이블 참조가 비어 있습니다.", DebugType.Enemy, this);
            return false;
        }

        if (_homeBaseHealth == null)
        {
            DebugTool.Error("기지 체력 참조가 비어 있습니다.", DebugType.Enemy, this);
            return false;
        }

        return true;
    }

    [ContextMenu("다음 웨이브 시작")]
    public void StartNextWave()
    {
        if (_isRunning == false)
        {
            DebugTool.Error("전투가 진행 중이 아닙니다.", DebugType.Enemy, this);
            return;
        }

        if (_isWaitingForNextWaveStart == false)
        {
            DebugTool.Error("다음 웨이브 시작 대기 상태가 아닙니다.", DebugType.Enemy, this);
            return;
        }

        StopIntermission(true);
        StartCurrentWave();
    }

    private void StartCurrentWave()
    {
        if (_mapBattleConfig.TryGetWaveAt(_currentWaveIndex, out WaveDataSO waveData) == false)
        {
            CompleteBattle();
            return;
        }

        // 현재 웨이브 전체 몬스터 수 계산
        _currentWaveTotalCount = CalculateWaveTotalCount(waveData);
        // 현재까지 소환 몬스터 수 0으로 리셋
        _currentWaveSpawnedCount = 0;

        if (_monsterSpawner.SetWaveData(waveData) == false)
        {
            DebugTool.Error("스포너에 현재 웨이브를 설정하지 못했습니다.", DebugType.Enemy, this);
            return;
        }

        _isWaitingForWaveEnd = false;
        _isWaitingForNextWaveStart = false;

        DebugTool.Log(
            $"전투 웨이브 시작 / 순서 : {_currentWaveIndex + 1} / 웨이브 번호 : {waveData.WaveNumber}",
            DebugType.Enemy,
            this);

        WaveStarted?.Invoke(waveData);

        WaveSpawnProgressChanged?.Invoke(CurrentWaveRemainingCount, CurrentWaveTotalCount);

        _monsterSpawner.StartSpawn(false);
    }

    private void HandleMonsterSpawned(MonsterController monster)
    {
        if (monster == null)
            return;

        // 몬스터 생성될 때마다 소환된 몬스터 수 증가
        _currentWaveSpawnedCount++;
        WaveSpawnProgressChanged?.Invoke(CurrentWaveRemainingCount, CurrentWaveTotalCount);

        if (_trackedMonsters.Add(monster) == false)
            return;

        monster.Died += HandleMonsterDied;
        monster.ReachedBase += HandleMonsterReachedBase;

        _aliveMonsterCount = _trackedMonsters.Count;
        AliveMonsterCountChanged?.Invoke(_aliveMonsterCount);
    }

    private void HandleWaveSpawnCompleted(WaveDataSO waveData)
    {
        _isWaitingForWaveEnd = true;
        TryAdvanceNextWave();
    }

    private void HandleMonsterDied(MonsterController monster)
    {
        if (monster == null) return;

        UntrackMonster(monster);

        KillRewardReady?.Invoke(monster, monster.KillReward);
        TryAdvanceNextWave();
    }

    private void HandleMonsterReachedBase(MonsterController monster)
    {
        if (monster == null) return;

        UntrackMonster(monster);
        TryAdvanceNextWave();
    }

    private void UntrackMonster(MonsterController monster)
    {
        if (_trackedMonsters.Remove(monster))
        {
            monster.Died -= HandleMonsterDied;
            monster.ReachedBase -= HandleMonsterReachedBase;
        }

        _aliveMonsterCount = _trackedMonsters.Count;
        AliveMonsterCountChanged?.Invoke(_aliveMonsterCount);
    }

    private void ClearTrackedMonsters()
    {
        foreach (MonsterController monster in _trackedMonsters)
        {
            if (monster == null)
                continue;

            monster.Died -= HandleMonsterDied;
            monster.ReachedBase -= HandleMonsterReachedBase;
        }

        _trackedMonsters.Clear();
        _aliveMonsterCount = 0;
        AliveMonsterCountChanged?.Invoke(_aliveMonsterCount);
    }

    private void HandleBaseDied(HomeBaseHealth homeBaseHealth)
    {
        if (_isRunning == false) return;

        _isRunning = false;
        _isWaitingForWaveEnd = false;
        _isWaitingForNextWaveStart = false;
        StopIntermission(false);
        _monsterSpawner.StopSpawn();
        ClearTrackedMonsters();

        _currentWaveTotalCount = 0;
        _currentWaveSpawnedCount = 0;
        WaveSpawnProgressChanged?.Invoke(CurrentWaveRemainingCount, CurrentWaveTotalCount);

        DebugTool.Log("기지 파괴로 전투 진행을 중단합니다.", DebugType.Enemy, this);
        BattleFailed?.Invoke();
    }

    private void TryAdvanceNextWave()
    {
        if (_isRunning == false) return;
        if (_isWaitingForWaveEnd == false) return;
        if (_aliveMonsterCount > 0) return;

        if (_mapBattleConfig.TryGetWaveAt(_currentWaveIndex, out WaveDataSO clearedWave))
        {
            WaveCleared?.Invoke(clearedWave);
            NotifyWaveClearReward(clearedWave);
        }

        _currentWaveIndex++;

        if (_currentWaveIndex >= _mapBattleConfig.WaveCount)
        {
            CompleteBattle();
            return;
        }

        if (_mapBattleConfig.TryGetWaveAt(_currentWaveIndex, out WaveDataSO nextWave) == false)
        {
            CompleteBattle();
            return;
        }

        _isWaitingForWaveEnd = false;
        _isWaitingForNextWaveStart = true;

        BeginIntermission(nextWave);
    }

    private void BeginIntermission(WaveDataSO nextWave)
    {
        if (nextWave == null)
            return;

        _isWaitingForWaveEnd = false;
        _isWaitingForNextWaveStart = true;
        _isIntermissionActive = true;
        _intermissionRemainingTime = _intermissionDuration;

        NextWaveReady?.Invoke(nextWave);
        IntermissionStarted?.Invoke(nextWave, _intermissionDuration);
        IntermissionTimeChanged?.Invoke(_intermissionRemainingTime);

        if (_intermissionRoutine != null)
            StopCoroutine(_intermissionRoutine);

        _intermissionRoutine = StartCoroutine(IntermissionRoutine());
    }

    private IEnumerator IntermissionRoutine()
    {
        while (_isIntermissionActive && _intermissionRemainingTime > 0f)
        {
            _intermissionRemainingTime = Mathf.Max(0f, _intermissionRemainingTime - Time.deltaTime);
            IntermissionTimeChanged?.Invoke(_intermissionRemainingTime);
            yield return null;
        }

        if (_isRunning == false || _isWaitingForNextWaveStart == false)
            yield break;

        StopIntermission(true);
        StartCurrentWave();
    }

    private void StopIntermission(bool notifyEnded)
    {
        if (_intermissionRoutine != null)
        {
            StopCoroutine(_intermissionRoutine);
            _intermissionRoutine = null;
        }

        bool wasIntermissionActive = _isIntermissionActive;
        float previousRemainingTime = _intermissionRemainingTime;

        _isIntermissionActive = false;
        _intermissionRemainingTime = 0f;

        if (notifyEnded == false || wasIntermissionActive == false)
            return;
        
        if (previousRemainingTime > 0f)
        {
            IntermissionTimeChanged?.Invoke(_intermissionRemainingTime);
        }

        IntermissionEnded?.Invoke();
    }

    private void NotifyWaveClearReward(WaveDataSO clearedWave)
    {
        if (clearedWave == null)
            return;

        if (_difficultyModifierTable.TryGetById(
                clearedWave.DifficultyModifierId,
                out DifficultyModifierEntry difficultyModifier) == false)
        {
            DebugTool.Error(
                $"웨이브 클리어 보상 계산에 사용할 난이도 보정 ID를 찾지 못했습니다. ID : {clearedWave.DifficultyModifierId}",
                DebugType.Enemy,
                this);
            return;
        }

        int reward = MonsterRuntimeValueCalculator.CalculateWaveClearReward(
            clearedWave.WaveNumber,
            difficultyModifier);

        WaveClearRewardReady?.Invoke(clearedWave, reward);
    }

    private void CompleteBattle()
    {
        _isRunning = false;
        _isWaitingForWaveEnd = false;
        _isWaitingForNextWaveStart = false;
        StopIntermission(false);

        _currentWaveTotalCount = 0;
        _currentWaveSpawnedCount = 0;
        WaveSpawnProgressChanged?.Invoke(CurrentWaveRemainingCount, CurrentWaveTotalCount);

        DebugTool.Log("모든 웨이브 진행이 끝났습니다.", DebugType.Enemy, this);
        BattleCompleted?.Invoke();
    }

    /// <summary>
    /// 전투 전체에서 한 번만 사용할 시작 기지 체력을 초기화
    /// 전투 시작 시점에만 호출
    /// </summary>
    private bool InitializeBattleBaseHealth()
    {
        if (_mapBattleConfig.TryGetWaveAt(0, out WaveDataSO firstWave) == false)
        {
            DebugTool.Error("첫 웨이브를 찾지 못해 기지 체력을 초기화할 수 없습니다.", DebugType.Enemy, this);
            return false;
        }

        if (_difficultyModifierTable.TryGetById(firstWave.DifficultyModifierId, out DifficultyModifierEntry difficultyModifier) == false)
        {
            DebugTool.Error(
                $"시작 기지 체력 계산에 사용할 난이도 보정 ID를 찾지 못했습니다. ID : {firstWave.DifficultyModifierId}",
                DebugType.Enemy,
                this);
            return false;
        }

        int startingBaseHp =
            MonsterRuntimeValueCalculator.CalculateStartingHomeBaseHp(_homeBaseHealth.BaseMaxHp, difficultyModifier);

        _homeBaseHealth.InitializeBaseHealth(startingBaseHp);
        return true;
    }

    // 현재 웨이브 전체 몬스터 수 계산
    private int CalculateWaveTotalCount(WaveDataSO waveData)
    {
        int total = 0;

        foreach (WaveSpawnEntry entry in waveData.SpawnEntries)
        {
            if (entry == null) continue;

            total += entry.Count;
        }

        return total;
    }
}