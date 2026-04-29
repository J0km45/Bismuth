using UnityEngine;

// 유닛 강화(골드 업그레이드) 결과를 UnitStatHub 에 PercentAdd 모디파이어로 기록.
// 공식 :
//   Final AttackPower = Base × (1 + UpgradeRatio × (stat.Level - 1))
// → Hub 모디파이어 : PercentAdd, value = UpgradeRatio × (stat.Level - 1)
//
// 강화가 여러 번 누적되어도 "UnitEnhance" 키 하나로 매번 갱신(Remove → Add)하므로
// 값만 바뀌고 모디파이어 라인은 항상 하나만 유지된다.
public static class UnitEnhanceApplier
{
    // 유닛 강화 모디파이어 키. 전 유닛 공통 상수 1개.
    public const string Key = "UnitEnhance";

    // stat.Level 이 이미 증가된 상태에서 호출할 것.
    // upgradeRatio 는 UnitEnhanceSO 의 EnhanceValue 원문을 그대로 넘김.
    public static void Apply(UnitStat stat, UnitStatHub hub, float upgradeRatio)
    {
        if (stat == null || hub == null)
        {
            DebugTool.Warnning("[UnitEnhanceApplier] stat/hub 가 null 이라 적용 스킵", DebugType.Unit, stat);
            return;
        }

        // 레벨 1 이면 보너스 0 → 모디파이어 제거만.
        int bonusSteps = Mathf.Max(0, stat.Level - 1);
        float percentAdd = upgradeRatio * bonusSteps;

        // 매번 이전 모디파이어 제거 후 재등록 (값만 바뀜)
        hub.Remove(Key);

        if (percentAdd > 0f)
        {
            hub.Add(new StatModifier(
                StatType.AttackPower,
                StatOperation.PercentAdd,
                percentAdd,
                ModifierSource.UnitEnhance,
                Key
            ));
        }

        DebugTool.Log(
            $"[UnitEnhanceApplier] unit={stat.Name}, level={stat.Level}, ratioPerLv={upgradeRatio * 100f:+0.##;-0.##;0}%, total={percentAdd * 100f:+0.##;-0.##;0}%",
            DebugType.Unit,
            stat
        );
    }
}
