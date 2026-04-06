using System;
using UnityEngine;

/// <summary>
/// 기지 체력을 관리하는 스크립트
/// 피해를 받거나 HP가 0이 되면 관련 이벤트를 발행
/// </summary>
public class HomeBaseHealth : MonoBehaviour
{
    [Tooltip("난이도 적용 전, 기지가 원래 가지고 있는 기본 최대 체력\n아군 기지 체력은 여기만 입력하세요!")]
    [SerializeField, Min(1)] private int _baseMaxHp = 10;

    [Tooltip("난이도 보정 등이 반영된 현재 최대 체력")]
    [SerializeField, Min(1)] private int _maxHp = 10;

    [Tooltip("현재 체력")]
    [SerializeField, Min(0)] private int _currentHp;
    
    [SerializeField] private BaseHPBar _baseHPBar;
    
    private bool _hasDied;
    
    public int BaseMaxHp => _baseMaxHp;
    public int MaxHp => _maxHp;
    public int CurrentHp => _currentHp;
    public bool IsDead => _currentHp <= 0;
    
    public event Action<HomeBaseHealth> Damaged;
    public event Action<HomeBaseHealth> Died;
    
    
    private void Awake()
    {
        _baseHPBar = GetComponent<BaseHPBar>();
        
        ResetToBaseHealth();
    }

    private void Start()
    {
        _baseHPBar.Initialize(MaxHp, CurrentHp);
    }

    private void OnValidate()
    {
        _baseMaxHp = Mathf.Max(1, _baseMaxHp);

        if (Application.isPlaying == false)
        {
            _maxHp = _baseMaxHp;
            _currentHp = _maxHp;
            _hasDied = false;
            return;
        }

        _maxHp = Mathf.Max(1, _maxHp);
        _currentHp = Mathf.Clamp(_currentHp, 0, _maxHp);
    }

    public void ApplyDamage(int damage)
    {
        if (damage <= 0) return;
        if (IsDead) return;
        
        _currentHp = Mathf.Max(0, _currentHp - damage);
        Damaged?.Invoke(this);
        
        _baseHPBar.SetHp(CurrentHp);
        _baseHPBar.Refresh();

        if (_currentHp > 0 || _hasDied)
            return;

        _hasDied = true;
        Died?.Invoke(this);
    }
    
    public void ResetToBaseHealth()
    {
        _maxHp = _baseMaxHp;
        _currentHp = _maxHp;
        _hasDied = false;
    }
    
    public void InitializeBaseHealth(int maxHp)
    {
        _maxHp = Mathf.Max(1, maxHp);
        _currentHp = _maxHp;
        _hasDied = false;
    }
    
}
