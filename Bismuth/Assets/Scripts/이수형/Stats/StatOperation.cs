// 스탯 연산 종류
// 최종값 공식 : (Base + ΣFlat) × (1 + ΣPercentAdd) × Π(1 + PercentMul)
public enum StatOperation
{
    Flat,         // 상수 가산. 예) 엘프 시너지 강화 (기본 공격력에 상수 합산)
    PercentAdd,   // 퍼센트 합연산. 예) 전사 시너지 + 유닛 강화 + 시너지 강화 공격력%
    PercentMul,   // 퍼센트 곱연산. 확장 여지 (최종 배율 버프 등)
}
