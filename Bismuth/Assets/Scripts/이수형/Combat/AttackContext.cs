using UnityEngine;

public struct AttackContext
{

    public bool IsNormalAttack;


    public bool IsWarriorBonus;


    public bool IsWizardBonus;


    public bool IsArcherBonus;


    public bool IsFurryBonus;


    /// <summary>
    /// 30008 같은 "버프 활성 동안 매 공격마다 1회 추가 시전" 스킬의 추가 공격 컨텍스트인지.
    /// 수인 추가타와 동일하게 PendingExtraAttack 큐를 거쳐서 발사되며, CountsForSynergyStacks 에서 제외.
    /// </summary>
    public bool IsSkillExtraAttackBonus;


    public bool IsActiveSkill;


    public float AnimSpeedMultiplier;


    /// <summary>
    /// 일반 공격력 대비 데미지 배수.
    /// 액티브 스킬이 데미지를 곱할 때 사용 (예: 30002 → 10f, 30001 → 1.5f).
    /// struct default 가 0 이라 EffectiveDamageMultiplier 로 안전하게 조회.
    /// </summary>
    public float DamageMultiplier;


    /// <summary>
    /// 액티브 스킬이 유닛의 AttackTargetCount 를 무시하고 강제로 사용하는 타겟 수.
    /// 0 이면 무시(유닛 본래 AttackTargetCount 사용).
    /// 예: 30001 → 999 (광역).
    /// </summary>
    public int SkillTargetCountOverride;


    /// <summary>
    /// 슬로우 디버프 강도 (%, 0~100). 0 이면 디버프 없음.
    /// </summary>
    public float DebuffSlowPercent;


    /// <summary>
    /// 슬로우 디버프 지속 시간(초). 0 이하이면 디버프 없음.
    /// </summary>
    public float DebuffDuration;


    /// <summary>
    /// MonsterMover 의 source 기반 슬로우 시스템에 넘길 키.
    /// 같은 스킬은 같은 키 → 재시전 시 갱신, 다른 슬로우(정령/타 스킬) 와는 max(percent) 로 공존.
    /// </summary>
    public object DebuffSourceKey;


    /// <summary>
    /// 데미지 베이스. 기본은 공격력. MaxHp 면 적 최대 체력 비례.
    /// </summary>
    public SkillDamageBaseType DamageBase;


    /// <summary>
    /// MaxHp 베이스일 때 사용하는 비율 (% 단위, 시트 DamageFormula 그대로).
    /// 예: 30007 → 15 (= 적 최대체력의 15%).
    /// </summary>
    public float MaxHpRatio;


    /// <summary>
    /// true 면 hitscan/projectile 결정 분기를 건너뛰고 무조건 투사체로 발사.
    /// 30003 추가 투사체 케이스: 근접 유닛이라도 추가 투사체는 투사체로 나감.
    /// </summary>
    public bool ForceProjectile;


    /// <summary>
    /// 투사체 크기 배율 (0/음수면 1f). 30003 추가 투사체는 약간 작게 (예: 0.7f).
    /// </summary>
    public float ProjectileScaleMultiplier;


    /// <summary>
    /// 사용할 투사체 프리팹 오버라이드. null 이면 유닛 본래 GetProjectilePrefab 사용.
    /// 근접 유닛(투사체 프리팹 없음)이 30003 같은 추가 투사체 스킬을 가질 때 SO 의 _extraProjectilePrefab 을 여기에 채워 넘긴다.
    /// </summary>
    public UnityEngine.GameObject ProjectilePrefabOverride;


    /// <summary>
    /// 액티브 스킬 피격 시 적에게 1회 스폰되는 이펙트 프리팹 (대상 추적).
    /// SkillDataSO.HitEffectPrefab 을 BuildSkillContext 에서 채워 넘긴다.
    /// 예: 30001 / 30007. 비어있으면 미스폰.
    /// 광역 디버프(30006)는 ApplyDamageToTarget 을 거치지 않으므로 별도 경로에서 처리.
    /// </summary>
    public UnityEngine.GameObject SkillHitEffectPrefab;


    /// <summary>
    /// 0/음수면 1f 로 보정한 투사체 크기 배율.
    /// </summary>
    public float EffectiveProjectileScale => ProjectileScaleMultiplier > 0f ? ProjectileScaleMultiplier : 1f;


    public static AttackContext Normal()
    {
        return new AttackContext
        {
            IsNormalAttack = true,
            AnimSpeedMultiplier = 1f,
            DamageMultiplier = 1f
        };
    }

    /// <summary>
    /// 액티브 스킬 발동용 컨텍스트.
    /// damageMultiplier 는 시트 DamageFormula 를 1f 기준으로 변환한 값(예: 1000% → 10f).
    /// </summary>
    public static AttackContext Skill(float damageMultiplier)
    {
        return new AttackContext
        {
            IsNormalAttack = false,
            IsActiveSkill = true,
            AnimSpeedMultiplier = 1f,
            DamageMultiplier = damageMultiplier > 0f ? damageMultiplier : 1f
        };
    }


    public bool CountsForSynergyStacks => IsNormalAttack && !IsWarriorBonus && !IsFurryBonus && !IsSkillExtraAttackBonus;

    /// <summary>
    /// 0 또는 음수일 때는 1f 로 보정한 안전 배수.
    /// (struct default / 옛 호출처에서 채우지 않고 넘긴 경우를 보호)
    /// </summary>
    public float EffectiveDamageMultiplier => DamageMultiplier > 0f ? DamageMultiplier : 1f;

    public override string ToString()
    {
        return $"normal={IsNormalAttack}, warrior={IsWarriorBonus}, wizard={IsWizardBonus}, archer={IsArcherBonus}, furry={IsFurryBonus}, skillExtra={IsSkillExtraAttackBonus}, skill={IsActiveSkill}, dmgBase={DamageBase}, dmgMul={EffectiveDamageMultiplier:F2}, maxHpRatio={MaxHpRatio:F0}%, targetOverride={SkillTargetCountOverride}, slow={DebuffSlowPercent:F0}%/{DebuffDuration:F1}s, animSpeed={AnimSpeedMultiplier:F1}";
    }
}




public enum ExtraAttackType
{
    Warrior,
    Furry,
    // 30008 같은 "버프 활성 동안 매 공격마다 추가 공격 1회" 스킬의 추가 공격.
    // 수인 추가타와 동일 큐/처리 흐름을 사용하되 컨텍스트 플래그가 다름 (IsSkillExtraAttackBonus).
    Skill
}




public struct PendingExtraAttack
{

    public ExtraAttackType Type;


    public int RemainingCount;


    public MonsterController ForcedTarget;


    public float AnimSpeedMultiplier;


    public bool HasRemaining => RemainingCount > 0;

    public override string ToString()
    {
        string targetName = ForcedTarget != null ? ForcedTarget.name : "Sensor";
        return $"type={Type}, remaining={RemainingCount}, target={targetName}, animSpeed={AnimSpeedMultiplier:F1}";
    }
}
