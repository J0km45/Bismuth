using UnityEngine;

// 격투가(Fighter) 시너지 스킬 효과를 UnitStatHub 에 Flat 모디파이어로 기록.
// 효과 : CritChance +N (Flat)
//
// 시트값 단위 : 혼재 가능성 (기존 DamageCalculator 의 정규화 규칙을 그대로 승계).
//   - 값이 1 초과면 퍼센트 원본으로 보고 × 0.01f (예: 5 → 0.05)
//   - 값이 1 이하면 이미 fractional 로 보고 그대로 사용 (예: 0.05 → 0.05)
// 최종적으로 Hub 에는 항상 fractional 로 저장.
//
// 키 "SynergySkill_Fighter" 하나로 매번 갱신(Remove → Add).
// Clamp01 은 Hub 가 하지 않는다. 최종 사용측(CombatManager)에서 Mathf.Clamp01 처리.
public static class FighterSynergyApplier
{
    public const string Key = "SynergySkill_Fighter";
    public static readonly int SynergyId = (int)SynergyManager.SynergyType.Fighter;

    public static void Apply(UnitStat stat, UnitStatHub hub, float effectRaw)
    {
        if (stat == null || hub == null)
        {
            DebugTool.Warnning("[FighterSynergyApplier] stat/hub 가 null 이라 적용 스킵", DebugType.Synergy, stat);
            return;
        }

        // 이전 모디파이어 제거. 태그/효과 여부와 무관하게 매번 정리한다.
        hub.Remove(Key);

        if (!HasFighterTag(stat) || effectRaw <= 0f)
        {
            DebugTool.Log(
                $"[FighterSynergyApplier] 제거만 적용 | unit={stat.Name}, hasTag={HasFighterTag(stat)}, effectRaw={effectRaw:F3}",
                DebugType.Synergy,
                stat
            );
            return;
        }

        // 단위 정규화 : 퍼센트 원본 / fractional 혼재 대응
        float fractional = effectRaw > 1f ? effectRaw * 0.01f : effectRaw;

        hub.Add(new StatModifier(
            StatType.CritChance,
            StatOperation.Flat,
            fractional,
            ModifierSource.SynergySkill,
            Key
        ));

        DebugTool.Log(
            $"[FighterSynergyApplier] 적용 | unit={stat.Name}, effectRaw={effectRaw:F3}, fractional={fractional:F3}",
            DebugType.Synergy,
            stat
        );
    }

    private static bool HasFighterTag(UnitStat stat)
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
