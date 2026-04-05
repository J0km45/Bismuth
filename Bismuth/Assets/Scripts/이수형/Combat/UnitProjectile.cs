using UnityEngine;

[DisallowMultipleComponent]
public class UnitProjectile : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 8f;
    [SerializeField, Min(0.01f)] private float hitDistance = 0.12f;
    [SerializeField, Min(0.1f)] private float maxLifetime = 3f;
    [SerializeField] private bool projectileLog = false;

    private enum ProjectileFacingAxis
    {
        Right = 0,
        Up = 1
    }

    [SerializeField] private bool rotateAlongVelocity = true;
    [SerializeField] private ProjectileFacingAxis facingAxis = ProjectileFacingAxis.Right;
    [SerializeField] private float rotationOffset = 0f;

    [SerializeField] private string sortingLayerName = "VFX";

    private MonsterController target;
    private GameObject hitEffect;
    private GameObject owner;
    private string sourceName;

    private float attackPower;
    private float critChance;

    private bool isAoe;
    private AttackContext attackContext;
    private float explosionRadius;

    private float lifeTimer;
    private ProjectilePoolMember poolMember;

    private SpriteRenderer[] spriteRenderers;
    private UnitStat unitStat;


    private void Awake()
    {
        poolMember = GetComponent<ProjectilePoolMember>();


    }

    private void ApplyVfxLayer()
    {
        int vfxLayer = LayerMask.NameToLayer("VFX");
        if (vfxLayer >= 0)
            gameObject.layer = vfxLayer;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            spriteRenderers[i].sortingLayerName = sortingLayerName;
        }
    }

    public void Initialize(
    float attackPower,
    float critChance,
    string sourceName,
    MonsterController target,
    GameObject hitEffect,
    GameObject owner,
    bool isAoe,
    AttackContext context,
    float explosionRadius,
    float moveSpeed,
    float hitDistance,
    float maxLifetime,
    bool projectileLog = false)
    {
        this.attackPower = attackPower;
        this.critChance = critChance;
        this.sourceName = sourceName;
        this.target = target;
        this.hitEffect = hitEffect;
        this.owner = owner;
        this.isAoe = isAoe;
        this.attackContext = context;
        this.explosionRadius = Mathf.Max(0.01f, explosionRadius);
        this.moveSpeed = Mathf.Max(0.1f, moveSpeed);
        this.hitDistance = Mathf.Max(0.01f, hitDistance);
        this.maxLifetime = Mathf.Max(0.1f, maxLifetime);
        this.projectileLog = projectileLog;
        unitStat = owner.GetComponent<UnitStat>();
        lifeTimer = 0f;

        if (poolMember == null)
            poolMember = GetComponent<ProjectilePoolMember>();

        ClearReusableVisuals();

        if (IsTargetValid())
            UpdateRotation(target.transform.position - transform.position);

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        ApplyVfxLayer();
    }

    private void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            if (projectileLog)
            {
                DebugTool.Log(
                    $"투사체 수명 종료 | source={sourceName}",
                    DebugType.Unit,
                    this
                );
            }

            ReleaseSelf();
            return;
        }

        if (!IsTargetValid())
        {
            if (projectileLog)
            {
                DebugTool.Log(
                    $"투사체 소멸 | 타겟이 무효화됨 | source={sourceName}",
                    DebugType.Unit,
                    this
                );
            }

            ReleaseSelf();
            return;
        }

        Vector3 targetPosition = target.transform.position;
        Vector3 moveDirection = targetPosition - transform.position;

        UpdateRotation(moveDirection);

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if ((transform.position - targetPosition).sqrMagnitude <= hitDistance * hitDistance)
        {
            Impact(targetPosition);
        }
    }

    private void Impact(Vector3 impactPosition)
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.ResolveProjectileHit(
                attackPower,
                critChance,
                target,
                hitEffect,
                owner,
                sourceName,
                isAoe,
                attackContext,
                explosionRadius,
                impactPosition,
                unitStat
            );
        }

        ReleaseSelf();
    }

    private void ReleaseSelf()
    {
        if (poolMember != null)
        {
            poolMember.ReleaseToPool();
            return;
        }

        Destroy(gameObject);
    }

    private bool IsTargetValid()
    {
        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        if (target.CurrentHp <= 0f)
            return false;

        return true;
    }

    private void UpdateRotation(Vector3 moveDirection)
    {
        if (!rotateAlongVelocity)
            return;

        if (moveDirection.sqrMagnitude <= 0.0001f)
            return;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

        if (facingAxis == ProjectileFacingAxis.Up)
            angle -= 90f;

        angle += rotationOffset;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ClearReusableVisuals()
    {
        TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
            trails[i].Clear();

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles[i].Play(true);
        }
    }

    private void OnDisable()
    {
        target = null;
        hitEffect = null;
        sourceName = null;
        attackPower = 0f;
        critChance = 0f;
        isAoe = false;
        attackContext = default;
        explosionRadius = 0f;
        lifeTimer = 0f;
    }
}