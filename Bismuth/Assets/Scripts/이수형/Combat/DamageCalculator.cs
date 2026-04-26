using UnityEngine;

public class DamageCalculator : MonoBehaviour
{
    private const int WarriorSynergyId = (int)SynergyManager.SynergyType.Warrior;
    private const int WizardSynergyId = (int)SynergyManager.SynergyType.Magician;
    private const int ArcherSynergyId = (int)SynergyManager.SynergyType.Archer;
    private const int ElfSynergyId = (int)SynergyManager.SynergyType.Elf;

    [Header("Synergy")]
    [SerializeField] private SynergyManager synergyManager;
    [SerializeField] private bool synergyLog = false;

    private bool warnedMissingSynergyManager;

    private void Awake()
    {
        TryResolveSynergyManager();
    }

    public int CalculateNormalDamage(float damageDealt, float defense, float crit)
    {
        return CalculateNormalDamage(null, damageDealt, defense, crit);
    }

    public int CalculateNormalDamage(UnitStat attackerStat, float damageDealt, float defense, float crit)
    {
        // Orc 시너지 보너스는 이제 Hub.AttackPower 안에 이미 포함되어 들어옴.
        // Warrior / Elf 는 아직 Hub 화 전이라 곱셈 멀티플라이어로 유지.
        float warriorMultiplier = GetWarriorAttackMultiplier(attackerStat) * 0.01f;
        float elfMultiplier = GetElfAttackMultiplier(attackerStat) * 0.01f;
        float calculatedDamage = damageDealt * (warriorMultiplier + elfMultiplier + 1f) * (1f + crit) * (100f / (defense + 100f));

        if (Random.value < calculatedDamage - (int)calculatedDamage)
            return (int)calculatedDamage + 1;

        return (int)calculatedDamage;
    }

    public int CalculateSkillDamage(float damageDealt, float defense, float crit)
    {
        return CalculateSkillDamage(null, damageDealt, defense, crit, false);
    }

    public int CalculateSkillDamage(UnitStat attackerStat, float damageDealt, float defense, float crit)
    {
        return CalculateSkillDamage(attackerStat, damageDealt, defense, crit, false);
    }

    public int CalculateSkillDamage(UnitStat attackerStat, float damageDealt, float defense, float crit, bool isWizardBonusAttack)
    {
        if (!isWizardBonusAttack)
            return 0;

        float wizardBonusPercent = GetWizardDamageBonusPercent(attackerStat);
        if (wizardBonusPercent <= 0f)
            return 0;

        float calculatedDamage = damageDealt * (wizardBonusPercent * 0.01f) * (1f + crit) * (100f / (defense + 100f));

        if (synergyLog)
        {
            DebugTool.Log(
                $"마법사 추가 대미지 적용 | unit={attackerStat?.Name ?? "None"}, bonusPercent={wizardBonusPercent:F2}, raw={damageDealt:F2}, result={calculatedDamage:F2}",
                DebugType.Synergy,
                this
            );
        }

        if (Random.value < calculatedDamage - (int)calculatedDamage)
            return (int)calculatedDamage + 1;

        return (int)calculatedDamage;
    }

    public int CalculateArcherSkillDamage(UnitStat attackerStat, MonsterController target, bool isArcherBonusAttack)
    {
        if (!isArcherBonusAttack)
            return 0;

        if (target == null)
            return 0;

        float archerBonusPercent = GetArcherDamageBonusPercent(attackerStat);
        if (archerBonusPercent <= 0f)
            return 0;

        float calculatedDamage = target.MaxHp * (archerBonusPercent * 0.01f);

        if (synergyLog)
        {
            DebugTool.Log(
                $"궁수 최대 체력 비례 대미지 적용 | unit={attackerStat?.Name ?? "None"}, target={target.name}, targetMaxHp={target.MaxHp:F2}, bonusPercent={archerBonusPercent:F2}, result={calculatedDamage:F2}",
                DebugType.Synergy,
                this
            );
        }

        if (Random.value < calculatedDamage - (int)calculatedDamage)
            return (int)calculatedDamage + 1;

        return (int)calculatedDamage;
    }

