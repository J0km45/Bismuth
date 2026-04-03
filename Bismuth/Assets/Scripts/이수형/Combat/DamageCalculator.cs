using UnityEngine;

public class DamageCalculator : MonoBehaviour
{
    private const int WarriorSynergyId = (int)SynergyManager.SynergyType.Warrior;
    private const int WizardSynergyId = (int)SynergyManager.SynergyType.Magician;
    private const int ArcherSynergyId = (int)SynergyManager.SynergyType.Archer;
    private const int FighterSynergyId = (int)SynergyManager.SynergyType.Fighter;
    private const int ElfSynergyId = (int)SynergyManager.SynergyType.Elf;

    [Header("Synergy")]
    [SerializeField] private SynergySO synergySO;
    [SerializeField] private SynergyManager synergyManager;
    [SerializeField] private bool synergyLog = false;

    private bool warnedMissingSynergySo;
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
        float warriorMultiplier = 1 + GetWarriorAttackMultiplier(attackerStat) * 0.01f;
        float elfMultiplier = 1 + GetElfAttackMultiplier(attackerStat) * 0.01f;
        float calculatedDamage = damageDealt * warriorMultiplier * elfMultiplier * (1f + crit) * (100f / (defense + 100f));

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

        if (synergySO == null)
        {
            WarnMissingSynergySo();
            return 0f;
        }

        SynergyData archerData = GetSynergyData(ArcherSynergyId);
        if (archerData == null || archerData.Levels == null || archerData.Levels.Count == 0)
            return 0f;

        int activeCount = synergyManager.GetSynergyLevel(ArcherSynergyId);
        float bonus = GetMatchedBonus(archerData, activeCount);

        if (synergyLog && bonus > 0f)
        {
            DebugTool.Log(
                $"궁수 최대 체력 비례 대미지 비율 적용 | unit={attackerStat.Name}, active={activeCount}, bonus={bonus:F2}",
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

        if (synergySO == null)
        {
            WarnMissingSynergySo();
            return 0f;
        }

        SynergyData wizardData = GetSynergyData(WizardSynergyId);
        if (wizardData == null || wizardData.Levels == null || wizardData.Levels.Count == 0)
            return 0f;

        int activeCount = synergyManager.GetSynergyLevel(WizardSynergyId);
        float bonus = GetMatchedBonus(wizardData, activeCount);

        if (synergyLog && bonus > 0f)
        {
            DebugTool.Log(
                $"마법사 추가 대미지 비율 적용 | unit={attackerStat.Name}, active={activeCount}, bonus={bonus:F2}",
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

        if (synergySO == null)
        {
            WarnMissingSynergySo();
            return 1f;
        }

        SynergyData warriorData = GetSynergyData(WarriorSynergyId);
        if (warriorData == null || warriorData.Levels == null || warriorData.Levels.Count == 0)
            return 1f;

        int activeCount = synergyManager.GetSynergyLevel(WarriorSynergyId);
        float bonus = GetMatchedBonus(warriorData, activeCount);

        if (synergyLog && bonus > 0f)
        {
            DebugTool.Log(
                $"전사 공격력 배율 적용 | unit={attackerStat.Name}, active={activeCount}, bonus={bonus:F2}, multiplier={1f + bonus:F2}",
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

        if (synergySO == null)
        {
            WarnMissingSynergySo();
            return 0f;
        }

        SynergyData elfData = GetSynergyData(ElfSynergyId);
        if (elfData == null || elfData.Levels == null || elfData.Levels.Count == 0)
            return 0f;

        int activeCount = synergyManager.GetSynergyLevel(ElfSynergyId);
        float bonusPerKill = GetMatchedBonus(elfData, activeCount);

        if (bonusPerKill <= 0f)
            return 0f;

        float totalBonus = bonusPerKill * attackerStat.ElfWaveKillCount;

        if (synergyLog && totalBonus > 0f)
        {
            DebugTool.Log(
                $"엘프 공격력 배율 적용 | unit={attackerStat.Name}, active={activeCount}, bonusPerKill={bonusPerKill:F2}, kills={attackerStat.ElfWaveKillCount}, total={totalBonus:F2}",
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

    public float GetFinalCritChance(UnitStat attackerStat, float baseCritChance)
    {
        float fighterBonus = GetFighterCritChanceBonus(attackerStat);
        float finalCritChance = Mathf.Clamp01(baseCritChance + fighterBonus);

        if (synergyLog && fighterBonus > 0f)
        {
            DebugTool.Log(
                $"격투가 치명타 확률 적용 | unit={attackerStat?.Name ?? "None"}, baseCrit={baseCritChance:F3}, fighterBonus={fighterBonus:F3}, finalCrit={finalCritChance:F3}",
                DebugType.Synergy,
                this
            );
        }

        return finalCritChance;
    }

    private float GetFighterCritChanceBonus(UnitStat attackerStat)
    {
        if (!HasSynergyTag(attackerStat, FighterSynergyId))
            return 0f;

        TryResolveSynergyManager();

        if (synergyManager == null)
        {
            WarnMissingSynergyManager();
            return 0f;
        }

        if (synergySO == null)
        {
            WarnMissingSynergySo();
            return 0f;
        }

        SynergyData fighterData = GetSynergyData(FighterSynergyId);
        if (fighterData == null || fighterData.Levels == null || fighterData.Levels.Count == 0)
            return 0f;

        int activeCount = synergyManager.GetSynergyLevel(FighterSynergyId);
        float bonus = GetMatchedBonus(fighterData, activeCount);


        if (bonus > 1f)
            bonus *= 0.01f;

        if (synergyLog && bonus > 0f)
        {
            DebugTool.Log(
                $"격투가 치명타 보너스 조회 | unit={attackerStat.Name}, active={activeCount}, bonus={bonus:F3}",
                DebugType.Synergy,
                this
            );
        }

        return bonus;
    }

    private void WarnMissingSynergyManager()
    {
        if (warnedMissingSynergyManager)
            return;

        warnedMissingSynergyManager = true;
        DebugTool.Warnning("SynergyManager를 찾지 못해 시너지 보너스를 적용하지 않습니다.", DebugType.Synergy, this);
    }

    private void WarnMissingSynergySo()
    {
        if (warnedMissingSynergySo)
            return;

        warnedMissingSynergySo = true;
        DebugTool.Warnning("SynergySO 참조가 없어 시너지 보너스를 적용하지 않습니다.", DebugType.Synergy, this);
    }

}