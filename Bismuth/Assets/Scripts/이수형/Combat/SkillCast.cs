using UnityEngine;

public class SkillCast : MonoBehaviour
{
    [SerializeField] private UnitAutoAttack unitAutoAttack;
    private void Awake()
    {
        unitAutoAttack = GetComponent<UnitAutoAttack>();
    }
    public void SynergyWarriorCast()
    {
        unitAutoAttack.ResetAttackInterval();
        DebugTool.Log("전사 시너지 발동! 공격 간격 초기화", DebugType.Synergy, this);
    }
    public void SynergyWizardCast()
    {

    }

    public void SynergyArcherCast()
    {
    }
    public void SynergyGunnerCast()
    {
    }

    public void SynergyFighterCast()
    {
    }

    public void SynergyHumanCast()
    {

    }

    public void SynergyElfCast()
    {

    }

    public void SynergyOrcCast()
    {

    }

    public void SynergyFurryCast()
    {

    }

    public void SynergyFairyCast()
    {

    }
}
