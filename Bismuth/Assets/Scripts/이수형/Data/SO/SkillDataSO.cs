using UnityEngine;

/// <summary>
/// 스킬 데미지의 베이스 종류.
/// 시트엔 정보가 없으므로 SO 인스펙터에서 수동 설정. SO Generator는 이 값을 덮어쓰지 않는다.
/// </summary>
public enum SkillDamageBaseType
{
    AttackPower,    // 공격력 × DamageMultiplier (기본). 30001/30002/30003 등
    MaxHp           // 적 최대 체력 × ratio. 방어력/치명타/슬로우보너스 무시. 30007
}

[CreateAssetMenu(fileName = "SkillData", menuName = "Data/Skill/Skill Data")]
public class SkillDataSO : ScriptableObject
{
    [Header("====기본 정보====")]
    [Tooltip("스킬 고유 ID")]
    [SerializeField] private int _id;

    [Tooltip("스킬 데미지의 베이스. AttackPower=공격력 비례, MaxHp=적 최대 체력 비례.\n시트엔 없는 정보라 인스펙터에서 직접 설정. SO Generator가 덮어쓰지 않음.")]
    [SerializeField] private SkillDamageBaseType _damageBase = SkillDamageBaseType.AttackPower;

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

    [Tooltip("버프가 적용될 스탯. AttackPower=공격력(30005), AttackSpeed=공속(30004) 등.\n시트엔 없는 정보라 인스펙터에서 직접 설정. SO Generator가 덮어쓰지 않음.")]
    [SerializeField] private StatType _buffStatType = StatType.AttackPower;

    [Space(5)]
    [Header("====타겟====")]
    [Tooltip("타겟 수\n999 = 무제한 의미")]
    [SerializeField, Min(0)] private int _targetCount;

    [Space(5)]
    [Header("====투사체====")]
    [Tooltip("스킬 발동 시 사용할 투사체 프리팹.\n근접 유닛이 30003 같은 추가 투사체 스킬을 가질 때 등 자체 프리팹이 필요한 경우 등록.\n비워두면 유닛 본래 투사체 프리팹 사용.\n시트엔 없는 정보라 인스펙터에서 직접 설정. SO Generator가 덮어쓰지 않음.")]
    [SerializeField] private GameObject _extraProjectilePrefab;


    public int Id => _id;
    public SkillDamageBaseType DamageBase => _damageBase;
    public float CoolDown => _coolDown;
    public float DamageFormula => _damageFormula;
    public float DebuffValue => _debuffValue;
    public float DebuffDuration => _debuffDuration;
    public float BuffValue => _buffValue;
    public float BuffDuration => _buffDuration;
    public StatType BuffStatType => _buffStatType;
    public int TargetCount => _targetCount;
    public GameObject ExtraProjectilePrefab => _extraProjectilePrefab;

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
