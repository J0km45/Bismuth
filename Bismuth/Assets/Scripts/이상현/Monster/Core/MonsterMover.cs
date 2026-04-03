using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MonsterMover : MonoBehaviour
{
    [Header("====컴포넌트 참조====")]
    [Tooltip("몬스터 이동에 사용할 Rigidbody2D")]
    [SerializeField] private Rigidbody2D _rigidbody2D;

    [Header("====이동 설정====")]
    [Tooltip("웨이포인트 도착 판정 거리")]
    [SerializeField, Min(0.01f)] private float _arriveDistance = 0.05f;

    private WaypointPath _path;
    private float _baseSpeed;
    private float _moveSpeed;
    private int _currentWaypointIndex;
    private bool _isInitialized;
    private bool _isPathCompleted;
    private bool _isSlowed;

    public bool IsMoving { get; private set; }
    public Vector2 MoveDirection { get; private set; }
    public bool IsSlowed => _isSlowed;

    public event Action PathCompleted;

    private void Reset()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (_rigidbody2D == null)
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }
    }

    private void OnDisable()
    {
        IsMoving = false;
        MoveDirection = Vector2.zero;
        _isInitialized = false;
        _isPathCompleted = false;
        _isSlowed = false;
    }

    private void FixedUpdate()
    {
        if (_isInitialized == false || _isPathCompleted) return;

        MoveAlongPath();
    }

    public void Initialize(WaypointPath path, float moveSpeed)
    {
        _path = path;
        _baseSpeed = moveSpeed;
        _moveSpeed = moveSpeed;
        _currentWaypointIndex = 1;
        _isPathCompleted = false;
        _isInitialized = false;
        _isSlowed = false;
        IsMoving = false;
        MoveDirection = Vector2.zero;

        if (_rigidbody2D == null)
        {
            DebugTool.Error("Rigidbody2D 참조가 비어 있습니다.", DebugType.Enemy, this);
            return;
        }

        if (_path == null)
        {
            DebugTool.Error("웨이포인트 경로가 비어 있습니다.", DebugType.Enemy, this);
            return;
        }

        if (_path.WaypointCount == 0)
        {
            DebugTool.Error("웨이포인트가 없습니다.", DebugType.Enemy, this);
            return;
        }

        Vector2 startPosition = _path.GetWaypoint(0).position;
        _rigidbody2D.position = startPosition;
        transform.position = startPosition;

        _isInitialized = true;


        if (_path.WaypointCount <= 1) CompletePath();
    }


    public void ApplySlow(float percent)
    {
        if (percent <= 0f)
            return;

        float clampedPercent = Mathf.Clamp(percent, 0f, 100f);
        _moveSpeed = _baseSpeed * (1f - clampedPercent * 0.01f);
        _isSlowed = true;

        DebugTool.Log(
            $"이동속도 감소 적용 | base={_baseSpeed:F2}, slow={clampedPercent:F1}%, current={_moveSpeed:F2}",
            DebugType.Enemy, this);
    }


    public void RemoveSlow()
    {
        if (!_isSlowed)
            return;

        _moveSpeed = _baseSpeed;
        _isSlowed = false;

        DebugTool.Log(
            $"이동속도 감소 해제 | 복원 속도={_baseSpeed:F2}",
            DebugType.Enemy, this);
    }

    private void MoveAlongPath()
    {
        if (_currentWaypointIndex >= _path.WaypointCount)
        {
            CompletePath();
            return;
        }

        Vector2 currentPosition = _rigidbody2D.position;
        Vector2 targetPosition = _path.GetWaypoint(_currentWaypointIndex).position;

        Vector2 nextPosition = Vector2.MoveTowards(
            currentPosition,
            targetPosition,
            _moveSpeed * Time.fixedDeltaTime);

        MoveDirection = nextPosition - currentPosition;
        IsMoving = MoveDirection.sqrMagnitude > 0.000001f;

        _rigidbody2D.MovePosition(nextPosition);

        float remainingDistance = Vector2.Distance(nextPosition, targetPosition);
        if (remainingDistance > _arriveDistance) return;

        _currentWaypointIndex++;

        if (_currentWaypointIndex >= _path.WaypointCount) CompletePath();
    }

    private void CompletePath()
    {
        if (_isPathCompleted) return;

        _isPathCompleted = true;
        IsMoving = false;
        MoveDirection = Vector2.zero;
        PathCompleted?.Invoke();
    }
}
