using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombinationTrioUI : MonoBehaviour, ICombinationUI
{
    [SerializeField] private UnitSO _allUnitSO;
    
    [Header("━━━━ 배경 이미지 ━━━━")] 
    [SerializeField] private Image _backgroundImage;
    [Header("━━━━ 재료 유닛 이미지 ━━━━")]
    [SerializeField] private Image _sourceIcon1;
    [SerializeField] private Image _sourceIcon2;
    [SerializeField] private Image _sourceIcon3;
    [Header("━━━━ 조합 대상 이미지 ━━━━")]
    [SerializeField] private Image _resultIcon;

    private PlayerUIController _playerUIController;
    private int _index;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClickCombine);
    }

    public void Init(PlayerUIController playerUIController, int index)
    {
        _playerUIController = playerUIController;
        _index = index;
    }

    public void SetData(List<int> sourceIds, int resultId, bool canCombine)
    {
        _sourceIcon1.sprite = GetIcon(sourceIds[0]);
        _sourceIcon2.sprite = GetIcon(sourceIds[1]);
        _sourceIcon3.sprite = GetIcon(sourceIds[2]);
        _resultIcon.sprite = GetIcon(resultId);

        _backgroundImage.color = canCombine ? new Color(1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f);
        _button.interactable = canCombine;
    }

    private Sprite GetIcon(int id)
    {
        UnitData unit = _allUnitSO.GetUnitById(id);

        return unit.Icon;
    }

    private void OnClickCombine()
    {
        _playerUIController.OnUnitCombine(_index);
    }
}