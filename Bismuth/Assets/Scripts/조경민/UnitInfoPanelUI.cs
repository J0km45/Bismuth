using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnitInfoPanelUI : MonoBehaviour
{
    [Header("━━━━ 텍스트 ━━━━")]
    [Tooltip("이름")]
    [SerializeField] private TMP_Text _nameText;
    [Tooltip("레벨")]
    [SerializeField] private TMP_Text _levelText;
    [Tooltip("단계")]
    [SerializeField] private TMP_Text _tierText;
    [Tooltip("타겟팅 방법")]
    [SerializeField] private TMP_Text _attackTypeText;
    [Tooltip("강화")]
    [SerializeField] private TMP_Text _upgradeText;
    [Tooltip("판매")]
    [SerializeField] private TMP_Text _sellText;
    [Tooltip("스탯")]
    [SerializeField] private TMP_Text _statText;
    [Tooltip("업그레이드 소모 비용")]
    [SerializeField] private TMP_Text _upgradeGoldText;
    [Tooltip("판매 획득 비용")]
    [SerializeField] private TMP_Text _sellGoldText;

    [Header("━━━━ 시너지 ━━━━")]
    [SerializeField] private Transform _tagGroup;
    [SerializeField] private GameObject _synergyTagPrefab; // 시너지 태그 프리팹

    private CollectUnitInfo _collectUnitInfo;
    private List<GameObject> _synergyTags = new List<GameObject>();

    private UnitStat _unitStat;

    private void Awake()
    {
        _collectUnitInfo = GetComponent<CollectUnitInfo>();
    }

    private void OnEnable()
    {
        RefreshUnitInfo();
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;
    }

    private void OnDisable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;
    }

    public void RefreshUnitInfo()
    {
        Clear();

        if (_collectUnitInfo == null)
        {
            DebugTool.Log("선택 유닛 컴포넌트 = null", DebugType.Game, this);
            gameObject.SetActive(false);
            return;
        }

        if (_collectUnitInfo.selectedUnit == null)
        {
            DebugTool.Log("선택 유닛 = null", DebugType.Game, this);
            gameObject.SetActive(false);
            return;
        }

        GameObject unit = _collectUnitInfo.selectedUnit;

        _unitStat = unit.GetComponent<UnitStat>();

        if (_unitStat == null || _unitStat.Id == 0)
        {
            DebugTool.Log("UnitStat 없음", DebugType.Game, this);
            return;
        }

        RefreshText();

        for (int i = 0; i < _unitStat.SynergIDs.Length; i++)
        {
            int synergyId = _unitStat.SynergIDs[i];
            if (synergyId == 0) continue;

            GameObject prefab = Instantiate(_synergyTagPrefab, _tagGroup);
            _synergyTags.Add(prefab);

            SynergyTagUI synergyTagUI = prefab.GetComponent<SynergyTagUI>();
            synergyTagUI.SetData(synergyId);
        }

        RefreshStats();
    }

    private void RefreshText()
    {
        _nameText.text = LocalizationManager.Instance.Get(_unitStat.Name);
        _tierText.text = $"{_unitStat.Tier} {LocalizationManager.Instance.Get("TIER")}";
        _upgradeText.text = LocalizationManager.Instance.Get("LEVEL_UP");
        _sellText.text = LocalizationManager.Instance.Get("SELL");
        _attackTypeText.text = $"[ {GetAttackType()} ]";
    }

    private string GetAttackType()
    {
        if (_unitStat.attackTypes == UnitData.AttackTypes.Targeting)
        {
            switch (_unitStat.AttackTargetCount)
            {
                case 1:  // 단일 타겟
                    return LocalizationManager.Instance.Get("SINGLE_TARGET");

                case 2:  // 2중 타겟
                    return LocalizationManager.Instance.Get("DOUBLE_TARGET");

                case 3:  // 3중 타겟
                    return LocalizationManager.Instance.Get("TRIPLE_TARGET");

                default:
                    return LocalizationManager.Instance.Get("WIDE_RANGE_ATTACK");
            }
        }
        else if (_unitStat.attackTypes == UnitData.AttackTypes.AOE)
        {
            return LocalizationManager.Instance.Get("WIDE_RANGE_ATTACK");
        }

        return "";
    }

    public void RefreshStats()
    {
        _levelText.text = $"Lv {_unitStat.Level}";

        // 공격력은 UnitStatHub 가 단일 소스. (강화/시너지/시너지강화 모두 Hub 로 합산)
        UnitStatHub hub = _unitStat.GetComponent<UnitStatHub>();
        float attackPower = hub != null ? hub.Get(StatType.AttackPower) : _unitStat.BaseAttackPower;

        _statText.text = $"{LocalizationManager.Instance.Get("ATTACK_POWER")} : {attackPower}" +
            $"\n{LocalizationManager.Instance.Get("ATTACK_SPEED")} : {_unitStat.AttackSpeed}" +
            $"\n{LocalizationManager.Instance.Get("RANGE")} : {_unitStat.Range}";

        PlayerUIController controller = _collectUnitInfo.PlayerUIController;
        if (_unitStat.Level >= controller.MaxUnitLevel)
        {
            _upgradeGoldText.text = "MAX";
        }
        else
        {
            int upgradeGold = controller.CalculateUpgradeGold(_unitStat.Level, _unitStat.Tier);
            _upgradeGoldText.text = $"{upgradeGold}";
        }

        int sellGold = controller.GetSellGold(_unitStat);
        _sellGoldText.text = $"{sellGold}";
    }

    private void Clear()
    {
        _nameText.text = "";
        _levelText.text = "";
        _tierText.text = "";
        _attackTypeText.text = "";
        _statText.text = "";
        _upgradeGoldText.text = "";
        _sellGoldText.text = "";

        ClearSynergyTags();
    }

    private void ClearSynergyTags()
    {
        for (int i = 0; i < _synergyTags.Count; i++)
        {
            if (_synergyTags[i] != null)
            {
                Destroy(_synergyTags[i]);
            }
        }

        _synergyTags.Clear();
    }
}
