// 가변 스탯 종류
// 시너지 스킬, 유닛 강화, 시너지 강화로 변동될 수 있는 스탯만 등재한다.
// Base 값은 UnitData(SO)에서 읽어와 UnitStatBoard.SetBase로 주입.
public enum StatType
{
    AttackPower,         // 공격력
    AttackSpeed,         // 초당 공격 횟수
    CritChance,          // 치명타 확률 (0~1)
    CritDamage,          // 치명타 대미지 계수. DamageCalculator 의 기존 crit 계수에 합연산으로 더해짐.
    Range,               // 사거리
    AttackArea,          // 공격 범위
    AttackTargetCount,   // 공격 대상 수
    BonusDamageVsSlowed, // 슬로우 상태 적에게 추가 피해 (정령 연계용)
}
