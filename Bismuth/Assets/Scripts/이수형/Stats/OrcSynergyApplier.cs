using UnityEngine;

// 오크(Orc) 시너지 스킬의 "버프 사이클" 효과를 UnitStatHub 에 PercentAdd 모디파이어로 기록.
// 효과 : AttackPower +N% (웨이브 중 일정 시간만 ON, 사이클 OFF 구간엔 Remove)
//
// 사이클 주체는 CombatManager.OrcSynergyRoutine. 본 Applier 는 그 코루틴이
// "ON 시점" 과 "OFF 시점" 에 호출하는 단일 책임 헬퍼.
//
// 시트값 단위 : 퍼센트 원본 (예: 30 == 30%). Hub 는 fractional → × 0.01f.
//
// 키 : "OrcBuff" (요약된 컨벤션과 일치).
//
// 호출 패턴 :
//   - ApplyOrcBuff(...)  → 모든 Orc 태그 유닛 순회하며 OrcSynergyApplier.Apply
//   - RemoveOrcBuff(...) → 모든 Orc 태그 유닛 순회하며 OrcSynergyApplier.Remove
public static class OrcSynergyApplier
{
    public const string Key = "OrcBuff";
    public static readonly int SynergyId = (int)SynergyManager.SynergyType.Orc;

    public static void Apply(UnitStat stat, UnitStatHub hub, float effectPercent)
    {
        if (stat == null || hub == null)
        {
            DebugTool.Warnning("[OrcSynergyApplier] stat/hub 가 null 이라 적용 스킵", DebugType.Synergy, stat);
            return;
        }

        // 이전 모디파이어 제거 (재진입 안전)
        hub.Remove(Key);

        // 태그 없는 유닛에는 적용하지 않음 (호출처에서 이미 필터링했어도 방어)
        if (!HasOrcTag(stat) || effectPercent <= 0f)
        {
            DebugTool.Log(
                $"[OrcSynergyApplier] 적용 스킵 | unit={stat.Name}, hasTag={HasOrcTag(stat)}, effect={effectPercent:F2}%",
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
            $"[OrcSynergyApplier] 적용 | unit={stat.Name}, effect={effectPercent:F2}% (fractional={fractional:F3})",
            DebugType.Synergy,
            stat
        );
    }

    public static void Remove(UnitStat stat, UnitStatHub hub)
    {
        if (hub == null) return;

        bool removed = hub.Remove(Key);

        if (removed)
        {
            DebugTool.Log(
                $"[OrcSynergyApplier] 제거 | unit={(stat != null ? stat.Name : "?")}",
                DebugType.Synergy,
                stat
            );
        }
    }

    private static bool HasOrcTag(UnitStat stat)
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
