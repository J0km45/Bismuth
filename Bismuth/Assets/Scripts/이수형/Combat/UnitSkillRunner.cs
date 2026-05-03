using System.Collections;
using UnityEngine;

/// <summary>
/// 한 유닛이 보유한 단일 스킬을 관리하는 컴포넌트.
///
/// 스킬 분류:
///   - 액티브 스킬   = DamageFormula > 0  (일반공격 대신 발동, UnitAutoAttack 이 분기)
///   - 비액티브 스킬 = DamageFormula == 0 (자체 타이머로 발동, 이 컴포넌트 Update 가 직접 처리)
///
/// 비액티브 스킬은 현재 자가 버프(30004/30005)만 처리. 30006(광역 디버프)은 추후.
/// 모든 비액티브 자가 버프는 웨이브 종료 시 즉시 해제 + 쿨다운 초기화 (시트 30004 비고 룰을 일관 적용).
/// </summary>
[DisallowMultipleComponent]
public class UnitSkillRunner : MonoBehaviour
{
    [Header("스킬 데이터")]
    [Tooltip("이 유닛이 보유한 스킬. null 이면 스킬 없음.\n한 유닛은 하나의 스킬만 가진다.")]
    [SerializeField] private SkillDataSO _skill;

    [Header("디버그")]
    [SerializeField] private bool _log = true;

    private UnitStatHub _statHub;
    private BattleWaveRunner _battleWaveRunner;

    private float _nextSkillReadyTime = 0f;
    private object _slowSourceKeyCache;
    private object _buffKeyCache;

    private Coroutine _buffExpireRoutine;

    public SkillDataSO Skill => _skill;

    public bool HasSkill => _skill != null;

    /// <summary>
    /// 데미지 효과가 있는 스킬 (DamageFormula > 0). 30001/30002/30003/30007.
    /// </summary>
    public bool IsActiveSkill => HasSkill && _skill.DamageFormula > 0f;

    /// <summary>
    /// 매 일반공격에 따라붙는 추가 투사체 스킬인지 (DamageFormula > 0 AND CoolDown == 0).
    /// 30003 케이스. 일반공격 대신 발동하지 않고, ApplyLockedHit 시점에 추가로 투사체 발사.
    /// </summary>
    public bool IsExtraProjectileSkill => IsActiveSkill && _skill.CoolDown <= 0f;

    /// <summary>
    /// 자가 버프 스킬인지 (BuffValue/Duration 둘 다 > 0).
    /// 30004(공속), 30005(공격력) 같은 비액티브 자가 버프.
    /// </summary>
    public bool IsBuffSkill => HasSkill && _skill.BuffValue > 0f && _skill.BuffDuration > 0f;

    /// <summary>
    /// 쿨다운이 다 차서 지금 발동 가능한 상태인가.
    /// CoolDown 이 0인 스킬(예: 30003)은 항상 true.
    /// </summary>
    public bool IsCooldownReady => HasSkill && Time.time >= _nextSkillReadyTime;

    /// <summary>
    /// 일반공격 타이밍에 일반공격 대신 스킬을 써야 하는지.
    /// UnitAutoAttack 이 매 공격 슬롯에서 이 값을 보고 분기한다.
    /// </summary>
    public bool ShouldCastInsteadOfNormalAttack()
    {
        // 30003(추가 투사체)은 일반공격 대신이 아니라 일반공격에 따라붙으므로 여기선 false.
        return IsActiveSkill && !IsExtraProjectileSkill && IsCooldownReady;
    }

    private void Awake()
    {
        _statHub = GetComponent<UnitStatHub>();
    }

    private void Start()
    {
        // 웨이브 종료 이벤트 구독 (자가 버프 종료 + 쿨다운 초기화 룰).
        _battleWaveRunner = FindFirstObjectByType<BattleWaveRunner>();
        if (_battleWaveRunner != null)
            _battleWaveRunner.WaveCleared += OnWaveCleared;
    }

    private void OnDestroy()
    {
        if (_battleWaveRunner != null)
            _battleWaveRunner.WaveCleared -= OnWaveCleared;
    }

    private void Update()
    {
        // 비액티브 스킬은 자체 타이머로 발동.
        // 액티브 스킬은 UnitAutoAttack 이 일반공격 슬롯에서 처리하므로 여기선 무시.
        if (!HasSkill || IsActiveSkill)
            return;

        if (!IsCooldownReady)
            return;

        // 현재 비액티브 중 자가 버프(30004/30005)만 발동. 30006(광역 디버프)은 추후.
        if (IsBuffSkill)
        {
            ExecuteBuffSkill();
            StartCooldown();
        }
    }

