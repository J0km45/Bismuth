using System.Collections.Generic;
using UnityEngine;

// 시너지 스킬의 "스탯 효과" 를 유닛들의 UnitStatHub 에 반영하는 중앙 오너.
//
// 구독 이벤트 :
//   - SynergyManager.OnSynergyChanged  : 시너지 레벨이 바뀌었을 때 (유닛 구성 변경 포함)
//   - SummonUnit.OnOwnedTowersChanged  : 유닛 소환/판매/합성 등 리스트 변동 시
//
// 두 이벤트 모두 RefreshAll 로 수렴. 유닛마다 Applier 를 호출해
// "태그 있으면 현재 효과값으로 갱신, 없으면 제거" 를 멱등적으로 처리한다.
//
// 현재 담당 : Gunner (AttackSpeed +%)
// 추후 확장 : Fighter (CritChance Flat), Orc (AttackPower +% : 웨이브 중 코루틴 주도라 일부 로직은 CombatManager 협조)
public class SynergyStatBinder : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private SynergyManager _synergyManager;
    [SerializeField] private SummonUnit _summonUnit;

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

        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        if (_synergyManager != null)
            _synergyManager.OnSynergyChanged -= HandleSynergyChanged;

        if (_summonUnit != null)
            _summonUnit.OnOwnedTowersChanged -= HandleOwnedTowersChanged;

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

    private void LateUpdate()
    {
        if (!_refreshPending) return;
        _refreshPending = false;
        RefreshAll();
    }

    // 모든 보유 유닛에 대해 현재 시너지 효과값으로 Hub 모디파이어를 재동기화.
    private void RefreshAll()
    {
        if (_summonUnit == null || _synergyManager == null)
            return;

        // 현재 효과값 조회. 단위 변환은 각 Applier 내부 책임.
        float gunnerEffect  = _synergyManager.GetEffectValue(GunnerSynergyApplier.SynergyId);
        float fighterEffect = _synergyManager.GetEffectValue(FighterSynergyApplier.SynergyId);
        float warriorEffect = _synergyManager.GetEffectValue(WarriorSynergyApplier.SynergyId);

        IReadOnlyList<SummonUnit.SummonedTowerRecord> towers = _summonUnit.OwnedTowers;

        if (_log)
        {
            DebugTool.Log(
                $"[SynergyStatBinder] RefreshAll | gunnerEffect={gunnerEffect:F2}%, fighterEffect={fighterEffect:F3}, warriorEffect={warriorEffect:F2}%, towers={towers.Count}",
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

            GunnerSynergyApplier.Apply(record.unitStat, hub, gunnerEffect);
            FighterSynergyApplier.Apply(record.unitStat, hub, fighterEffect);
            WarriorSynergyApplier.Apply(record.unitStat, hub, warriorEffect);
        }
    }
}
