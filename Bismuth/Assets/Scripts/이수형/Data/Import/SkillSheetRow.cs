using System;

/// <summary>
/// 스킬 시트 한 행을 그대로 담는 raw DTO.
/// 시트의 A, B, E, F, G, H, I, J 열만 파싱한다.
/// (C, D - 효과 유형 컬럼, K - 스킬 설명, L - 비고 는 파싱하지 않음)
/// </summary>
[Serializable]
public class SkillSheetRow
{
    // A.ID  B.CoolDown  E.데미지 공식  F.디버프  G.디버프 시간
    // H.버프  I.버프 시간  J.타겟 수
    public int Id;
    public float CoolDown;

    public float DamageFormula;

    public float DebuffValue;
    public float DebuffDuration;

    public float BuffValue;
    public float BuffDuration;

    public int TargetCount;     // 999 = 무제한 의미
}
