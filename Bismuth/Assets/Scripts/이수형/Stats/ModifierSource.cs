// 스탯 모디파이어 출처 태그
// 일괄 제거(RemoveBySource) 및 디버그 필터링용.
public enum ModifierSource
{
    None,
    UnitEnhance,     // 유닛 강화 (플레이어가 개별 유닛을 업그레이드)
    SynergySkill,    // 시너지 자체 효과 (전사/거너/엘프/오크 등)
    SynergyEnhance,  // 시너지 강화 (시너지 단위 강화, 해당 태그 보유 유닛 전체 적용)
    Debug,           // 디버그 툴에서 주입
}
