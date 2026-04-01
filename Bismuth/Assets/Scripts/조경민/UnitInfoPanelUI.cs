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

    private void Awake()
    {
        _collectUnitInfo = GetComponent<CollectUnitInfo>();
    }

    private void OnEnable()
    {
        RefreshUnitInfo();
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

        UnitStat unitStat = unit.GetComponent<UnitStat>();

        if (unitStat == null || unitStat.Id == 0)
        {
            DebugTool.Log("선택 유닛 컴포넌트 = null", DebugType.Game, this);
            return;
        }

        // TODO : 로컬라이징 적용
        _nameText.text = unitStat.Name;
        _levelText.text = $"Lv {unitStat.Level}";
        _tierText.text = $"{unitStat.Tier} 단계";
        _statText.text = $"공격력 : {unitStat.CurrentAttackPower}\n공속 : {unitStat.AttackSpeed}";
        _descriptionText.text = "설명";

        for (int i = 0; i < unitStat.SynergIDs.Length; i++)
        {
            int synergyId = unitStat.SynergIDs[i];
            if (synergyId == 0) continue;

            GameObject prefab = Instantiate(_synergyTagPrefab, _tagGroup);
            _synergyTags.Add(prefab);

            SynergyTagUI synergyTagUI = prefab.GetComponent<SynergyTagUI>();
            synergyTagUI.SetData(unitStat.SynergIDs[i]);
        }

        unit = null;
    }

    public void Clear()
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
