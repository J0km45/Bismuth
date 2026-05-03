using UnityEngine;

// 전사(Warrior) 시너지 스킬의 "공격력 %" 효과를 UnitStatHub 에 PercentAdd 모디파이어로 기록.
// 효과 : AttackPower +N% (effectIndex=0)
//
// 또 다른 효과인 "확률 추가 공격" 은 스탯 변경이 아니라 스킬 트리거이므로
// CombatManager.TryTriggerWarriorExtraAttack / SkillCast.SynergyWarriorCast 경로 그대로 유지.
//
// 시트값 단위 : 퍼센트 원본 (예: 30 == 30%). Hub 는 fractional → × 0.01f.
//
// 키 "SynergySkill_Warrior" 하나로 매번 갱신(Remove → Add).
public static class WarriorSynergyApplier
{
    public const string Key = "SynergySkill_Warrior";
    public static readonly int SynergyId = (int)SynergyManager.SynergyType.Warrior;

    public static void Apply(UnitStat stat, UnitStatHub hub, float effectPercent)
    {
        if (stat == null || hub == null)
        {
            DebugTool.Warnning("[WarriorSynergyApplier] stat/hub 가 null 이라 적용 스킵", DebugType.Synergy, stat);
            return;
        }

        hub.Remove(Key);

        if (!HasWarriorTag(stat) || effectPercent <= 0f)
        {
            DebugTool.Log(
                $"[WarriorSynergyApplier] 제거만 적용 | unit={stat.Name}, hasTag={HasWarriorTag(stat)}, effect={effectPercent:F2}%",
                DebugType.Synergy,
                stat
            );
            return;
        }

        float fractional = effectPercent * 0.01f;
        hub.Add(new StatModifier(
            StatType.AttackPower,
            StatOperation.PercentAdd,
            fractional,
            ModifierSource.SynergySkill,
            Key
        ));

        DebugTool.Log(
            $"[WarriorSynergyApplier] 적용 | unit={stat.Name}, effect={effectPercent:F2}% (fractional={fractional:F3})",
            DebugType.Synergy,
            stat
        );
    }

    private static bool HasWarriorTag(UnitStat stat)
    {
        if (stat.SynergIDs == null) return false;
        for (int i = 0; i < stat.SynergIDs.Length; i++)
        {
            if (stat.SynergIDs[i] == SynergyId)
                return true;
        }
        return false;
    }
}
