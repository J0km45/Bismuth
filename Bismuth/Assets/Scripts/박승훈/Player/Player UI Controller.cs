using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

public class PlayerUIController : MonoBehaviour
{
    [Header("--- 참조 ---")]
    [SerializeField] private PlayerDataManager _player;
    [SerializeField] private SummonManager _summonManger;
    [SerializeField] private CombineManager _combineManger;
    [SerializeField] private UnitEnhanceSO _unitEnhanceSO;
    [SerializeField] private UnitInfoPanelUI _unitInfoPanelUI;
    [SerializeField] private ControlPanelUI _controlPanelUI;

    private PlayerAction _playerAction;
    public PlayerAction PlayerAction => _playerAction;
    
    private readonly int MAX_PLAYER_LEVEL = 5;
    private readonly int MAX_UNIT_LEVEL = 20;
    private readonly int COMBINE_GOLD = 50;
    private readonly int SUMMON_GOLD = 50;
    private readonly int[] SELL_GOLD_BY_TIER = { 20, 40, 50, 200 };
    private readonly int[] PLAYER_UPGRADE_GOLD = { 110, 220, 330, 440, 550 };

    public int MaxUnitLevel => MAX_UNIT_LEVEL;

    private int SelectedId = 0;

    private void Awake()
        => Init();

    private void OnEnable()
    {
        _playerAction.Enable();
        
        _playerAction.UI.Spawn.performed += OnSummonUnit;
        _playerAction.UI.PlayerLevelUp.started += OnPlayerLevelUpgrade;
    }

    private void OnDisable()
    {
        _playerAction.UI.Spawn.performed -= OnSummonUnit;
        _playerAction.UI.PlayerLevelUp.started -= OnPlayerLevelUpgrade;
        
        _playerAction.Disable();
    }

    // 유닛 판매 시 호출
    public void OnUnitSell(GameObject unit)
    {
        DebugTool.Log("유닛 판매 버튼", DebugType.Unit, this);

        if (unit == null)
        {
            DebugTool.Log("유닛이 없습니다.", DebugType.Game, this);
            return;
        }

        TowerUnit towerUnit = unit.GetComponent<TowerUnit>();

        if (towerUnit == null)
        {
            DebugTool.Log("TowerUnit 컴포넌트를 찾을 수 없습니다.", DebugType.Game, this);
            return;
        }

        UnitStat stat = unit.GetComponent<UnitStat>();

        if (stat == null)
        {
            DebugTool.Log("UnitStat 컴포넌트를 찾을 수 없습니다.", DebugType.Game, this);
            return;
        }

        int payback = (stat.Level - 1) * 10;

        int sellGold = SellUnit(payback, stat.Tier);

        _player.Gold += sellGold;

        SFXController.Instance.OnUnitSell();
        _unitInfoPanelUI.gameObject.SetActive(false);
        _summonManger?.DespawnUnit(towerUnit);
    }

    private int SellUnit(int payback, int tier)
        => SELL_GOLD_BY_TIER[tier - 1] + payback;

    // 유닛 합성 시 호출
    public void OnUnitCombine(int index)
    {
        if (index < 0)
        {
            DebugTool.Log("잘못된 인덱스입니다.", DebugType.Combine, this);
            return;
        }

        if (_player.Gold < COMBINE_GOLD)
        {
            NotEnoughGold();
            return;
        }

        if (!_combineManger.CombineUnit(index))
            return;

        SFXController.Instance.OnMerge();
        _player.Gold -= COMBINE_GOLD;
        _unitInfoPanelUI.gameObject.SetActive(false);
    }

