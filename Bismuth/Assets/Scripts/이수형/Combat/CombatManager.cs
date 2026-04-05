using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [SerializeField] private DamageCalculator damageCalculator;
    [SerializeField] private GameObject gameManager;
    [SerializeField] private Shader shader;

    [Header("Hit Effect")]
    [SerializeField] private List<GameObject> hitEffectPrefabs = new List<GameObject>();

    [Header("Projectile")]
    [SerializeField] private List<GameObject> projectilePrefabs_tier1 = new List<GameObject>();
    [SerializeField] private List<GameObject> projectilePrefabs_tier2 = new List<GameObject>();
    [SerializeField] private List<GameObject> projectilePrefabs_tier3_4 = new List<GameObject>();
    [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0.01f)] private float projectileHitDistance = 0.12f;
    [SerializeField, Min(0.1f)] private float projectileMaxLifetime = 4f;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField] private bool projectileLog = false;


    [Header("공격 사운드")]
    [SerializeField] private SoundManager soundManager;

    [Header("AOE")]
    [SerializeField] private LayerMask monsterLayerMask;
    [SerializeField, Min(1)] private int aoeOverlapBufferSize = 32;
    [SerializeField, Min(0.01f)] private float aoeEffectScalePerRadius = 2f;


    private Collider2D[] aoeOverlapResults;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (damageCalculator == null)
            damageCalculator = GetComponent<DamageCalculator>();

        if (projectilePool == null)
            projectilePool = GetComponent<ProjectilePool>();

        if (projectilePool == null)
            projectilePool = gameObject.AddComponent<ProjectilePool>();

        TrySetDefaultMonsterLayer();
        EnsureAoeBuffer();

        if (gameManager != null)
            gameManager.GetComponent<SynergyManager>();

        if (soundManager == null)
            soundManager = FindFirstObjectByType<SoundManager>();
    }

    private void OnValidate()
    {
        TrySetDefaultMonsterLayer();
        EnsureAoeBuffer();
    }

    public bool DamageOccured(GameObject unit, MonsterController currentTarget)
    {
        return DamageOccured(unit, currentTarget, null);
    }

    public bool DamageOccured(GameObject unit, MonsterController currentTarget, UnitAttackSensor attackSensor)
    {
        TowerUnit towerUnit = unit.GetComponent<TowerUnit>();

        if (!TryGetUnitStat(towerUnit, out UnitStat unitStat))
            return false;

        List<MonsterController> targets = SelectTargets(unitStat, currentTarget, attackSensor);
        if (targets.Count == 0)
        {
            DebugTool.Log("피해를 줄 유효한 타겟이 없습니다.", DebugType.Unit, this);
            return false;
        }

        soundManager?.RandomAttackUnit(unitStat);

        if (unitStat.Range > 1.3f)
            return FireProjectiles(unit, towerUnit, unitStat, targets);

        return ApplyHitscan(unit, unitStat, towerUnit != null ? towerUnit.name : unitStat.Name, targets);
    }

    public bool ResolveProjectileHit(
    float attackPower,
    float critChance,
    MonsterController target,
    GameObject hitEffect,
    GameObject unit,
    string sourceName,
    bool isAoe,
    float explosionRadius,
    Vector3 impactPosition,
    UnitStat unitStat)
    {
        if (isAoe)
        {
            SpawnExplosionEffect(hitEffect, impactPosition, explosionRadius);

            return ApplyExplosionDamage(
                attackPower,
                critChance,
                impactPosition,
                explosionRadius,
                sourceName,
                unitStat,
                unit
            );
        }

        return ApplyDamageToTarget(
            attackPower,
            critChance,
            target,
            hitEffect,
            sourceName,
            "투사체",
            unitStat,
            unit
        );
    }

    private void SpawnExplosionEffect(GameObject hitEffect, Vector3 impactPosition, float radius)
    {
        if (hitEffect == null)
            return;

        float scaleMultiplier = Mathf.Max(0.01f, radius * aoeEffectScalePerRadius);

        HitEffectPool.SpawnPooled(
            hitEffect,
            impactPosition,
            Quaternion.identity,
            scaleMultiplier
        );

        DebugTool.Log(
            $"AOE 폭발 이펙트 생성 | radius={radius:F2}, scale={scaleMultiplier:F2}",
            DebugType.Unit,
            this
        );
    }

    private bool TryGetUnitStat(TowerUnit towerUnit, out UnitStat unitStat)
    {
        unitStat = null;

        if (towerUnit == null)
        {
            DebugTool.Log("타워 유닛 참조가 없습니다.", DebugType.Unit, this);
            return false;
        }

        unitStat = towerUnit.GetComponent<UnitStat>();
        if (unitStat == null)
        {
            DebugTool.Log("타워 유닛에 UnitStat 컴포넌트가 없습니다.", DebugType.Unit, this);
            return false;
        }

        if (damageCalculator == null)
        {
            DebugTool.Log("DamageCalculator 참조가 없습니다.", DebugType.Unit, this);
            return false;
        }

        return true;
    }

    private List<MonsterController> SelectTargets(UnitStat unitStat, MonsterController currentTarget, UnitAttackSensor attackSensor)
    {
        int targetCount = Mathf.Max(1, unitStat.AttackTargetCount);

        if (attackSensor == null)
        {
            List<MonsterController> fallbackTargets = new List<MonsterController>();

            if (IsTargetValid(currentTarget))
                fallbackTargets.Add(currentTarget);

            return fallbackTargets;
        }

        return attackSensor.GetTargets(targetCount, currentTarget);
    }

    private bool ApplyHitscan(GameObject unit, UnitStat unitStat, string sourceName, List<MonsterController> targets)
    {
        GameObject hitEffect = GetHitEffect(unitStat);
        int appliedCount = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterController target = targets[i];
            if (!IsTargetValid(target))
                continue;

            bool success = ApplyDamageToTarget(
                unitStat.CurrentAttackPower,
                unitStat.CritChance,
                target,
                hitEffect,
                sourceName,
                "히트스캔",
                unitStat,
                unit
            );

            if (success)
                appliedCount++;
        }

        if (appliedCount > 0)
        {
            DebugTool.Log(
                $"히트스캔 공격 완료 | 요청 수={Mathf.Max(1, unitStat.AttackTargetCount)}, 실제 타격 수={appliedCount}",
                DebugType.Unit,
                this
            );
        }


        return appliedCount > 0;
    }

    private bool FireProjectiles(GameObject unit, TowerUnit towerUnit, UnitStat unitStat, List<MonsterController> targets)
    {
        GameObject projectilePrefab = GetProjectilePrefab(unitStat);
        GameObject hitEffect = GetHitEffect(unitStat);
        string sourceName = towerUnit != null ? towerUnit.name : unitStat.Name;

        bool isAoe = unitStat.attackTypes == UnitData.AttackTypes.AOE;
        float explosionRadius = Mathf.Max(0.01f, unitStat.AttackArea);

        int spawnedCount = 0;
        Vector3 spawnPosition = towerUnit != null ? towerUnit.transform.position : Vector3.zero;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterController target = targets[i];
            if (!IsTargetValid(target))
                continue;

            GameObject projectileObject = null;

            if (projectilePrefab != null && projectilePool != null)
            {
                projectileObject = projectilePool.Get(projectilePrefab, spawnPosition, Quaternion.identity);
            }
            else if (projectilePrefab != null)
            {
                projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                projectileObject = new GameObject($"{sourceName}_Projectile");
                projectileObject.transform.position = spawnPosition;
            }

            UnitProjectile projectile = projectileObject.GetComponent<UnitProjectile>();
            if (projectile == null)
                projectile = projectileObject.AddComponent<UnitProjectile>();

            projectile.Initialize(
                unitStat.CurrentAttackPower,
                unitStat.CritChance,
                sourceName,
                target,
                hitEffect,
                unit,
                isAoe,
                explosionRadius,
                projectileSpeed,
                projectileHitDistance,
                projectileMaxLifetime,
                projectileLog
            );

            spawnedCount++;
        }

        if (spawnedCount > 0)
        {
            DebugTool.Log(
                $"투사체 발사 완료 | unit={sourceName}, type={unitStat.attackTypes}, 요청 수={Mathf.Max(1, unitStat.AttackTargetCount)}, 실제 발사 수={spawnedCount}",
                DebugType.Unit,
                this
            );
        }

        return spawnedCount > 0;
    }

    private bool ApplyExplosionDamage(
    float attackPower,
    float critChance,
    Vector3 impactPosition,
    float radius,
    string sourceName,
    UnitStat unitStat,
    GameObject unit)
    {
        EnsureAoeBuffer();

        int hitCount = Physics2D.OverlapCircleNonAlloc(
            impactPosition,
            radius,
            aoeOverlapResults,
            monsterLayerMask
        );

        HashSet<MonsterController> uniqueTargets = new HashSet<MonsterController>();
        int appliedCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = aoeOverlapResults[i];
            if (hit == null)
                continue;

            MonsterController monster = hit.GetComponentInParent<MonsterController>();
            if (!IsTargetValid(monster))
                continue;

            if (!uniqueTargets.Add(monster))
                continue;

            bool success = ApplyDamageToTarget(
                attackPower,
                critChance,
                monster,
                null,
                sourceName,
                "투사체 폭발",
                unitStat,
                unit
            );

            if (success)
                appliedCount++;
        }

        if (appliedCount > 0)
        {
            DebugTool.Log(
                $"AOE 폭발 적용 | source={sourceName}, radius={radius:F2}, hitCount={appliedCount}",
                DebugType.Unit,
                this
            );
        }
        else
        {
            DebugTool.Log(
                $"AOE 폭발 발생 | source={sourceName}, radius={radius:F2}, 범위 내 유효 타겟 없음",
                DebugType.Unit,
                this
            );
        }

        return appliedCount > 0;
    }

    private bool ApplyDamageToTarget(
        float attackPower,
        float critChance,
        MonsterController target,
        GameObject hitEffect,
        string sourceName,
        string attackChannel,
        UnitStat unitStat,
        GameObject unit)
    {
        if (!IsTargetValid(target))
            return false;
        int dealtDamage = 0;
        float clampedCritChance = Mathf.Clamp01(critChance);
        float crit = (Random.value < clampedCritChance) ? 0.5f : 0f;

        int normalDamage = damageCalculator.CalculateNormalDamage(attackPower, target.BaseDefense, crit);
        int skillDamage = damageCalculator.CalculateSkillDamage(attackPower, target.BaseDefense, crit);
        int finalDamage = normalDamage + skillDamage;
        dealtDamage = finalDamage;
        if (target.TakeDamage(finalDamage, hitEffect))
        {
            unitStat.KillCount++;
            DebugTool.Log(
                $"몬스터 처치 수 | source={sourceName}, killCount={unitStat.KillCount}",
                DebugType.Unit,
                this
            );
        }
        unitStat.DealtDamage += dealtDamage;
        DebugTool.Log(
            $"{attackChannel} 피해 적용 | source={sourceName}, target={target.name}, final={finalDamage}, normal={normalDamage}, skill={skillDamage}, crit={(crit > 0f ? "Yes" : "No")}",
            DebugType.Unit,
            this
        );
        DebugTool.Log(
            $"{attackChannel} 피해 적용 | source={sourceName}, target={target.name}, final={finalDamage}, normal={normalDamage}, skill={skillDamage}, crit={(crit > 0f ? "Yes" : "No")}",
            DebugType.Unit,
            this
        );



        return true;
    }

    private GameObject GetHitEffect(UnitStat unitStat)
    {
        if (unitStat == null)
            return null;

        if (unitStat.SynergIDs == null || unitStat.SynergIDs.Length <= 1)
            return null;

        int unitSynergy = unitStat.SynergIDs[1];
        int unitTier = unitStat.Tier;

        //if (unitTier >= 3)
        //    return null;

        int effectIndex = (unitSynergy - 50006) + unitTier * 5;
        //int effectIndex = 0;
        if (effectIndex < 0 || effectIndex >= hitEffectPrefabs.Count)
            return null;

        return hitEffectPrefabs[effectIndex];
    }

    private GameObject GetProjectilePrefab(UnitStat unitStat)
    {
        if (unitStat == null)
            return null;

        List<GameObject> sourceList = null;

        if (unitStat.Tier == 1)
            sourceList = projectilePrefabs_tier1;
        else if (unitStat.Tier == 2)
            sourceList = projectilePrefabs_tier2;
        else
            sourceList = projectilePrefabs_tier3_4;

        if (sourceList == null || sourceList.Count == 0)
            return null;

        if (unitStat.SynergIDs != null && unitStat.SynergIDs.Length > 1)
        {
            if (unitStat.Tier < 3)
            {
                int synergyIndex = unitStat.SynergIDs[1] - 50003;
                if (synergyIndex >= 0 && synergyIndex < sourceList.Count)
                    return sourceList[synergyIndex];
            }
            else
            {
                int synergyIndex = unitStat.Id - 10032;
                if (synergyIndex >= 0 && synergyIndex < sourceList.Count)
                    return sourceList[synergyIndex];
            }


        }

        return sourceList[0];
    }

    private bool IsTargetValid(MonsterController target)
    {
        return target != null && target.gameObject.activeInHierarchy && target.CurrentHp > 0f;
    }

    private void TrySetDefaultMonsterLayer()
    {
        if (monsterLayerMask.value != 0)
            return;

        int monsterLayer = LayerMask.NameToLayer("Monster");
        if (monsterLayer >= 0)
            monsterLayerMask = 1 << monsterLayer;
    }

    private void EnsureAoeBuffer()
    {
        if (aoeOverlapResults == null || aoeOverlapResults.Length != aoeOverlapBufferSize)
            aoeOverlapResults = new Collider2D[aoeOverlapBufferSize];
    }
}