using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 몬스터 1마리의 런타임 상태와 생명주기를 관리
/// 사망 또는 기지 도달 시 종료 이벤트를 발행
/// </summary>
public class MonsterController : MonoBehaviour
{
    [Header("====컴포넌트 참조====")]
    [Tooltip("이동 처리 컴포넌트")]
    [SerializeField] private MonsterMover _mover;

    [Tooltip("애니메이션 처리 컴포넌트")]
    [SerializeField] private MonsterAnimationController _animationController;

    [Header("====테스트 설정====")]
    [Tooltip("기지 도달 후 비활성화 대기 시간")]
    [SerializeField, Min(0f)] private float _deactivateDelay = 0.2f;

    [Header("====런타임 상태====")]
    [Tooltip("현재 사용 중인 몬스터 데이터")]
    [SerializeField] private MonsterDataSO _monsterData;

    [Tooltip("최대 체력")]
    [SerializeField, Min(0f)] private float _maxHp;
    
    [Tooltip("현재 체력")]
    [SerializeField, Min(0f)] private float _currentHp;

    [Tooltip("기본 방어력")]
    [SerializeField, Min(0f)] private float _baseDefense;

    [Tooltip("기지 피해")]
    [SerializeField, Min(0)] private int _damageToBase;

    [Tooltip("처치 보상")]
    [SerializeField, Min(0)] private int _killReward;
    
    private bool _isInitialized;
    private bool _hasDied;
    private bool _hasReachedBase;

    public event Action<MonsterController> ReachedBase;
    public event Action<MonsterController> Died;
    public event Action<MonsterController> HealthChanged;

    public MonsterDataSO MonsterData => _monsterData;
    public float MaxHp => _maxHp;
    public float CurrentHp => _currentHp;
    public int DamageToBase => _damageToBase;
    public int KillReward => _killReward;
    public float BaseDefense => _baseDefense;

    private void Reset()
    {
        _mover = GetComponent<MonsterMover>();
        _animationController = GetComponentInChildren<MonsterAnimationController>();
    }

    private void Awake()
    {
        if (_mover != null)
        {
            _mover.PathCompleted += HandlePathCompleted;
        }
    }

    private void OnDisable()
    {
        _isInitialized = false;
        StopAllCoroutines();

        if (_animationController != null)
        {
            _animationController.SetMoving(false);
        }
    }

    private void OnDestroy()
    {
        if (_mover != null)
        {
            _mover.PathCompleted -= HandlePathCompleted;
        }
    }

    private void Update()
    {
        if (_isInitialized == false) return;

        if (_animationController == null || _mover == null) return;

        _animationController.SetMoving(_mover.IsMoving);
        _animationController.SetFaceDirection(_mover.MoveDirection);
    }

    public void Initialize(WaypointPath path, MonsterRuntimeValues runtimeValues)
    {
        _monsterData = runtimeValues.MonsterData;
        _maxHp = runtimeValues.CurrentHp;
        _currentHp = runtimeValues.CurrentHp;
        _damageToBase = runtimeValues.DamageToBase;
        _killReward = runtimeValues.KillReward;
        _baseDefense = runtimeValues.Defense;

        _hasDied = false;
        _hasReachedBase = false;

        _mover.Initialize(path, runtimeValues.MoveSpeed);
        _isInitialized = true;
        
        HealthChanged?.Invoke(this);
    }

    public bool TakeDamage(float damage, GameObject hitEffect)
    {
        if (damage <= 0f) return false;
        if (_hasDied) return false;
        if (_hasReachedBase) return false;

        _currentHp = Mathf.Max(0f, _currentHp - damage);
        HealthChanged?.Invoke(this);
        DebugTool.Log($"몬스터 좌표 {transform.position}", DebugType.Enemy, this);
        Vector3 pos = transform.position + Vector3.up * 1.4f;
        DamageTextManager.Instance.ShowDamageText((int)damage, pos);

        if (_currentHp <= 0f)
        {
            _hasDied = true;

            if (_animationController != null)
            {
                _animationController.SetMoving(false);
            }

            Died?.Invoke(this);
            gameObject.SetActive(false);
            
            Destroy(gameObject);
            return true;
        }

        if (hitEffect != null)
        {
            GameObject spawnedEffect = HitEffectPool.SpawnPooled(hitEffect, transform.position, Quaternion.identity);

            if (spawnedEffect != null)
            {
                HitEffectSpawner effectSpawner = spawnedEffect.GetComponent<HitEffectSpawner>();
                if (effectSpawner != null)
                {
                    effectSpawner.ConfigureFollowTarget(transform, true);
                }
                else
                {
                    DebugTool.Warnning("피격 이펙트에 HitEffectSpawner가 없어 추적을 적용하지 못했습니다.", DebugType.Enemy, this);
                }
            }
        }
        return false;

    }

    private IEnumerator DeactivateAfterDelay()
    {
        yield return new WaitForSeconds(_deactivateDelay);
        gameObject.SetActive(false);
    }

    private void HandlePathCompleted()
    {
        if (_hasDied || _hasReachedBase) return;
        _hasReachedBase = true;

        if (_animationController != null)
        {
            _animationController.SetMoving(false);
        }

        ReachedBase?.Invoke(this);
        StartCoroutine(DeactivateAfterDelay());
    }
}

public struct MonsterRuntimeValues
{
    public MonsterDataSO MonsterData;
    public float CurrentHp;
    public int DamageToBase;
    public int KillReward;
    public float MoveSpeed;
    public float Defense;
}