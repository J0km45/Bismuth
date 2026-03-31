using UnityEngine;
using UnityEngine.UI;

public class CatalogUIController : MonoBehaviour
{
    [Header("━━━━ 데이터 ━━━━")]
    [SerializeField] private UnitCatalogSO _unitCatalogSO;
    [SerializeField] private UnitSO _unitSO;

    [Header("━━━━ UI 연결 ━━━━")]
    [SerializeField] private GameObject _encyclopediaPopup;
    [SerializeField] private Transform _leftSlotGrid;
    [SerializeField] private Transform _rightSlotGrid;
    [SerializeField] private GameObject _slotPrefab;

    private int _currentPage;
    private const int _SlotsPerPage = 12;

    
    // 버튼
    public void OpenCatalog()
    {
        _encyclopediaPopup.SetActive(true);
        _currentPage = 0;
        ShowPage(_currentPage);
    }

    public void CloseCatalog()
    {
        _encyclopediaPopup.SetActive(false);
    }

    // 
    public void ShowPage(int pageIndex)
    {
        _currentPage = pageIndex;

        int leftStart = pageIndex * _SlotsPerPage * 2;
        int rightStart = leftStart + _SlotsPerPage;

        FillGrid(_leftSlotGrid, leftStart);
        FillGrid(_rightSlotGrid, rightStart);
    }

    private void FillGrid(Transform grid, int startIndex)
    {
        // 기존 슬롯 제거
        for (int i = grid.childCount - 1; i >= 0; i--)
            Destroy(grid.GetChild(i).gameObject);

        for (int i = 0; i < _SlotsPerPage; i++)
        {
            int catalogIndex = startIndex + i;
            GameObject slot = Instantiate(_slotPrefab, grid);
            Image icon = slot.transform.Find("Icon").GetComponent<Image>();

            if (catalogIndex >= _unitCatalogSO.UnitCatalog.Count)
            {
                icon.color = new Color(0.3f, 0.3f, 0.3f);
                continue;
            }

            UnitIdSummonedPair pair = _unitCatalogSO.UnitCatalog[catalogIndex];

            if (pair.Summoned)
            {
                UnitData unitData = FindUnitData(pair.UnitId);
                if (unitData != null && unitData.Sprite != null)
                    icon.sprite = unitData.Sprite;

                icon.color = Color.white;
            }
            else
            {
                icon.color = Color.black;
            }
        }
    }

    private UnitData FindUnitData(int unitId)
    {
        foreach (UnitData data in _unitSO.Units)
        {
            if (data.Id == unitId)
                return data;
        }
        return null;
    }
}