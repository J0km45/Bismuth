using UnityEngine;

public class MonsterVisualDirectionFlipper : MonoBehaviour
{
    [Header("====컴포넌트 참조====")]
    [Tooltip("부모 몬스터의 이동 방향을 읽어올 MonsterMover")]
    [SerializeField] private MonsterMover _mover;

    [Tooltip("좌우 반전을 적용할 비주얼 루트")]
    [SerializeField] private Transform _flipRoot;

    [Header("====방향 반전 설정====")]
    [Tooltip("이 비주얼이 기본적으로 왼쪽을 바라보는지 여부")]
    [SerializeField] private bool _isDefaultFacingLeft = true;

    [Tooltip("좌우 방향 전환으로 인정할 최소 X 이동값")]
    [SerializeField, Min(0f)] private float _flipThreshold = 0.01f;

    private Vector3 _defaultLocalScale;

    private void Reset()
    {
        _mover = GetComponentInParent<MonsterMover>();
        _flipRoot = transform;
    }

    private void Awake()
    {
        if (_mover == null)
        {
            _mover = GetComponentInParent<MonsterMover>();
        }

        if (_flipRoot == null)
        {
            _flipRoot = transform;
        }

        _defaultLocalScale = _flipRoot.localScale;
    }

    private void LateUpdate()
    {
        if (_mover == null || _flipRoot == null)
        {
            return;
        }

        float moveX = _mover.MoveDirection.x;
        if (Mathf.Abs(moveX) < _flipThreshold)
        {
            return;
        }

        bool shouldFaceLeft = moveX < 0f;
        float baseSign = Mathf.Sign(_defaultLocalScale.x);
        if (Mathf.Approximately(baseSign, 0f))
        {
            baseSign = 1f;
        }

        float absScaleX = Mathf.Abs(_defaultLocalScale.x);
        if (absScaleX < 0.001f)
        {
            absScaleX = Mathf.Abs(_flipRoot.localScale.x);
        }

        bool shouldUseDefaultSign =
            (_isDefaultFacingLeft && shouldFaceLeft) ||
            (_isDefaultFacingLeft == false && shouldFaceLeft == false);

        float targetSign = shouldUseDefaultSign ? baseSign : -baseSign;

        Vector3 scale = _flipRoot.localScale;
        scale.x = absScaleX * targetSign;
        _flipRoot.localScale = scale;
    }
}