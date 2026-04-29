using UnityEngine;

// 거너(Gunner) 시너지 스킬 효과를 UnitStatHub 에 PercentAdd 모디파이어로 기록.
// 효과 : AttackSpeed +N%
//
// 시트값 단위 : 퍼센트 원본값 (예: 30 == 30%).
// Hub 는 fractional(0.3 = 30%) 을 사용하므로 내부에서 × 0.01f 변환.
//
// 키 "SynergySkill_Gunner" 하나로 매번 갱신(Remove → Add).
// 아래 경우엔 Remove 만 수행하고 Add 하지 않는다:
//   - 유닛이 거너 시너지 태그를 보유하지 않음
//   - effectPercent <= 0  (시너지가 비활성이거나 레벨 미달)
//
// 호출자(SynergyStatBinder) 가 시너지 레벨/유닛 목록 변동 시 각 유닛에 대해 Apply 를 호출한다.
public static class GunnerSynergyApplier
{
    public const string Key = "SynergySkill_Gunner";
    public static readonly int SynergyId = (int)SynergyManager.SynergyType.Gunner;

    public static void Apply(UnitStat stat, UnitStatHub hub, float effectPercent)
    {
        if (stat == null || hub == null)
        {
            DebugTool.Warnning("[GunnerSynergyApplier] stat/hub 가 null 이라 적용 스킵", DebugType.Synergy, stat);
            return;
        }

        // 이전 모디파이어 제거. 태그/효과 여부와 무관하게 매번 정리한다.
        hub.Remove(Key);

        // 거너 태그가 없거나 효과 없음 → Remove 만 하고 종료
        if (!HasGunnerTag(stat) || effectPercent <= 0f)
        {
            DebugTool.Log(
                $"[GunnerSynergyApplier] 제거만 적용 | unit={stat.Name}, hasTag={HasGunnerTag(stat)}, effect={effectPercent:F2}%",
                DebugType.Synergy,
                stat
            );
            return;
        }

        float fractional = effectPercent * 0.01f;
        hub.Add(new StatModifier(
            StatType.AttackSpeed,
            StatOperation.PercentAdd,
            fractional,
            ModifierSource.SynergySkill,
            Key
        ));

        DebugTool.Log(
            $"[GunnerSynergyApplier] 적용 | unit={stat.Name}, effect={effectPercent:F2}% (fractional={fractional:F3})",
            DebugType.Synergy,
            stat
        );
    }

    private static bool HasGunnerTag(UnitStat stat)
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
