using UnityEngine;
using UnityEngine.UI;

public class CatalogSlotUI : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private GameObject _lockObject;

    private UnitData _unitData;
    private CatalogUIController _controller;

    private void Awake()
    {
        _button = GetComponent<Button>();

        _button.onClick.AddListener(OnClickSlot);
    }

    public void SetEmpty()
    {
        _unitData = null;

        _icon.sprite = null;
        _icon.gameObject.SetActive(false);

        _lockObject.SetActive(false);

        _button.interactable = false;
    }

    public void SetData(UnitData unitData, bool isSummoned, CatalogUIController controller)
    {
        _unitData = unitData;
        _controller = controller;

        _icon.gameObject.SetActive(true);
        _icon.sprite = unitData.Sprite;

        _lockObject.SetActive(!isSummoned);

        _button.interactable = isSummoned;
    }

    private void OnClickSlot()
    {
        _controller.ShowUnitDetail(_unitData);
    }

    private void OnClickClose()
    {
        _controller.CloseUnitDetail();
    }
}
