using System.Collections.Generic;
using UnityEngine;

// 시너지 강화의 5레벨(MAX) 도달 시 발동하는 bonus_value 를 Hub 모디파이어로 적재.
//
// 시트 컬럼 의미 :
//   - bonus_value   : 적용 수치 (시너지마다 단위 다름. % 면 ×0.01, Flat 이면 그대로)
//   - bonus_explain : 효과 설명 자연어 (코드는 안 씀)
//   - type          : "강화" / "쿨타임" / "조건" 분류
//
// 본 Applier 는 type="강화" 6개 + type="조건" 중 Hub 적재 가능한 정령 1개를 처리한다.
//   - 50001 전사   : 사거리 +1
//   - 50002 마법사 : 공격 타겟 수 +1
//   - 50003 궁수   : 공격속도 +30%
//   - 50005 격투가 : 사거리 +1
//   - 50008 오크   : 사거리 +1
//   - 50009 수인   : 공격 타겟 수 +1
//   - 50010 정령   : 슬로우 적 추가 피해 +70% (Hub 적재만, 발동 분기는 DamageCalculator)
//
// 비대상 :
//   - 50006 인간 (시너지 보상 재화 +1) : 외부 보상 시스템
//   - 50004 거너 (5초 쿨타임)           : 별도 코루틴 (Step 후속)
//   - 50007 엘프 (처치 조건)            : 별도 이벤트 시스템 (Step 후속)
//
// 키 컨벤션 : "SynergyEnhanceBonus_<Name>_<Stat>"
//   기존 단계별 효과 키("SynergyEnhance_<Name>_<Stat>") 와 분리되어 있어
//   한 시너지가 단계 효과 + 5레벨 보너스 둘 다 동시에 가질 수 있다.
public static class SynergyEnhanceBonusApplier
{
    public const string KeyPrefix = "SynergyEnhanceBonus";
    public const int MaxLevel = 5;

    private readonly struct BonusEffect
    {
        public readonly StatType Stat;
        public readonly StatOperation Op;
        // 시트값이 퍼센트 원본(예: 30 = 30%) 이면 true → fractional 변환(×0.01).
        // 절대값 (예: 사거리 +1) 이면 false → 그대로.
        // Op 와 독립적으로 둠 : Flat 이어도 시트값이 % 단위인 케이스(정령) 가 있어서.
        public readonly bool IsPercentValue;
        public BonusEffect(StatType stat, StatOperation op, bool isPercentValue)
        {
            Stat = stat;
            Op = op;
            IsPercentValue = isPercentValue;
        }
    }

    // 5레벨 도달 시 발동되는 bonus 효과 매핑.
    private static readonly Dictionary<int, BonusEffect> BonusMap = new()
    {
        { (int)SynergyManager.SynergyType.Warrior,  new BonusEffect(StatType.Range,               StatOperation.Flat,       false) },  // 사거리 +1
        { (int)SynergyManager.SynergyType.Magician, new BonusEffect(StatType.AttackTargetCount,   StatOperation.Flat,       false) },  // 공격 타겟 수 +1
        { (int)SynergyManager.SynergyType.Archer,   new BonusEffect(StatType.AttackSpeed,         StatOperation.PercentAdd, true ) },  // 공격속도 +30%
        { (int)SynergyManager.SynergyType.Fighter,  new BonusEffect(StatType.Range,               StatOperation.Flat,       false) },  // 사거리 +1
        { (int)SynergyManager.SynergyType.Orc,      new BonusEffect(StatType.Range,               StatOperation.Flat,       false) },  // 사거리 +1
        { (int)SynergyManager.SynergyType.Furry,    new BonusEffect(StatType.AttackTargetCount,   StatOperation.Flat,       false) },  // 공격 타겟 수 +1
        // 정령 : Hub 에는 Flat 으로 적재 (Base=0 이라 PercentAdd 면 결과 0). 시트값(70)은 % 단위 → ×0.01 변환.
        { (int)SynergyManager.SynergyType.Spirit,   new BonusEffect(StatType.BonusDamageVsSlowed, StatOperation.Flat,       true ) },  // 슬로우 적 추가 피해 +70%
    };

    /// <summary>
    /// 한 유닛에 대해 5레벨 보너스를 갱신.
    /// 5레벨 미만이거나 매핑 없는 시너지면 Remove 만 (멱등).
    /// </summary>
    public static void Apply(UnitStat stat, UnitStatHub hub, int synergyId, int level, int bonusValue)
    {
        if (stat == null || hub == null) return;

        // 매핑 없는 시너지 (인간/거너/엘프/정령) → 본 Applier 의 책임 아님
        if (!BonusMap.TryGetValue(synergyId, out BonusEffect effect))
            return;

        string key = MakeKey(synergyId, effect.Stat);

        // 멱등 : 무조건 이전 모디파이어 제거
        hub.Remove(key);

        // 발동 조건 : 시너지 태그 보유 + 5레벨 도달 + bonusValue > 0
        if (!HasSynergyTag(stat, synergyId) || level < MaxLevel || bonusValue <= 0)
            return;

        // 단위 변환 : 시트값이 % 표기면 ×0.01 (정령/궁수), 절대값이면 그대로 (사거리/타겟수).
        float value = effect.IsPercentValue
            ? bonusValue * 0.01f
            : bonusValue;

        hub.Add(new StatModifier(
            effect.Stat,
            effect.Op,
            value,
            ModifierSource.SynergyEnhance,
            key
        ));

        DebugTool.Log(
            $"[SynergyEnhanceBonusApplier] 5레벨 보너스 적용 | unit={stat.Name}, synergy={(SynergyManager.SynergyType)synergyId}, bonus={bonusValue}, value={value:F3}, op={effect.Op}, key={key}",
            DebugType.Synergy,
            stat
        );
    }

    /// <summary> 시너지 ID 가 매핑에 등록되어 있는지. (디버그/검증용) </summary>
    public static bool HasMapping(int synergyId) => BonusMap.ContainsKey(synergyId);

    /// <summary> 키 생성 규칙. UI 의 GetContributionByKey 와 호환. </summary>
    public static string MakeKey(int synergyId, StatType stat)
    {
        string name = ((SynergyManager.SynergyType)synergyId).ToString();
        return $"{KeyPrefix}_{name}_{stat}";
    }

    private static bool HasSynergyTag(UnitStat stat, int synergyId)
    {
        if (stat.SynergIDs == null) return false;
        for (int i = 0; i < stat.SynergIDs.Length; i++)
            if (stat.SynergIDs[i] == synergyId) return true;
        return false;
    }
}
