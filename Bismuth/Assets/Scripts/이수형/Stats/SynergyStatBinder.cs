using System;
using System.Collections.Generic;
using UnityEngine;

// 시너지 스킬 + 시너지 강화의 "스탯 효과" 를 유닛들의 UnitStatHub 에 반영하는 중앙 오너.
//
// 구독 이벤트 :
//   - SynergyManager.OnSynergyChanged              : 시너지 레벨이 바뀌었을 때 (유닛 구성 변경 포함)
//   - SummonUnit.OnOwnedTowersChanged              : 유닛 소환/판매/합성 등 리스트 변동 시
//   - SynergyEnhanceLevelManager.OnEnhanceLevelChanged : 시너지 강화 레벨 변동
//
// 모든 이벤트는 RefreshAll 로 수렴. 유닛마다 Applier 를 호출해
// "태그 있으면 현재 효과값으로 갱신, 없으면 제거" 를 멱등적으로 처리한다.
//
// 현재 담당 :
//   - 시너지 스킬 : Gunner / Fighter / Warrior
//   - 시너지 강화 : 10개 시너지 전체 (SynergyEnhanceApplier 의 EffectMap 참고)
//
// Orc 시너지 스킬은 웨이브 중 코루틴 주도라 CombatManager 측에서 직접 Hub 에 적재 (본 Binder 미관여).
public class SynergyStatBinder : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private SynergyManager _synergyManager;
    [SerializeField] private SummonUnit _summonUnit;
    [SerializeField] private SynergyEnhanceLevelManager _enhanceLevelManager;

    [SerializeField] private bool _log = true;

    private bool _subscribed;

    // 한 프레임에 OnSynergyChanged + OnOwnedTowersChanged 가 연달아 터지는 상황에서
    // 첫 번째 이벤트 시점엔 아직 ownedTowers / synergiesDict 중 한쪽이 반영 안 된 상태일 수 있다.
    // dirty 플래그로 호출을 모아, LateUpdate 에서 "두 상태가 모두 안정된" 최종 1회만 Refresh 한다.
    private bool _refreshPending;

    private void Awake()
    {
        if (_synergyManager == null)
            _synergyManager = FindAnyObjectByType<SynergyManager>();

        if (_summonUnit == null)
            _summonUnit = FindAnyObjectByType<SummonUnit>();

        if (_enhanceLevelManager == null)
            _enhanceLevelManager = FindAnyObjectByType<SynergyEnhanceLevelManager>();
    }

    private void OnEnable()
    {
        Subscribe();

        // 후입장(혹은 비활성 → 활성 복귀) Binder 가 현재 상태와 어긋나지 않도록 1회 동기화
        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (_subscribed) return;

        if (_synergyManager != null)
            _synergyManager.OnSynergyChanged += HandleSynergyChanged;

        if (_summonUnit != null)
            _summonUnit.OnOwnedTowersChanged += HandleOwnedTowersChanged;

        if (_enhanceLevelManager != null)
            _enhanceLevelManager.OnEnhanceLevelChanged += HandleEnhanceLevelChanged;

        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        if (_synergyManager != null)
            _synergyManager.OnSynergyChanged -= HandleSynergyChanged;

        if (_summonUnit != null)
            _summonUnit.OnOwnedTowersChanged -= HandleOwnedTowersChanged;

        if (_enhanceLevelManager != null)
            _enhanceLevelManager.OnEnhanceLevelChanged -= HandleEnhanceLevelChanged;

        _subscribed = false;
    }

    private void HandleSynergyChanged(Dictionary<int, List<int>> _)
    {
        _refreshPending = true;
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

    // 시너지 강화 1라인 — 미리 계산해둔 (시너지ID, 현재레벨, 시트 단계값) 묶음.
    // 매 RefreshAll 마다 한 번만 계산해 모든 유닛 루프에서 재사용.
    private readonly struct EnhanceEntry
    {
        public readonly int SynergyId;
        public readonly int Level;
        public readonly int RawValue;
        public EnhanceEntry(int synergyId, int level, int rawValue)
        {
            SynergyId = synergyId;
            Level = level;
            RawValue = rawValue;
        }
    }

    private readonly List<EnhanceEntry> _enhanceEntries = new();

    // 모든 보유 유닛에 대해 현재 시너지 효과값으로 Hub 모디파이어를 재동기화.
    private void RefreshAll()
    {
        if (_summonUnit == null || _synergyManager == null)
            return;

        // ─── 시너지 스킬 효과값 (단위 변환은 각 Applier 내부 책임) ───
        float gunnerEffect  = _synergyManager.GetEffectValue(GunnerSynergyApplier.SynergyId);
        float fighterEffect = _synergyManager.GetEffectValue(FighterSynergyApplier.SynergyId);
        float warriorEffect = _synergyManager.GetEffectValue(WarriorSynergyApplier.SynergyId);

        // ─── 시너지 강화 단계값 미리 계산 ───
        BuildEnhanceEntries();

        IReadOnlyList<SummonUnit.SummonedTowerRecord> towers = _summonUnit.OwnedTowers;

        if (_log)
        {
            DebugTool.Log(
                $"[SynergyStatBinder] RefreshAll | gunnerEffect={gunnerEffect:F2}%, fighterEffect={fighterEffect:F3}, warriorEffect={warriorEffect:F2}%, towers={towers.Count}, enhanceEntries={_enhanceEntries.Count}",
                DebugType.Synergy,
                this
            );
        }

        for (int i = 0; i < towers.Count; i++)
        {
            SummonUnit.SummonedTowerRecord record = towers[i];
            if (record == null || record.unitStat == null) continue;

            UnitStatHub hub = record.unitStat.GetComponent<UnitStatHub>();
            if (hub == null) continue;

            // 시너지 스킬 (Hub 화 완료된 것만)
            GunnerSynergyApplier.Apply(record.unitStat, hub, gunnerEffect);
            FighterSynergyApplier.Apply(record.unitStat, hub, fighterEffect);
            WarriorSynergyApplier.Apply(record.unitStat, hub, warriorEffect);

            // 시너지 강화 (10개 시너지 전체 갱신. level=0 이면 Applier 가 Remove 만 수행하므로 안전)
            for (int e = 0; e < _enhanceEntries.Count; e++)
            {
                EnhanceEntry entry = _enhanceEntries[e];
                SynergyEnhanceApplier.Apply(record.unitStat, hub, entry.SynergyId, entry.Level, entry.RawValue);
            }
        }
    }

    // 시너지 강화 단계 정보를 enum 전체 순회로 한 번 생성. (시너지 10개 고정)
    // 데이터 조회는 LevelManager 가 단일 진입점.
    private void BuildEnhanceEntries()
    {
        _enhanceEntries.Clear();

        if (_enhanceLevelManager == null) return;

        foreach (object enumValue in Enum.GetValues(typeof(SynergyManager.SynergyType)))
        {
            SynergyManager.SynergyType st = (SynergyManager.SynergyType)enumValue;
            if (st == SynergyManager.SynergyType.None) continue;

            int synergyId = (int)st;
            int level = _enhanceLevelManager.GetLevel(synergyId);
            int rawValue = _enhanceLevelManager.GetLevelValue(synergyId, level);

            _enhanceEntries.Add(new EnhanceEntry(synergyId, level, rawValue));
        }
    }
}
