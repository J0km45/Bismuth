using Unity.VisualScripting;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitAutoAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitStat unitStat;
    [SerializeField] private UnitAttackSensor attackSensor;
    [SerializeField] private AnimationController anim;

    [Header("Attack Sync")]
    [SerializeField, Min(0)] private int attackAnimationIndex = 0;
    [SerializeField, Range(0.05f, 0.95f)] private float hitNormalizedTime = 0.5f;
    [SerializeField] private bool forceHitOnEarlyExit = true;

    [Header("Debug")]
    [SerializeField] private bool attackLog = true;

    [Header("Skill")]
    [SerializeField] private SkillCast skillCast;

    private const int GunnerSynergyId = (int)SynergyManager.SynergyType.Gunner;
    private const int WizardSynergyId = (int)SynergyManager.SynergyType.Magician;
    private const float WizardBonusCooldownSeconds = 5f;

    [Header("Synergy")]
    [SerializeField] private SynergyDataController synergyDataController;

    private bool warnedMissingGunnerSynergyDataController = false;
    private bool warnedMissingWizardSynergyDataController = false;


    private TowerUnit towerUnit;
    private MonsterController currentTarget;
    private MonsterController lockedTarget;

    private float attackInterval = 1f;
    private float nextAttackReadyTime = 0f;

    private bool isAttacking = false;
    private bool hasEnteredAttackState = false;
    private bool hasAppliedHit = false;

    private bool isRanged = false;

    private bool currentAttackIsWarriorBonus = false;
    private bool currentAttackIsWizardBonus = false;
    private int pendingWarriorExtraAttackCount = 0;
    private float nextWizardBonusReadyTime = 0f;

    private void Awake()
    {
        if (unitStat == null)
            unitStat = GetComponent<UnitStat>();

        if (anim == null)
            anim = GetComponent<AnimationController>();

        towerUnit = GetComponent<TowerUnit>();

        skillCast = GetComponent<SkillCast>();
        if (skillCast == null)
            skillCast = gameObject.AddComponent<SkillCast>();

        EnsureSensor();
        //attackAnimationIndex = unitStat.Range > 1f ? 2 : 0;
    }

    private void Start()
    {
        RefreshFromCurrentStat();
    }

    private void Update()
    {
        if (unitStat == null || attackSensor == null)
            return;

        if (anim == null)
            anim = GetComponent<AnimationController>();

        attackSensor.PruneInvalidTargets();
        UpdateTarget();

        attackInterval = CalculateAttackInterval();

        if (isAttacking)
        {
            UpdateAttackProgress();
            return;
        }

        if (TryStartPendingWarriorExtraAttack())
            return;

        if (currentTarget == null)
            return;

        if (Time.time < nextAttackReadyTime)
            return;

        StartAttack(currentTarget, false);
    }

    public void RefreshFromCurrentStat()
    {
        if (unitStat == null)
            unitStat = GetComponent<UnitStat>();

        if (anim == null)
            anim = GetComponent<AnimationController>();

        EnsureSensor();
        attackAnimationIndex = unitStat != null && unitStat.Range > 1f ? 2 : 0;

        if (unitStat == null)
        {
            DebugTool.Warnning("UnitStat 참조가 없습니다.", DebugType.Unit, this);
            return;
        }

        if (attackSensor == null)
        {
            DebugTool.Warnning("UnitAttackSensor 생성/참조에 실패했습니다.", DebugType.Unit, this);
            return;
        }

        attackInterval = CalculateAttackInterval();
        attackSensor.SyncRadiusFromUnitStat();

        currentTarget = null;
        lockedTarget = null;

        isAttacking = false;
        hasEnteredAttackState = false;
        hasAppliedHit = false;
        currentAttackIsWarriorBonus = false;
        currentAttackIsWizardBonus = false;
        pendingWarriorExtraAttackCount = 0;

        nextAttackReadyTime = Time.time + attackInterval;
        nextWizardBonusReadyTime = Time.time + WizardBonusCooldownSeconds;

        if (anim != null)
            anim.ResetAnimatorSpeed();

        if (attackLog)
        {
            DebugTool.Log(
                $"자동공격 초기화 완료 | Range={unitStat.Range}, AttackSpeed={unitStat.AttackSpeed}, Interval={attackInterval:F2}s, HitNormalized={hitNormalizedTime:F2}, Type={unitStat.attackTypes}",
                DebugType.Unit,
                this
            );
        }
    }

    private float CalculateAttackInterval()
    {
        float attackSpeedPerSecond = Mathf.Max(0.01f, unitStat.AttackSpeed);

        float gunnerBonusPercent = GetGunnerAttackSpeedBonusPercent();
        attackSpeedPerSecond *= 1f + gunnerBonusPercent * 0.01f;

        return 1f / attackSpeedPerSecond;
    }

    private void EnsureSensor()
    {
        if (attackSensor != null)
            return;

        attackSensor = GetComponentInChildren<UnitAttackSensor>(true);
        if (attackSensor != null)
            return;

        GameObject sensorObject = new GameObject("AttackSensor");
        sensorObject.transform.SetParent(transform, false);
        sensorObject.transform.localPosition = Vector3.zero;
        sensorObject.layer = gameObject.layer;

        CircleCollider2D circle = sensorObject.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;

        attackSensor = sensorObject.AddComponent<UnitAttackSensor>();
    }

    private void UpdateTarget()
    {
        bool currentValid =
            currentTarget != null &&
            currentTarget.gameObject.activeInHierarchy &&
            attackSensor.Contains(currentTarget);

        if (currentValid)
            return;

        if (currentTarget != null && attackLog)
        {
            DebugTool.Log(
                $"타겟 상실 - {currentTarget.name}",
                DebugType.Unit,
                this
            );
        }

        currentTarget = attackSensor.GetFirstTarget();

        if (currentTarget != null && attackLog)
        {
            DebugTool.Log(
                $"타겟 획득 - {currentTarget.name}",
                DebugType.Unit,
                this
            );
        }
    }

    private void StartAttack(MonsterController target, bool isWarriorBonusAttack)
    {
        if (target == null)
            return;

        if (towerUnit == null)
            towerUnit = GetComponent<TowerUnit>();

        if (anim == null)
            anim = GetComponent<AnimationController>();

        if (towerUnit == null)
        {
            DebugTool.Warnning("TowerUnit 참조가 없습니다.", DebugType.Unit, this);
            return;
        }

        if (anim == null)
        {
            DebugTool.Warnning("AnimationController 참조가 없습니다.", DebugType.Unit, this);
            return;
        }

        currentTarget = target;
        lockedTarget = target;
        isAttacking = true;
        hasEnteredAttackState = false;
        hasAppliedHit = false;
        currentAttackIsWarriorBonus = isWarriorBonusAttack;
        currentAttackIsWizardBonus = CanUseWizardBonusThisAttack();

        nextAttackReadyTime = Time.time + attackInterval;

        AttackPlaybackData playback = anim.PlayAttackAnimation(attackAnimationIndex, unitStat.AttackSpeed);

        if (!playback.Success)
        {
            CancelAttack("공격 애니메이션 재생에 실패했습니다.");
            return;
        }

        if (attackLog)
        {
            DebugTool.Log(
                $"공격 시작 | target={lockedTarget.name}, interval={attackInterval:F3}, animSpeed={playback.AnimatorSpeed:F2}, animDuration={playback.ActualDuration:F3}, hitNormalized={hitNormalizedTime:F2}, warriorBonus={currentAttackIsWarriorBonus}, wizardBonus={currentAttackIsWizardBonus}",
                DebugType.Unit,
                this
            );
        }
    }

    private void UpdateAttackProgress()
    {
        if (anim == null)
        {
            CancelAttack("AnimationController 참조가 없습니다.");
            return;
        }

        if (!hasEnteredAttackState)
        {
            if (!anim.IsAttackStatePlaying())
                return;

            hasEnteredAttackState = true;

            if (attackLog)
            {
                DebugTool.Log(
                    $"공격 상태 진입 | target={(lockedTarget != null ? lockedTarget.name : "None")}",
                    DebugType.Unit,
                    this
                );
            }
        }

        float normalizedTime = anim.GetCurrentAttackNormalizedTime();

        if (!hasAppliedHit && normalizedTime >= hitNormalizedTime)
            ApplyLockedHit(normalizedTime);

        if (TryStartPendingWarriorExtraAttack())
            return;

        bool attackStatePlaying = anim.IsAttackStatePlaying();

        if (attackStatePlaying && normalizedTime < 1f)
            return;

        if (!hasAppliedHit && forceHitOnEarlyExit)
        {
            if (attackLog)
            {
                DebugTool.Log(
                    $"공격 상태가 예상보다 빨리 종료되어 히트 보정 적용 | normalized={normalizedTime:F2}",
                    DebugType.Unit,
                    this
                );
            }

            ApplyLockedHit(normalizedTime);

            if (TryStartPendingWarriorExtraAttack())
                return;
        }

        FinishAttack();
    }

    private void ApplyLockedHit(float normalizedTime)
    {
        if (hasAppliedHit)
            return;

        hasAppliedHit = true;

        if (towerUnit == null || CombatManager.Instance == null)
        {
            DebugTool.Warnning("TowerUnit 또는 CombatManager 참조가 없습니다.", DebugType.Unit, this);
            return;
        }

        if (!CanHitLockedTarget())
        {
            if (attackLog)
            {
                DebugTool.Log(
                    $"히트 실패 | 타겟이 이미 무효화됨",
                    DebugType.Unit,
                    this
                );
            }
            return;
        }

        bool success = CombatManager.Instance.DamageOccured(
            this.gameObject,
            lockedTarget,
            attackSensor,
            currentAttackIsWarriorBonus,
            currentAttackIsWizardBonus
        );

        if (success && currentAttackIsWizardBonus)
            ConsumeWizardBonus();

        if (attackLog)
        {
            DebugTool.Log(
                $"히트 판정 | target={lockedTarget.name}, normalized={normalizedTime:F2}, success={success}, warriorBonus={currentAttackIsWarriorBonus}, wizardBonus={currentAttackIsWizardBonus}",
                DebugType.Unit,
                this
            );
        }
    }


    private bool CanHitLockedTarget()
    {
        if (lockedTarget == null)
            return false;

        if (!lockedTarget.gameObject.activeInHierarchy)
            return false;

        if (lockedTarget.CurrentHp <= 0f)
            return false;

        return true;
    }

    private void FinishAttack()
    {
        if (attackLog)
        {
            DebugTool.Log(
                $"공격 종료 | lockedTarget={(lockedTarget != null ? lockedTarget.name : "None")}, warriorBonus={currentAttackIsWarriorBonus}, wizardBonus={currentAttackIsWizardBonus}",
                DebugType.Unit,
                this
            );
        }

        isAttacking = false;
        hasEnteredAttackState = false;
        hasAppliedHit = false;
        currentAttackIsWarriorBonus = false;
        currentAttackIsWizardBonus = false;
        lockedTarget = null;

        if (anim != null)
            anim.ResetAnimatorSpeed();
    }

    private void CancelAttack(string reason)
    {
        if (attackLog)
        {
            DebugTool.Warnning(
                $"공격 취소 | {reason}",
                DebugType.Unit,
                this
            );
        }

        isAttacking = false;
        hasEnteredAttackState = false;
        hasAppliedHit = false;
        currentAttackIsWarriorBonus = false;
        currentAttackIsWizardBonus = false;
        lockedTarget = null;

        if (anim != null)
            anim.ResetAnimatorSpeed();
    }
    public void RequestWarriorExtraAttack()
    {
        pendingWarriorExtraAttackCount++;
        nextAttackReadyTime = 0f;

        if (attackLog)
        {
            DebugTool.Log(
                $"전사 추가 공격 요청 | pending={pendingWarriorExtraAttackCount}",
                DebugType.Synergy,
                this
            );
        }
    }

    private bool TryStartPendingWarriorExtraAttack()
    {
        if (pendingWarriorExtraAttackCount <= 0)
            return false;

        UpdateTarget();

        if (currentTarget == null)
        {
            if (attackLog)
            {
                DebugTool.Log(
                    $"전사 추가 공격 취소 | 사거리 내 다음 타겟이 없어 pending을 비웁니다.",
                    DebugType.Synergy,
                    this
                );
            }

            pendingWarriorExtraAttackCount = 0;
            return false;
        }

        pendingWarriorExtraAttackCount--;

        if (attackLog)
        {
            DebugTool.Log(
                $"전사 추가 공격 시작 | target={currentTarget.name}, 남은 pending={pendingWarriorExtraAttackCount}",
                DebugType.Synergy,
                this
            );
        }

        if (isAttacking)
        {
            if (anim != null)
                anim.ResetAnimatorSpeed();

            isAttacking = false;
            hasEnteredAttackState = false;
            hasAppliedHit = false;
            lockedTarget = null;
        }

        StartAttack(currentTarget, true);
        return true;
    }


    private bool CanUseWizardBonusThisAttack()
    {
        if (unitStat == null)
            return false;

        if (!HasSynergyTag(WizardSynergyId))
            return false;

        if (Time.time < nextWizardBonusReadyTime)
            return false;

        float bonusPercent = GetWizardDamageBonusPercent();
        return bonusPercent > 0f;
    }

    private void ConsumeWizardBonus()
    {
        nextWizardBonusReadyTime = Time.time + WizardBonusCooldownSeconds;

        if (attackLog)
        {
            DebugTool.Log(
                $"마법사 추가 대미지 발동 | nextReadyTime={nextWizardBonusReadyTime:F2}",
                DebugType.Synergy,
                this
            );
        }
    }

    private float GetWizardDamageBonusPercent()
    {
        if (unitStat == null)
            return 0f;

        if (!HasSynergyTag(WizardSynergyId))
            return 0f;

        if (CombatManager.Instance == null)
            return 0f;

        TryResolveSynergyDataController();

        if (synergyDataController == null)
        {
            WarnMissingWizardSynergyDataController();
            return 0f;
        }

        SynergyData wizardData = synergyDataController.GetById(WizardSynergyId);
        if (wizardData == null || wizardData.Levels == null || wizardData.Levels.Count == 0)
            return 0f;

        int activeCount = CombatManager.Instance.GetSynergyLevel(WizardSynergyId);
        float bonusPercent = 0f;

        for (int i = 0; i < wizardData.Levels.Count; i++)
        {
            SynergyLevelData level = wizardData.Levels[i];
            if (level == null)
                continue;

            if (activeCount < level.ActiveCount)
                continue;

            if (level.EffectValues == null || level.EffectValues.Count == 0)
                continue;

            bonusPercent = level.EffectValues[0];
        }

        if (attackLog && bonusPercent > 0f)
        {
            DebugTool.Log(
                $"마법사 추가 대미지 준비 가능 | unit={unitStat.Name}, active={activeCount}, bonusPercent={bonusPercent:F2}",
                DebugType.Synergy,
                this
            );
        }

        return bonusPercent;
    }

    private float GetGunnerAttackSpeedBonusPercent()
    {
        if (unitStat == null)
            return 0f;

        if (!HasSynergyTag(GunnerSynergyId))
            return 0f;

        if (CombatManager.Instance == null)
            return 0f;

        TryResolveSynergyDataController();

        if (synergyDataController == null)
        {
            WarnMissingGunnerSynergyDataController();
            return 0f;
        }

        SynergyData gunnerData = synergyDataController.GetById(GunnerSynergyId);
        if (gunnerData == null || gunnerData.Levels == null || gunnerData.Levels.Count == 0)
            return 0f;

        int activeCount = CombatManager.Instance.GetSynergyLevel(GunnerSynergyId);
        float bonusPercent = 0f;

        for (int i = 0; i < gunnerData.Levels.Count; i++)
        {
            SynergyLevelData level = gunnerData.Levels[i];
            if (level == null)
                continue;

            if (activeCount < level.ActiveCount)
                continue;

            if (level.EffectValues == null || level.EffectValues.Count == 0)
                continue;

            // 거너 시너지는 첫 번째 effect value를 공속 증가량(%)으로 사용
            bonusPercent = level.EffectValues[0];
        }

        if (attackLog && bonusPercent > 0f)
        {
            DebugTool.Log(
                $"거너 공속 보너스 적용 | unit={unitStat.Name}, active={activeCount}, bonusPercent={bonusPercent:F2}",
                DebugType.Synergy,
                this
            );
        }

        return bonusPercent;
    }

    private bool HasSynergyTag(int synergyId)
    {
        if (unitStat == null || unitStat.SynergIDs == null)
            return false;

        for (int i = 0; i < unitStat.SynergIDs.Length; i++)
        {
            if (unitStat.SynergIDs[i] == synergyId)
                return true;
        }

        return false;
    }

    private void TryResolveSynergyDataController()
    {
        if (synergyDataController != null)
            return;

        synergyDataController = FindAnyObjectByType<SynergyDataController>();

    }

    private void WarnMissingGunnerSynergyDataController()
    {
        if (warnedMissingGunnerSynergyDataController)
            return;

        warnedMissingGunnerSynergyDataController = true;
        DebugTool.Warnning("SynergyDataController 참조가 없어 거너 공속 시너지를 적용하지 않습니다.", DebugType.Synergy, this);
    }

    private void WarnMissingWizardSynergyDataController()
    {
        if (warnedMissingWizardSynergyDataController)
            return;

        warnedMissingWizardSynergyDataController = true;
        DebugTool.Warnning("SynergyDataController 참조가 없어 마법사 추가 대미지 시너지를 적용하지 않습니다.", DebugType.Synergy, this);
    }
}