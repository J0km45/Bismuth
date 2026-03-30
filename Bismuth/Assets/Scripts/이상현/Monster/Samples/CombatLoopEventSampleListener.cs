using UnityEngine;

/// <summary>
/// 이벤트를 정상적으로 받을 수 있는지 확인하기 위해 로그만 출력
/// 이벤트 수신 확인용
/// </summary>
public class CombatLoopEventSampleListener : MonoBehaviour
{
    [Header("====이벤트 참조====")]
    [Tooltip("이벤트를 구독할 기지 체력 컴포넌트")]
    [SerializeField] private HomeBaseHealth _homeBaseHealth;

    private void Reset()
    {
        _homeBaseHealth = GetComponent<HomeBaseHealth>();
    }

    private void OnEnable()
    {
        if (_homeBaseHealth == null)
        {
            DebugTool.Error("기지 체력 참조가 비어 있습니다.", DebugType.Enemy, this);
            return;
        }

        _homeBaseHealth.Damaged += HandleDamaged;
        _homeBaseHealth.Died += HandleDied;
    }

    private void OnDisable()
    {
        if (_homeBaseHealth == null) return;
        
        _homeBaseHealth.Damaged -= HandleDamaged;
        _homeBaseHealth.Died -= HandleDied;
    }

    //  HP UI 갱신이나 피격 연출 가능
    private void HandleDamaged(HomeBaseHealth homeBaseHealth)
    {
        DebugTool.Log($"[샘플리스너] 기지 피해 / 현재 HP : {homeBaseHealth.CurrentHp} / 최대 HP : {homeBaseHealth.MaxHp}",
            DebugType.Enemy, this);
    }
    
    /// <summary>
    /// 기지 HP가 0 이하가 되었을 때 호출
    /// 게임 오버 판정과 전투 종료 흐름
    /// </summary>
    private void HandleDied(HomeBaseHealth homeBaseHealth)
    {
        DebugTool.Log("[샘플리스너] 기지 파괴", DebugType.Enemy, this);
    }
}