    /// <summary>
    /// 시트 DamageFormula(% 단위)를 1f 기준 멀티플라이어로 변환.
    /// 1000(=1000%) → 10f, 150 → 1.5f.
    /// </summary>
    public float GetDamageMultiplier()
    {
        if (!HasSkill)
            return 1f;

        return Mathf.Max(0f, _skill.DamageFormula / 100f);
    }

    /// <summary>
    /// 이 스킬의 슬로우 source 식별자.
    /// 같은 스킬 재시전 시 같은 키를 반환 → MonsterMover 가 갱신.
    /// 다른 스킬/시너지 슬로우와 분리되어 max(percent) 로 공존.
    /// </summary>
    public object GetSlowSourceKey()
    {
        if (!HasSkill)
            return null;

        if (_slowSourceKeyCache == null)
            _slowSourceKeyCache = $"Skill_{_skill.Id}";

        return _slowSourceKeyCache;
    }

    /// <summary>
    /// 30003 추가 투사체용 컨텍스트 빌드.
    /// 일반공격은 그대로 진행되고, 이 컨텍스트로 추가 투사체가 SelectTargets 대상자(최대 TargetCount명)에게 1발씩 발사된다.
    /// 추가 투사체는 항상 투사체 형태(ForceProjectile=true)이며 일반 투사체보다 약간 작게(0.7배) 발사.
    /// </summary>
    public AttackContext BuildExtraProjectileContext()
    {
        AttackContext ctx = AttackContext.Skill(GetDamageMultiplier());

        // attackSensor 안의 적 중 최대 TargetCount 명에게 분배 (1명이면 1발, 3명이면 3발).
        int targetCount = Mathf.Max(1, _skill.TargetCount);
        ctx.SkillTargetCountOverride = targetCount;

        // 근접 유닛이라도 추가 투사체는 투사체로 발사.
        ctx.ForceProjectile = true;

        // 일반 투사체보다 살짝 작게.
        ctx.ProjectileScaleMultiplier = 0.7f;

        // 근접 유닛은 자체 투사체 프리팹이 없으므로 SO 의 추가 투사체 프리팹을 컨텍스트로 전달.
        // SO 슬롯이 비어 있으면 null → CombatManager.FireProjectiles 가 유닛 본래 프리팹으로 fallback.
        if (_skill.ExtraProjectilePrefab != null)
            ctx.ProjectilePrefabOverride = _skill.ExtraProjectilePrefab;

        return ctx;
    }

    /// <summary>
    /// 자가 버프 모디파이어의 Hub 키. 같은 스킬 재시전 시 멱등 갱신.
    /// </summary>
    public object GetBuffKey()
    {
        if (!HasSkill)
            return null;

        if (_buffKeyCache == null)
            _buffKeyCache = $"Skill_{_skill.Id}_Buff";

        return _buffKeyCache;
    }

    /// <summary>
    /// 이 스킬의 SO 데이터를 한 번에 AttackContext 로 변환.
    /// 액티브 스킬 분기에서 호출하여 광역 타겟 / 디버프 / 데미지 멀티플라이어를 한꺼번에 채운다.
    /// </summary>
    public AttackContext BuildSkillContext()
    {
        if (!HasSkill)
            return AttackContext.Normal();

        // 데미지 베이스에 따라 컨텍스트 빌드.
        //   AttackPower : 공격력 × DamageMultiplier (기존 30001/30002 등)
        //   MaxHp       : 적 최대 체력 × MaxHpRatio (30007. 방어력/치명타/슬로우보너스 무시)
        AttackContext ctx;
        if (_skill.DamageBase == SkillDamageBaseType.MaxHp)
        {
            ctx = AttackContext.Skill(1f); // DamageMultiplier 미사용
            ctx.DamageBase = SkillDamageBaseType.MaxHp;
            ctx.MaxHpRatio = _skill.DamageFormula; // 시트 값 그대로 (% 단위)
        }
        else
        {
            ctx = AttackContext.Skill(GetDamageMultiplier());
            // ctx.DamageBase 는 default 인 AttackPower
        }

        // 광역 타겟 오버라이드 (1 이하이면 유닛 본래 AttackTargetCount 사용)
        if (_skill.TargetCount > 1)
            ctx.SkillTargetCountOverride = _skill.TargetCount;

        // 슬로우 디버프 (현재는 슬로우만 지원. 추후 디버프 종류 늘면 enum 분기)
        if (_skill.DebuffValue > 0f && _skill.DebuffDuration > 0f)
        {
            ctx.DebuffSlowPercent = _skill.DebuffValue;
            ctx.DebuffDuration = _skill.DebuffDuration;
            ctx.DebuffSourceKey = GetSlowSourceKey();
        }

        return ctx;
    }

