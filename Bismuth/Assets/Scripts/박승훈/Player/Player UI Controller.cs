using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

public class PlayerUIController : MonoBehaviour
{
    [SerializeField] private PlayerDataManager _player;
    [SerializeField] private SummonManager _summonManger;
    [SerializeField] private CombineManager _combineManger;
    [SerializeField] private UnitEnhanceSO _unitEnhanceSO;
    [SerializeField] private UnitInfoPanelUI _unitInfoPanelUI;
    [SerializeField] private SynergyManager _synergyManager;
    
    
    private readonly int MAX_PLAYER_LEVEL = 5;
    private readonly int MAX_UNIT_LEVEL = 20;
    private readonly int COMBINE_GOLD = 50;
    private readonly int SUMMON_GOLD = 50;
    private readonly int[] SELL_GOLD_BY_TIER = { 20, 40, 50, 200 };
    private readonly int[] PLAYER_UPGRADE_GOLD = { 110, 220, 330, 440, 550 };

    private void Awake()
        => Init();

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
        
        if(stat == null)
        {
            DebugTool.Log("UnitStat 컴포넌트를 찾을 수 없습니다.", DebugType.Game, this);
            return;
        }

        int payback = stat.Level * 10;

        int sellGold = SellUnit(payback, stat.Tier);

        _player.Gold += sellGold;
        _summonManger?.DespawnUnit(towerUnit);
        
        unit = null;
        _unitInfoPanelUI.gameObject.SetActive(false);
        _synergyManager.OnUnitRemoved?.Invoke(stat);
    }

    private int SellUnit(int payback, int tier)
        => SELL_GOLD_BY_TIER[tier-1] + payback;
    
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
            return;
        }

        int gold; 
        switch (unitTier)
        {
            case 1:
                gold = unitLevel * 3 + 15;
                break;
            case 2:
                gold = unitLevel * 5 + 30;
                break;
            case 3:
                gold = unitLevel * 10 + 50;
                break;
            case 4:
                gold = unitLevel * 20 + 100;
                break;
            default:
                gold = 10000;
                break;
        }

        if (_player.Gold < gold)
        {
            NotEnoughGold();
            return;
        }
        
        float UpgradeRatio = _unitEnhanceSO.UnitEnhanceDatas[unitID - 10001].EnhanceValue;

        _player.Gold -= gold;
        stat.Level++;
        stat.CurrentAttackPower = stat.BaseAttackPower + (stat.BaseAttackPower * UpgradeRatio) * (unitLevel - 1);
        DebugTool.Log($"유닛 강화 성공! [유닛 레벨 : {unitLevel} | 소모 골드 : {gold}", DebugType.Unit, this);

        _unitInfoPanelUI.RefreshUnitInfo();
    }
    
    // 플레이어 레벨 업그레이드 시 호출
    public void OnPlayerLevelUpgrade()
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
            return;
        }
        
        int gold = PLAYER_UPGRADE_GOLD[level];
        
        if (_player.Gold < gold)
            NotEnoughGold();
        else
        {
            _player.Gold -= gold;
            _player.Level++;
        }
        
        DebugTool.Log($"골드 : {gold}, 레벨 : {level}", DebugType.Game, this);
    }
    
    // 유닛 소환 시 호출
    public void OnSummonUnit(int id)
    {
        if(id > 0)
        {
            int index = id - 10001;
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

        if(_summonManger.SummonRandomUnit())
            _player.Gold -= SUMMON_GOLD;
    }

    private void NotEnoughGold()
        => DebugTool.Log("골드가 부족합니다.", DebugType.Game, this);

    private void Init()
    {
        _player = GetComponent<PlayerDataManager>();
        _summonManger = GetComponent<SummonManager>();
        _combineManger = GetComponent<CombineManager>();
        _synergyManager = GetComponent<SynergyManager>();
    }
}
