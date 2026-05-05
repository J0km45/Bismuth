using System;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    [SerializeField] private ControlPanelUI _controlPanelUI;
    
    [Header("━━━━ 플레이어 스탯 데이터 ━━━━")]
    [Tooltip("플레이어 스탯 SO")]
    [SerializeField] private PlayerSO _playerStat;
    
    [Tooltip("플레이어 레벨(소환 확률 레벨)")]
    [SerializeField] private int _level;

    [Tooltip("플레이어 소지 재화")]
    [SerializeField] private int _gold;
    
    [Tooltip("최대 베이스 체력")]
    [SerializeField] private int _maxBaseHealth;
    public int MaxBaseHealth { get => _maxBaseHealth; set => _maxBaseHealth = value; }
    
    [Tooltip("현재 베이스 체력")]
    [SerializeField] private int _currentBaseHealth;

    [Header("━━━━ 배치 수 업그레이드 ━━━━")]
    [Tooltip("현재 배치 수 업그레이드 단계")]
    [SerializeField] private int _placementUpgradeLevel;

    [Tooltip("업그레이드 0단계에서 사용할 수 있는 최초 배치 가능 슬롯 수")]
    [SerializeField] private int _basePlaceableTileCount = 13;

    [Tooltip("배치 수 업그레이드 최대 단계")]
    [SerializeField] private int _maxPlacementUpgradeLevel = 7;

    [Tooltip("업그레이드 1단계마다 증가하는 배치 가능 슬롯 수")]
    [SerializeField] private int _placeableTileIncreasePerLevel = 1;

    public event Action OnLevelChanged;
    public event Action OnPlacementUpgradeLevelChanged;
    public event Action<int> OnPlaceableTileCountChanged;

    public int Level
    {
        get => _level;
        set
        {
            if (_level == value)
                return;

            _level = value;
            OnLevelChanged?.Invoke();
        }
    }

    public int PlacementUpgradeLevel
    {
        get => Mathf.Clamp(_placementUpgradeLevel, 0, MaxPlacementUpgradeLevel);
        set
        {
            int previousPlaceableTileCount = PlaceableTileCount;
            int nextLevel = Mathf.Clamp(value, 0, MaxPlacementUpgradeLevel);

            if (_placementUpgradeLevel == nextLevel)
                return;

            _placementUpgradeLevel = nextLevel;
            OnPlacementUpgradeLevelChanged?.Invoke();

            if (previousPlaceableTileCount != PlaceableTileCount)
                OnPlaceableTileCountChanged?.Invoke(PlaceableTileCount);
        }
    }

    public int BasePlaceableTileCount => Mathf.Max(0, _basePlaceableTileCount);
    public int MaxPlacementUpgradeLevel => Mathf.Max(0, _maxPlacementUpgradeLevel);

    public int PlaceableTileCount
    {
        get
        {
            int increaseValue = Mathf.Max(0, _placeableTileIncreasePerLevel);
            return BasePlaceableTileCount + PlacementUpgradeLevel * increaseValue;
        }
    }

    public int MaxPlaceableTileCount
    {
        get
        {
            int increaseValue = Mathf.Max(0, _placeableTileIncreasePerLevel);
            return BasePlaceableTileCount + MaxPlacementUpgradeLevel * increaseValue;
        }
    }

    public bool IsMaxPlacementUpgradeLevel => PlacementUpgradeLevel >= MaxPlacementUpgradeLevel;

    private void Start()
    {
        PlayerStatInit();
        OnPlaceableTileCountChanged?.Invoke(PlaceableTileCount);
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

    public bool TryIncreasePlacementUpgradeLevel()
    {
        if (IsMaxPlacementUpgradeLevel)
            return false;

        PlacementUpgradeLevel++;
        return true;
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
