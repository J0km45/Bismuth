using System.Collections.Generic;
using UnityEngine;

// 시너지 강화 레벨에 따라 유닛에 부착되는 오라 이펙트를 관리하는 바인더.
//
// 표시 규칙 (확정):
//   - 유닛이 보유한 시너지들 중 강화 레벨 최댓값을 기준으로 한 종류만 표시.
//   - 최댓값 5 → Max 오라 (Normal 보다 우선)
//   - 최댓값 1~4 → Normal 오라
//   - 최댓값 0 또는 시너지 없음 → 표시 안 함
//
// 갱신 트리거 (SynergyStatBinder 와 동일 패턴) :
//   - SummonUnit.OnOwnedTowersChanged           : 유닛 소환/판매/합성
//   - SynergyEnhanceLevelManager.OnEnhanceLevelChanged : 강화 레벨 변동
//   - 한 프레임에 두 이벤트가 연쇄되어도 LateUpdate 에서 1회만 RefreshAll 수렴
//
// 부착 방식 :
//   - 오라를 유닛 transform 의 자식으로 Instantiate (localPosition = 0).
//   - 유닛 GameObject 가 파괴되면 자식도 함께 파괴되어 누수 자동 방지.
//
// 라이프사이클 : 한 게임 안에서만. OnDisable 시 모든 오라 정리.
public class SynergyAuraBinder : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private SummonUnit _summonUnit;
    [SerializeField] private SynergyEnhanceLevelManager _enhanceLevelManager;

    [Header("Aura Prefabs (공통)")]
    // 시너지 종류와 무관하게 단일 프리팹을 모든 강화 시너지에 사용.
    // 색/모양 차별화가 필요해지면 시너지별 매핑으로 확장.
    [SerializeField] private GameObject _auraNormalPrefab;  // 레벨 1~4
    [SerializeField] private GameObject _auraMaxPrefab;     // 레벨 5

    [SerializeField] private bool _log = false;

    private enum AuraKind
    {
        None,
        Normal,
        Max
    }

    private struct AuraState
    {
        public AuraKind Kind;
        public GameObject Instance;
    }

    // 유닛별 현재 부착 오라 상태. UnitStat 참조 키로 사용.
    private readonly Dictionary<UnitStat, AuraState> _auras = new();

    // RefreshAll 시 dict 정리에 쓰이는 임시 셋 (할당 회피용 캐시).
    private readonly HashSet<UnitStat> _aliveScratch = new();
    private readonly List<UnitStat> _toRemoveScratch = new();

    private bool _subscribed;
    private bool _refreshPending;

    private void Awake()
    {
        if (_summonUnit == null)
            _summonUnit = FindAnyObjectByType<SummonUnit>();

        if (_enhanceLevelManager == null)
            _enhanceLevelManager = FindAnyObjectByType<SynergyEnhanceLevelManager>();
    }

    private void OnEnable()
    {
        Subscribe();

        // 후입장(혹은 비활성 → 활성 복귀) 시 현재 상태와 어긋나지 않도록 1회 동기화.
        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();
        CleanupAll();
    }

    private void Subscribe()
    {
        if (_subscribed) return;

        if (_summonUnit != null)
            _summonUnit.OnOwnedTowersChanged += HandleOwnedTowersChanged;

        if (_enhanceLevelManager != null)
            _enhanceLevelManager.OnEnhanceLevelChanged += HandleEnhanceLevelChanged;

        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        if (_summonUnit != null)
            _summonUnit.OnOwnedTowersChanged -= HandleOwnedTowersChanged;

        if (_enhanceLevelManager != null)
            _enhanceLevelManager.OnEnhanceLevelChanged -= HandleEnhanceLevelChanged;

        _subscribed = false;
    }

    private void HandleOwnedTowersChanged()
    {
        _refreshPending = true;
    }

    private void HandleEnhanceLevelChanged(Dictionary<int, int> _)
    {
        _refreshPending = true;
    }

    private void LateUpdate()
    {
        if (!_refreshPending) return;
        _refreshPending = false;
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (_summonUnit == null || _enhanceLevelManager == null)
            return;

        IReadOnlyList<SummonUnit.SummonedTowerRecord> towers = _summonUnit.OwnedTowers;

        _aliveScratch.Clear();

        // 1) 살아있는 유닛 순회 → desired 오라 적용 (변경 시에만 실제 작업)
        for (int i = 0; i < towers.Count; i++)
        {
            SummonUnit.SummonedTowerRecord record = towers[i];
            if (record == null || record.unitStat == null) continue;

            UnitStat stat = record.unitStat;
            _aliveScratch.Add(stat);

            AuraKind desired = DetermineAuraKind(stat);
            ApplyAura(stat, desired);
        }

        // 2) dict 에 남은 죽은 유닛 정리 (외부에서 OwnedTowers 에서 빠진 케이스).
        // GameObject 가 이미 파괴됐으면 instance == null 로 자연 정리되지만 dict 엔트리는 명시적 제거.
        _toRemoveScratch.Clear();
        foreach (KeyValuePair<UnitStat, AuraState> kv in _auras)
        {
            if (!_aliveScratch.Contains(kv.Key))
                _toRemoveScratch.Add(kv.Key);
        }

        for (int i = 0; i < _toRemoveScratch.Count; i++)
        {
            DestroyAura(_toRemoveScratch[i]);
        }

        if (_log)
        {
            DebugTool.Log(
                $"[SynergyAuraBinder] RefreshAll | towers={towers.Count}, activeAuras={_auras.Count}",
                DebugType.Synergy,
                this
            );
        }
    }

    // 유닛의 모든 시너지 강화 레벨 중 최댓값을 기준으로 어떤 오라가 필요한지 판정.
    private AuraKind DetermineAuraKind(UnitStat stat)
    {
        if (stat.SynergIDs == null || stat.SynergIDs.Length == 0)
            return AuraKind.None;

        int maxLevel = 0;
        for (int i = 0; i < stat.SynergIDs.Length; i++)
        {
            int sid = stat.SynergIDs[i];
            int lv = _enhanceLevelManager.GetLevel(sid);
            if (lv > maxLevel) maxLevel = lv;
        }

        if (maxLevel >= 5) return AuraKind.Max;
        if (maxLevel >= 1) return AuraKind.Normal;
        return AuraKind.None;
    }

    private void ApplyAura(UnitStat stat, AuraKind desired)
    {
        _auras.TryGetValue(stat, out AuraState current);

        // 종류가 동일하고 인스턴스도 살아있으면 그대로 유지 (시각적 끊김 방지).
        if (current.Kind == desired && current.Instance != null)
            return;

        // 기존 인스턴스 정리 (있으면).
        if (current.Instance != null)
            Destroy(current.Instance);

        if (desired == AuraKind.None)
        {
            _auras.Remove(stat);

            if (_log)
            {
                DebugTool.Log(
                    $"[SynergyAuraBinder] 오라 제거 | unit={stat.Name}",
                    DebugType.Synergy,
                    stat
                );
            }
            return;
        }

        GameObject prefab = desired == AuraKind.Max ? _auraMaxPrefab : _auraNormalPrefab;
        if (prefab == null)
        {
            // 프리팹 미할당 시 dict 에서 빼고 종료. 추후 인스펙터에 채우면 다음 Refresh 에 자동 적용.
            _auras.Remove(stat);
            return;
        }

        GameObject instance = Instantiate(prefab, stat.transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        _auras[stat] = new AuraState { Kind = desired, Instance = instance };

        if (_log)
        {
            DebugTool.Log(
                $"[SynergyAuraBinder] 오라 부착 | unit={stat.Name}, kind={desired}, prefab={prefab.name}",
                DebugType.Synergy,
                stat
            );
        }
    }

    private void DestroyAura(UnitStat stat)
    {
        if (stat == null)
        {
            // 키 자체가 null/Destroyed 라 정상 Remove 가 안 될 수 있음. 안전 패스.
            _auras.Remove(stat);
            return;
        }

        if (_auras.TryGetValue(stat, out AuraState state))
        {
            _auras.Remove(stat);
            if (state.Instance != null)
                Destroy(state.Instance);
        }
    }

    private void CleanupAll()
    {
        foreach (KeyValuePair<UnitStat, AuraState> kv in _auras)
        {
            if (kv.Value.Instance != null)
                Destroy(kv.Value.Instance);
        }

        _auras.Clear();
        _aliveScratch.Clear();
        _toRemoveScratch.Clear();
    }
}
