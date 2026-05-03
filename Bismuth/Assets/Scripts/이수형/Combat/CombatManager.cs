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
    [SerializeField] private SynergyEnhanceLevelManager enhanceLevelManager;

    [Header("Wizard Follow-Up")]
    [SerializeField, Min(0.05f)] private float wizardFollowUpDelay = 0.35f;
    [SerializeField] private bool wizardFollowUpLog = false;

    [Header("Elf Synergy")]
    [SerializeField] private BattleWaveRunner _battleWaveRunner;

    [Header("Spirit Synergy")]
    [SerializeField, Min(0.1f)] private float spiritCooldown = 10f;
    [SerializeField] private bool spiritSynergyLog = false;

    private const string SpiritSlowSourceKey = "Synergy_Spirit";

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
    private static readonly Color OrcBuffTintColor = new Color(1f, 0.5f, 0.5f, 1f);

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
        if (enhanceLevelManager == null)
            enhanceLevelManager = FindFirstObjectByType<SynergyEnhanceLevelManager>();
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

        // AttackTargetCount 는 Hub 가 단일 소스. 시너지 강화 보너스 자동 반영.
        // 단, 액티브 스킬이 SkillTargetCountOverride 를 채워서 들어오면 그 값 우선 사용 (예: 30001 광역).
        UnitStatHub hub = unit.GetComponent<UnitStatHub>();
        int currentTargetCount = context.SkillTargetCountOverride > 0
            ? context.SkillTargetCountOverride
            : Mathf.Max(1, Mathf.RoundToInt(hub.Get(StatType.AttackTargetCount)));

        List<MonsterController> targets = SelectTargets(currentTargetCount, currentTarget, attackSensor);
        if (targets.Count == 0)
        {
            DebugTool.Log("피해를 줄 유효한 타겟이 없습니다.", DebugType.Unit, this);
            return false;
        }

        soundManager?.RandomAttackUnit(unitStat);

        // ForceProjectile 컨텍스트(예: 30003 추가 투사체)는 분기 무시하고 무조건 투사체.
        if (context.ForceProjectile)
            return FireProjectiles(unit, towerUnit, unitStat, targets, context);

        // 공격 방식 분기는 Base Range 로 결정.
        // 시너지 강화로 사거리가 늘어도 원래 근접이었던 유닛은 계속 히트스캔, 원래 원거리는 계속 투사체.
        if (hub.GetBase(StatType.Range) > 1.3f)
            return FireProjectiles(unit, towerUnit, unitStat, targets, context);

        return ApplyHitscan(unit, unitStat, towerUnit != null ? towerUnit.name : unitStat.Name, targets, context);
    }

    public bool ResolveProjectileHit(
    float attackPower,
    float critChance,
    float critDamage,
    float bonusVsSlowed,
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
                critDamage,
                bonusVsSlowed,
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
            critDamage,
            bonusVsSlowed,
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

    private List<MonsterController> SelectTargets(int targetCount, MonsterController currentTarget, UnitAttackSensor attackSensor)
    {
        targetCount = Mathf.Max(1, targetCount);

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
        // 공격력/치명타 확률/치명타 데미지/슬로우 보너스 모두 Hub 에서 직접 조회. 반복문 돌기 전에 한 번만 캐시.
        UnitStatHub hub = unit.GetComponent<UnitStatHub>();

        // 거너 5레벨 보너스 : 발사 시점에 쿨다운 충족이면 임시 buff Add (hub.Get 에 자동 반영)
        TryApplyGunnerNextShotCharge(unitStat, hub);

        float attackPower = hub.Get(StatType.AttackPower);
        float critChance  = hub.Get(StatType.CritChance);            // Base + Fighter 시너지 합산
        float critDamage  = hub.Get(StatType.CritDamage);            // 격투가 시너지 강화 등으로 누적
        float bonusVsSlowed = hub.Get(StatType.BonusDamageVsSlowed); // 정령 시너지 강화 5레벨 보너스 (슬로우 적 한정)
        int appliedCount = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterController target = targets[i];
            if (!IsTargetValid(target))
                continue;

            bool success = ApplyDamageToTarget(
                attackPower,
                critChance,
                critDamage,
                bonusVsSlowed,
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
            int requestedCount = Mathf.Max(1, Mathf.RoundToInt(hub.Get(StatType.AttackTargetCount)));
            DebugTool.Log(
                $"히트스캔 공격 완료 | 요청 수={requestedCount}, 실제 타격 수={appliedCount}",
                DebugType.Unit,
                this
            );
        }

        // 거너 5레벨 보너스 : 발사 끝나면 차지 소비 (다음 첫 공격만 효과)
        ConsumeGunnerNextShotCharge(hub);

        return appliedCount > 0;
    }

    private bool FireProjectiles(GameObject unit, TowerUnit towerUnit, UnitStat unitStat, List<MonsterController> targets, AttackContext context)
    {
        // 컨텍스트에 프리팹 오버라이드가 있으면 우선 사용 (예: 30003 근접 유닛의 추가 투사체).
        // 없으면 유닛 본래 프리팹.
        GameObject projectilePrefab = context.ProjectilePrefabOverride != null
            ? context.ProjectilePrefabOverride
            : GetProjectilePrefab(unitStat);
        GameObject hitEffect = GetHitEffect(unitStat);
        string sourceName = towerUnit != null ? towerUnit.name : unitStat.Name;

        // 공격력/치명타 확률/치명타 데미지/슬로우 보너스 모두 Hub 에서 직접 조회. 투사체 발사 전 한 번만 캐시.
        UnitStatHub hub = unit.GetComponent<UnitStatHub>();

        // 거너 5레벨 보너스 : 발사 시점에 쿨다운 충족이면 임시 buff Add (hub.Get 에 자동 반영)
        TryApplyGunnerNextShotCharge(unitStat, hub);

        float attackPower = hub.Get(StatType.AttackPower);
        float critChance  = hub.Get(StatType.CritChance);            // Base + Fighter 시너지 합산
        float critDamage  = hub.Get(StatType.CritDamage);            // 격투가 시너지 강화 등으로 누적
        float bonusVsSlowed = hub.Get(StatType.BonusDamageVsSlowed); // 정령 시너지 강화 5레벨 보너스

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

            // 추가 투사체 등 ProjectileScaleMultiplier 가 채워져 있으면 크기 적용.
            // 풀에서 재사용된 오브젝트도 매번 새로 셋업되도록 절대값으로 설정.
            float scaleMul = context.EffectiveProjectileScale;
            if (!Mathf.Approximately(scaleMul, 1f))
            {
                Vector3 baseScale = Vector3.one;
                projectileObject.transform.localScale = baseScale * scaleMul;
            }
            else
            {
                projectileObject.transform.localScale = Vector3.one;
            }

            UnitProjectile projectile = projectileObject.GetComponent<UnitProjectile>();
            if (projectile == null)
                projectile = projectileObject.AddComponent<UnitProjectile>();

            projectile.Initialize(
                attackPower,
                critChance,
                critDamage,
                bonusVsSlowed,
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
            int requestedCount = Mathf.Max(1, Mathf.RoundToInt(hub.Get(StatType.AttackTargetCount)));
            DebugTool.Log(
                $"투사체 발사 완료 | unit={sourceName}, type={unitStat.attackTypes}, 요청 수={requestedCount}, 실제 생성 수={spawnedCount}",
                DebugType.Unit,
                this
            );
        }

        // 거너 5레벨 보너스 : 발사 끝나면 차지 소비 (이미 캐시된 attackPower 가 투사체에 전달됐으므로 안전)
        ConsumeGunnerNextShotCharge(hub);

        return spawnedCount > 0;
    }

    private bool ApplyExplosionDamage(
    float attackPower,
    float critChance,
    float critDamage,
    float bonusVsSlowed,
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
                critDamage,
                bonusVsSlowed,
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

    // 기본 치명타 데미지 계수. 향후 시트화 가능.
    // 시너지 강화(격투가)의 CritDamage 가 이 위에 합연산으로 들어간다.
    private const float BaseCritMultiplier = 0.5f;

    // 타겟의 슬로우 상태 확인 (정령 시너지 강화 5레벨 보너스 발동 조건용).
    // MonsterController 가 _mover 를 private 으로 들고 있어 GetComponent 경유.
    private static bool IsTargetSlowed(MonsterController target)
    {
        if (target == null) return false;
        MonsterMover mover = target.GetComponent<MonsterMover>();
        return mover != null && mover.IsSlowed;
    }

    // ───────── 거너 시너지 강화 5레벨 보너스 ─────────
    // "5초마다 다음 첫 공격 공격력 +N%" (시트 bonusValue=300 → fractional 3.0)
    // ApplyHitscan / FireProjectiles 진입 시 쿨다운 충족 여부 검사 → Hub 에 임시 모디파이어 Add.
    // hub.Get(AttackPower) 가 자동 반영. 발사 끝나면 모디파이어 Remove → "다음 첫 공격만" 효과.
    private const string GunnerNextShotKey = "SynergyEnhanceBonus_Gunner_NextShotAttackPower";
    private const float GunnerCooldownSeconds = 5f;
    private static readonly int GunnerSynergyIdCached = (int)SynergyManager.SynergyType.Gunner;
    // 유닛별 다음 charge 시점 (Time.time 기준)
    private readonly Dictionary<UnitStat, float> _gunnerNextChargeTimes = new Dictionary<UnitStat, float>();

    /// <summary>
    /// 거너 강화 5레벨 + 거너 태그 + 쿨다운 충족 시 Hub 에 임시 모디파이어를 Add.
    /// 호출자는 발사 후 ConsumeGunnerNextShotCharge 로 정리.
    /// </summary>
    private void TryApplyGunnerNextShotCharge(UnitStat unitStat, UnitStatHub hub)
    {
        if (unitStat == null || hub == null) return;
        if (enhanceLevelManager == null) return;
        if (enhanceLevelManager.GetLevel(GunnerSynergyIdCached) < SynergyEnhanceBonusApplier.MaxLevel) return;
        if (!HasSynergyTag(unitStat, GunnerSynergyIdCached)) return;

        // 첫 등록 : 즉시 차지하지 않고 5초 후부터 차지 가능 (5초 사이클의 시작점)
        if (!_gunnerNextChargeTimes.TryGetValue(unitStat, out float nextChargeTime))
        {
            _gunnerNextChargeTimes[unitStat] = Time.time + GunnerCooldownSeconds;
            return;
        }

        // 쿨다운 미충족
        if (Time.time < nextChargeTime) return;

        int bonusValue = enhanceLevelManager.GetBonusValue(GunnerSynergyIdCached);
        if (bonusValue <= 0) return;

        // fractional 변환 (시트 300 → 3.0)
        float fractional = bonusValue * 0.01f;

        // 멱등 : 이전 키 제거 후 새로 Add
        hub.Remove(GunnerNextShotKey);
        hub.Add(new StatModifier(
            StatType.AttackPower,
            StatOperation.PercentAdd,
            fractional,
            ModifierSource.SynergyEnhance,
            GunnerNextShotKey
        ));

        // 다음 차지까지 5초
        _gunnerNextChargeTimes[unitStat] = Time.time + GunnerCooldownSeconds;

        DebugTool.Log(
            $"[거너 5레벨 보너스] 다음 첫 공격 buff 차지 | unit={unitStat.Name}, bonus={bonusValue}%, value={fractional:F2}",
            DebugType.Synergy,
            unitStat
        );
    }

    /// <summary> 발사 후 거너 next-shot buff 모디파이어 제거 (있으면). 멱등. </summary>
    private static void ConsumeGunnerNextShotCharge(UnitStatHub hub)
    {
        if (hub == null) return;
        hub.Remove(GunnerNextShotKey);
    }

    // ───────── 엘프 시너지 강화 5레벨 보너스 ─────────
    // "적 처치 시 5초간 공격속도 증가 +N%" (시트 bonusValue=50 → fractional 0.5)
    // 처치 이벤트 후크 (ApplyDamageToTarget / 마법사 후속타) 에서 호출.
    // 같은 유닛이 5초 안에 또 처치하면 코루틴 정지 후 재시작 (Add 멱등으로 시간만 갱신).
    private const string ElfKillBonusKey = "SynergyEnhanceBonus_Elf_AttackSpeed";
    private const float ElfKillBonusDurationSeconds = 5f;
    private readonly Dictionary<UnitStat, Coroutine> _elfBuffRoutines = new Dictionary<UnitStat, Coroutine>();

    private void TryTriggerElfKillBonus(UnitStat attackerStat)
    {
        if (attackerStat == null) return;

        // 엘프 태그 보유 + 강화 5레벨 도달
        if (!HasSynergyTag(attackerStat, (int)SynergyManager.SynergyType.Elf)) return;
        if (enhanceLevelManager == null) return;
        if (enhanceLevelManager.GetLevel((int)SynergyManager.SynergyType.Elf) < SynergyEnhanceBonusApplier.MaxLevel) return;

        int bonusValue = enhanceLevelManager.GetBonusValue((int)SynergyManager.SynergyType.Elf);
        if (bonusValue <= 0) return;

        UnitStatHub hub = attackerStat.GetComponent<UnitStatHub>();
        if (hub == null) return;

        // 시트값 % → fractional 변환
        float fractional = bonusValue * 0.01f;

        // Hub 모디파이어 멱등 갱신
        hub.Remove(ElfKillBonusKey);
        hub.Add(new StatModifier(
            StatType.AttackSpeed,
            StatOperation.PercentAdd,
            fractional,
            ModifierSource.SynergyEnhance,
            ElfKillBonusKey
        ));

        // 기존 만료 코루틴 정지 (같은 유닛이 5초 안에 또 처치한 경우)
        if (_elfBuffRoutines.TryGetValue(attackerStat, out Coroutine prev) && prev != null)
            StopCoroutine(prev);

        _elfBuffRoutines[attackerStat] = StartCoroutine(ElfKillBonusExpireRoutine(attackerStat, hub));

        DebugTool.Log(
            $"[엘프 5레벨 보너스] 처치 시 공속 buff 발동 | unit={attackerStat.Name}, bonus={bonusValue}%, duration={ElfKillBonusDurationSeconds}s",
            DebugType.Synergy,
            attackerStat
        );
    }

    private IEnumerator ElfKillBonusExpireRoutine(UnitStat stat, UnitStatHub hub)
    {
        yield return new WaitForSeconds(ElfKillBonusDurationSeconds);

        if (hub != null)
            hub.Remove(ElfKillBonusKey);

        if (stat != null)
            _elfBuffRoutines.Remove(stat);

        if (stat != null)
        {
            DebugTool.Log(
                $"[엘프 5레벨 보너스] 공속 buff 해제 | unit={stat.Name}",
                DebugType.Synergy,
                stat
            );
        }
    }

    private bool ApplyDamageToTarget(
    float attackPower,
    float critChance,
    float critDamage,
    float bonusVsSlowed,
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

        // critChance/critDamage 는 호출측이 Hub 에서 조회한 값.
        //   critChance : Base + Fighter 시너지 스킬 합산
        //   critDamage : Base(0) + 격투가 시너지 강화(Flat) 합산
        // 치명타 발동 시 데미지 계수 = BaseCritMultiplier + critDamage
        float clampedCritChance = Mathf.Clamp01(critChance);
        float crit = (Random.value < clampedCritChance) ? (BaseCritMultiplier + critDamage) : 0f;

        // 정령 시너지 강화 5레벨 보너스 : 타겟이 슬로우 상태일 때만 발동.
        // bonusVsSlowed 자체는 공격자 Hub 에서 미리 조회된 값(태그 없으면 0).
        float effectiveBonusVsSlowed = (bonusVsSlowed > 0f && IsTargetSlowed(target)) ? bonusVsSlowed : 0f;

        int normalDamage;

        if (context.IsActiveSkill && context.DamageBase == SkillDamageBaseType.MaxHp)
        {
            // 적 최대 체력 비례 데미지 (방어력/치명타/슬로우보너스 무시).
            // 궁수 시너지 스킬과 동일 판정 — DamageCalculator.CalculateMaxHpRatioDamage 재사용.
            normalDamage = damageCalculator.CalculateMaxHpRatioDamage(target, context.MaxHpRatio);
        }
        else
        {
            normalDamage = damageCalculator.CalculateNormalDamage(unitStat, attackPower, target.BaseDefense, crit, effectiveBonusVsSlowed);

            // 액티브 스킬 데미지 멀티플라이어 적용. 일반공격은 1f 라 영향 없음.
            // EffectiveDamageMultiplier 가 0/음수일 땐 자동으로 1f 로 보정되어 안전.
            normalDamage = Mathf.RoundToInt(normalDamage * context.EffectiveDamageMultiplier);
        }

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

                // 엘프 시너지 강화 5레벨 보너스 : 처치 시 5초 공속 buff
                TryTriggerElfKillBonus(unitStat);
            }
        }

        unitStat.DealtDamage += finalDamage;

        // 슬로우 디버프 적용 (액티브 스킬이 컨텍스트에 채워서 들어옴, 살아있는 적에만).
        // MonsterMover 가 source 기반이라 같은 스킬 재시전 시 갱신, 정령/타 슬로우와 max(percent) 공존.
        if (context.DebuffSlowPercent > 0f
            && context.DebuffDuration > 0f
            && context.DebuffSourceKey != null
            && target != null
            && target.gameObject.activeInHierarchy
            && target.CurrentHp > 0f)
        {
            MonsterMover mover = target.GetComponent<MonsterMover>();
            if (mover != null)
            {
                mover.ApplySlow(context.DebuffSourceKey, context.DebuffSlowPercent, context.DebuffDuration);
            }
        }

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

        if (synergyManager == null)
            return false;

        int goldBonus = Mathf.RoundToInt(synergyManager.GetEffectValue((int)SynergyManager.SynergyType.Human));

        if (goldBonus <= 0)
            return false;

        if (playerDataManager == null)
            return false;

        playerDataManager.Gold += goldBonus;

        DebugTool.Log(
            $"인간 시너지 골드 획득 | unit={attackerStat.Name}, goldBonus={goldBonus}",
            DebugType.Synergy,
            this
        );

        return true;
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

    // SynergyManager 로의 passthrough. UnitAutoAttack 등 외부에서 CombatManager.Instance 를 통해 간편 호출.
    public float GetSynergyEffectValue(int synergyId, int effectIndex = 0)
    {
        return synergyManager != null ? synergyManager.GetEffectValue(synergyId, effectIndex) : 0f;
    }

    public bool TryGetSynergyEffectValue(int synergyId, int effectIndex, out float value)
    {
        if (synergyManager == null)
        {
            value = 0f;
            return false;
        }
        return synergyManager.TryGetEffectValue(synergyId, effectIndex, out value);
    }

    public bool IsSynergyActive(int synergyId)
    {
        return synergyManager != null && synergyManager.IsSynergyActive(synergyId);
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

                        // 엘프 시너지 강화 5레벨 보너스 : 처치 시 5초 공속 buff
                        TryTriggerElfKillBonus(unitStat);
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

        // 정령 시너지가 활성 상태인데 코루틴이 없으면 시작 (웨이브 시작 직후 즉시 첫 슬로우)
        if (_isSpiritActive && _spiritRoutine == null)
        {
            _spiritRoutine = StartCoroutine(SpiritSynergyRoutine());

            if (spiritSynergyLog)
                DebugTool.Log("정령 시너지 사이클 시작 | 웨이브 시작", DebugType.Synergy, this);
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

        // 정령 코루틴 정지 + 슬로우 즉시 해제
        if (_spiritRoutine != null)
        {
            StopCoroutine(_spiritRoutine);
            _spiritRoutine = null;
        }

        RemoveAllSlows();

        if (spiritSynergyLog)
            DebugTool.Log("정령 슬로우 해제 및 쿨타임 초기화 | 웨이브 클리어", DebugType.Synergy, this);
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

        // 웨이브 진행 중이면 즉시 사이클 시작, 정비시간이면 다음 웨이브 시작 시 OnWaveStarted 가 시작
        if (_isWaveActive)
        {
            _spiritRoutine = StartCoroutine(SpiritSynergyRoutine());
            DebugTool.Log("정령 시너지 활성화 | 사이클 즉시 시작", DebugType.Synergy, this);
        }
        else
        {
            DebugTool.Log("정령 시너지 활성화 | 정비시간이므로 웨이브 시작 시 발동 예정", DebugType.Synergy, this);
        }
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

    // 적이 등장할 때까지 폴링하는 간격. 사이클 첫 발동을 시각적으로 보장하려는 용도라 짧게.
    private const float SpiritWaitForMonstersInterval = 0.1f;

    private IEnumerator SpiritSynergyRoutine()
    {
        while (_isSpiritActive && _isWaveActive)
        {

            if (!TryGetSpiritEffectValues(out float duration, out float slowPercent))
            {
                if (spiritSynergyLog)
                    DebugTool.Log("정령 시너지 효과값 조회 실패 | 다음 쿨다운 대기", DebugType.Synergy, this);

                yield return new WaitForSeconds(spiritCooldown);
                continue;
            }

            // 적이 1마리도 없으면 등장 대기 (웨이브 시작 직후 적 스폰 전 ApplySlow 가 빈 결과로 끝나는 걸 방지).
            // 적이 등장하자마자 즉시 슬로우 발동.
            while (_isSpiritActive && _isWaveActive && !HasAnyActiveMonster())
                yield return new WaitForSeconds(SpiritWaitForMonstersInterval);

            if (!_isSpiritActive || !_isWaveActive)
                yield break;

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

            if (!_isSpiritActive || !_isWaveActive)
                yield break;
        }
    }

    private bool TryGetSpiritEffectValues(out float duration, out float slowPercent)
    {
        duration = 0f;
        slowPercent = 0f;

        if (synergyManager == null)
            return false;

        const int spiritId = (int)SynergyManager.SynergyType.Spirit;

        if (!synergyManager.TryGetEffectValue(spiritId, 0, out duration))
            return false;

        if (!synergyManager.TryGetEffectValue(spiritId, 1, out slowPercent))
            return false;

        return true;
    }

    // 활성 상태인 몬스터가 1마리라도 있는지 빠르게 확인 (정령 사이클 폴링용).
    private static bool HasAnyActiveMonster()
    {
        MonsterMover[] all = FindObjectsByType<MonsterMover>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.activeInHierarchy)
                return true;
        }
        return false;
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

            // 정령 슬로우는 자체 사이클 코루틴이 명시적으로 RemoveSlow 호출.
            // duration 무한(-1) 으로 두고 source key 로 식별. 30001/30006 등 다른 슬로우와 max(percent) 로 공존.
            mover.ApplySlow(SpiritSlowSourceKey, slowPercent, -1f);
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

            mover.RemoveSlow(SpiritSlowSourceKey);
        }

        _slowedMonsters.Clear();
    }

    private int GetSpiritMinActiveCount()
    {
        if (synergyManager == null)
            return int.MaxValue;

        return synergyManager.GetMinActiveCount((int)SynergyManager.SynergyType.Spirit);
    }



    private void HandleSynergyChangedForOrc(Dictionary<int, List<int>> synergiesDict)
    {
        const int orcId = (int)SynergyManager.SynergyType.Orc;

        bool shouldBeActive = synergiesDict.ContainsKey(orcId)
                              && synergiesDict[orcId].Count >= GetOrcMinActiveCount();

        if (shouldBeActive && !_isOrcActive)
        {
            StartOrcSynergy();
            return;
        }

        if (!shouldBeActive && _isOrcActive)
        {
            StopOrcSynergy();
            return;
        }

        // 활성 유지 + 사이클 ON 중 : 단계 변동(효과값 변화) / 새 Orc 유닛 가입을 즉시 반영.
        // OFF 구간(쿨다운) 이면 다음 사이클의 ApplyOrcBuff 가 자연스레 새 값을 읽으므로 불필요.
        if (_isOrcActive && _isOrcBuffActive)
        {
            if (TryGetOrcEffectValues(out _, out float refreshedPercent))
            {
                if (orcSynergyLog)
                    DebugTool.Log(
                        $"오크 시너지 단계 변동 감지 → 버프 즉시 갱신 | newPercent={refreshedPercent:F1}%",
                        DebugType.Synergy, this);

                ApplyOrcBuff(refreshedPercent);
            }
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

        if (synergyManager == null)
            return false;

        const int orcId = (int)SynergyManager.SynergyType.Orc;

        if (!synergyManager.TryGetEffectValue(orcId, 0, out attackPercent))
            return false;

        duration = 5f;
        return true;
    }

    private void ApplyOrcBuff(float attackPercent)
    {
        // 재진입 안전 : 사이클 중 단계 변동으로 다시 호출될 수 있다.
        // 기존 틴트가 남아있으면 먼저 원색 복원 후 새로 적용한다.
        for (int t = 0; t < _orcTintedUnits.Count; t++)
            _orcTintedUnits[t]?.Remove();
        _orcTintedUnits.Clear();

        _isOrcBuffActive = true;

        const int orcId = (int)SynergyManager.SynergyType.Orc;
        UnitStat[] allUnits = FindObjectsByType<UnitStat>(FindObjectsSortMode.None);

        int hubAppliedCount = 0;
        for (int i = 0; i < allUnits.Length; i++)
        {
            if (!HasSynergyTag(allUnits[i], orcId))
                continue;

            // 비주얼 : 틴트
            SpriteColorTint tint = new SpriteColorTint(allUnits[i].gameObject);
            tint.Apply(OrcBuffTintColor);
            _orcTintedUnits.Add(tint);

            // 스탯 : Hub 모디파이어
            UnitStatHub hub = allUnits[i].GetComponent<UnitStatHub>();
            if (hub != null)
            {
                OrcSynergyApplier.Apply(allUnits[i], hub, attackPercent);
                hubAppliedCount++;
            }
        }

        if (orcSynergyLog)
            DebugTool.Log(
                $"오크 버프 적용 | percent={attackPercent:F1}%, tintedUnits={_orcTintedUnits.Count}, hubApplied={hubAppliedCount}",
                DebugType.Synergy, this);
    }

    private void RemoveOrcBuff()
    {
        _isOrcBuffActive = false;

        for (int i = 0; i < _orcTintedUnits.Count; i++)
        {
            _orcTintedUnits[i]?.Remove();
        }

        _orcTintedUnits.Clear();

        // Hub 모디파이어는 Orc 태그 유닛 전체 순회로 제거. 키 단위라 없는 유닛에는 무영향.
        const int orcId = (int)SynergyManager.SynergyType.Orc;
        UnitStat[] allUnits = FindObjectsByType<UnitStat>(FindObjectsSortMode.None);
        int hubRemovedCount = 0;
        for (int i = 0; i < allUnits.Length; i++)
        {
            if (!HasSynergyTag(allUnits[i], orcId))
                continue;

            UnitStatHub hub = allUnits[i].GetComponent<UnitStatHub>();
            if (hub != null)
            {
                OrcSynergyApplier.Remove(allUnits[i], hub);
                hubRemovedCount++;
            }
        }

        if (orcSynergyLog)
            DebugTool.Log($"오크 버프 해제 | 틴트 제거 완료, hubRemoved={hubRemovedCount}", DebugType.Synergy, this);
    }

    private int GetOrcMinActiveCount()
    {
        if (synergyManager == null)
            return int.MaxValue;

        return synergyManager.GetMinActiveCount((int)SynergyManager.SynergyType.Orc);
    }
}