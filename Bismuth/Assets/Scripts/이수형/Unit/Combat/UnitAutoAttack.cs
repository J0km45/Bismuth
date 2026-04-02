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
    private const int ArcherSynergyId = (int)SynergyManager.SynergyType.Archer;
    private const int FurrySynergyId = (int)SynergyManager.SynergyType.Furry;
    private const int ArcherRequiredAttackCount = 5;
    private const int FurryRequiredAttackCount = 3;
    private const float WizardBonusCooldownSeconds = 5f;
    private const float FurryTriggerAnimSpeedBoost = 1.3f;

    [Header("Synergy")]
    [SerializeField] private SynergyDataController synergyDataController;

    private bool warnedMissingGunnerSynergyDataController = false;
    private bool warnedMissingWizardSynergyDataController = false;
    private bool warnedMissingArcherSynergyDataController = false;


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

        if (TryStartPendingExtraAttack())
            return;

        if (currentTarget == null)
            return;

        if (Time.time < nextAttackReadyTime)
            return;

        StartAttack(currentTarget, AttackContext.Normal());
    }

    public void RefreshFromCurrentStat()
    {
        if (unitStat == null)
            unitStat = GetComponent<UnitStat>();

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

        // 전달받은 컨텍스트에 마법사/궁수 보너스 여부를 추가 설정
        context.IsWizardBonus = CanUseWizardBonusThisAttack();
        context.IsArcherBonus = CanUseArcherBonusThisAttack();

        // 일반공격이 수인 추가타를 유발할 경우 애니메이션 부스트
        if (context.IsNormalAttack && WillTriggerFurryExtraAttack())
            context.AnimSpeedMultiplier = Mathf.Max(context.AnimSpeedMultiplier, FurryTriggerAnimSpeedBoost);

        // AnimSpeedMultiplier가 설정되지 않은 경우 기본값 1
        if (context.AnimSpeedMultiplier <= 0f)
            context.AnimSpeedMultiplier = 1f;

        currentAttackContext = context;

        nextAttackReadyTime = Time.time + attackInterval;

        // 애니메이션 배속 적용: 기본 공속에 컨텍스트 배율을 곱한다
        float effectiveAttackSpeed = unitStat.AttackSpeed * context.AnimSpeedMultiplier;

        // 스킬 공격(전사·수인 추가타·마법사 차징)은 index 1(스킬 모션), 일반 공격은 index 0(기본 모션)
        bool isSkillAttack = context.IsWarriorBonus || context.IsFurryBonus || context.IsWizardBonus;
        int animIndex = isSkillAttack ? 1 : attackAnimationIndex;

        AttackPlaybackData playback = anim.PlayAttackAnimation(animIndex, effectiveAttackSpeed);

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

        // 현재 공격의 히트 판정이 끝난 후에만 다음 추가타를 시작한다.
        // 이 가드가 없으면 히트 전에 매 프레임 큐에서 꺼내서 추가타가 씹힌다.
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
        }

        if (attackLog)
        {
            DebugTool.Log(
                $"히트 판정 | target={lockedTarget.name}, normalized={normalizedTime:F2}, success={success}, context=[{currentAttackContext}]",
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

        // Play()로 ATTACK에 강제 진입했으므로, 명시적으로 IDLE 복귀시킨다.
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


    /// 추가 공격 배치를 큐에 등록한다.
    /// 전사, 수인 등 시너지별로 이 메서드를 통해 추가타를 요청한다.

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

        // 큐 앞쪽을 꺼내서 확인 (아직 Dequeue 하지 않음)
        PendingExtraAttack front = pendingExtraAttacks.Peek();

        // 타겟 결정: ForcedTarget이 유효하면 사용, 아니면 센서 타겟
        MonsterController target = ResolveExtraAttackTarget(front);

        if (target == null)
        {
            // 타겟 없음 → 이 배치를 버린다
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

        // 배치에서 1회 차감
        front.RemainingCount--;

        if (front.HasRemaining)
        {
            // 아직 남은 타수가 있으면 업데이트된 값으로 다시 넣기 (struct이므로 Dequeue 후 Enqueue)
            pendingExtraAttacks.Dequeue();

            // 큐 맨 앞에 다시 넣기 위해 임시 보관
            PendingExtraAttack[] remaining = pendingExtraAttacks.ToArray();
            pendingExtraAttacks.Clear();
            pendingExtraAttacks.Enqueue(front);
            for (int i = 0; i < remaining.Length; i++)
                pendingExtraAttacks.Enqueue(remaining[i]);
        }
        else
        {
            // 이 배치 소진 → 제거
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

        // 현재 진행 중인 공격이 있으면 중단
        if (isAttacking)
        {
            if (anim != null)
                anim.ResetAnimatorSpeed();

            isAttacking = false;
            hasEnteredAttackState = false;
            hasAppliedHit = false;
            lockedTarget = null;
        }

        // 타입에 맞는 AttackContext 생성
        AttackContext context = BuildExtraAttackContext(front.Type, front.AnimSpeedMultiplier);
        StartAttack(target, context);
        return true;
    }

    /// 추가 공격의 타겟을 결정한다.
    /// ForcedTarget이 유효하면 그것을 사용하고, 아니면 센서에서 탐색한다.

    private MonsterController ResolveExtraAttackTarget(PendingExtraAttack entry)
    {
        if (entry.ForcedTarget != null)
        {
            // 강제 타겟이 지정된 경우 (수인 등): 유효하면 사용, 죽었으면 null (센서 폴백 없음)
            if (entry.ForcedTarget.gameObject.activeInHierarchy
                && entry.ForcedTarget.CurrentHp > 0f)
            {
                return entry.ForcedTarget;
            }

            return null;
        }

        // 강제 타겟이 없는 경우 (전사 등) → 센서에서 탐색
        UpdateTarget();
        return currentTarget;
    }


    /// 추가 공격 타입에 맞는 AttackContext를 생성한다.

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

        TryResolveSynergyDataController();

        if (synergyDataController == null)
        {
            WarnMissingArcherSynergyDataController();
            return 0f;
        }

        SynergyData archerData = synergyDataController.GetById(ArcherSynergyId);
        if (archerData == null || archerData.Levels == null || archerData.Levels.Count == 0)
            return 0f;

        int activeCount = CombatManager.Instance.GetSynergyLevel(ArcherSynergyId);
        float bonusPercent = 0f;

        for (int i = 0; i < archerData.Levels.Count; i++)
        {
            SynergyLevelData level = archerData.Levels[i];
            if (level == null)
                continue;

            if (activeCount < level.ActiveCount)
                continue;

            if (level.EffectValues == null || level.EffectValues.Count == 0)
                continue;

            bonusPercent = level.EffectValues[0];
        }

        return bonusPercent;
    }

    private void WarnMissingArcherSynergyDataController()
    {
        if (warnedMissingArcherSynergyDataController)
            return;

        warnedMissingArcherSynergyDataController = true;
        DebugTool.Warnning("SynergyDataController 참조가 없어 궁수 최대 체력 비례 시너지를 적용하지 않습니다.", DebugType.Synergy, this);
    }

    // ─────────────────────────────── 수인 시너지 ───────────────────────────────

    /// <summary>
    /// 이번 일반공격이 수인 추가타를 유발하는 3번째 타격인지 사전 판별한다.
    /// StartAttack에서 애니메이션 부스트 여부를 결정하는 데 사용한다.
    /// </summary>
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

    /// <summary>
    /// 히트 성공 후 수인 카운터를 갱신하고, 조건 충족 시 추가타를 큐에 등록한다.
    /// </summary>
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


    /// 현재 시너지 레벨에 따른 수인 추가타 횟수를 반환한다. (0이면 미활성)

    private int GetFurryExtraAttackCount()
    {
        if (unitStat == null)
            return 0;

        if (!HasSynergyTag(FurrySynergyId))
            return 0;

        if (CombatManager.Instance == null)
            return 0;

        TryResolveSynergyDataController();

        if (synergyDataController == null)
            return 0;

        SynergyData furryData = synergyDataController.GetById(FurrySynergyId);
        if (furryData == null || furryData.Levels == null || furryData.Levels.Count == 0)
            return 0;

        int activeCount = CombatManager.Instance.GetSynergyLevel(FurrySynergyId);
        int extraAttacks = 0;

        for (int i = 0; i < furryData.Levels.Count; i++)
        {
            SynergyLevelData level = furryData.Levels[i];
            if (level == null)
                continue;

            if (activeCount < level.ActiveCount)
                continue;

            if (level.EffectValues == null || level.EffectValues.Count == 0)
                continue;

            extraAttacks = (int)level.EffectValues[0];
        }

        return extraAttacks;
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