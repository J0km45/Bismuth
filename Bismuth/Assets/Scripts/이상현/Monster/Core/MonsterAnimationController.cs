using UnityEngine;

/// <summary>
/// 몬스터 이동 상태와 방향을 Animator에 반영하는 스크립트
/// </summary>
public class MonsterAnimationController : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [Tooltip("이동 상태를 반영할 애니메이터")]
    [SerializeField] private Animator _animator;
    
    [Tooltip("좌우 반전에 사용할 스프라이트랜더러")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    
    private static readonly int IS_MOVING_HASH = Animator.StringToHash("IsMoving");

    private void Reset()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void SetMoving(bool isMoving)
    {
        if (_animator == null) return;
        _animator.SetBool(IS_MOVING_HASH, isMoving);
    }

    public void SetFaceDirection(Vector2 moveDirection)
    {
        if (_spriteRenderer == null) return;
        if (Mathf.Abs(moveDirection.x) < 0.001f) return;
        
        _spriteRenderer.flipX = moveDirection.x < 0f;
    }
    
}
