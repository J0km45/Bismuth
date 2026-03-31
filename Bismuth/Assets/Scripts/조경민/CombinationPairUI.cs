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

    private CombineManager _combineManager;
    private int _index;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClickCombine);
    }

    public void Init(CombineManager combineManager, int index)
    {
        _combineManager = combineManager;
        _index = index;
    }

    public void SetData(List<int> sourceIds, int resultId, bool canCombine)
    {
        _sourceIcon1.sprite = GetIcon(sourceIds[0]);
        _sourceIcon2.sprite = GetIcon(sourceIds[1]);
        _resultIcon.sprite = GetIcon(resultId);

        _button.interactable = canCombine;
    }

    private Sprite GetIcon(int id)
    {
        UnitData unit = _allUnitSO.GetUnitById(id);

        return unit.Sprite;
    }

    private void OnClickCombine()
    {
        _combineManager.CombineUnit(_index);
    }
}
