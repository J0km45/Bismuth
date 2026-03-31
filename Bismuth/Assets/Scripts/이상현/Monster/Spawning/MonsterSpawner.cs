using System.Collections;
using UnityEngine;

/// <summary>
/// 웨이브 데이터를 읽어 몬스터를 생성하는 스크립트
/// 몬스터 종료 이벤트를 받아 기지 피해 입력을 연결
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    [Header("====데이터 참조====")]
    [Tooltip("실행할 웨이브 데이터")]
    [SerializeField] private WaveDataSO _waveData;

    [Tooltip("난이도 보정 조회용 테이블")]
    [SerializeField] private DifficultyModifierTableSO _difficultyModifierTable;

    [Tooltip("몬스터 이동 경로")]
    [SerializeField] private WaypointPath _waypointPath;

    [Tooltip("기지 체력 참조")]
    [SerializeField] private HomeBaseHealth _homeBaseHealth;
    
    [Space(5)]
    [Header("====실행 설정====")]
    [Tooltip("시작 시 자동 소환")]
    [SerializeField] private bool _spawnOnStart = true;

    [Tooltip("소환 로그 출력")]
    [SerializeField] private bool _enableSpawnLog = true;
    
    private Coroutine _spawnRoutine;
    private bool _isSpawning;
    
    private void Start()
    {
        if (_spawnOnStart == false) return;
        StartSpawn();
    }
    
    [ContextMenu("웨이브 소환 시작")]
    public void StartSpawn()
    {
        if (Application.isPlaying == false)
        {
            DebugTool.Error("플레이 모드에서만 실행할 수 있습니다.",DebugType.Enemy, this);
            return;
        }

        if (_isSpawning || _spawnRoutine != null)
        {
            DebugTool.Error("이미 소환 중입니다.", DebugType.Enemy, this);
            return;
        }

        if (ValidateSpawnSettings() == false) return;
        
        _isSpawning = true;
        _spawnRoutine = StartCoroutine(SpawnWaveRoutine());
    }
    
    private bool ValidateSpawnSettings()
    {
        if (_waveData == null)
        {
            DebugTool.Error("웨이브 데이터가 비어 있습니다.", DebugType.Enemy, this);
            return false;
        }

        if (_difficultyModifierTable == null)
        {
            DebugTool.Error("난이도 보정 테이블이 비어 있습니다.", DebugType.Enemy, this);
            return false;
        }

        if (_waypointPath == null)
        {
            DebugTool.Error("웨이포인트 경로가 비어 있습니다.", DebugType.Enemy, this);
            return false;
        }

        if (_waypointPath.WaypointCount == 0)
        {
            DebugTool.Error("웨이포인트가 없습니다.", DebugType.Enemy, this);
            return false;
        }

        if (_homeBaseHealth == null)
        {
            DebugTool.Error("기지 체력 참조가 비어 있습니다.", DebugType.Enemy, this);
            return false;
        }

        if (_waveData.SpawnEntries == null || _waveData.SpawnEntries.Count == 0)
        {
            DebugTool.Error("소환 엔트리가 없습니다.", DebugType.Enemy, this);
            return false;
        }

        if (_difficultyModifierTable.TryGetById(_waveData.DifficultyModifierId, out _) == false)
        {
            DebugTool.Error(
                $"난이도 보정 ID를 찾지 못했습니다. ID : {_waveData.DifficultyModifierId}",
                DebugType.Enemy,
                this);
            return false;
        }

        return true;
    }
    
    private IEnumerator SpawnWaveRoutine()
    {
        if (_enableSpawnLog)
        {
            DebugTool.Log(
                $"웨이브 {_waveData.WaveNumber} 소환 시작",
                DebugType.Enemy,
                this);
        }
        
        foreach (WaveSpawnEntry entry in _waveData.SpawnEntries)
        {
            if (entry == null) continue;

            if (entry.StartDelay > 0f)
                yield return new WaitForSeconds(entry.StartDelay);

            for (int i = 0; i < entry.Count; i++)
            {
                SpawnMonster(entry);

                if (i < entry.Count - 1 && entry.SpawnInterval > 0f)
                    yield return new WaitForSeconds(entry.SpawnInterval);
            }
        }
        
        _spawnRoutine = null;
        _isSpawning = false;
        
        if (_enableSpawnLog)
        {
            DebugTool.Log($"웨이브 {_waveData.WaveNumber} 소환 완료", DebugType.Enemy, this);
        }
    }
    
    private void SpawnMonster(WaveSpawnEntry entry)
    {
        if (entry.MonsterData == null)
        {
            DebugTool.Error("엔트리의 몬스터 데이터가 비어 있습니다.", DebugType.Enemy, this);
            return;
        }

        if (entry.MonsterData.Prefab == null)
        {
            DebugTool.Error(
                $"몬스터 프리팹이 비어 있습니다. ID : {entry.MonsterData.Id}",
                DebugType.Enemy,
                this);
            return;
        }

        Vector3 spawnPosition = _waypointPath.GetWaypoint(0).position;

        GameObject monsterObject = Instantiate(
            entry.MonsterData.Prefab,
            spawnPosition,
            Quaternion.identity);

        MonsterController monsterController = monsterObject.GetComponent<MonsterController>();
        if (monsterController == null)
        {
            DebugTool.Error(
                $"MonsterController가 없습니다. ID : {entry.MonsterData.Id}",
                DebugType.Enemy,
                this);

            Destroy(monsterObject);
            return;
        }

        MonsterRuntimeValues runtimeValues = BuildRuntimeValues(entry.MonsterData);

        monsterController.Initialize(_waypointPath, runtimeValues);
        monsterController.ReachedBase += HandleMonsterReachedBase;
        monsterController.Died += HandleMonsterDied;
    }
    
    
    // TODO: 기지피해, 처치보상, 웨이브 성장치 계산까지 함께 필요해지면 별도 계산 책임으로 분리
    private MonsterRuntimeValues BuildRuntimeValues(MonsterDataSO monsterData)
    {
        float currentHp = monsterData.BaseHp;
        
        // 현재 웨이브가 사용하는 난이도 보정 ID로 적 체력 계수를 찾는다.
        if (_difficultyModifierTable.TryGetById(_waveData.DifficultyModifierId,
                out DifficultyModifierEntry difficultyModifierEntry))
        {
            currentHp *= difficultyModifierEntry.EnemyHpMultiplier;
        }
        
        return new MonsterRuntimeValues
        {
            MonsterData = monsterData,
            CurrentHp = currentHp,
            DamageToBase = monsterData.BaseDamageToBase,
            KillReward = monsterData.KillReward,
            MoveSpeed = monsterData.MoveSpeed
        };
    }

    // 종료 이벤트 해제
    private void UnsubscribeMonsterEvents(MonsterController monster)
    {
        if (monster == null)
            return;

        monster.ReachedBase -= HandleMonsterReachedBase;
        monster.Died -= HandleMonsterDied;
    }
    
    // 기지 도달 처리
    private void HandleMonsterReachedBase(MonsterController monster)
    {
        if (monster == null)
            return;

        UnsubscribeMonsterEvents(monster);
        _homeBaseHealth.ApplyDamage(monster.DamageToBase);
    }
    
    // 사망 처리
    private void HandleMonsterDied(MonsterController monster)
    {
        if (monster == null)
            return;

        UnsubscribeMonsterEvents(monster);
    }
}