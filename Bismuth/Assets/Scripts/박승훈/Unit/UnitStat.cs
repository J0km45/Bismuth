using System;
using Unity.VisualScripting;
using UnityEngine;

public class UnitStat : MonoBehaviour
{
    public int Id;
    public int Tier;
    public int Level;
    public int SummonTime;
    public string Name;
    public float BaseAttackPower;
    public float CurrentAttackPower;
    public float AttackSpeed;
    public float CritChance;
    public float Range;
    public float AttackArea;
    public UnitData.AttackTypes attackTypes;
    public int AttackTargetCount;
    public int[] SynergIDs;
    public int KillCount;
    public int DealtDamage;

    public int ElfWaveKillCount;
}
