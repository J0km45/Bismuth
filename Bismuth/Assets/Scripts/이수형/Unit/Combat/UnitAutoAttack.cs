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
    [SerializeField] private bool attackLog = false;
    

    private TowerUnit towerUnit;
    private MonsterController currentTarget;
    private MonsterController lockedTarget;

    private float attackInterval = 1f;
    private float nextAttackReadyTime = 0f;

    private bool isAttacking = false;
    private bool hasEnteredAttackState = false;
    private bool hasAppliedHit = false;

    private bool isRanged = false;

    private void Awake()
    {
        if (unitStat == null)
            unitStat = GetComponent<UnitStat>();

        if (anim == null)
            anim = GetComponent<AnimationController>();

        towerUnit = GetComponent<TowerUnit>();
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

        if (currentTarget == null)
            return;

        if (Time.time < nextAttackReadyTime)
            return;

        StartAttack(currentTarget);
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

        nextAttackReadyTime = Time.time + attackInterval;

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

    private void StartAttack(MonsterController target)
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

        lockedTarget = target;
        isAttacking = true;
        hasEnteredAttackState = false;
        hasAppliedHit = false;

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
                $"공격 시작 | target={lockedTarget.name}, interval={attackInterval:F3}, animSpeed={playback.AnimatorSpeed:F2}, animDuration={playback.ActualDuration:F3}, hitNormalized={hitNormalizedTime:F2}",
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

        bool success = CombatManager.Instance.DamageOccured(this.gameObject, lockedTarget, attackSensor);

        if (attackLog)
        {
            DebugTool.Log(
                $"히트 판정 | target={lockedTarget.name}, normalized={normalizedTime:F2}, success={success}",
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
                $"공격 종료 | lockedTarget={(lockedTarget != null ? lockedTarget.name : "None")}",
                DebugType.Unit,
                this
            );
        }

        isAttacking = false;
        hasEnteredAttackState = false;
        hasAppliedHit = false;
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
        lockedTarget = null;

        if (anim != null)
            anim.ResetAnimatorSpeed();
    }
}