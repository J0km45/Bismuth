using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터/기지 이벤트가 외부 소비자에서 정상적으로 구독되는지 확인하기 위한 샘플 리스너
/// 최종 전투 루프가 아니라 로컬 검증용 로그 소비자다.
/// </summary>
public class CombatLoopEventSampleListener : MonoBehaviour
{
    [Header("====이벤트 참조====")]
    [Tooltip("몬스터 생성 이벤트를 구독할 스포너")]
    [SerializeField] private MonsterSpawner _monsterSpawner;
    
    [Tooltip("웨이브 진행 이벤트를 구독할 전투 진행자")]
    [SerializeField] private BattleWaveRunner _battleWaveRunner;

    [Tooltip("기지 이벤트를 구독할 기지 체력 컴포넌트")]
    [SerializeField] private HomeBaseHealth _homeBaseHealth;

    [Space(5)]
    [Header("====런타임 확인값====")]
    [Tooltip("현재 추적 중인 몬스터 수")]
    [SerializeField, Min(0)] private int _trackedMonsterCount;

    [Tooltip("사망으로 제거된 몬스터 수")]
    [SerializeField, Min(0)] private int _diedMonsterCount;

    [Tooltip("기지 도달로 제거된 몬스터 수")]
    [SerializeField, Min(0)] private int _reachedBaseMonsterCount;
    
    [Tooltip("현재 진행 중으로 보고 있는 웨이브 번호")]
    [SerializeField, Min(0)] private int _currentWaveNumber;

    [Tooltip("전투 진행자가 보고한 현재 남은 몬스터 수")]
    [SerializeField, Min(0)] private int _runnerAliveMonsterCount;

    [Tooltip("전체 전투 종료 이벤트 수신 여부")]
    [SerializeField] private bool _battleCompleted;
    
    [Tooltip("전체 전투 실패 이벤트 수신 여부")]
    [SerializeField] private bool _battleFailed;
    
    [Header("====보상 관련====")]
    [Tooltip("마지막으로 받은 처치 보상 값")]
    [SerializeField, Min(0)] private int _lastKillReward;

    [Tooltip("마지막으로 받은 웨이브 클리어 보상 값")]
    [SerializeField, Min(0)] private int _lastWaveClearReward;

    [Tooltip("실제 지급이 아니라, 샘플 리스너가 받은 보상 이벤트 누적 확인값")]
    [SerializeField, Min(0)] private int _rewardPreviewTotal;
    
    [Space(5)]
    [Header("====정비시간 관련====")]
    [Tooltip("다음 정비시간 뒤에 시작될 예정으로 받은 웨이브 번호")]
    [SerializeField, Min(0)] private int _nextWaveNumber;

    [Tooltip("정비시간 시작 이벤트를 현재 받은 상태인지 여부")]
    [SerializeField] private bool _intermissionStarted;

    [Tooltip("샘플 리스너가 마지막으로 받은 정비시간 시작 길이(초)")]
    [SerializeField, Min(0f)] private float _lastIntermissionDuration;

    [Tooltip("샘플 리스너가 마지막으로 받은 정비시간 남은 시간(초)")]
    [SerializeField, Min(0f)] private float _lastIntermissionRemainingTime;

    [Tooltip("정비시간 종료 이벤트를 현재 받은 상태인지 여부")]
    [SerializeField] private bool _intermissionEnded;
    
    private readonly HashSet<MonsterController> _trackedMonsters = new();

    private void Reset()
    {
        _monsterSpawner = GetComponent<MonsterSpawner>();
        _battleWaveRunner = GetComponent<BattleWaveRunner>();
        _homeBaseHealth = GetComponent<HomeBaseHealth>();
    }

