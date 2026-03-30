using System;
using UnityEngine;

/// <summary>
/// 기지 체력을 관리하는 스크립트
/// 피해를 받거나 HP가 0이 되면 관련 이벤트를 발행
/// </summary>
public class HomeBaseHealth : MonoBehaviour
{
    [Header("====기지 체력====")]
    [Tooltip("기지 최대 체력")]
    [SerializeField, Min(1)] private int _maxHp = 100;
    
    [Tooltip("현재 체력")]
    [SerializeField, Min(0)] private int _currentHp;
    
    private bool _hasDied;
    
    public int MaxHp => _maxHp;
    public int CurrentHp => _currentHp;
    public bool IsDead => _currentHp <= 0;
    
    public event Action<HomeBaseHealth> Damaged;
    public event Action<HomeBaseHealth> Died;
    
    private void Awake()
    {
        _currentHp = _maxHp;
        _hasDied = false;
    }

    private void OnValidate()
    {
        if (_maxHp < 1) _maxHp = 1;
        
        _currentHp = Mathf.Clamp(_currentHp, 0, _maxHp);
    }

    public void ApplyDamage(int damage)
    {
        if (damage <= 0) return;
        if (IsDead) return;
        
        _currentHp = Mathf.Max(0, _currentHp - damage);
        Damaged?.Invoke(this);

        if (_currentHp > 0 || _hasDied)
            return;

        _hasDied = true;
        Died?.Invoke(this);
    }
    
    [ContextMenu("기지 체력 1 감소 테스트")]
    private void DebugApplyOneDamage()
    {
        if (Application.isPlaying == false) return;
        ApplyDamage(1);
    }
}
