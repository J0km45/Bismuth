using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerDataManager : MonoBehaviour
{
    [SerializeField] private ControlPanelUI _controlPanelUI;
    
    [Header("━━━━ 플레이어 스탯 데이터 ━━━━")]
    [Tooltip("플레이어 스탯 SO")]
    [SerializeField] private PlayerSO _playerStat;
    
    [Tooltip("플레이어 레벨(소환 레벨)")]
    [SerializeField] private int _level;

    [Tooltip("플레이어 소지 골드")]
    [SerializeField] private int _gold;
    
    [Tooltip("최대 베이스 체력")]
    [SerializeField] private int _maxBaseHealth;
    public int MaxBaseHealth { get => _maxBaseHealth; set => _maxBaseHealth = value; }
    
    [Tooltip("현재 베이스 체력")]
    [SerializeField] private int _currentBaseHealth;
    
    private void Awake()
    {
        Debug.Log($"[TRACE] Awake : {name}", this);
    }
    
    public int Level
    {
        get => _level;
        set
        {
            _level = value;
            _controlPanelUI.RefreshLevel();
            _controlPanelUI.RefreshUpgradeGold();
        }
    }

    private void OnEnable()
    {
        Debug.Log($"[TRACE] OnEnable : {name}", this);
    }

    private void Start()
    {
        PlayerStatInit();
        
        Debug.Log($"[TRACE] Start : {name}", this);
    }

    private void OnDisable()
    {
        string parentName = transform.parent != null ? transform.parent.name : "None";

        Debug.Log(
            $"[TRACE] OnDisable : {name} | parent={parentName} | enabled={enabled} | activeSelf={gameObject.activeSelf} | activeInHierarchy={gameObject.activeInHierarchy}\n" +
            $"{System.Environment.StackTrace}",
            this);
    }
    
    public int Gold
    {
        get => _gold;
        set
        {
            _gold = value;
            _controlPanelUI.RefreshGold();
        }
    }

    public int CurrentBaseHealth
    {
        get => _currentBaseHealth;
        set
        {
            _currentBaseHealth = value;
            // 베이스 체력 UI 리프레시
        }
    }

    private void PlayerStatInit()
    {
        Level = _playerStat.Level;
        Gold = _playerStat.Gold;
        MaxBaseHealth = _playerStat.MaxBaseHealth;
        CurrentBaseHealth = MaxBaseHealth;
    }
}
