using UnityEngine;

[DisallowMultipleComponent]
public class UnitAutoAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitStat unitStat;
    [SerializeField] private UnitAttackSensor attackSensor;

    [Header("Debug")]
    [SerializeField] private bool attackLog = true;

    private MonsterController currentTarget;
    private float attackInterval = 1f;
    private float attackTimer = 0f;

    private void Awake()
    {
        if (unitStat == null)
            unitStat = GetComponent<UnitStat>();

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

        attackSensor.PruneInvalidTargets();

        UpdateTarget();

        if (currentTarget == null)
            return;

        attackTimer += Time.deltaTime;

        if (attackTimer < attackInterval)
            return;

        attackTimer -= attackInterval;
        DoAttack(currentTarget);
        DoDebugAttack(currentTarget);
    }

    public void RefreshFromCurrentStat()
    {
        if (unitStat == null)
            unitStat = GetComponent<UnitStat>();

        EnsureSensor();

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

        float attackSpeedPerSecond = Mathf.Max(0.01f, unitStat.AttackSpeed);
        attackInterval = 1f / attackSpeedPerSecond;

        attackSensor.SyncRadiusFromUnitStat();

        currentTarget = null;
        attackTimer = 0f;

        if (attackLog)
        {
            DebugTool.Log(
                $"자동공격 초기화 완료 | StatRange={unitStat.Range},SensorRadius={attackSensor.SensorCollider.radius}, AttackSpeed={unitStat.AttackSpeed}, Interval={attackInterval:F2}s, Type={unitStat.attackTypes}",
                DebugType.Unit,
                this
            );
        }
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
        attackTimer = 0f;

        if (currentTarget != null && attackLog)
        {
            DebugTool.Log(
                $"타겟 획득 - {currentTarget.name}",
                DebugType.Unit,
                this
            );
        }
    }

    private void DoAttack(MonsterController target)
    {
        target.TakeDamage(unitStat.AttackPower);
    }


    private void DoDebugAttack(MonsterController target)
    {
        if (!attackLog || target == null)
            return;

        DebugTool.Log(
            $"공격 판정 - {name} -> {target.name} | Power={unitStat.AttackPower} | HP={target.CurrentHp} / {target.MaxHp} ",
            DebugType.Unit,
            this
        );
        
    }
}