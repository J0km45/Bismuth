using System;
using System.Collections.Generic;
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

    private SpriteColorTint _colorTint;
    private static readonly Color SlowTintColor = new Color(0.5f, 0.5f, 1f, 1f);

    // Source 기반 슬로우 보관소.
    //   Key   : 슬로우를 적용한 source 식별자 (예: "Synergy_Spirit", "Skill_30001")
    //   Value : (percent 0~100, expireTime - 음수면 무한)
    // 활성 source 중 max(percent) 만큼만 _moveSpeed 에 반영. 각 source 는 자체 만료 시간을 가진다.
    private readonly Dictionary<object, SlowEntry> _slowEntries = new();

    private struct SlowEntry
    {
        public float Percent;
        public float ExpireTime;

        public bool IsInfinite => ExpireTime < 0f;
    }

    private readonly List<object> _expiredKeyBuffer = new();

    public bool IsMoving { get; private set; }
    public Vector2 MoveDirection { get; private set; }
    public bool IsSlowed => _slowEntries.Count > 0;

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
        _slowEntries.Clear();
        _moveSpeed = _baseSpeed;
        _colorTint?.Remove();
    }

    private void Update()
    {
        if (_slowEntries.Count == 0)
            return;

        // 만료된 슬로우 source 제거
        _expiredKeyBuffer.Clear();
        foreach (var kvp in _slowEntries)
        {
            SlowEntry entry = kvp.Value;
            if (!entry.IsInfinite && Time.time >= entry.ExpireTime)
                _expiredKeyBuffer.Add(kvp.Key);
        }

        if (_expiredKeyBuffer.Count == 0)
            return;

        for (int i = 0; i < _expiredKeyBuffer.Count; i++)
        {
            object expiredKey = _expiredKeyBuffer[i];
            _slowEntries.Remove(expiredKey);

            DebugTool.Log(
                $"슬로우 만료 | source={expiredKey}",
                DebugType.Enemy, this);
        }

        RecalculateSlow();
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
        _slowEntries.Clear();
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
        _colorTint = new SpriteColorTint(gameObject);

        if (_path.WaypointCount <= 1) CompletePath();
    }


    /// <summary>
    /// Source 기반 슬로우 적용.
    /// duration &lt;= 0 이면 만료 시간 없음(명시적 RemoveSlow(source) 호출 전까지 유지).
    /// 같은 source 키로 재호출하면 갱신(percent, duration 모두 새 값).
    /// 활성 source 중 max(percent) 가 실제 _moveSpeed 에 반영된다.
    /// </summary>
    public void ApplySlow(object source, float percent, float duration)
    {
        if (source == null)
        {
            DebugTool.Warnning("[MonsterMover] ApplySlow source가 null입니다.", DebugType.Enemy, this);
            return;
        }

        if (percent <= 0f)
            return;

        float clampedPercent = Mathf.Clamp(percent, 0f, 100f);
        float expireTime = duration > 0f ? Time.time + duration : -1f;

        _slowEntries[source] = new SlowEntry
        {
            Percent = clampedPercent,
            ExpireTime = expireTime
        };

        RecalculateSlow();

        DebugTool.Log(
            $"이동속도 감소 적용 | source={source}, percent={clampedPercent:F1}%, duration={(duration > 0f ? duration.ToString("F1") + "s" : "infinite")}, current={_moveSpeed:F2}",
            DebugType.Enemy, this);
    }

    /// <summary>
    /// 특정 source 의 슬로우만 제거. 다른 source 슬로우는 영향 없음.
    /// 제거 후 max(percent) 재계산.
    /// </summary>
    public void RemoveSlow(object source)
    {
        if (source == null)
            return;

        if (_slowEntries.Remove(source))
        {
            RecalculateSlow();

            DebugTool.Log(
                $"이동속도 감소 해제 | source={source}, current={_moveSpeed:F2}",
                DebugType.Enemy, this);
        }
    }

    /// <summary>
    /// 전체 슬로우 source 일괄 제거. (웨이브 종료, 몬스터 비활성화 등)
    /// </summary>
    public void RemoveAllSlows()
    {
        if (_slowEntries.Count == 0)
            return;

        int count = _slowEntries.Count;
        _slowEntries.Clear();
        RecalculateSlow();

        DebugTool.Log(
            $"이동속도 감소 전체 해제 | 제거된 source 수={count}",
            DebugType.Enemy, this);
    }

    /// <summary>
    /// 활성 슬로우 source 중 가장 높은 percent 를 _moveSpeed 에 반영한다.
    /// 슬로우가 비어 있으면 base 속도 복원 + 색 복원.
    /// </summary>
    private void RecalculateSlow()
    {
        if (_slowEntries.Count == 0)
        {
            _moveSpeed = _baseSpeed;
            _colorTint?.Remove();
            return;
        }

        float maxPercent = 0f;
        foreach (var entry in _slowEntries.Values)
        {
            if (entry.Percent > maxPercent)
                maxPercent = entry.Percent;
        }

        _moveSpeed = _baseSpeed * (1f - maxPercent * 0.01f);
        _colorTint?.Apply(SlowTintColor);
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
