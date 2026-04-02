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
    [Tooltip("스탯")]
    [SerializeField] private TMP_Text _statText;
    [Tooltip("강화")]
    [SerializeField] private TMP_Text _upgradeText;
    [Tooltip("판매")]
    [SerializeField] private TMP_Text _sellText;
    [Tooltip("설명")]
    [SerializeField] private TMP_Text _descriptionText;

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
        _descriptionText.text = "---";
    }

    public void RefreshStats()
    {
        _levelText.text = $"Lv {_unitStat.Level}";

        _statText.text =
            $"{LocalizationManager.Instance.Get("ATTACK_POWER")} : {_unitStat.CurrentAttackPower}" +
            $"\n{LocalizationManager.Instance.Get("ATTACK_SPEED")} : {_unitStat.AttackSpeed}";
    }

    private void Clear()
    {
        _nameText.text = "";
        _levelText.text = "";
        _tierText.text = "";
        _statText.text = "";
        _descriptionText.text = "";

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
