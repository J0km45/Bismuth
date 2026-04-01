using UnityEngine;

public struct AttackPlaybackData
{
    public bool Success;
    public int ClipIndex;
    public float ClipLength;
    public float AnimatorSpeed;
    public float ActualDuration;
}

[DisallowMultipleComponent]
public class AnimationController : MonoBehaviour
{
    [Header("Attack Animation")]
    [SerializeField, Range(0.1f, 1f)] private float attackAnimationOccupancy = 0.9f;
    [SerializeField, Min(0.01f)] private float minAttackAnimationDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float defaultAnimatorSpeed = 1f;
    [SerializeField] private string attackStateName = "ATTACK";

    [Header("Debug")]
    [SerializeField] private bool animationLog = false;

    private SPUM_Prefabs spum;
    private Animator cachedAnimator;
    private int attackStateShortHash;

    public Animator Animator => cachedAnimator;
    public bool IsInitialized => spum != null && cachedAnimator != null;

    public void Initialize()
    {
        DebugTool.Log("AnimationController 초기화 시작", DebugType.Summon, this);

        spum = GetComponent<SPUM_Prefabs>();
        if (spum == null)
            spum = GetComponentInChildren<SPUM_Prefabs>();

        if (spum == null)
        {
            DebugTool.Error("SPUM_Prefabs를 찾지 못했습니다.", DebugType.Unit, this);
            return;
        }

        if (spum._anim == null)
            spum._anim = GetComponentInChildren<Animator>();

        cachedAnimator = spum._anim;

        if (cachedAnimator == null)
        {
            DebugTool.Error("Animator를 찾지 못했습니다.", DebugType.Unit, this);
            return;
        }

        if (spum.ATTACK_List == null || spum.ATTACK_List.Count == 0)
            spum.PopulateAnimationLists();

        spum.OverrideControllerInit();
        attackStateShortHash = Animator.StringToHash(attackStateName);

        ResetAnimatorSpeed();
    }

    private bool EnsureInitialized()
    {
        if (!IsInitialized)
            Initialize();

        return IsInitialized;
    }

    public AttackPlaybackData PlayAttackAnimation(int index, float attackSpeedPerSecond)
    {
        AttackPlaybackData result = new AttackPlaybackData
        {
            Success = false,
            ClipIndex = index,
            ClipLength = 0f,
            AnimatorSpeed = defaultAnimatorSpeed,
            ActualDuration = 0f
        };

        if (!EnsureInitialized())
            return result;

        if (spum.ATTACK_List == null || spum.ATTACK_List.Count == 0)
        {
            DebugTool.Warnning("ATTACK 애니메이션 리스트가 비어 있습니다.", DebugType.Unit, this);
            return result;
        }

        index = Mathf.Clamp(index, 0, spum.ATTACK_List.Count - 1);

        AnimationClip clip = spum.ATTACK_List[index];
        if (clip == null)
        {
            DebugTool.Warnning($"ATTACK 애니메이션 클립이 null 입니다. index={index}", DebugType.Unit, this);
            return result;
        }

        float attackInterval = 1f / Mathf.Max(0.01f, attackSpeedPerSecond);
        float targetAnimDuration = Mathf.Min(
            clip.length,
            Mathf.Max(minAttackAnimationDuration, attackInterval * attackAnimationOccupancy)
        );

        float animatorSpeed = Mathf.Max(0.01f, clip.length / targetAnimDuration);

        cachedAnimator.speed = animatorSpeed;
        spum.PlayAnimation(PlayerState.ATTACK, index);

        result.Success = true;
        result.ClipIndex = index;
        result.ClipLength = clip.length;
        result.AnimatorSpeed = animatorSpeed;
        result.ActualDuration = clip.length / animatorSpeed;

        if (animationLog)
        {
            DebugTool.Log(
                $"공격 애니메이션 재생 | index={index}, clip={clip.name}, clipLength={clip.length:F3}, animSpeed={animatorSpeed:F2}, actualDuration={result.ActualDuration:F3}",
                DebugType.Unit,
                this
            );
        }

        return result;
    }

    public bool IsAttackStatePlaying()
    {
        if (!EnsureInitialized())
            return false;

        AnimatorStateInfo stateInfo = cachedAnimator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.shortNameHash == attackStateShortHash;
    }

    public float GetCurrentAttackNormalizedTime()
    {
        if (!EnsureInitialized())
            return 0f;

        AnimatorStateInfo stateInfo = cachedAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.shortNameHash != attackStateShortHash)
            return 0f;

        return stateInfo.normalizedTime;
    }

    public void ResetAnimatorSpeed()
    {
        if (cachedAnimator != null)
            cachedAnimator.speed = defaultAnimatorSpeed;
    }
}