    private void OnEnable()
    {
        if (_monsterSpawner == null)
        {
            DebugTool.Error("몬스터 스포너 참조가 비어 있습니다.", DebugType.Enemy, this);
            return;
        }

        if (_battleWaveRunner == null)
        {
            DebugTool.Error("웨이브 진행자 참조가 비어 있습니다.", DebugType.Enemy, this);
            return;
        }

        if (_homeBaseHealth == null)
        {
            DebugTool.Error("기지 체력 참조가 비어 있습니다.", DebugType.Enemy, this);
            return;
        }

        // 생성된 몬스터를 외부 소비자가 이어서 구독할 수 있는 진입점
        _monsterSpawner.MonsterSpawned += HandleMonsterSpawned;

        // 기지 HP 변화와 파괴 시점을 외부에서도 받을 수 있는지 확인
        _homeBaseHealth.Damaged += HandleDamaged;
        _homeBaseHealth.Died += HandleBaseDied;

        // 웨이브 진행 상태를 외부 소비자가 어떤 단위로 받을 수 있는지 확인
        _battleWaveRunner.WaveStarted += HandleWaveStarted;
        _battleWaveRunner.WaveCleared += HandleWaveCleared;
        _battleWaveRunner.AliveMonsterCountChanged += HandleAliveMonsterCountChanged;
        _battleWaveRunner.BattleCompleted += HandleBattleCompleted;
        _battleWaveRunner.BattleFailed += HandleBattleFailed;
        
        // 리워드 보상 이벤트
        _battleWaveRunner.KillRewardReady += HandleKillRewardReady;
        _battleWaveRunner.WaveClearRewardReady += HandleWaveClearRewardReady;
        
        // 정비시간 이벤트
        _battleWaveRunner.NextWaveReady += HandleNextWaveReady;
        _battleWaveRunner.IntermissionStarted += HandleIntermissionStarted;
        _battleWaveRunner.IntermissionTimeChanged += HandleIntermissionTimeChanged;
        _battleWaveRunner.IntermissionEnded += HandleIntermissionEnded;
    }

    private void OnDisable()
    {
        if (_monsterSpawner != null)
        {
            _monsterSpawner.MonsterSpawned -= HandleMonsterSpawned;
        }

        if (_homeBaseHealth != null)
        {
            _homeBaseHealth.Damaged -= HandleDamaged;
            _homeBaseHealth.Died -= HandleBaseDied;
        }

        ClearTrackedMonsters();

        if (_battleWaveRunner != null)
        {
            _battleWaveRunner.WaveStarted -= HandleWaveStarted;
            _battleWaveRunner.WaveCleared -= HandleWaveCleared;
            _battleWaveRunner.AliveMonsterCountChanged -= HandleAliveMonsterCountChanged;
            _battleWaveRunner.BattleCompleted -= HandleBattleCompleted;
            _battleWaveRunner.BattleFailed -= HandleBattleFailed;
            
            // 보상 관련
            _battleWaveRunner.KillRewardReady -= HandleKillRewardReady;
            _battleWaveRunner.WaveClearRewardReady -= HandleWaveClearRewardReady;
            
            // 정비시간 관련
            _battleWaveRunner.NextWaveReady -= HandleNextWaveReady;
            _battleWaveRunner.IntermissionStarted -= HandleIntermissionStarted;
            _battleWaveRunner.IntermissionTimeChanged -= HandleIntermissionTimeChanged;
            _battleWaveRunner.IntermissionEnded -= HandleIntermissionEnded;
        }

        _trackedMonsters.Clear();
        _trackedMonsterCount = 0;
        _diedMonsterCount = 0;
        _reachedBaseMonsterCount = 0;
        _currentWaveNumber = 0;
        _runnerAliveMonsterCount = 0;
        _battleCompleted = false;
        _battleFailed = false;
        
        // 보상 관련
        _lastKillReward = 0;
        _lastWaveClearReward = 0;
        _rewardPreviewTotal = 0;
        
        _nextWaveNumber = 0;
        _intermissionStarted = false;
        _lastIntermissionDuration = 0f;
        _lastIntermissionRemainingTime = 0f;
        _intermissionEnded = false;
    }

