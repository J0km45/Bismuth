using System;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    [SerializeField] private ControlPanelUI _controlPanelUI;
    
    [Header("━━━━ 플레이어 스탯 데이터 ━━━━")]
    [Tooltip("플레이어 스탯 SO")]
    [SerializeField] private PlayerSO _playerStat;
    
    [Tooltip("플레이어 강화 단계")]
    [SerializeField] private int _level;

    [Tooltip("플레이어 소지 재화")]
    [SerializeField] private int _gold;
    
    [Tooltip("최대 베이스 체력")]
    [SerializeField] private int _maxBaseHealth;
    public int MaxBaseHealth { get => _maxBaseHealth; set => _maxBaseHealth = value; }
    
    [Tooltip("현재 베이스 체력")]
    [SerializeField] private int _currentBaseHealth;

    [Header("━━━━ 배치 수 업그레이드 ━━━━")]
    [Tooltip("업그레이드 0단계에서 사용할 수 있는 최초 배치 가능 슬롯 수")]
    [SerializeField] private int _basePlaceableTileCount = 12;

    [Tooltip("배치 수 업그레이드 최대 단계")]
    [SerializeField] private int _maxPlacementUpgradeLevel = 8;

    [Tooltip("업그레이드 1단계마다 증가하는 배치 가능 슬롯 수")]
    [SerializeField] private int _placeableTileIncreasePerLevel = 1;

    public event Action OnLevelChanged;
    public event Action<int> OnPlaceableTileCountChanged;

    public int Level
    {
        get => _level;
        set
        {
            int previousPlaceableTileCount = PlaceableTileCount;
            int maxLevel = Mathf.Max(0, _maxPlacementUpgradeLevel);
            int nextLevel = Mathf.Clamp(value, 0, maxLevel);

            if (_level == nextLevel)
                return;

            _level = nextLevel;
            OnLevelChanged?.Invoke();

            if (previousPlaceableTileCount != PlaceableTileCount)
                OnPlaceableTileCountChanged?.Invoke(PlaceableTileCount);
        }
    }

    public int PlacementUpgradeLevel => Mathf.Clamp(Level, 0, Mathf.Max(0, _maxPlacementUpgradeLevel));

    public int PlaceableTileCount
    {
        get
        {
            int baseCount = Mathf.Max(0, _basePlaceableTileCount);
            int increaseValue = Mathf.Max(0, _placeableTileIncreasePerLevel);
            return baseCount + PlacementUpgradeLevel * increaseValue;
        }
    }

    public int MaxPlaceableTileCount
    {
        get
        {
            int baseCount = Mathf.Max(0, _basePlaceableTileCount);
            int maxLevel = Mathf.Max(0, _maxPlacementUpgradeLevel);
            int increaseValue = Mathf.Max(0, _placeableTileIncreasePerLevel);
            return baseCount + maxLevel * increaseValue;
        }
    }

    private void Start()
    {
        PlayerStatInit();
    }

    public int Gold
    {
        get => _gold;
        set
        {
            _gold = value;

            if (_controlPanelUI != null)
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
        if (_playerStat == null)
        {
            DebugTool.Warnning("PlayerSO가 비어 있습니다.", DebugType.Game, this);
            return;
        }

        Level = _playerStat.Level;
        Gold = _playerStat.Gold;
        MaxBaseHealth = _playerStat.MaxBaseHealth;
        CurrentBaseHealth = MaxBaseHealth;
    }
}
