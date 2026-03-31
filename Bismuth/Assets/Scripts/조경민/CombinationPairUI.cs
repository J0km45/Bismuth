using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombinationPairUI : MonoBehaviour, ICombinationUI
{
    [SerializeField] private UnitSO _allUnitSO;

    [Header("━━━━ 재료 유닛 이미지 ━━━━")]
    [SerializeField] private Image _sourceIcon1;
    [SerializeField] private Image _sourceIcon2;
    [Header("━━━━ 조합 대상 이미지 ━━━━")]
    [SerializeField] private Image _resultIcon;

    private CombineData _data;
    private SummonManager _summonManager;
    private SummonUnit _summonUnit;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClickCombine);
    }

    public void Init(SummonManager summonManager, SummonUnit summonUnit)
    {
        _summonManager = summonManager;
        _summonUnit = summonUnit;
    }

    public void SetData(CombineData data, bool canCombine)
    {
        _data = data;
        _sourceIcon1.sprite =  GetIcon(data.SourceUnit[0]);
        _sourceIcon2.sprite = GetIcon(data.SourceUnit[1]);
        _resultIcon.sprite = GetIcon(data.ResultUnit);

        _button.interactable = canCombine;
    }

    private Sprite GetIcon(int id)
    {
        UnitData unit = _allUnitSO.GetUnitById(id);

        return unit.Sprite;
    }

    public void OnClickCombine()
    {
        List<TowerUnit> removeUnits = GetRemoveUnits(_data);

        if (removeUnits == null) return;

        for (int i = 0; i < removeUnits.Count; i++)
        {
            if (removeUnits[i] != null)
            {
                _summonManager.DespawnUnit(removeUnits[i]); // 유닛 제거
            }
        }

        _summonManager.SummonCombineUnit(_data); // 조합된 유닛 소환
    }

    private List<TowerUnit> GetRemoveUnits(CombineData data)
    {
        // 최종적으로 제거할 유닛을 담을 리스트
        List<TowerUnit> result = new List<TowerUnit>();
        // 현재 보유 유닛 중 조합에 필요한 유닛과 일치하는 유닛을 담을 리스트
        List<SummonUnit.SummonedTowerRecord> candidates = new List<SummonUnit.SummonedTowerRecord>();

        for (int i = 0; i < _summonUnit.OwnedTowers.Count; i++)
        {
            SummonUnit.SummonedTowerRecord record = _summonUnit.OwnedTowers[i];

            if (record == null || record.towerUnit == null) continue;

            // 사용 가능한 유닛만 리스트에 추가
            candidates.Add(record);
        }

        // 조합식의 재료 ID를 하나씩 확인
        foreach (int sourceId in data.SourceUnit)
        {
            if (sourceId == 0) continue;

            bool found = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                // 조합식의 재료 ID와 일치하면 제거리스트에 담고 후보에서 제거
                if (candidates[i].Id == sourceId)
                {
                    result.Add(candidates[i].towerUnit);
                    candidates.RemoveAt(i);
                    found = true;
                    break;
                }
            }

            if (!found) return null;
        }

        return result;
    }
}