    /// <summary>
    /// 스포너가 새 몬스터를 만들었을 때, 외부 소비자도 그 개체의 생명주기 이벤트를 이어서 구독한다.
    /// 샘플 리스너 입장에서는 여기서부터 "개체 단위 추적"이 시작된다.
    /// </summary>
    private void HandleMonsterSpawned(MonsterController monster)
    {
        if (monster == null)
            return;

        if (_trackedMonsters.Add(monster) == false)
            return;

        monster.Died += HandleMonsterDied;
        monster.ReachedBase += HandleMonsterReachedBase;
        monster.HealthChanged += HandleMonsterHealthChanged;

        _trackedMonsterCount = _trackedMonsters.Count;

        DebugTool.Log(
            $"[샘플리스너] 몬스터 생성 감지 / 추적 중 : {_trackedMonsterCount}",
            DebugType.Enemy,
            this);
    }

    /// <summary>
    /// 피격 결과로 현재 HP가 바뀌었을 때 들어오는 콜백이다.
    /// 체력바 갱신, 피격 피드백, 전투 로그 같은 외부 소비 지점이 이 이벤트를 사용할 수 있음을 보여준다.
    /// </summary>
    private void HandleMonsterHealthChanged(MonsterController monster)
    {
        if (monster == null)
            return;

        DebugTool.Log(
            $"[샘플리스너] 몬스터 체력 변경 / 현재 HP : {monster.CurrentHp} / 최대 HP : {monster.MaxHp}",
            DebugType.Enemy,
            this);
    }

    /// <summary>
    /// 몬스터가 사망으로 전투에서 빠졌을 때 추적 목록과 누적 수를 갱신한다.
    /// 외부 소비자 입장에서는 이 시점부터 해당 몬스터를 더 이상 추적할 필요가 없다.
    /// </summary>
    private void HandleMonsterDied(MonsterController monster)
    {
        if (monster == null)
            return;

        UntrackMonster(monster);
        _diedMonsterCount++;

        DebugTool.Log(
            $"[샘플리스너] 몬스터 사망 / 사망 누적 : {_diedMonsterCount} / 현재 추적 중 : {_trackedMonsterCount}",
            DebugType.Enemy,
            this);
    }

    /// <summary>
    /// 몬스터가 기지에 도달해 전투에서 빠졌을 때 추적 목록과 누적 수를 갱신한다.
    /// 사망과 종료 원인은 다르지만, 샘플 리스너 관점에서는 "추적 종료"라는 점이 동일하다.
    /// </summary>
    private void HandleMonsterReachedBase(MonsterController monster)
    {
        if (monster == null)
            return;

        UntrackMonster(monster);
        _reachedBaseMonsterCount++;

        DebugTool.Log(
            $"[샘플리스너] 몬스터 기지 도달 / 도달 누적 : {_reachedBaseMonsterCount} / 현재 추적 중 : {_trackedMonsterCount}",
            DebugType.Enemy,
            this);
    }

    /// <summary>
    /// 기지가 피해를 받았을 때 현재 HP 변화를 외부에서도 받을 수 있음을 확인한다.
    /// 게임 오버 판정이나 UI 갱신은 최종 구조에서 별도 소비자가 맡더라도, 입력 계약은 여기서 검증 가능하다.
    /// </summary>
    private void HandleDamaged(HomeBaseHealth homeBaseHealth)
    {
        DebugTool.Log(
            $"[샘플리스너] 기지 피해 / 현재 HP : {homeBaseHealth.CurrentHp} / 최대 HP : {homeBaseHealth.MaxHp}",
            DebugType.Enemy,
            this);
    }

    /// <summary>
    /// 기지 HP가 0 이하가 되었을 때 게임 오버 입력 계약이 외부까지 전달되는지 확인한다.
    /// 이 리스너는 판정 주체가 아니라, 해당 이벤트를 받을 수 있다는 사실만 검증한다.
    /// </summary>
    private void HandleBaseDied(HomeBaseHealth homeBaseHealth)
    {
        DebugTool.Log("[샘플리스너] 기지 파괴", DebugType.Enemy, this);
    }

