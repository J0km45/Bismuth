using System;
using System.Collections.Generic;
using UnityEngine;

// 시너지 강화의 현재 레벨을 관리하는 매니저.
// - 시너지 ID(50001~50010) → 현재 레벨 (0=미강화, 1~5=강화 단계)
// - 강화 시도 : 골드 소비 → 레벨 +1 → 이벤트 발화
// - 레벨 변동 시 전체 맵 통보 (SynergyManager.OnSynergyChanged 와 동일 스타일)
//
// 라이프사이클 : 한 게임 안에서만. 게임 종료 시 자연 소멸 (영구 저장 X)
// 강화 게이팅 : 없음. 골드만 충분하면 언제든 가능.
//
// 데이터 진입 : SynergyEnhanceSO 직접 참조. (SynergyManager 가 SynergySO 를 직접 받는 패턴과 동일)
// SynergyEnhanceDataController 는 시트 → SO 로드용일 뿐, 본 매니저는 SO 만 보면 충분.
//
// 효과 적용은 본 매니저의 책임이 아님. SynergyStatBinder 가 OnEnhanceLevelChanged 를 구독해
// 각 유닛의 UnitStatHub 에 모디파이어를 갱신하는 형태로 분리한다.
public class SynergyEnhanceLevelManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private SynergyEnhanceSO _enhanceSO;

    [Header("Scene References")]
    [SerializeField] private PlayerDataManager _playerDataManager;

    [Header("Config")]
    [SerializeField] private int _maxLevel = 5;

    [SerializeField] private bool _log = true;

    // 시너지 ID → 현재 레벨 (없으면 0)
    private readonly Dictionary<int, int> _levels = new();

    // 변동 시 전체 맵 통보 (SynergyManager 와 동일한 스타일).
    // 구독자(Binder) 가 매번 모든 시너지를 다시 동기화한다.
    public event Action<Dictionary<int, int>> OnEnhanceLevelChanged;

    /// <summary> 현재 레벨 맵 (읽기 전용). UI 표시용. </summary>
    public IReadOnlyDictionary<int, int> Levels => _levels;

    public int MaxLevel => _maxLevel;

    private void Awake()
    {
        if (_playerDataManager == null)
            _playerDataManager = FindAnyObjectByType<PlayerDataManager>();
    }

    // ───────── 조회 ─────────

    /// <summary> 해당 시너지의 현재 강화 레벨. 미강화면 0. </summary>
    public int GetLevel(int synergyId)
    {
        return _levels.TryGetValue(synergyId, out int lv) ? lv : 0;
    }

    /// <summary> 다음 단계 강화에 필요한 골드. 향후 산식 확정되면 본 메소드 본문만 교체. </summary>
    public int CalculateEnhanceCost(int synergyId, int currentLevel)
    {
        // TODO : 강화 비용 산식 미정. 임시로 1 고정.
        return 1;
    }

    /// <summary> 더 강화 가능한지 (최대 레벨 미달 여부만 본다. 골드는 별도). </summary>
    public bool CanEnhance(int synergyId)
    {
        return GetLevel(synergyId) < _maxLevel;
    }

    /// <summary>
    /// 시너지 ID 의 단계 수치 (시트 정의). 레벨 0 또는 데이터 없음 → 0.
    /// Binder/Applier 의 단일 진입점.
    /// </summary>
    public int GetLevelValue(int synergyId, int level)
    {
        if (level <= 0) return 0;
        SynergyEnhanceData data = FindData(synergyId);
        return data != null ? data.GetLevelValue(level) : 0;
    }

    /// <summary> 시너지 ID 의 강화 데이터(시트 한 행). 없으면 null. </summary>
    public SynergyEnhanceData FindData(int synergyId)
    {
        if (_enhanceSO == null) return null;

        IReadOnlyList<SynergyEnhanceData> rows = _enhanceSO.Rows;
        if (rows == null) return null;

        for (int i = 0; i < rows.Count; i++)
        {
            SynergyEnhanceData row = rows[i];
            if (row != null && row.Target == synergyId)
                return row;
        }
        return null;
    }

    // ───────── 강화 시도 ─────────

    /// <summary>
    /// 골드를 소비해 한 단계 강화한다. 성공 시 true.
    /// 실패 사유 : 데이터 없음 / 최대 레벨 도달 / 골드 부족.
    /// </summary>
    public bool TryEnhance(int synergyId)
    {
        // 1) 데이터 확인
        if (_enhanceSO == null)
        {
            DebugTool.Warnning("[SynergyEnhanceLevelManager] SynergyEnhanceSO 가 연결되지 않았습니다. 인스펙터에서 SynergyEnhanceTableSO 를 드래그해 주세요.", DebugType.Synergy, this);
            return false;
        }

        SynergyEnhanceData data = FindData(synergyId);
        if (data == null)
        {
            DebugTool.Warnning($"[SynergyEnhanceLevelManager] 시너지 ID {synergyId} 의 강화 데이터를 찾을 수 없습니다.", DebugType.Synergy, this);
            return false;
        }

        // 2) 최대 레벨 체크
        int currentLevel = GetLevel(synergyId);
        if (currentLevel >= _maxLevel)
        {
            DebugTool.Log(
                $"[SynergyEnhanceLevelManager] 시너지 {synergyId}({data.Label}) 는 이미 최대 레벨({_maxLevel}) 입니다.",
                DebugType.Synergy, this);
            return false;
        }

        // 3) 비용 계산 + 골드 체크
        int cost = CalculateEnhanceCost(synergyId, currentLevel);

        if (_playerDataManager == null)
        {
            DebugTool.Warnning("[SynergyEnhanceLevelManager] PlayerDataManager 가 없어 골드 차감을 진행할 수 없습니다.", DebugType.Synergy, this);
            return false;
        }

        if (_playerDataManager.Gold < cost)
        {
            DebugTool.Log(
                $"[SynergyEnhanceLevelManager] 골드 부족 | synergyId={synergyId}({data.Label}), 필요={cost}, 보유={_playerDataManager.Gold}",
                DebugType.Synergy, this);
            return false;
        }

        // 4) 골드 차감 + 레벨 업
        _playerDataManager.Gold -= cost;
        int newLevel = currentLevel + 1;
        _levels[synergyId] = newLevel;

        if (_log)
        {
            DebugTool.Log(
                $"[SynergyEnhanceLevelManager] 강화 성공 | synergyId={synergyId}({data.Label}), level {currentLevel} → {newLevel}, cost={cost}",
                DebugType.Synergy, this);
        }

        // 5) 이벤트 발화 (전체 맵)
        OnEnhanceLevelChanged?.Invoke(_levels);

        return true;
    }
}
