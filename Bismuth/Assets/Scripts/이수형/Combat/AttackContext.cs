/// <summary>
/// 공격 한 번의 맥락을 담는 구조체.
/// bool 파라미터 나열 대신 이 구조체 하나를 전달한다.
/// </summary>
public struct AttackContext
{
    /// <summary> 일반 공격(평타)인지 여부. false면 시너지 추가타 등 스킬 공격이다. </summary>
    public bool IsNormalAttack;

    /// <summary> 전사 시너지 추가타 여부 </summary>
    public bool IsWarriorBonus;

    /// <summary> 마법사 시너지 추가 대미지 여부 </summary>
    public bool IsWizardBonus;

    /// <summary> 궁수 시너지 강화 공격 여부 </summary>
    public bool IsArcherBonus;

    /// <summary> 수인 시너지 추가타 여부 (Step 3에서 사용) </summary>
    public bool IsFurryBonus;

    /// <summary> 애니메이션 배속 배율. 기본=1, 수인 추가타=N </summary>
    public float AnimSpeedMultiplier;

    /// <summary> 기본 일반공격 컨텍스트를 생성한다. </summary>
    public static AttackContext Normal()
    {
        return new AttackContext { IsNormalAttack = true, AnimSpeedMultiplier = 1f };
    }

    /// <summary> 시너지 스택 적립 대상인지 판별한다. 일반공격만 적립 대상이다. </summary>
    public bool CountsForSynergyStacks => IsNormalAttack && !IsWarriorBonus && !IsFurryBonus;

    public override string ToString()
    {
        return $"normal={IsNormalAttack}, warrior={IsWarriorBonus}, wizard={IsWizardBonus}, archer={IsArcherBonus}, furry={IsFurryBonus}, animSpeed={AnimSpeedMultiplier:F1}";
    }
}

/// <summary>
/// 추가 공격의 종류를 구분하는 열거형.
/// </summary>
public enum ExtraAttackType
{
    Warrior,
    Furry
}

/// <summary>
/// 대기 중인 추가 공격 배치 하나를 표현한다.
/// 큐에 넣어 순서대로 처리한다.
/// </summary>
public struct PendingExtraAttack
{
    /// <summary> 추가 공격 종류 </summary>
    public ExtraAttackType Type;

    /// <summary> 이 배치에서 남은 공격 횟수 </summary>
    public int RemainingCount;

    /// <summary> 강제 타겟. null이면 센서에서 가장 가까운 적을 사용한다. </summary>
    public MonsterController ForcedTarget;

    /// <summary> 애니메이션 배속 배율. 전사=1, 수인=추가타 횟수 </summary>
    public float AnimSpeedMultiplier;

    /// <summary> 이 배치가 아직 처리할 공격이 남아있는지 </summary>
    public bool HasRemaining => RemainingCount > 0;

    public override string ToString()
    {
        string targetName = ForcedTarget != null ? ForcedTarget.name : "Sensor";
        return $"type={Type}, remaining={RemainingCount}, target={targetName}, animSpeed={AnimSpeedMultiplier:F1}";
    }
}