    /// <summary>
    /// 자가 버프를 시전자 본인의 UnitStatHub 에 적용한다.
    /// BuffValue 는 시트 % 단위(150 = 150%) → fractional(1.5)로 변환.
    /// PercentAdd 로 누적되어 hub.Get(stat) 이 자동으로 반영된 값을 반환.
    /// 같은 키 재시전 시 멱등 갱신.
    /// </summary>
    private void ExecuteBuffSkill()
    {
        if (_statHub == null)
        {
            DebugTool.Warnning(
                $"[SkillRunner] UnitStatHub 가 없어 자가 버프를 적용하지 못했습니다. skillId={_skill.Id}",
                DebugType.Unit, this);
            return;
        }

        object key = GetBuffKey();
        float fractional = _skill.BuffValue * 0.01f;

        StatModifier mod = new StatModifier(
            _skill.BuffStatType,
            StatOperation.PercentAdd,
            fractional,
            ModifierSource.UnitSkill,
            key);

        _statHub.Remove(key); // 멱등 갱신
        _statHub.Add(mod);

        // 이전 만료 코루틴 진행 중이면 중지 후 재시작
        if (_buffExpireRoutine != null)
            StopCoroutine(_buffExpireRoutine);

        _buffExpireRoutine = StartCoroutine(BuffExpireRoutine(_skill.BuffDuration, key));

        if (_log)
        {
            DebugTool.Log(
                $"[SkillRunner] 자가 버프 발동 | skillId={_skill.Id}, stat={_skill.BuffStatType}, percent={_skill.BuffValue:F1}%, duration={_skill.BuffDuration:F1}s",
                DebugType.Unit, this);
        }
    }

    private IEnumerator BuffExpireRoutine(float duration, object key)
    {
        yield return new WaitForSeconds(duration);

        if (_statHub != null)
            _statHub.Remove(key);

        _buffExpireRoutine = null;

        if (_log)
        {
            DebugTool.Log(
                $"[SkillRunner] 자가 버프 만료 | skillId={(_skill != null ? _skill.Id : -1)}, key={key}",
                DebugType.Unit, this);
        }
    }

    /// <summary>
    /// 웨이브 종료 시 자가 버프 즉시 해제 + 쿨다운 초기화.
    /// 시트 30004 비고 룰을 모든 비액티브 자가 버프에 일관 적용.
    /// </summary>
    private void OnWaveCleared(WaveDataSO _)
    {
        if (!HasSkill)
            return;

        // 액티브 스킬은 자가 버프 흐름과 무관하므로 영향 없음.
        if (IsActiveSkill)
            return;

        bool removedBuff = false;

        if (_buffExpireRoutine != null)
        {
            StopCoroutine(_buffExpireRoutine);
            _buffExpireRoutine = null;
        }

        if (_statHub != null && IsBuffSkill)
        {
            removedBuff = _statHub.Remove(GetBuffKey());
        }

        ResetCooldown();

        if (_log)
        {
            DebugTool.Log(
                $"[SkillRunner] 웨이브 종료 → 자가 버프 종료 + 쿨다운 초기화 | skillId={_skill.Id}, buffRemoved={removedBuff}",
                DebugType.Unit, this);
        }
    }

    /// <summary>
    /// 스킬 발동 직후 쿨다운 타이머를 시작한다.
    /// CoolDown 이 0이면 쿨다운 없음 (다음 슬롯에 즉시 재발동 가능).
    /// </summary>
    public void StartCooldown()
    {
        if (!HasSkill)
            return;

        _nextSkillReadyTime = Time.time + _skill.CoolDown;

        if (_log)
        {
            DebugTool.Log(
                $"[SkillRunner] 쿨다운 시작 | skillId={_skill.Id}, cooldown={_skill.CoolDown:F2}s, nextReady={_nextSkillReadyTime:F2}",
                DebugType.Unit, this);
        }
    }

    /// <summary>
    /// 외부에서 강제로 쿨다운 초기화.
    /// (예: 시트 비고의 "타겟 사망시 쿨타임 초기화", "웨이브 종료시 쿨타임 초기화")
    /// </summary>
    public void ResetCooldown()
    {
        _nextSkillReadyTime = 0f;

        if (_log && HasSkill)
        {
            DebugTool.Log(
                $"[SkillRunner] 쿨다운 초기화 | skillId={_skill.Id}",
                DebugType.Unit, this);
        }
    }
}
