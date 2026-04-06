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
    [Header("━━━━ 미보유 유닛 이미지 ━━━━")]
    [SerializeField] private Sprite _unownedUnitSprite;

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

    public void SetData(List<int> sourceIds, int resultId, bool canCombine, HashSet<int> set)
    {
        SetSourceIcon(_sourceIcon1, sourceIds[0], set);
        SetSourceIcon(_sourceIcon2, sourceIds[1], set);
        SetSourceIcon(_sourceIcon3, sourceIds[2], set);
        _resultIcon.sprite = GetIcon(resultId);

        _backgroundImage.color = canCombine ? new Color(1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f);
        _button.interactable = canCombine;
    }

    private void SetSourceIcon(Image image, int id, HashSet<int> set)
    {
        if (set.Contains(id))
        {
            image.sprite = GetIcon(id);
        }
        else
        {
            image.sprite = _unownedUnitSprite;
        }
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