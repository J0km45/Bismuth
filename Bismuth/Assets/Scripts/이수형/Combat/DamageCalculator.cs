using UnityEngine;

public class DamageCalculator : MonoBehaviour
{
    private const int WarriorSynergyId = (int)SynergyManager.SynergyType.Warrior;

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
        float calculatedDamage = damageDealt * warriorMultiplier * (1f + crit) * (100f / (defense + 100f));

        if (Random.value < calculatedDamage - (int)calculatedDamage)
            return (int)calculatedDamage + 1;

        return (int)calculatedDamage;
    }

    public int CalculateSkillDamage(float damageDealt, float defense, float crit)
    {
        return CalculateSkillDamage(null, damageDealt, defense, crit);
    }

    public int CalculateSkillDamage(UnitStat attackerStat, float damageDealt, float defense, float crit)
    {
        // 현재 스킬 대미지는 미구현 상태 유지
        return 0;
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

            // 전사 시너지는 첫 번째 effect value를 공격력 배율 보너스로 사용
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

    private void WarnMissingSynergyManager()
    {
        if (warnedMissingSynergyManager)
            return;

        warnedMissingSynergyManager = true;
        DebugTool.Warnning("SynergyManager를 찾지 못해 전사 공격력 배율을 적용하지 않습니다.", DebugType.Synergy, this);
    }

    private void WarnMissingSynergySo()
    {
        if (warnedMissingSynergySo)
            return;

        warnedMissingSynergySo = true;
        DebugTool.Warnning("SynergySO 참조가 없어 전사 공격력 배율을 적용하지 않습니다.", DebugType.Synergy, this);
    }
}