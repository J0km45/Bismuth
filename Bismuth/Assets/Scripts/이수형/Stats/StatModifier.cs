using System;

// 단일 스탯 변경 한 줄.
// Key : 중도 제거를 위한 식별자. 모든 모디파이어는 중간에 제거 가능해야 하므로 반드시 부여된다.
//       null 로 주면 자동 Guid 부여.
//
// Key 네이밍 컨벤션 (시너지 이름 기반) :
//   "UnitEnhance"                          : 유닛 강화 공격력 라인
//   "SynergySkill_Warrior"                 : 전사 시너지 공격력 %
//   "SynergySkill_Elf_Kill"                : 엘프 시너지 처치 보너스
//   "SynergyEnhance_Warrior_AttackPower"   : 시너지 강화 (전사 - 공격력)
//   "SynergyEnhance_Elf_AttackPower"       : 시너지 강화 (엘프 - 공격력, Flat)
//   "OrcBuff"                              : 오크 시너지 웨이브 버프
public class StatModifier
{
    public StatType Target;
    public StatOperation Op;
    public float Value;
    public ModifierSource Source;
    public object Key;

    public StatModifier(StatType target, StatOperation op, float value, ModifierSource source, object key = null)
    {
        Target = target;
        Op = op;
        Value = value;
        Source = source;
        Key = key ?? Guid.NewGuid();
    }

    public override string ToString()
    {
        // 내부 저장은 fractional (0.2 = 20%). 표시만 단위에 맞춰 변환.
        string valueText = Op switch
        {
            StatOperation.PercentAdd => $"{Value * 100f:+0.##;-0.##;0}%",
            StatOperation.PercentMul => $"{Value * 100f:+0.##;-0.##;0}%",
            _ => $"{Value:+0.###;-0.###;0}",
        };
        return $"[{Source}/{Op}] {Target} {valueText} (key={Key})";
    }
}
