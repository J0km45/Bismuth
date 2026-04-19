using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [SerializeField] private DamageCalculator damageCalculator;
    [SerializeField] private GameObject gameManager;
    [SerializeField] private Shader shader;

    [Header("Hit Effect")]
    [SerializeField] private List<GameObject> hitEffectPrefabs = new List<GameObject>();

    [Header("Attack Effect (Tier4)")]
    [SerializeField] private List<GameObject> attackEffectPrefabs = new List<GameObject>();
    [SerializeField] private bool attackEffectLog = false;

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


    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private SynergyManager synergyManager;

    [Header("Wizard Follow-Up")]
    [SerializeField, Min(0.05f)] private float wizardFollowUpDelay = 0.35f;
    [SerializeField] private bool wizardFollowUpLog = false;

    [Header("Elf Synergy")]
    [SerializeField] private BattleWaveRunner _battleWaveRunner;

    [Header("Synergy SO")]
    [SerializeField] private SynergySO synergySO;
    public SynergySO SynergySO => synergySO;
    [SerializeField, Min(0.1f)] private float spiritCooldown = 10f;
    [SerializeField] private bool spiritSynergyLog = false;

    private Coroutine _spiritRoutine;
    private readonly List<MonsterMover> _slowedMonsters = new();
    private bool _isSpiritActive;

    [Header("Orc Synergy")]
    [SerializeField, Min(0.1f)] private float orcCooldown = 10f;
    [SerializeField] private bool orcSynergyLog = false;

    private Coroutine _orcRoutine;
    private readonly List<SpriteColorTint> _orcTintedUnits = new();
    private bool _isOrcActive;
    private bool _isOrcBuffActive;
    private bool _isWaveActive;
    private float _orcBuffPercent;
    private static readonly Color OrcBuffTintColor = new Color(1f, 0.5f, 0.5f, 1f);

    public bool IsOrcBuffActive => _isOrcBuffActive;
    public float OrcBuffPercent => _orcBuffPercent;

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
        if (synergyManager == null && gameManager != null)
            synergyManager = gameManager.GetComponent<SynergyManager>();
        if (_battleWaveRunner == null)
            _battleWaveRunner = FindFirstObjectByType<BattleWaveRunner>();
        if (playerDataManager == null)
            playerDataManager = FindFirstObjectByType<PlayerDataManager>();
    }

    private void OnEnable()
    {
        if (_battleWaveRunner != null)
        {
            _battleWaveRunner.WaveStarted += OnWaveStarted;
            _battleWaveRunner.WaveCleared += OnWaveCleared;
        }

        if (synergyManager != null)
            synergyManager.OnSynergyChanged += HandleSynergyChangedForSpirit;

        if (synergyManager != null)
            synergyManager.OnSynergyChanged += HandleSynergyChangedForOrc;
    }

    private void OnDisable()
    {
        if (_battleWaveRunner != null)
        {
            _battleWaveRunner.WaveStarted -= OnWaveStarted;
            _battleWaveRunner.WaveCleared -= OnWaveCleared;
        }

        if (synergyManager != null)
            synergyManager.OnSynergyChanged -= HandleSynergyChangedForOrc;
    }

    private void OnValidate()
    {
        TrySetDefaultMonsterLayer();
        EnsureAoeBuffer();
    }

    public bool DamageOccured(GameObject unit, MonsterController currentTarget)
    {
        return DamageOccured(unit, currentTarget, null, AttackContext.Normal());
    }

    public bool DamageOccured(GameObject unit, MonsterController currentTarget, UnitAttackSensor attackSensor, AttackContext context)
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
            return FireProjectiles(unit, towerUnit, unitStat, targets, context);

        return ApplyHitscan(unit, unitStat, towerUnit != null ? towerUnit.name : unitStat.Name, targets, context);
    }

    public bool ResolveProjectileHit(
    float attackPower,
    float critChance,
    MonsterController target,
    GameObject hitEffect,
    GameObject unit,
    string sourceName,
    bool isAoe,
    AttackContext context,
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
                unit,
                context
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
            unit,
            context
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

    private bool ApplyHitscan(GameObject unit, UnitStat unitStat, string sourceName, List<MonsterController> targets, AttackContext context)
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
                unit,
                context
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

    private bool FireProjectiles(GameObject unit, TowerUnit towerUnit, UnitStat unitStat, List<MonsterController> targets, AttackContext context)
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
                context,
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
                $"투사체 발사 완료 | unit={sourceName}, type={unitStat.attackTypes}, 요청 수={Mathf.Max(1, unitStat.AttackTargetCount)}, 실제 생성 수={spawnedCount}",
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
    GameObject unit,
    AttackContext context)
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
                unit,
                context
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
    GameObject unit,
    AttackContext context)
    {
        if (!IsTargetValid(target))
            return false;

        float finalCritChance = critChance;
        if (damageCalculator != null)
            finalCritChance = damageCalculator.GetFinalCritChance(unitStat, critChance);

        float clampedCritChance = Mathf.Clamp01(finalCritChance);
        float crit = (Random.value < clampedCritChance) ? 0.5f : 0f;

        int normalDamage = damageCalculator.CalculateNormalDamage(unitStat, attackPower, target.BaseDefense, crit);
        int archerSkillDamage = damageCalculator.CalculateArcherSkillDamage(unitStat, target, context.IsArcherBonus);

        int finalDamage = normalDamage + archerSkillDamage;


        if (context.IsWizardBonus)
        {
            int wizardDamage = damageCalculator.CalculateSkillDamage(unitStat, attackPower, target.BaseDefense, crit, true);
            if (wizardDamage > 0)
            {
                Vector3 targetPosition = target.transform.position;
                ScheduleWizardFollowUp(target, targetPosition, wizardDamage, hitEffect, sourceName, unitStat);
            }
        }

        if (target.TakeDamage(finalDamage, hitEffect))
        {
            unitStat.KillCount++;
            DebugTool.Log(
                $"몬스터 처치 수 | source={sourceName}, killCount={unitStat.KillCount}",
                DebugType.Unit,
                this
            );

            if (HasSynergyTag(unitStat, (int)SynergyManager.SynergyType.Human))
            {
                Debug.Log(
                        $"인간 시너지 존재확인");
                if (GainHumanSynergyGold(unitStat))
                {
                    Debug.Log(
                        $"인간 시너지 골드 획득 | source={sourceName}");
                }

            }

            TryTriggerWarriorExtraAttack(unit, unitStat, sourceName, context);


            if (HasSynergyTag(unitStat, (int)SynergyManager.SynergyType.Elf))
            {
                if (unitStat.ElfWaveKillCount < 10)
                    unitStat.ElfWaveKillCount++;
                DebugTool.Log(
                    $"엘프 웨이브 킬 적립 | source={sourceName}, elfKill={unitStat.ElfWaveKillCount}",
                    DebugType.Synergy, this);
            }
        }

        unitStat.DealtDamage += finalDamage;

        DebugTool.Log(
            $"{attackChannel} 피해 적용 | source={sourceName}, target={target.name}, final={finalDamage}, normal={normalDamage}, archerSkill={archerSkillDamage}, wizardFollowUp={context.IsWizardBonus}, crit={(crit > 0f ? "Yes" : "No")}, context=[{context}]",
            DebugType.Unit,
            this
        );

        return true;
    }

    private bool GainHumanSynergyGold(UnitStat attackerStat)
    {
        if (!HasSynergyTag(attackerStat, (int)SynergyManager.SynergyType.Human))
            return false;



        SynergyData humanData = GetSynergyData((int)SynergyManager.SynergyType.Human);


        int activeCount = synergyManager.GetSynergyLevel((int)SynergyManager.SynergyType.Human);

        Debug.Log(
            $"인간 시너지 레벨 조회 | activeCount={activeCount}"
        );

        int goldBonus = Mathf.RoundToInt(GetMatchedBonus(humanData, activeCount));

        Debug.Log(
            $"인간 시너지 골드 보너스 계산 | goldBonus={goldBonus}"
        );

        if (goldBonus <= 0)
            return false;
        playerDataManager.Gold += goldBonus;

        return true;
    }
    private SynergyData GetSynergyData(int synergyId)
    {
        if (synergySO == null || synergySO.Rows == null)
            return null;

        for (int i = 0; i < synergySO.Rows.Count; i++)
        {
            SynergyData data = synergySO.Rows[i];
            if (data != null && data.ID == synergyId)
                return data;
        }

        return null;
    }

    private float GetMatchedBonus(SynergyData synergyData, int activeCount)
    {
        float bonus = 0f;

        for (int i = 0; i < synergyData.Levels.Count; i++)
        {
            SynergyLevelData level = synergyData.Levels[i];
            if (level == null)
                continue;

            if (activeCount < level.ActiveCount)
                continue;

            if (level.EffectValues == null || level.EffectValues.Count == 0)
                continue;

            bonus = level.EffectValues[0];
        }

        return bonus;
    }

    private void TryTriggerWarriorExtraAttack(GameObject unit, UnitStat unitStat, string sourceName, AttackContext context)
    {
        if (!CanTriggerWarriorExtraAttack(unitStat, context))
            return;

        SkillCast skillCast = unit != null ? unit.GetComponent<SkillCast>() : null;
        if (skillCast == null)
        {
            DebugTool.Warnning("SkillCast 참조가 없어 전사 추가 공격을 요청하지 못했습니다.", DebugType.Synergy, this);
            return;
        }

        skillCast.SynergyWarriorCast();

        DebugTool.Log(
            $"전사 추가 공격 발동 | source={sourceName}, context=[{context}]",
            DebugType.Synergy,
            this
        );
    }

    private bool CanTriggerWarriorExtraAttack(UnitStat unitStat, AttackContext context)
    {
        if (unitStat == null)
            return false;

        if (context.IsWarriorBonus)
            return false;

        if (!HasSynergyTag(unitStat, (int)SynergyManager.SynergyType.Warrior))
            return false;

        if (synergyManager == null && gameManager != null)
            synergyManager = gameManager.GetComponent<SynergyManager>();

        if (synergyManager == null)
        {
            DebugTool.Warnning("SynergyManager 참조가 없어 전사 활성 여부를 확인하지 못했습니다.", DebugType.Synergy, this);
            return false;
        }

        int warriorLevel = synergyManager.GetSynergyLevel((int)SynergyManager.SynergyType.Warrior);
        return warriorLevel >= 3;
    }

    private bool HasSynergyTag(UnitStat unitStat, int synergyId)
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

    private GameObject GetAttackEffect(UnitStat unitStat)
    {
        if (unitStat == null)
            return null;

        if (unitStat.Tier < 4)
            return null;

        if (unitStat.Id == null)
            return null;

        int effectIndex = unitStat.Id - 10044;
        if (effectIndex < 0 || effectIndex >= attackEffectPrefabs.Count)
            return null;

        return attackEffectPrefabs[effectIndex];
    }

    public void SpawnAttackEffect(TowerUnit towerUnit, UnitStat unitStat)
    {
        if (towerUnit == null || unitStat == null)
            return;

        GameObject prefab = GetAttackEffect(unitStat);
        if (prefab == null)
            return;


        float facingSign = Mathf.Sign(towerUnit.transform.localScale.x);


        AttackEffectAnchor anchor = towerUnit.GetComponentInChildren<AttackEffectAnchor>();
        if (anchor != null)
        {
            DebugTool.Log(
                $"공격 이펙트 앵커 발견 | anchor={anchor.name}, unit={towerUnit.name}, pos ={anchor.transform.position}",
                DebugType.Unit,
                this
            );
        }
        Vector3 spawnPos;
        Vector3 offsetGun = new Vector3(0.6f, 0.0f, 0.0f);
        if (anchor != null)
        {
            spawnPos = anchor.transform.position + offsetGun;
        }
        else
        {
            Vector2 offset = towerUnit.AttackEffectOffset;
            spawnPos = towerUnit.transform.position
                + new Vector3(offset.x * facingSign, offset.y, 0f);
        }

        GameObject effect = HitEffectPool.SpawnPooled(prefab, spawnPos, Quaternion.identity);

        if (effect != null)
        {

            Vector3 effectScale = effect.transform.localScale;
            effectScale.x = Mathf.Abs(effectScale.x) * facingSign;
            effect.transform.localScale = effectScale;


            if (anchor != null)
            {
                HitEffectSpawner spawner = effect.GetComponent<HitEffectSpawner>();
                if (spawner != null)
                    spawner.ConfigureFollowTarget(anchor.transform, offsetGun);
            }
        }

        if (attackEffectLog)
        {
            DebugTool.Log(
                $"공격 이펙트 생성 | unit={unitStat.Name}, prefab={prefab.name}, facing={(facingSign > 0f ? "오른쪽" : "왼쪽")}, anchor={(anchor != null ? anchor.name : "None")}, pos={spawnPos}",
                DebugType.Unit,
                this
            );
        }
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
                int synergyIndex = unitStat.Id - 10031;
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

    public int GetSynergyLevel(int synergyId)
    {
        return synergyManager.GetSynergyLevel(synergyId);
    }


    private void ScheduleWizardFollowUp(
        MonsterController target,
        Vector3 hitPosition,
        int damage,
        GameObject hitEffect,
        string sourceName,
        UnitStat unitStat)
    {
        StartCoroutine(ExecuteWizardFollowUp(target, hitPosition, damage, hitEffect, sourceName, unitStat));

        if (wizardFollowUpLog)
        {
            DebugTool.Log(
                $"마법사 후속타격 예약 | target={target.name}, damage={damage}, delay={wizardFollowUpDelay:F2}s",
                DebugType.Synergy,
                this
            );
        }
    }


    private IEnumerator ExecuteWizardFollowUp(
        MonsterController target,
        Vector3 hitPosition,
        int damage,
        GameObject hitEffect,
        string sourceName,
        UnitStat unitStat)
    {
        yield return new WaitForSeconds(wizardFollowUpDelay);


        Vector3 effectPosition = hitPosition;
        bool targetAlive = target != null
                           && target.gameObject.activeInHierarchy
                           && target.CurrentHp > 0f;

        if (targetAlive)
            effectPosition = target.transform.position;


        if (hitEffect != null)
        {
            GameObject spawnedEffect = HitEffectPool.SpawnPooled(hitEffect, effectPosition, Quaternion.identity);


            if (spawnedEffect != null && targetAlive)
            {
                HitEffectSpawner effectSpawner = spawnedEffect.GetComponent<HitEffectSpawner>();
                if (effectSpawner != null)
                    effectSpawner.ConfigureFollowTarget(target.transform, true);
            }
        }


        if (targetAlive)
        {

            if (target.TakeDamage(damage, null))
            {
                if (unitStat != null)
                {
                    unitStat.KillCount++;
                    DebugTool.Log(
                        $"마법사 후속타격으로 처치 | source={sourceName}, killCount={unitStat.KillCount}",
                        DebugType.Synergy,
                        this
                    );


                    if (HasSynergyTag(unitStat, (int)SynergyManager.SynergyType.Elf))
                    {
                        unitStat.ElfWaveKillCount++;
                        DebugTool.Log(
                            $"엘프 웨이브 킬 적립 (마법사 후속) | source={sourceName}, elfKill={unitStat.ElfWaveKillCount}",
                            DebugType.Synergy, this);
                    }
                }
            }

            if (unitStat != null)
                unitStat.DealtDamage += damage;
        }

        if (wizardFollowUpLog)
        {
            DebugTool.Log(
                $"마법사 후속타격 실행 | target={(target != null ? target.name : "Destroyed")}, alive={targetAlive}, damage={damage}, position={effectPosition}",
                DebugType.Synergy,
                this
            );
        }
    }



    private void OnWaveStarted(WaveDataSO waveData)
    {
        ResetAllElfWaveKillCounts();

        _isWaveActive = true;

        // 오크 시너지가 활성 상태인데 코루틴이 없으면 시작
        if (_isOrcActive && _orcRoutine == null)
        {
            _orcRoutine = StartCoroutine(OrcSynergyRoutine());

            if (orcSynergyLog)
                DebugTool.Log("오크 시너지 버프 사이클 시작 | 웨이브 시작", DebugType.Synergy, this);
        }
    }

    private void OnWaveCleared(WaveDataSO waveData)
    {
        _isWaveActive = false;

        // 오크 버프 즉시 해제 + 코루틴 정지 + 쿨타임 초기화
        if (_orcRoutine != null)
        {
            StopCoroutine(_orcRoutine);
            _orcRoutine = null;
        }

        RemoveOrcBuff();

        if (orcSynergyLog)
            DebugTool.Log("오크 시너지 버프 해제 및 쿨타임 초기화 | 웨이브 클리어", DebugType.Synergy, this);
    }

    private void ResetAllElfWaveKillCounts()
    {
        UnitStat[] allUnits = FindObjectsByType<UnitStat>(FindObjectsSortMode.None);

        for (int i = 0; i < allUnits.Length; i++)
        {
            if (allUnits[i].ElfWaveKillCount > 0)
            {
                DebugTool.Log(
                    $"엘프 웨이브 킬 초기화 | unit={allUnits[i].Name}, was={allUnits[i].ElfWaveKillCount}",
                    DebugType.Synergy, this);

                allUnits[i].ElfWaveKillCount = 0;
            }
        }
    }



    private void HandleSynergyChangedForSpirit(Dictionary<int, List<int>> synergiesDict)
    {
        const int spiritId = (int)SynergyManager.SynergyType.Spirit;

        bool shouldBeActive = synergiesDict.ContainsKey(spiritId)
                              && synergiesDict[spiritId].Count >= GetSpiritMinActiveCount();

        if (shouldBeActive && !_isSpiritActive)
        {
            StartSpiritSynergy();
        }
        else if (!shouldBeActive && _isSpiritActive)
        {
            StopSpiritSynergy();
        }
    }

    private void StartSpiritSynergy()
    {
        if (_isSpiritActive)
            return;

        _isSpiritActive = true;
        _spiritRoutine = StartCoroutine(SpiritSynergyRoutine());

        DebugTool.Log("정령 시너지 활성화 | 쿨다운 타이머 시작", DebugType.Synergy, this);
    }

    private void StopSpiritSynergy()
    {
        if (!_isSpiritActive)
            return;

        _isSpiritActive = false;

        if (_spiritRoutine != null)
        {
            StopCoroutine(_spiritRoutine);
            _spiritRoutine = null;
        }

        RemoveAllSlows();

        DebugTool.Log("정령 시너지 비활성화 | 슬로우 해제 및 타이머 중단", DebugType.Synergy, this);
    }

    private IEnumerator SpiritSynergyRoutine()
    {
        while (_isSpiritActive)
        {

            if (!TryGetSpiritEffectValues(out float duration, out float slowPercent))
            {
                if (spiritSynergyLog)
                    DebugTool.Log("정령 시너지 효과값 조회 실패 | 다음 쿨다운 대기", DebugType.Synergy, this);

                yield return new WaitForSeconds(spiritCooldown);
                continue;
            }


            ApplySlowToAllMonsters(slowPercent);

            if (spiritSynergyLog)
                DebugTool.Log(
                    $"정령 시너지 발동 | slow={slowPercent:F1}%, duration={duration:F1}s, targets={_slowedMonsters.Count}",
                    DebugType.Synergy, this);


            yield return new WaitForSeconds(duration);


            RemoveAllSlows();

            if (spiritSynergyLog)
                DebugTool.Log("정령 시너지 슬로우 해제 | 쿨다운 재시작", DebugType.Synergy, this);


            yield return new WaitForSeconds(spiritCooldown);

            if (!_isSpiritActive)
                yield break;
        }
    }

    private bool TryGetSpiritEffectValues(out float duration, out float slowPercent)
    {
        duration = 0f;
        slowPercent = 0f;

        if (synergySO == null || synergyManager == null)
            return false;

        const int spiritId = (int)SynergyManager.SynergyType.Spirit;

        SynergyData spiritData = GetSpiritSynergyData(spiritId);
        if (spiritData == null || spiritData.Levels == null || spiritData.Levels.Count == 0)
            return false;

        int activeCount = synergyManager.GetSynergyLevel(spiritId);


        SynergyLevelData matchedLevel = null;
        for (int i = 0; i < spiritData.Levels.Count; i++)
        {
            SynergyLevelData level = spiritData.Levels[i];
            if (level == null)
                continue;

            if (activeCount < level.ActiveCount)
                continue;

            matchedLevel = level;
        }

        if (matchedLevel == null || matchedLevel.EffectValues == null || matchedLevel.EffectValues.Count < 2)
            return false;

        duration = matchedLevel.EffectValues[0];
        slowPercent = matchedLevel.EffectValues[1];
        return true;
    }

    private void ApplySlowToAllMonsters(float slowPercent)
    {
        _slowedMonsters.Clear();

        MonsterMover[] allMovers = FindObjectsByType<MonsterMover>(FindObjectsSortMode.None);

        for (int i = 0; i < allMovers.Length; i++)
        {
            MonsterMover mover = allMovers[i];

            if (mover == null || !mover.gameObject.activeInHierarchy)
                continue;

            mover.ApplySlow(slowPercent);
            _slowedMonsters.Add(mover);
        }
    }

    private void RemoveAllSlows()
    {
        for (int i = 0; i < _slowedMonsters.Count; i++)
        {
            MonsterMover mover = _slowedMonsters[i];

            if (mover == null || !mover.gameObject.activeInHierarchy)
                continue;

            mover.RemoveSlow();
        }

        _slowedMonsters.Clear();
    }

    private SynergyData GetSpiritSynergyData(int synergyId)
    {
        if (synergySO == null || synergySO.Rows == null)
            return null;

        for (int i = 0; i < synergySO.Rows.Count; i++)
        {
            SynergyData data = synergySO.Rows[i];
            if (data != null && data.ID == synergyId)
                return data;
        }

        return null;
    }


    private int GetSpiritMinActiveCount()
    {
        if (synergySO == null)
            return int.MaxValue;

        const int spiritId = (int)SynergyManager.SynergyType.Spirit;
        SynergyData spiritData = GetSpiritSynergyData(spiritId);

        if (spiritData == null || spiritData.Levels == null || spiritData.Levels.Count == 0)
            return int.MaxValue;

        return spiritData.Levels[0].ActiveCount;
    }



    private void HandleSynergyChangedForOrc(Dictionary<int, List<int>> synergiesDict)
    {
        const int orcId = (int)SynergyManager.SynergyType.Orc;

        bool shouldBeActive = synergiesDict.ContainsKey(orcId)
                              && synergiesDict[orcId].Count >= GetOrcMinActiveCount();

        if (shouldBeActive && !_isOrcActive)
        {
            StartOrcSynergy();
        }
        else if (!shouldBeActive && _isOrcActive)
        {
            StopOrcSynergy();
        }
    }

    private void StartOrcSynergy()
    {
        if (_isOrcActive)
            return;

        _isOrcActive = true;

        // 웨이브 진행 중일 때만 코루틴 즉시 시작, 정비시간이면 웨이브 시작 시 시작됨
        if (_isWaveActive)
        {
            _orcRoutine = StartCoroutine(OrcSynergyRoutine());
            DebugTool.Log("오크 시너지 활성화 | 버프 사이클 즉시 시작", DebugType.Synergy, this);
        }
        else
        {
            DebugTool.Log("오크 시너지 활성화 | 정비시간이므로 웨이브 시작 시 발동 예정", DebugType.Synergy, this);
        }
    }

    private void StopOrcSynergy()
    {
        if (!_isOrcActive)
            return;

        _isOrcActive = false;

        if (_orcRoutine != null)
        {
            StopCoroutine(_orcRoutine);
            _orcRoutine = null;
        }

        RemoveOrcBuff();

        DebugTool.Log("오크 시너지 비활성화 | 버프 해제 및 사이클 중단", DebugType.Synergy, this);
    }

    private IEnumerator OrcSynergyRoutine()
    {
        while (_isOrcActive && _isWaveActive)
        {
            if (!TryGetOrcEffectValues(out float duration, out float attackPercent))
            {
                if (orcSynergyLog)
                    DebugTool.Log("오크 시너지 효과값 조회 실패 | 다음 쿨다운 대기", DebugType.Synergy, this);

                yield return new WaitForSeconds(orcCooldown);
                continue;
            }


            ApplyOrcBuff(attackPercent);

            if (orcSynergyLog)
                DebugTool.Log(
                    $"오크 시너지 발동 | attackBonus={attackPercent:F1}%, duration={duration:F1}s",
                    DebugType.Synergy, this);

            yield return new WaitForSeconds(duration);


            RemoveOrcBuff();

            if (orcSynergyLog)
                DebugTool.Log("오크 시너지 버프 해제 | 쿨다운 재시작", DebugType.Synergy, this);

            yield return new WaitForSeconds(orcCooldown);

            if (!_isOrcActive || !_isWaveActive)
                yield break;
        }
    }

    private bool TryGetOrcEffectValues(out float duration, out float attackPercent)
    {
        duration = 0f;
        attackPercent = 0f;

        if (synergySO == null || synergyManager == null)
            return false;

        const int orcId = (int)SynergyManager.SynergyType.Orc;

        SynergyData orcData = GetSynergyData(orcId);
        if (orcData == null || orcData.Levels == null || orcData.Levels.Count == 0)
            return false;

        int activeCount = synergyManager.GetSynergyLevel(orcId);

        SynergyLevelData matchedLevel = null;
        for (int i = 0; i < orcData.Levels.Count; i++)
        {
            SynergyLevelData level = orcData.Levels[i];
            if (level == null)
                continue;

            if (activeCount < level.ActiveCount)
                continue;

            matchedLevel = level;
        }

        if (matchedLevel == null || matchedLevel.EffectValues == null || matchedLevel.EffectValues.Count == 0)
            return false;

        attackPercent = matchedLevel.EffectValues[0];
        duration = 5f;
        return true;
    }

    private void ApplyOrcBuff(float attackPercent)
    {
        _orcBuffPercent = attackPercent;
        _isOrcBuffActive = true;
        _orcTintedUnits.Clear();

        const int orcId = (int)SynergyManager.SynergyType.Orc;
        UnitStat[] allUnits = FindObjectsByType<UnitStat>(FindObjectsSortMode.None);

        for (int i = 0; i < allUnits.Length; i++)
        {
            if (!HasSynergyTag(allUnits[i], orcId))
                continue;

            SpriteColorTint tint = new SpriteColorTint(allUnits[i].gameObject);
            tint.Apply(OrcBuffTintColor);
            _orcTintedUnits.Add(tint);
        }

        if (orcSynergyLog)
            DebugTool.Log(
                $"오크 버프 적용 | percent={attackPercent:F1}%, tintedUnits={_orcTintedUnits.Count}",
                DebugType.Synergy, this);
    }

    private void RemoveOrcBuff()
    {
        _isOrcBuffActive = false;
        _orcBuffPercent = 0f;

        for (int i = 0; i < _orcTintedUnits.Count; i++)
        {
            _orcTintedUnits[i]?.Remove();
        }

        _orcTintedUnits.Clear();

        if (orcSynergyLog)
            DebugTool.Log("오크 버프 해제 | 틴트 제거 완료", DebugType.Synergy, this);
    }

    private int GetOrcMinActiveCount()
    {
        if (synergySO == null)
            return int.MaxValue;

        const int orcId = (int)SynergyManager.SynergyType.Orc;
        SynergyData orcData = GetSynergyData(orcId);

        if (orcData == null || orcData.Levels == null || orcData.Levels.Count == 0)
            return int.MaxValue;

        return orcData.Levels[0].ActiveCount;
    }
}