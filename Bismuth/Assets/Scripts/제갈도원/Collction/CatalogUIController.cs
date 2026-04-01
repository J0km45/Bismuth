using System.Collections.Generic;
using TMPro;
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
    [Tooltip("비우면 PageRoot/LeftPage/PageTitle 등에서 자동 탐색")]
    [SerializeField] private TextMeshProUGUI _leftPageTitle;
    [SerializeField] private TextMeshProUGUI _rightPageTitle;

    private int _currentSpread;
    private const int _SlotsPerPage = 12;
    private const int _SlotsPerSpread = _SlotsPerPage * 2;

    private int TotalUnits => _unitSO.Units.Count;
    private int MaxSpread => Mathf.Max(1, Mathf.CeilToInt((float)TotalUnits / _SlotsPerSpread));
    private int TotalPages => Mathf.Max(1, Mathf.CeilToInt((float)TotalUnits / _SlotsPerPage));

    private HashSet<int> _summonedIds;

    private void Awake()
    {
        if (_leftPageTitle == null)
            _leftPageTitle = transform.Find("PageRoot/LeftPage/PageTitle")?.GetComponent<TextMeshProUGUI>();
        if (_rightPageTitle == null)
            _rightPageTitle = transform.Find("PageRoot/RightPage/PageTitle")?.GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        BuildSummonedSet();
        _currentSpread = 0;
        ShowSpread(_currentSpread);
    }

    public void OpenCatalog()
    {
        RefreshSummonedState();
        _encyclopediaPopup.SetActive(true);
    }

    public void CloseCatalog()
    {
        _encyclopediaPopup.SetActive(false);
    }

    public void RefreshSummonedState()
    {
        BuildSummonedSet();
        ShowSpread(_currentSpread);
    }

    public void NextPage()
    {
        if (_currentSpread < MaxSpread - 1)
            ShowSpread(_currentSpread + 1);
    }

    public void PrevPage()
    {
        if (_currentSpread > 0)
            ShowSpread(_currentSpread - 1);
    }

    public void ShowSpread(int spreadIndex)
    {
        _currentSpread = Mathf.Clamp(spreadIndex, 0, MaxSpread - 1);

        int leftStart = _currentSpread * _SlotsPerSpread;
        int rightStart = leftStart + _SlotsPerPage;

        FillGrid(_leftSlotGrid, leftStart);
        FillGrid(_rightSlotGrid, rightStart);
        UpdatePageTitles();
    }

    private void UpdatePageTitles()
    {
        int leftNum = _currentSpread * 2 + 1;
        int rightNum = _currentSpread * 2 + 2;

        SetPageTitle(_leftPageTitle, leftNum <= TotalPages ? leftNum : 0);
        SetPageTitle(_rightPageTitle, rightNum <= TotalPages ? rightNum : 0);
    }

    private static void SetPageTitle(TextMeshProUGUI label, int pageNumber)
    {
        if (label == null) return;
        label.text = pageNumber > 0 ? $"Page {pageNumber}" : "";
    }

    private static readonly Color _dimColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    private void FillGrid(Transform grid, int startIndex)
    {
        ClearGridChildren(grid);

        List<UnitData> units = _unitSO.Units;

        for (int i = 0; i < _SlotsPerPage; i++)
        {
            int unitIndex = startIndex + i;
            GameObject slot = Instantiate(_slotPrefab, grid);
            Image icon = slot.transform.Find("Icon").GetComponent<Image>();

            if (unitIndex >= units.Count)
            {
                icon.sprite = null;
                icon.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                continue;
            }

            UnitData unitData = units[unitIndex];

            icon.sprite = unitData.Sprite;

            bool summoned = _summonedIds != null && _summonedIds.Contains(unitData.Id);
            icon.color = summoned ? Color.white : _dimColor;
        }
    }

    /// <summary>
    /// Destroy()는 프레임 끝에 처리되어 같은 프레임에 Instantiate하면 이전 슬롯이 겹쳐 보일 수 있음.
    /// </summary>
    private static void ClearGridChildren(Transform grid)
    {
        for (int i = grid.childCount - 1; i >= 0; i--)
            DestroyImmediate(grid.GetChild(i).gameObject);
    }

    private void BuildSummonedSet()
    {
        _summonedIds = new HashSet<int>();

        if (_unitCatalogSO == null) return;

        foreach (UnitIdSummonedPair pair in _unitCatalogSO.UnitCatalog)
        {
            if (pair.Summoned)
                _summonedIds.Add(pair.UnitId);
        }
    }
}