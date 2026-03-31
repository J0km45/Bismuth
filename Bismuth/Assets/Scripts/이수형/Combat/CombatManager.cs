using System.Collections.Generic;
using Unity.Android.Gradle.Manifest;
using UnityEditor.ShaderGraph;
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

        SynergyManager synergyManager = gameManager.GetComponent<SynergyManager>();
    }

    public bool DamageOccured(TowerUnit towerUnit, MonsterController currentTarget)
    {
        if (towerUnit == null || currentTarget == null)
        {
            DebugTool.Log("타워 유닛 또는 현재 타겟 몬스터가 할당되지 않았습니다.", DebugType.Unit, this);
            return false;
        }
        UnitStat unitStat = towerUnit.GetComponent<UnitStat>();
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
        float attackPower = unitStat.CurrentAttackPower;

        float critChance = unitStat.CritChance;
        float crit = (Random.value < critChance) ? 0.5f : 0f;
        int normalDamage = damageCalculator.CalculateNormalDamage(attackPower, currentTarget.BaseDefense, crit);
        int skillDamage = damageCalculator.CalculateSkillDamage(attackPower, currentTarget.BaseDefense, crit);




        // damageCalculator.CalculateSkillDamge

        int finalDamage = normalDamage + skillDamage;
        int unitSynergy = unitStat.SynergIDs[1]; 
        
        int unitTier = unitStat.Tier;
        GameObject hitEffect = null;

        int synergyLevel = gameManager.GetComponent<SynergyManager>().GetSynergyLevel(unitSynergy);
        if (unitTier < 3)
        {
            hitEffect = hitEffectPrefabs[(unitSynergy - 50006) + unitTier * 5];
        }



        currentTarget.TakeDamage(finalDamage,hitEffect);

        DebugTool.Log($"타워 유닛이 몬스터(방어력: {currentTarget.BaseDefense})에게 피해를 입혔습니다. 최종피해량: {finalDamage} (일반: {normalDamage}, 크리티컬: {(crit > 0 ? "Yes" : "No")})", DebugType.Unit, this);
        return true;
    }

    



}
