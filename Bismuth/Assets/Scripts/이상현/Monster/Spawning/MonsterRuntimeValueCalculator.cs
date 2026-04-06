using UnityEngine;

/// <summary>
/// 몬스터/웨이브/난이도 계산식을 한 곳에 모아두는 런타임 계산기
/// SO 원본 데이터는 그대로 두고, 실제 전투에 사용할 최종값만 계산한다.
/// </summary>
public static class MonsterRuntimeValueCalculator
{
    /// <summary>
    /// 현재 전투 문맥에서 몬스터 1마리의 시작 런타임 값을 조립한다.
    /// </summary>
    public static MonsterRuntimeValues BuildRuntimeValues(
        MonsterDataSO monsterData,
        WaveDataSO waveData,
        DifficultyModifierEntry difficultyModifier,
        float homeBaseMaxHp)
    {
        return new MonsterRuntimeValues
        {
            MonsterData = monsterData,
            CurrentHp = CalculateCurrentHp(monsterData, waveData.WaveNumber, difficultyModifier),
            Defense = CalculateDefense(monsterData, waveData.WaveNumber),
            DamageToBase = CalculateDamageToBase(monsterData, difficultyModifier, homeBaseMaxHp),
            KillReward = CalculateKillReward(monsterData),
            MoveSpeed = monsterData.MoveSpeed
        };
    }

    /// <summary>
    /// 일반/보스 성장 기준 웨이브와 적 체력 계수를 반영한 시작 체력
    /// 소수점은 마지막에 반올림한다.
    /// </summary>
    public static float CalculateCurrentHp(
        MonsterDataSO monsterData,
        int waveNumber,
        DifficultyModifierEntry difficultyModifier)
    {
        int growthWave = GetGrowthWave(monsterData.Category, waveNumber);

        float finalHp =
            (monsterData.HpGrowth * growthWave + monsterData.BaseHp)
            * difficultyModifier.EnemyHpMultiplier;

        return Mathf.RoundToInt(finalHp);
    }

    /// <summary>
    /// 일반/보스 성장 기준 웨이브를 반영한 최종 방어력
    /// 방어력은 난이도 계수를 적용하지 않는다.
    /// </summary>
    public static float CalculateDefense(MonsterDataSO monsterData, int waveNumber)
    {
        int growthWave = GetGrowthWave(monsterData.Category, waveNumber);

        return monsterData.DefenseGrowth * growthWave * growthWave + monsterData.BaseDefense;
    }

    /// <summary>
    /// 일반 몬스터는 고정 기지 피해 × 적 유닛 피해 계수,
    /// 보스 몬스터는 기지 최대 체력의 퍼센트 피해 × 적 유닛 피해 계수로 계산한다.
    /// 일반 유닛은 최종 계산 후 반올림한다.
    /// </summary>
    public static int CalculateDamageToBase(
        MonsterDataSO monsterData,
        DifficultyModifierEntry difficultyModifier,
        float homeBaseMaxHp)
    {
        if (monsterData.Category == MonsterCategory.Boss)
        {
            float finalDamage =
                homeBaseMaxHp
                * (monsterData.BaseDamageToBase / 100f)
                * difficultyModifier.EnemyToBaseDamageMultiplier;

            return Mathf.RoundToInt(finalDamage);
        }

        float normalDamage =
            monsterData.BaseDamageToBase * difficultyModifier.EnemyToBaseDamageMultiplier;

        return Mathf.RoundToInt(normalDamage);
    }

    /// <summary>
    /// 처치 보상은 난이도와 무관하게 분류 기준 고정값을 사용한다.
    /// 일반 = 1, 보스 = 20
    /// </summary>
    public static int CalculateKillReward(MonsterDataSO monsterData)
    {
        return monsterData.Category == MonsterCategory.Boss ? 20 : 1;
    }

    /// <summary>
    /// 시작 기지 체력 = 기본 기지 체력 × 아군 기지 체력 계수
    /// </summary>
    public static int CalculateStartingHomeBaseHp(
        int defaultBaseHp,
        DifficultyModifierEntry difficultyModifier)
    {
        return Mathf.RoundToInt(defaultBaseHp * difficultyModifier.TeamBaseHpMultiplier);
    }

    /// <summary>
    /// 웨이브 클리어 보상 계산
    /// 기본 보상 = (50 + W × 50) + P × 2
    /// 최종 보상 = 기본 보상 × 난이도 재화 계수
    /// </summary>
    public static int CalculateWaveClearReward(
        int waveNumber,
        DifficultyModifierEntry difficultyModifier)
    {
        int w = Mathf.FloorToInt((waveNumber - 1) / 10f);
        int p = (waveNumber - 1) % 10;

        int baseReward = (50 + w * 50) + p * 2;
        float finalReward = baseReward * difficultyModifier.ResourceGainMultiplier;

        return Mathf.RoundToInt(finalReward);
    }

    /// <summary>
    /// 일반은 1웨이브 기준, 보스는 5웨이브 기준으로 성장한다.
    /// </summary>
    private static int GetGrowthWave(MonsterCategory category, int waveNumber)
    {
        return category == MonsterCategory.Boss
            ? waveNumber - 5
            : waveNumber - 1;
    }
}