    /// <summary>
    /// 몬스터 추적을 해제하고 현재 추적 수를 갱신한다.
    /// 종료 원인이 사망이든 기지 도달이든, 외부 소비자 입장에서는 더 이상 추적 대상이 아니다.
    /// </summary>
    private void UntrackMonster(MonsterController monster)
    {
        if (_trackedMonsters.Remove(monster) == false)
            return;

        monster.Died -= HandleMonsterDied;
        monster.ReachedBase -= HandleMonsterReachedBase;
        monster.HealthChanged -= HandleMonsterHealthChanged;

        _trackedMonsterCount = _trackedMonsters.Count;
    }
    
    private void ClearTrackedMonsters()
    {
        foreach (MonsterController monster in _trackedMonsters)
        {
            if (monster == null)
                continue;

            monster.Died -= HandleMonsterDied;
            monster.ReachedBase -= HandleMonsterReachedBase;
            monster.HealthChanged -= HandleMonsterHealthChanged;
        }

        _trackedMonsters.Clear();
        _trackedMonsterCount = 0;
    }

    /// <summary>
    /// 상위 웨이브 진행자가 새 웨이브를 시작했다고 알릴 때 현재 웨이브 번호를 갱신한다.
    /// 현재는 로컬 검증용이므로, UI 대신 로그와 인스펙터 값 확인을 우선한다.
    /// </summary>
    private void HandleWaveStarted(WaveDataSO waveData)
    {
        if (waveData == null)
            return;

        _currentWaveNumber = waveData.WaveNumber;
        _battleCompleted = false;
        _battleFailed = false;

        _nextWaveNumber = 0;
        _intermissionStarted = false;
        _intermissionEnded = false;
        _lastIntermissionDuration = 0f;
        _lastIntermissionRemainingTime = 0f;
        
        DebugTool.Log(
            $"[샘플리스너] 웨이브 시작 / 현재 웨이브 : {_currentWaveNumber}",
            DebugType.Enemy,
            this);
    }

    /// <summary>
    /// 웨이브의 추가 소환이 끝나고 남은 적까지 모두 정리되어 다음 단계로 넘어갈 수 있을 때 호출된다.
    /// 정비시간, 웨이브 보상, 다음 웨이브 준비 같은 상위 루프 입력 후보 지점이다.
    /// </summary>
    private void HandleWaveCleared(WaveDataSO waveData)
    {
        if (waveData == null)
            return;

        DebugTool.Log(
            $"[샘플리스너] 웨이브 클리어 / 클리어 웨이브 : {waveData.WaveNumber}",
            DebugType.Enemy,
            this);
    }
    
    /// <summary>
    /// 다음 웨이브 준비 이벤트를 받아, 정비시간 뒤에 어떤 웨이브가 시작될 예정인지 확인한다.
    /// 현재는 실제 진행 제어가 아니라, 외부 소비자가 다음 웨이브 정보를 받을 수 있는지 검증하는 용도다.
    /// </summary>
    private void HandleNextWaveReady(WaveDataSO nextWave)
    {
        if (nextWave == null)
            return;

        _nextWaveNumber = nextWave.WaveNumber;
        _intermissionEnded = false;

        DebugTool.Log(
            $"[샘플리스너] 다음 웨이브 준비 / 다음 웨이브 : {_nextWaveNumber}",
            DebugType.Enemy,
            this);
    }

    /// <summary>
    /// 정비시간 시작 이벤트를 받아, 외부 소비자가 다음 웨이브 정보와 시작 길이를 함께 받을 수 있는지 확인한다.
    /// 실제 UI 타이머 소비 전 단계의 계약 검증용이다.
    /// </summary>
    private void HandleIntermissionStarted(WaveDataSO nextWave, float duration)
    {
        _intermissionStarted = true;
        _intermissionEnded = false;
        _lastIntermissionDuration = duration;
        _lastIntermissionRemainingTime = duration;

        if (nextWave != null)
            _nextWaveNumber = nextWave.WaveNumber;

        DebugTool.Log(
            $"[샘플리스너] 정비시간 시작 / 다음 웨이브 : {_nextWaveNumber} / 정비시간 : {duration:F1}초",
            DebugType.Enemy,
            this);
    }

