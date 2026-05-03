

public struct AttackContext
{

    public bool IsNormalAttack;


    public bool IsWarriorBonus;


    public bool IsWizardBonus;


    public bool IsArcherBonus;


    public bool IsFurryBonus;


    public float AnimSpeedMultiplier;


    public static AttackContext Normal()
    {
        return new AttackContext { IsNormalAttack = true, AnimSpeedMultiplier = 1f };
    }


    public bool CountsForSynergyStacks => IsNormalAttack && !IsWarriorBonus && !IsFurryBonus;

    public override string ToString()
    {
        return $"normal={IsNormalAttack}, warrior={IsWarriorBonus}, wizard={IsWizardBonus}, archer={IsArcherBonus}, furry={IsFurryBonus}, animSpeed={AnimSpeedMultiplier:F1}";
    }
}




public enum ExtraAttackType
{
    Warrior,
    Furry
}




public struct PendingExtraAttack
{

    public ExtraAttackType Type;


    public int RemainingCount;


    public MonsterController ForcedTarget;


    public float AnimSpeedMultiplier;


    public bool HasRemaining => RemainingCount > 0;

    public override string ToString()
    {
        string targetName = ForcedTarget != null ? ForcedTarget.name : "Sensor";
        return $"type={Type}, remaining={RemainingCount}, target={targetName}, animSpeed={AnimSpeedMultiplier:F1}";
    }
}
