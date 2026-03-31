using Unity.VisualScripting;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private SPUM_Prefabs spum;
    public void Initialize()
    {
        DebugTool.Log("AnimationController 초기화 시작",DebugType.Summon, this);
        spum = GetComponent<SPUM_Prefabs>();
        if (spum == null)
            spum = GetComponentInChildren<SPUM_Prefabs>();

        if (spum == null)
        {
            Debug.LogError("SPUM_Prefabs를 찾지 못했습니다.");
            return;
        }

        if (spum._anim == null)
            spum._anim = GetComponentInChildren<Animator>();

        // 이미 prefab에 리스트가 들어있지만, 비어 있을 가능성에 대비
        if (spum.ATTACK_List == null || spum.ATTACK_List.Count == 0)
            spum.PopulateAnimationLists();

        spum.OverrideControllerInit();
    }

    public void PlayAttackAnimation(int index)
    {
        DebugTool.Log($"공격 애니메이션 재생: 인덱스 {index}", DebugType.Unit, this);
        if (spum == null)
        {
            Debug.LogError("SPUM_Prefabs가 초기화되지 않았습니다.");
            return;
        }
        spum.PlayAnimation(PlayerState.ATTACK, index);
    }
}