    private float GetArcherDamageBonusPercent(UnitStat attackerStat)
    {
        if (!HasSynergyTag(attackerStat, ArcherSynergyId))
            return 0f;

        TryResolveSynergyManager();

        if (synergyManager == null)
        {
            WarnMissingSynergyManager();
            return 0f;
        }

        float bonus = synergyManager.GetEffectValue(ArcherSynergyId);

        if (synergyLog && bonus > 0f)
        {
            DebugTool.Log(
                $"궁수 최대 체력 비례 대미지 비율 적용 | unit={attackerStat.Name}, active={synergyManager.GetSynergyLevel(ArcherSynergyId)}, bonus={bonus:F2}",
                DebugType.Synergy,
                this
            );
        }

        return bonus;
    }


    private float GetWizardDamageBonusPercent(UnitStat attackerStat)
    {
        if (!HasSynergyTag(attackerStat, WizardSynergyId))
            return 0f;

        TryResolveSynergyManager();

        if (synergyManager == null)
        {
            WarnMissingSynergyManager();
            return 0f;
        }

        float bonus = synergyManager.GetEffectValue(WizardSynergyId);

        if (synergyLog && bonus > 0f)
        {
            DebugTool.Log(
                $"마법사 추가 대미지 비율 적용 | unit={attackerStat.Name}, active={synergyManager.GetSynergyLevel(WizardSynergyId)}, bonus={bonus:F2}",
                DebugType.Synergy,
                this
            );
        }

        return bonus;
    }

    private float GetWarriorAttackMultiplier(UnitStat attackerStat)
    {
        if (!HasSynergyTag(attackerStat, WarriorSynergyId))
            return 1f;

        TryResolveSynergyManager();

        if (synergyManager == null)
        {
            WarnMissingSynergyManager();
            return 1f;
        }

        // ID 하나만 넘기면 현재 활성 수에 맞는 효과값이 바로 나옴 (없으면 0)
        float bonus = synergyManager.GetEffectValue(WarriorSynergyId);

        if (synergyLog && bonus > 0f)
        {
            DebugTool.Log(
                $"전사 공격력 배율 적용 | unit={attackerStat.Name}, active={synergyManager.GetSynergyLevel(WarriorSynergyId)}, bonus={bonus:F2}, multiplier={1f + bonus:F2}",
                DebugType.Synergy,
                this
            );
        }

        return 1f + bonus;
    }

    private float GetElfAttackMultiplier(UnitStat attackerStat)
    {
        if (!HasSynergyTag(attackerStat, ElfSynergyId))
            return 0f;

        if (attackerStat.ElfWaveKillCount <= 0)
            return 0f;

        TryResolveSynergyManager();

        if (synergyManager == null)
        {
            WarnMissingSynergyManager();
            return 0f;
        }

        float bonusPerKill = synergyManager.GetEffectValue(ElfSynergyId);

        if (bonusPerKill <= 0f)
            return 0f;

        float totalBonus = bonusPerKill * attackerStat.ElfWaveKillCount;

        if (synergyLog && totalBonus > 0f)
        {
            DebugTool.Log(
                $"엘프 공격력 배율 적용 | unit={attackerStat.Name}, active={synergyManager.GetSynergyLevel(ElfSynergyId)}, bonusPerKill={bonusPerKill:F2}, kills={attackerStat.ElfWaveKillCount}, total={totalBonus:F2}",
                DebugType.Synergy,
                this
            );
        }

        return totalBonus;
    }

    private void TryResolveSynergyManager()
    {
        if (synergyManager != null)
            return;

        synergyManager = FindAnyObjectByType<SynergyManager>();
    }

    private bool HasSynergyTag(UnitStat attackerStat, int synergyId)
    {
        if (attackerStat == null || attackerStat.SynergIDs == null)
            return false;

        for (int i = 0; i < attackerStat.SynergIDs.Length; i++)
        {
            if (attackerStat.SynergIDs[i] == synergyId)
                return true;
        }

        return false;
    }

    private void WarnMissingSynergyManager()
    {
        if (warnedMissingSynergyManager)
            return;

        warnedMissingSynergyManager = true;
        DebugTool.Warnning("SynergyManager를 찾지 못해 시너지 보너스를 적용하지 않습니다.", DebugType.Synergy, this);
    }

}