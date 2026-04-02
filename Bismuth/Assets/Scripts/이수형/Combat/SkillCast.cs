using UnityEngine;

public class SkillCast : MonoBehaviour
{
    [SerializeField] private UnitAutoAttack unitAutoAttack;

    private void Awake()
    {
        if (unitAutoAttack == null)
            unitAutoAttack = GetComponent<UnitAutoAttack>();
    }

    public void SynergyWarriorCast()
    {
        if (unitAutoAttack == null)
            unitAutoAttack = GetComponent<UnitAutoAttack>();

        if (unitAutoAttack == null)
        {
            DebugTool.Warnning("UnitAutoAttack 참조가 없어 전사 추가 공격을 요청하지 못했습니다.", DebugType.Synergy, this);
            return;
        }

        unitAutoAttack.RequestWarriorExtraAttack();
        DebugTool.Log("전사 시너지 발동! 즉시 추가 공격을 요청했습니다.", DebugType.Synergy, this);
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