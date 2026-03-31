using UnityEngine;

public class PlayerUIController : MonoBehaviour
{
    [SerializeField] private PlayerDataManager _player;
    [SerializeField] private SummonManager _summonManger;
    [SerializeField] private CombineManager _combineManger;
    
    private readonly int MAX_LEVEL = 5;
    private readonly int COMBINE_GOLD = 50;
    private readonly int SUMMON_GOLD = 50;
    private readonly int[] PLAYER_UPGRADE_GOLD = { 110, 220, 330, 440, 550 };
    private readonly int[] UNIT_UPGRAGE_GOLD = { 3, 5, 10, 20 };

    private void Awake()
        => Init();

    // 유닛 판매 시 호출
    public void OnUnitSell(GameObject unit)
    {
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
        
        _summonManger?.DespawnUnit(towerUnit);
        
        UnitStat stat = unit.GetComponent<UnitStat>();
        if(stat == null)
        {
            DebugTool.Log("UnitStat 컴포넌트를 찾을 수 없습니다.", DebugType.Game, this);
            return;
        }

        int sellGold = stat.Level * 10;
        switch (stat.Tier)
        {
            case 1: sellGold += 20;
                break;
            case 2: sellGold += 40;
                break;
            case 3: sellGold += 50;
                break;
            case 4: sellGold += 200;
                break;
        }

        _player.Gold += sellGold;
    }
    
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
        DebugTool.Log("유닛 합성 완료", DebugType.Combine, this);
    }
    
    // 유닛 업그레이드 시 호출
    public void OnUnitUpgrade(GameObject unit)
    {
        
    }
    
    // 플레이어 레벨 업그레이드 시 호출
    public void OnPlayerLevelUpgrade()
    {
        int level = _player.Level;
        CompareGoldLevel(level);
    }

    private void CompareGoldLevel(int level)
    {
        if (level == MAX_LEVEL)
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

    public void OnSummonUnit()
    {
        if (_player.Gold < SUMMON_GOLD)
        {
            NotEnoughGold();
            return;
        }
        _player.Gold -= SUMMON_GOLD;
        _summonManger.SummonRandomUnit();
    }

    private void NotEnoughGold()
        => DebugTool.Log("골드가 부족합니다.", DebugType.Game, this);

    private void Init()
    {
        _player = GetComponent<PlayerDataManager>();
        _summonManger = GetComponent<SummonManager>();
        _combineManger = GetComponent<CombineManager>();
    }
}
