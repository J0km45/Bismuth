using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Data/Skill/Skill Data")]
public class SkillDataSO : ScriptableObject
{
    [Header("====기본 정보====")]
    [Tooltip("스킬 고유 ID")]
    [SerializeField] private int _id;

    [Space(5)]
    [Header("====쿨다운====")]
    [Tooltip("스킬 쿨다운 (초)")]
    [SerializeField, Min(0)] private float _coolDown;

    [Space(5)]
    [Header("====데미지 / 효과====")]
    [Tooltip("데미지 공식 (% 단위)")]
    [SerializeField, Min(0)] private float _damageFormula;

    [Tooltip("디버프 수치 (% 단위)")]
    [SerializeField, Min(0)] private float _debuffValue;

    [Tooltip("디버프 지속 시간 (초)")]
    [SerializeField, Min(0)] private float _debuffDuration;

    [Tooltip("버프 수치 (% 단위)")]
    [SerializeField, Min(0)] private float _buffValue;

    [Tooltip("버프 지속 시간 (초)")]
    [SerializeField, Min(0)] private float _buffDuration;

    [Space(5)]
    [Header("====타겟====")]
    [Tooltip("타겟 수\n999 = 무제한 의미")]
    [SerializeField, Min(0)] private int _targetCount;


    public int Id => _id;
    public float CoolDown => _coolDown;
    public float DamageFormula => _damageFormula;
    public float DebuffValue => _debuffValue;
    public float DebuffDuration => _debuffDuration;
    public float BuffValue => _buffValue;
    public float BuffDuration => _buffDuration;
    public int TargetCount => _targetCount;

    /// <summary>
    /// Tools 계층에서 변환한 스킬 데이터를 현재 SO에 덮어씀.
    /// 시트가 소유하는 값만 갱신.
    /// </summary>
    public void OverwriteData(SkillDataValues values)
    {
        _id = values.Id;
        _coolDown = values.CoolDown;
        _damageFormula = values.DamageFormula;
        _debuffValue = values.DebuffValue;
        _debuffDuration = values.DebuffDuration;
        _buffValue = values.BuffValue;
        _buffDuration = values.BuffDuration;
        _targetCount = values.TargetCount;
    }
}

public struct SkillDataValues
{
    public int Id;

    public float CoolDown;

    public float DamageFormula;

    public float DebuffValue;
    public float DebuffDuration;

    public float BuffValue;
    public float BuffDuration;

    public int TargetCount;
}