    /// <summary>
    /// 정비시간 남은 시간 변화를 받아 Inspector에서 카운트다운을 확인한다.
    /// </summary>
    private void HandleIntermissionTimeChanged(float remainingTime)
    {
        _lastIntermissionRemainingTime = remainingTime;
    }

    /// <summary>
    /// 정비시간 종료 이벤트를 받아, 다음 웨이브 시작 직전 종료 알림이 외부까지 전달되는지 확인한다.
    /// </summary>
    private void HandleIntermissionEnded()
    {
        _intermissionStarted = false;
        _intermissionEnded = true;
        _lastIntermissionRemainingTime = 0f;

        DebugTool.Log("[샘플리스너] 정비시간 종료", DebugType.Enemy, this);
    }
    
    /// <summary>
    /// 몬스터 사망 시점에 준비된 처치 보상 값을 외부 소비자가 받을 수 있는지 확인한다.
    /// 현재는 실제 재화에 더하지 않고, 샘플 리스너 내부 확인값만 누적한다.
    /// </summary>
    private void HandleKillRewardReady(MonsterController monster, int reward)
    {
        _lastKillReward = reward;
        _rewardPreviewTotal += reward;

        string monsterName = monster != null ? monster.name : "알 수 없는 몬스터";

        DebugTool.Log(
            $"[샘플리스너] 처치 보상 준비 / 대상 : {monsterName} / 보상 : {reward} / 누적 확인값 : {_rewardPreviewTotal}",
            DebugType.Enemy,
            this);
    }

    /// <summary>
    /// 웨이브가 완전히 정리된 뒤 준비된 클리어 보상 값을 외부 소비자가 받을 수 있는지 확인한다.
    /// 현재는 실제 지급이 아니라, 이벤트 계약 검증용으로만 누적 확인한다.
    /// </summary>
    private void HandleWaveClearRewardReady(WaveDataSO waveData, int reward)
    {
        _lastWaveClearReward = reward;
        _rewardPreviewTotal += reward;

        int waveNumber = waveData != null ? waveData.WaveNumber : -1;

        DebugTool.Log(
            $"[샘플리스너] 웨이브 클리어 보상 준비 / 웨이브 : {waveNumber} / 보상 : {reward} / 누적 확인값 : {_rewardPreviewTotal}",
            DebugType.Enemy,
            this);
    }

    /// <summary>
    /// 전투 진행자가 판단한 현재 남은 몬스터 수를 반영한다.
    /// 몬스터 개체 직접 추적 수와 별개로, 최종 게임 루프/UI가 받을 대표 값이 무엇인지 보여주는 용도다.
    /// </summary>
    private void HandleAliveMonsterCountChanged(int aliveMonsterCount)
    {
        _runnerAliveMonsterCount = aliveMonsterCount;

        DebugTool.Log(
            $"[샘플리스너] 남은 몬스터 수 변경 / 현재 수 : {_runnerAliveMonsterCount}",
            DebugType.Enemy,
            this);
    }

    /// <summary>
    /// 모든 웨이브 진행이 끝났음을 상위 진행자가 알렸을 때 호출된다.
    /// 최종 구조에서는 결과창, 클리어 처리, 다음 흐름 연결의 입력 지점이 될 수 있다.
    /// </summary>
    private void HandleBattleCompleted()
    {
        _battleCompleted = true;
        _battleFailed = false;
        _intermissionStarted = false;
        _intermissionEnded = false;
        _nextWaveNumber = 0;
        _lastIntermissionDuration = 0f;
        _lastIntermissionRemainingTime = 0f;
        ClearTrackedMonsters();

        DebugTool.Log("[샘플리스너] 전체 전투 종료", DebugType.Enemy, this);
    }
    
    private void HandleBattleFailed()
    {
        _battleFailed = true;
        _battleCompleted = false;
        _intermissionStarted = false;
        _intermissionEnded = false;
        _nextWaveNumber = 0;
        _lastIntermissionDuration = 0f;
        _lastIntermissionRemainingTime = 0f;

        DebugTool.Log("[샘플리스너] 전체 전투 실패", DebugType.Enemy, this);
    }
}