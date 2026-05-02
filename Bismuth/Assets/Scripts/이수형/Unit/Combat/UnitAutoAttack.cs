using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitAutoAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitStat unitStat;
    [SerializeField] private UnitAttackSensor attackSensor;
    [SerializeField] private AnimationController anim;
    [SerializeField] private UnitStatHub statHub;

    [Header("Attack Sync")]
    [SerializeField, Min(0)] private int attackAnimationIndex = 0;
    [SerializeField, Range(0.05f, 0.95f)] private float hitNormalizedTime = 0.5f;
    [SerializeField] private bool forceHitOnEarlyExit = true;

    [Header("Debug")]
    [SerializeField] private bool attackLog = true;

    [Header("Flip")]
    [SerializeField] private bool enableFlip = true;

    [Header("Skill")]
    [SerializeField] private SkillCast skillCast;
    [SerializeField] private UnitSkillRunner skillRunner;

    private const int WizardSynergyId = (int)SynergyManager.SynergyType.Magician;
    private const int ArcherSynergyId = (int)SynergyManager.SynergyType.Archer;
    private const int FurrySynergyId = (int)SynergyManager.SynergyType.Furry;
    private const int ArcherRequiredAttackCount = 5;
    private const int FurryRequiredAttackCount = 3;
    private const float WizardBonusCooldownSeconds = 5f;
    private const float FurryTriggerAnimSpeedBoost = 1.3f;

    private TowerUnit towerUnit;
    private MonsterController currentTarget;
    private MonsterController lockedTarget;

    private float attackInterval = 1f;
    private float nextAttackReadyTime = 0f;

    private bool isAttacking = false;
    private bool hasEnteredAttackState = false;
    private bool hasAppliedHit = false;

    private bool isRanged = false;

    private AttackContext currentAttackContext;

    private Queue<PendingExtraAttack> pendingExtraAttacks = new Queue<PendingExtraAttack>();
    private int archerAttackCount = 0;
    private int furryNormalAttackCount = 0;
    private float nextWizardBonusReadyTime = 0f;

    private void Awake()
    {
        if (unitStat == null)
            unitStat = GetComponent<UnitStat>();

        if (statHub == null)
            statHub = GetComponent<UnitStatHub>();

        if (anim == null)
            anim = GetComponent<AnimationController>();

        towerUnit = GetComponent<TowerUnit>();

        skillCast = GetComponent<SkillCast>();
        if (skillCast == null)
            skillCast = gameObject.AddComponent<SkillCast>();

        // SkillRunner 는 스킬 보유 유닛에만 붙는다. 없으면 null 유지(스킬 없는 유닛).
        if (skillRunner == null)
            skillRunner = GetComponent<UnitSkillRunner>();

        EnsureSensor();

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

        if (TryStartPendingExtraAttack())
            return;

        if (currentTarget == null)
            return;

        if (Time.time < nextAttackReadyTime)
            return;

        // 보유 스킬이 액티브 + 쿨다운 충족이면 일반공격 대신 스킬 발동
        if (skillRunner != null && skillRunner.ShouldCastInsteadOfNormalAttack())
        {
            AttackContext skillContext = skillRunner.BuildSkillContext();
            StartAttack(currentTarget, skillContext);
            return;
        }

        StartAttack(currentTarget, AttackContext.Normal());
    }

    public void RefreshFromCurrentStat()
    {
        if (unitStat == null)
            unitStat = GetComponent<UnitStat>();

        if (statHub == null)
            statHub = GetComponent<UnitStatHub>();

        if (anim == null)
            anim = GetComponent<AnimationController>();

        EnsureSensor();
        attackAnimationIndex = 0;

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
        currentAttackContext = default;
        pendingExtraAttacks.Clear();
        archerAttackCount = 0;
        furryNormalAttackCount = 0;

        nextAttackReadyTime = Time.time + attackInterval;
        nextWizardBonusReadyTime = Time.time + WizardBonusCooldownSeconds;


        Vector3 resetScale = transform.localScale;
        resetScale.x = Mathf.Abs(resetScale.x);
        transform.localScale = resetScale;

        if (anim != null)
            anim.ResetAnimatorSpeed();

        if (attackLog)
        {
            DebugTool.Log(
                $"자동공격 초기화 완료 | Range={statHub.Get(StatType.Range):F2}, AttackSpeed={statHub.Get(StatType.AttackSpeed):F2}, Interval={attackInterval:F2}s, HitNormalized={hitNormalizedTime:F2}, Type={unitStat.attackTypes}",
                DebugType.Unit,
                this
            );
        }
    }

    private float CalculateAttackInterval()
    {
        // AttackSpeed 는 Hub 가 단일 소스. 거너 시너지 효과는
        // SynergyStatBinder → GunnerSynergyApplier 를 통해 이미 Hub 모디파이어로 누적됨.
        float attackSpeedPerSecond = statHub != null
            ? statHub.Get(StatType.AttackSpeed)
            : unitStat.AttackSpeed;

        attackSpeedPerSecond = Mathf.Max(0.01f, attackSpeedPerSecond);
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

        FlipTowardsTarget();
    }

    private void FlipTowardsTarget()
    {

        if (!enableFlip)
            return;

        if (currentTarget == null)
            return;

        float directionX = currentTarget.transform.position.x - transform.position.x;

        if (Mathf.Approximately(directionX, 0f))
            return;

        DebugTool.Log(
            $"FlipTowardsTarget 진행 | directionX={directionX:F2}",
            DebugType.Unit,
            this
        );

        Vector3 scale = transform.localScale;
        float desiredSignX = directionX > 0f ? -1f : 1f;

        if (Mathf.Sign(scale.x) != desiredSignX)
        {
            scale.x = Mathf.Abs(scale.x) * desiredSignX;
            transform.localScale = scale;

            if (attackLog)
            {
                DebugTool.Log(
                    $"좌우반전 | direction={(desiredSignX > 0f ? "오른쪽" : "왼쪽")}, target={currentTarget.name}",
                    DebugType.Unit,
                    this
                );
            }
        }
    }

    private void StartAttack(MonsterController target, AttackContext context)
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


        context.IsWizardBonus = CanUseWizardBonusThisAttack();
        context.IsArcherBonus = CanUseArcherBonusThisAttack();


        if (context.IsNormalAttack && WillTriggerFurryExtraAttack())
            context.AnimSpeedMultiplier = Mathf.Max(context.AnimSpeedMultiplier, FurryTriggerAnimSpeedBoost);


        if (context.AnimSpeedMultiplier <= 0f)
            context.AnimSpeedMultiplier = 1f;

        currentAttackContext = context;

        nextAttackReadyTime = Time.time + attackInterval;


        float effectiveAttackSpeed = unitStat.AttackSpeed * context.AnimSpeedMultiplier;


        bool isSkillAttack = context.IsWarriorBonus || context.IsFurryBonus || context.IsWizardBonus || context.IsActiveSkill;
        int animIndex = isSkillAttack ? 1 : attackAnimationIndex;

        AttackPlaybackData playback = anim.PlayAttackAnimation(animIndex, effectiveAttackSpeed);
        FlipTowardsTarget();

        if (!playback.Success)
        {
            CancelAttack("공격 애니메이션 재생에 실패했습니다.");
            return;
        }

        if (attackLog)
        {
            DebugTool.Log(
                $"공격 시작 | target={lockedTarget.name}, animIndex={animIndex}({(isSkillAttack ? "스킬" : "일반")}), interval={attackInterval:F3}, animSpeed={playback.AnimatorSpeed:F2}, animDuration={playback.ActualDuration:F3}, hitNormalized={hitNormalizedTime:F2}, context=[{currentAttackContext}]",
                DebugType.Unit,
                this
            );
        }

        // 액티브 스킬 발동 → 쿨다운 시작 (히트 성공/실패 무관, 발동 자체가 일어나면 카운트)
        if (context.IsActiveSkill && skillRunner != null)
            skillRunner.StartCooldown();
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


        if (hasAppliedHit && TryStartPendingExtraAttack())
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

            if (TryStartPendingExtraAttack())
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

        // 비고 룰: "스킬 시전 중 타겟 사망시 다음 타겟에게 재시전, 없으면 쿨타임 초기화"
        // 액티브 스킬이 허망하게 빗나가서 쿨타임만 까먹는 걸 방지.
        if (currentAttackContext.IsActiveSkill && !CanHitLockedTarget())
        {
            if (TryRetargetSkill())
            {
                if (attackLog)
                {
                    DebugTool.Log(
                        $"[Skill] 시전 중 타겟 사망 → 새 타겟으로 재시전 | newTarget={lockedTarget.name}",
                        DebugType.Unit, this);
                }
                // lockedTarget 갱신 완료 → 아래 정상 흐름으로 진행
            }
            else
            {
                if (skillRunner != null)
                    skillRunner.ResetCooldown();

                if (attackLog)
                {
                    DebugTool.Log(
                        $"[Skill] 시전 중 타겟 사망 + 다음 타겟 없음 → 쿨타임 초기화 (허망 발동 방지)",
                        DebugType.Unit, this);
                }
                return;
            }
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
            currentAttackContext
        );

        if (success)
        {
            if (currentAttackContext.IsWizardBonus)
                ConsumeWizardBonus();

            UpdateArcherAttackProgressAfterSuccessfulHit();
            UpdateFurryAttackProgressAfterSuccessfulHit();

            // 4티어 공격 이펙트
            if (unitStat != null && unitStat.Tier >= 4 && towerUnit != null)
            {
                CombatManager.Instance.SpawnAttackEffect(towerUnit, unitStat);
            }
        }

        if (attackLog)
        {
            DebugTool.Log(
                $"히트 판정 | target={lockedTarget.name}, normalized={normalizedTime:F2}, success={success}, context=[{currentAttackContext}]",
                DebugType.Unit,
                this
            );
        }

        // 30003 케이스: 매 공격에 추가 투사체가 따라붙음.
        // 일반공격 hit 가 정상 발동된 경우에만 (락 타겟 무효 분기엔 들어오지 않음) 발사.
        TryFireExtraProjectiles();
    }

    /// <summary>
    /// 30003(추가 투사체 패시브)을 보유한 경우, 일반공격 hit 시점에 추가 투사체를 발사한다.
    /// CombatManager.DamageOccured 를 한 번 더 호출하되 ForceProjectile 컨텍스트로 분기.
    /// </summary>
    private void TryFireExtraProjectiles()
    {
        if (skillRunner == null || !skillRunner.IsExtraProjectileSkill)
            return;

        if (CombatManager.Instance == null)
            return;

        AttackContext extraCtx = skillRunner.BuildExtraProjectileContext();
        CombatManager.Instance.DamageOccured(this.gameObject, lockedTarget, attackSensor, extraCtx);

        if (attackLog)
        {
            DebugTool.Log(
                $"[Skill 추가 투사체] 발사 | skillId={skillRunner.Skill.Id}, target={lockedTarget.name}, context=[{extraCtx}]",
                DebugType.Unit, this);
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

    /// <summary>
    /// 액티브 스킬 시전 중 락 타겟이 사망했을 때 attackSensor 에서 새 타겟을 찾아 락을 갱신.
    /// 새 타겟이 있으면 lockedTarget/currentTarget 모두 갱신하고 true.
    /// </summary>
    private bool TryRetargetSkill()
    {
        if (attackSensor == null)
            return false;

        MonsterController newTarget = attackSensor.GetFirstTarget();

        if (newTarget == null
            || !newTarget.gameObject.activeInHierarchy
            || newTarget.CurrentHp <= 0f)
        {
            return false;
        }

        lockedTarget = newTarget;
        currentTarget = newTarget;
        return true;
    }

    private void FinishAttack()
    {
        if (attackLog)
        {
            DebugTool.Log(
                $"공격 종료 | lockedTarget={(lockedTarget != null ? lockedTarget.name : "None")}, context=[{currentAttackContext}]",
                DebugType.Unit,
                this
            );
        }

        isAttacking = false;
        hasEnteredAttackState = false;
        hasAppliedHit = false;
        currentAttackContext = default;
        lockedTarget = null;


        if (anim != null)
            anim.PlayIdleAnimation();
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
        currentAttackContext = default;
        lockedTarget = null;

        if (anim != null)
            anim.PlayIdleAnimation();
    }
    public void RequestWarriorExtraAttack()
    {
        EnqueueExtraAttack(new PendingExtraAttack
        {
            Type = ExtraAttackType.Warrior,
            RemainingCount = 1,
            ForcedTarget = null,
            AnimSpeedMultiplier = 1f
        });
    }




    public void EnqueueExtraAttack(PendingExtraAttack entry)
    {
        pendingExtraAttacks.Enqueue(entry);
        nextAttackReadyTime = 0f;

        if (attackLog)
        {
            DebugTool.Log(
                $"추가 공격 등록 | [{entry}], 큐 크기={pendingExtraAttacks.Count}",
                DebugType.Synergy,
                this
            );
        }
    }

    private bool TryStartPendingExtraAttack()
    {
        if (pendingExtraAttacks.Count == 0)
            return false;


        PendingExtraAttack front = pendingExtraAttacks.Peek();


        MonsterController target = ResolveExtraAttackTarget(front);

        if (target == null)
        {

            pendingExtraAttacks.Dequeue();

            if (attackLog)
            {
                DebugTool.Log(
                    $"추가 공격 취소 | type={front.Type}, 유효 타겟 없음, 큐 남은 크기={pendingExtraAttacks.Count}",
                    DebugType.Synergy,
                    this
                );
            }

            return false;
        }


        front.RemainingCount--;

        if (front.HasRemaining)
        {

            pendingExtraAttacks.Dequeue();


            PendingExtraAttack[] remaining = pendingExtraAttacks.ToArray();
            pendingExtraAttacks.Clear();
            pendingExtraAttacks.Enqueue(front);
            for (int i = 0; i < remaining.Length; i++)
                pendingExtraAttacks.Enqueue(remaining[i]);
        }
        else
        {

            pendingExtraAttacks.Dequeue();
        }

        if (attackLog)
        {
            DebugTool.Log(
                $"추가 공격 시작 | type={front.Type}, target={target.name}, 배치 남은={front.RemainingCount}, 큐 크기={pendingExtraAttacks.Count}",
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


        AttackContext context = BuildExtraAttackContext(front.Type, front.AnimSpeedMultiplier);
        StartAttack(target, context);
        return true;
    }


    private MonsterController ResolveExtraAttackTarget(PendingExtraAttack entry)
    {
        if (entry.ForcedTarget != null)
        {

            if (entry.ForcedTarget.gameObject.activeInHierarchy
                && entry.ForcedTarget.CurrentHp > 0f)
            {
                return entry.ForcedTarget;
            }

            return null;
        }


        UpdateTarget();
        return currentTarget;
    }




    private AttackContext BuildExtraAttackContext(ExtraAttackType type, float animSpeedMultiplier = 1f)
    {
        switch (type)
        {
            case ExtraAttackType.Warrior:
                return new AttackContext { IsWarriorBonus = true, AnimSpeedMultiplier = animSpeedMultiplier };

            case ExtraAttackType.Furry:
                return new AttackContext { IsFurryBonus = true, AnimSpeedMultiplier = animSpeedMultiplier };

            default:
                return new AttackContext { AnimSpeedMultiplier = animSpeedMultiplier };
        }
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

    private bool CanUseArcherBonusThisAttack()
    {
        if (unitStat == null)
            return false;

        if (!HasSynergyTag(ArcherSynergyId))
            return false;

        if (archerAttackCount < ArcherRequiredAttackCount)
            return false;

        float bonusPercent = GetArcherDamageBonusPercent();
        return bonusPercent > 0f;
    }

    private void UpdateArcherAttackProgressAfterSuccessfulHit()
    {
        if (unitStat == null)
            return;

        if (!HasSynergyTag(ArcherSynergyId))
            return;

        float bonusPercent = GetArcherDamageBonusPercent();
        if (bonusPercent <= 0f)
            return;

        if (currentAttackContext.IsArcherBonus)
        {
            ConsumeArcherBonus(bonusPercent);
            return;
        }

        archerAttackCount = Mathf.Min(ArcherRequiredAttackCount, archerAttackCount + 1);

        if (attackLog)
        {
            DebugTool.Log(
                $"궁수 카운트 적립 | unit={unitStat.Name}, count={archerAttackCount}/{ArcherRequiredAttackCount}, bonusPercent={bonusPercent:F2}",
                DebugType.Synergy,
                this
            );
        }
    }

    private void ConsumeArcherBonus(float bonusPercent)
    {
        if (attackLog)
        {
            DebugTool.Log(
                $"궁수 강화 공격 발동 | unit={unitStat.Name}, bonusPercent={bonusPercent:F2}",
                DebugType.Synergy,
                this
            );
        }

        archerAttackCount = 0;
    }

    private float GetArcherDamageBonusPercent()
    {
        if (unitStat == null)
            return 0f;

        if (!HasSynergyTag(ArcherSynergyId))
            return 0f;

        if (CombatManager.Instance == null)
            return 0f;

        return CombatManager.Instance.GetSynergyEffectValue(ArcherSynergyId);
    }


    private bool WillTriggerFurryExtraAttack()
    {
        if (unitStat == null)
            return false;

        if (!HasSynergyTag(FurrySynergyId))
            return false;

        if (furryNormalAttackCount < FurryRequiredAttackCount - 1)
            return false;

        int extraCount = GetFurryExtraAttackCount();
        return extraCount > 0;
    }


    private void UpdateFurryAttackProgressAfterSuccessfulHit()
    {
        if (unitStat == null)
            return;

        if (!HasSynergyTag(FurrySynergyId))
            return;

        // 수인 추가타·전사 추가타 등 스킬 공격은 카운터를 올리지 않는다
        if (!currentAttackContext.CountsForSynergyStacks)
            return;

        int extraCount = GetFurryExtraAttackCount();
        if (extraCount <= 0)
            return;

        furryNormalAttackCount++;

        if (attackLog)
        {
            DebugTool.Log(
                $"수인 카운트 적립 | unit={unitStat.Name}, count={furryNormalAttackCount}/{FurryRequiredAttackCount}",
                DebugType.Synergy,
                this
            );
        }

        if (furryNormalAttackCount >= FurryRequiredAttackCount)
        {
            furryNormalAttackCount = 0;

            EnqueueExtraAttack(new PendingExtraAttack
            {
                Type = ExtraAttackType.Furry,
                RemainingCount = extraCount,
                ForcedTarget = lockedTarget,
                AnimSpeedMultiplier = Mathf.Max(1f, extraCount)
            });

            if (attackLog)
            {
                DebugTool.Log(
                    $"수인 추가타 발동 | unit={unitStat.Name}, extraCount={extraCount}, target={(lockedTarget != null ? lockedTarget.name : "None")}",
                    DebugType.Synergy,
                    this
                );
            }
        }
    }



    private int GetFurryExtraAttackCount()
    {
        if (unitStat == null)
            return 0;

        if (!HasSynergyTag(FurrySynergyId))
            return 0;

        if (CombatManager.Instance == null)
            return 0;

        return (int)CombatManager.Instance.GetSynergyEffectValue(FurrySynergyId);
    }

    private float GetWizardDamageBonusPercent()
    {
        if (unitStat == null)
            return 0f;

        if (!HasSynergyTag(WizardSynergyId))
            return 0f;

        if (CombatManager.Instance == null)
            return 0f;

        float bonusPercent = CombatManager.Instance.GetSynergyEffectValue(WizardSynergyId);

        if (attackLog && bonusPercent > 0f)
        {
            DebugTool.Log(
                $"마법사 추가 대미지 준비 가능 | unit={unitStat.Name}, active={CombatManager.Instance.GetSynergyLevel(WizardSynergyId)}, bonusPercent={bonusPercent:F2}",
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


}