using System.Collections.Generic;
using UnityEngine;

// 시너지 강화(SynergyEnhance) 효과를 UnitStatHub 에 모디파이어로 적재.
//
// 흐름 :
//   1. SynergyEnhanceLevelManager 가 시너지별 현재 레벨 + SO 데이터 진입점을 모두 담당
//   2. levelManager.GetLevelValue(synergyId, level) 로 단계 수치 조회
//   3. 본 Applier 가 (시너지ID → StatType + StatOperation) 매핑으로 Hub 에 적재
//
// 매핑 출처 : 시트의 explain 컬럼.
//   "공격력 증가 +{0}%"        → AttackPower  PercentAdd
//   "공격속도 증가 +{0}%"      → AttackSpeed  PercentAdd
//   "치명타 데미지 증가 +{0}%" → CritDamage   PercentAdd
//   "공격력 증가 +{0}" (% 없음, 엘프 전용) → AttackPower Flat
//
// 단위 변환 :
//   PercentAdd : 시트값 퍼센트 원본 → × 0.01f (fractional)
//   Flat       : 시트값 그대로 (절대값)
//
// 키 컨벤션 : "SynergyEnhance_<SynergyName>_<Stat>"
//   예) "SynergyEnhance_Warrior_AttackPower", "SynergyEnhance_Elf_AttackPower"
// 시너지마다 키가 다르므로, 한 유닛에 여러 시너지 강화가 동시에 걸려도 별도 모디파이어로 공존.
public static class SynergyEnhanceApplier
{
    public const string KeyPrefix = "SynergyEnhance";

    private readonly struct Effect
    {
        public readonly StatType Stat;
        public readonly StatOperation Op;
        public Effect(StatType stat, StatOperation op) { Stat = stat; Op = op; }
    }

    // 시너지 ID → 효과 매핑. 시너지 10개 고정 + explain 보고 결정.
    private static readonly Dictionary<int, Effect> EffectMap = new()
    {
        { (int)SynergyManager.SynergyType.Warrior,  new Effect(StatType.AttackPower, StatOperation.PercentAdd) },
        { (int)SynergyManager.SynergyType.Magician, new Effect(StatType.AttackPower, StatOperation.PercentAdd) },
        { (int)SynergyManager.SynergyType.Archer,   new Effect(StatType.AttackSpeed, StatOperation.PercentAdd) },
        { (int)SynergyManager.SynergyType.Gunner,   new Effect(StatType.AttackPower, StatOperation.PercentAdd) },
        // Fighter : 시트 explain 표기는 "+%" 지만 의미상 데미지 식의 +0.5 계수에 합산되는 구조 → Flat.
        // (격투가 1단계 raw=50 → fractional 0.5 → 치명타 시 +0.5 추가)
        { (int)SynergyManager.SynergyType.Fighter,  new Effect(StatType.CritDamage,  StatOperation.Flat      ) },
        { (int)SynergyManager.SynergyType.Human,    new Effect(StatType.AttackPower, StatOperation.PercentAdd) },
        { (int)SynergyManager.SynergyType.Elf,      new Effect(StatType.AttackPower, StatOperation.Flat      ) },
        { (int)SynergyManager.SynergyType.Orc,      new Effect(StatType.AttackSpeed, StatOperation.PercentAdd) },
        { (int)SynergyManager.SynergyType.Furry,    new Effect(StatType.AttackSpeed, StatOperation.PercentAdd) },
        { (int)SynergyManager.SynergyType.Spirit,   new Effect(StatType.AttackSpeed, StatOperation.PercentAdd) },
    };

    /// <summary>
    /// 한 유닛의 한 시너지 강화 효과를 갱신.
    /// level <= 0 또는 시너지 태그 없음 → Remove 만 (멱등).
    /// </summary>
    public static void Apply(UnitStat stat, UnitStatHub hub, int synergyId, int level, int rawValueAtLevel)
    {
        if (stat == null || hub == null)
        {
            DebugTool.Warnning("[SynergyEnhanceApplier] stat/hub 가 null 이라 적용 스킵", DebugType.Synergy, stat);
            return;
        }

        if (!EffectMap.TryGetValue(synergyId, out Effect effect))
        {
            DebugTool.Warnning(
                $"[SynergyEnhanceApplier] 시너지 ID {synergyId} 의 효과 매핑이 없습니다 (EffectMap 누락)",
                DebugType.Synergy, stat);
            return;
        }

        string key = MakeKey(synergyId, effect.Stat);

        // 멱등 : 무조건 이전 모디파이어 제거
        hub.Remove(key);

        if (!HasSynergyTag(stat, synergyId) || level <= 0 || rawValueAtLevel <= 0)
        {
            // 태그 없음 / 미강화 / 0값 → Remove 만 하고 종료
            return;
        }

        // 단위 변환
        float value = effect.Op == StatOperation.PercentAdd
            ? rawValueAtLevel * 0.01f
            : rawValueAtLevel;

        hub.Add(new StatModifier(
            effect.Stat,
            effect.Op,
            value,
            ModifierSource.SynergyEnhance,
            key
        ));

        DebugTool.Log(
            $"[SynergyEnhanceApplier] 적용 | unit={stat.Name}, synergy={(SynergyManager.SynergyType)synergyId}, level={level}, raw={rawValueAtLevel}, value={value:F3}, op={effect.Op}, key={key}",
            DebugType.Synergy,
            stat
        );
    }

    /// <summary> 시너지 ID 가 매핑에 등록되어 있는지. (디버그/검증용) </summary>
    public static bool HasMapping(int synergyId) => EffectMap.ContainsKey(synergyId);

    /// <summary>
    /// 키 생성 규칙. UI 의 <see cref="UnitStatHub.GetContributionByKey"/> 로
    /// 특정 시너지 강화의 기여량만 콕 집어 조회할 때도 동일 키를 사용한다.
    /// </summary>
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