    // 유닛 업그레이드 시 호출
    public void OnUnitUpgrade(GameObject unit)
    {
        DebugTool.Log("유닛 업그레이드 버튼", DebugType.Unit, this);

        if (unit == null)
        {
            DebugTool.Log("게임 오브젝트를 찾을 수 없습니다.", DebugType.Unit, this);
            return;
        }

        UnitStat stat = unit.GetComponent<UnitStat>();

        int unitID = stat.Id;
        int unitTier = stat.Tier;
        int unitLevel = stat.Level;

        if (unitLevel == MAX_UNIT_LEVEL)
        {
            DebugTool.Log($"유닛 레벨 {unitLevel} : 이미 최고 레벨 입니다.", DebugType.Unit, this);
            SFXController.Instance.OnUIFailure();
            _controlPanelUI.ShowWarningText(LocalizationManager.Instance.Get("MAX_LEVEL"));
            return;
        }

        int gold = CalculateUpgradeGold(unitLevel, unitTier);

        if (_player.Gold < gold)
        {
            NotEnoughGold();
            return;
        }

        float UpgradeRatio = _unitEnhanceSO.UnitEnhanceDatas[unitID - 10001].EnhanceValue;

        _player.Gold -= gold;
        stat.Level++;
        float upgradeAttackPower = stat.BaseAttackPower * UpgradeRatio * unitLevel;
        float beforeAttackPower = stat.CurrentAttackPower;
        stat.CurrentAttackPower = stat.BaseAttackPower + upgradeAttackPower;
        DebugTool.Log($"유닛 강화 성공! [유닛 레벨 : {unitLevel} | 소모 골드 : {gold}\n" +
                      $"이전 공격력 : {beforeAttackPower}, 추가 공격력 : {upgradeAttackPower}, 현재 공격력 : {stat.CurrentAttackPower}", DebugType.Unit, this);

        SFXController.Instance.OnEnforce();
        _unitInfoPanelUI.RefreshStats();
    }

    public int CalculateUpgradeGold(int unitLevel, int unitTier)
    {
        switch (unitTier)
        {
            case 1: 
                return unitLevel * 3 + 15;
            case 2: 
                return unitLevel * 5 + 30;
            case 3: 
                return unitLevel * 10 + 50;
            case 4: 
                return unitLevel * 20 + 100;
            default: 
                return 10000;
        }
    }

    public void OnPlayerLevelUpgrade(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            PlayerLevelUpgrade();
        }
    }

    // 플레이어 레벨 업그레이드 시 호출
    public void PlayerLevelUpgrade()
    {
        int level = _player.Level;
        CompareGoldLevel(level);
    }

    // 플레이어 레벨과 골드 비교
    private void CompareGoldLevel(int level)
    {
        if (level == MAX_PLAYER_LEVEL)
        {
            DebugTool.Log("이미 최대 레벨 입니다.", DebugType.Game, this);
            SFXController.Instance.OnUIFailure();
            _controlPanelUI.ShowWarningText(LocalizationManager.Instance.Get("MAX_LEVEL"));
            return;
        }

        int gold = PLAYER_UPGRADE_GOLD[level];

        if (_player.Gold < gold)
            NotEnoughGold();
        else
        {
            _player.Gold -= gold;
            _player.Level++;
            SFXController.Instance.OnEnforceGatcha();
        }

        DebugTool.Log($"골드 : {gold}, 레벨 : {level}", DebugType.Game, this);
    }

    public void OnSummonUnit(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            DebugTool.Log("스페이스바 눌림", DebugType.UI);
            OnSummonUnit(SelectedId);
        }
    }
    
    // 유닛 소환 시 호출
    public void OnSummonUnit(int id)
    {
        SelectedId = id;
        if (SelectedId > 0)
        {
            int index = SelectedId - 10001;
            if (index < 0)
            {
                DebugTool.Log("옳지 않은 인덱스 입니다.", DebugType.Unit, this);
                return;
            }

            UnitData data = _combineManger.Units.Units[index];

            if (data == null)
            {
                DebugTool.Log("존재하지 않는 유닛입니다.", DebugType.Unit, this);
                return;
            }

            _summonManger.SummonCombineUnit(data);
            return;
        }


        if (_player.Gold < SUMMON_GOLD)
        {
            NotEnoughGold();
            return;
        }

        if (_summonManger.SummonRandomUnit())
        {
            _player.Gold -= SUMMON_GOLD;
            SFXController.Instance.OnDrawSuccess();
        }
    }

    private void NotEnoughGold()
    {
        DebugTool.Log("골드가 부족합니다.", DebugType.Game, this);
        SFXController.Instance.OnUIFailure();
        _controlPanelUI.ShowWarningText(LocalizationManager.Instance.Get("NO_GOLD"));
    }

    public int GetUpgradeGold(int level)
    {
        if (level >= PLAYER_UPGRADE_GOLD.Length) return -1;

        return PLAYER_UPGRADE_GOLD[level];
    }

    public int GetSellGold(UnitStat stat)
        => SellUnit((stat.Level - 1) * 10, stat.Tier);


    private void Init()
    {
        _playerAction = new PlayerAction();
        
        _player = GetComponent<PlayerDataManager>();
        _summonManger = GetComponent<SummonManager>();
        _combineManger = GetComponent<CombineManager>();
    }
}
