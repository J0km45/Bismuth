using UnityEngine;

public class DamageCalculator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    

    


    public int CalculateNormalDamage(float damageDealt, float defense, float crit)
    {
        int finalDamage = 0;
        float calculatedDamage = damageDealt * (1f + crit) * (100f / (defense + 100f));

        float probability = calculatedDamage - (float)(int)calculatedDamage;

        if (Random.value < calculatedDamage - (float)(int)calculatedDamage)
        {
            finalDamage = (int)calculatedDamage + 1;
        }
        else
        {
            finalDamage = (int)calculatedDamage;
        }

        return finalDamage;
    }
    public int CalculateSkillDamage(float damageDealt, float defense, float crit)
    {
        //int finalDamage = 0;
        //float calculatedDamage = damageDealt * (1 + crit) * (100 / defense + 100) * 1.5f;
        //float probability = calculatedDamage - (float)(int)calculatedDamage;
        //if (Random.value < calculatedDamage - (float)(int)calculatedDamage)
        //{
        //    finalDamage = (int)calculatedDamage + 1;
        //}
        //else
        //{
        //    finalDamage = (int)calculatedDamage;
        //}
        return 0;
    }
}
