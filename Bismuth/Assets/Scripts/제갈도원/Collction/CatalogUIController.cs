using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatalogUIController : MonoBehaviour
{
    [Header("━━━━ 데이터 ━━━━")]
    [SerializeField] private UnitCatalogSO _unitCatalogSO; // 소환 여부가 저장된 도감 데이터
    [SerializeField] private UnitSO _unitSO;               // 전체 유닛 정보 리스트

    [Header("━━━━ UI 연결 ━━━━")]
    [SerializeField] private GameObject _encyclopediaPopup;    // 도감 팝업 오브젝트
    [SerializeField] private Transform _leftSlotGrid;          // 왼쪽 페이지 슬롯 부모
    [SerializeField] private Transform _rightSlotGrid;         // 오른쪽 페이지 슬롯 부모
    [SerializeField] private GameObject _slotPrefab;           // 슬롯 프리팹
    [SerializeField] private TextMeshProUGUI _leftPageTitle;   // 왼쪽 페이지 번호 텍스트
    [SerializeField] private TextMeshProUGUI _rightPageTitle;  // 오른쪽 페이지 번호 텍스트
    [SerializeField] private GameObject _howToPlayPanel;
    [SerializeField] private GameObject _mapScrollView;
    [SerializeField] private GameObject _prevButton;
    [SerializeField] private GameObject _nextButton;

    [Header("━━━━ 상세 패널 ━━━━")]
    [SerializeField] private GameObject _detailPanel;            // 상세 정보 패널
    [SerializeField] private Image _illustration;                // 상세 패널 이미지
    [SerializeField] private TextMeshProUGUI _unitNameText;      // 상세 패널 이름
    [SerializeField] private TextMeshProUGUI _descriptionText;   // 상세 패널 설명
    [SerializeField] private TextMeshProUGUI _backText;

    private int _currentSpread; // 현재 펼쳐진 양면 페이지 인덱스
    private Coroutine _spreadRoutine;    // 페이지 갱신 코루틴
    private UnitData _currentUnitData;

    private const int _SlotsPerPage = 12;  // 한 페이지에 들어갈 슬롯 수
    private const int _SlotsPerSpread = _SlotsPerPage * 2; // 양면(왼쪽+오른쪽) 전체 슬롯 수

    // 전체 유닛 수
    private int TotalUnits => _unitSO.Units.Count;

    // 전체 양면 페이지 수
    private int MaxSpread => Mathf.Max(1, Mathf.CeilToInt((float)TotalUnits / _SlotsPerSpread));

    // 전체 페이지 수
    private int TotalPages => Mathf.Max(1, Mathf.CeilToInt((float)TotalUnits / _SlotsPerPage));

    private void OnEnable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded += UpdatePageTitles;
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshDetail;
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;
        UpdatePageTitles();
        RefreshText();
    }

    private void OnDisable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded -= UpdatePageTitles;
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshDetail;
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;
    }

    private void RefreshDetail()
    {
        if (_currentUnitData != null)
            RefreshUnitDetailText(_currentUnitData);
    }

    private void RefreshText()
    {
        _backText.text = LocalizationManager.Instance.Get("BACK");
    }

    private void Start()
    {
        // 시작 시 첫 번째 양면 페이지 표시
        _currentSpread = 0;
        ShowSpread(_currentSpread);
    }

    // 도감 버튼 클릭(토글)
    public void OpenCatalog()
    {
        bool isOpening = !_encyclopediaPopup.activeSelf;
        _howToPlayPanel.SetActive(false);
        _mapScrollView.SetActive(false);

        if (isOpening)
        {
            _encyclopediaPopup.SetActive(true);
            RefreshSummonedState();
        }
        else
        {
            _encyclopediaPopup.SetActive(false);
        }
    }

    public void CloseCatalog()
    {
        // 도감 팝업 닫기
        _encyclopediaPopup.SetActive(false);
    }

    public void RefreshSummonedState()
    {
        // 현재 페이지를 다시 그려 소환 상태 갱신
        ShowSpread(_currentSpread);
    }

    public void NextPage()
    {
        // 마지막 양면 페이지가 아니면 다음 페이지로 이동
        if (_currentSpread < MaxSpread - 1)
            ShowSpread(_currentSpread + 1);
    }

    public void PrevPage()
    {
        // 첫 번째 양면 페이지가 아니면 이전 페이지로 이동
        if (_currentSpread > 0)
            ShowSpread(_currentSpread - 1);
    }

    public void ShowSpread(int spreadIndex)
    {
        // 현재 양면 페이지 인덱스를 범위 안에서 보정
        _currentSpread = Mathf.Clamp(spreadIndex, 0, MaxSpread - 1);

        // 비활성 오브젝트에서는 코루틴 시작 불가 -> 열릴 때 RefreshSummonedState로 갱신
        if (!gameObject.activeInHierarchy)
        {
            _spreadRoutine = null;
            return;
        }

        // 이미 페이지 갱신 중이면 중지 후 다시 시작
        if (_spreadRoutine != null)
            StopCoroutine(_spreadRoutine);

        _spreadRoutine = StartCoroutine(ShowSpreadRoutine());
    }

    private IEnumerator ShowSpreadRoutine()
    {
        // 현재 양면 페이지 기준으로 왼쪽/오른쪽 시작 인덱스 계산
        int leftStart = _currentSpread * _SlotsPerSpread;
        int rightStart = leftStart + _SlotsPerPage;

        // 기존 슬롯 삭제
        ClearGridChildren(_leftSlotGrid);
        ClearGridChildren(_rightSlotGrid);

        // 한 프레임 쉬고 다시 생성
        yield return null;

        // 왼쪽/오른쪽 페이지 슬롯 채우기
        FillGrid(_leftSlotGrid, leftStart);
        FillGrid(_rightSlotGrid, rightStart);

        // 페이지 번호 갱신
        UpdatePageTitles();

        _spreadRoutine = null;
    }


    // 페이지
    private void UpdatePageTitles()
    {
        // 양면 페이지 기준 실제 페이지 번호 계산
        int leftNum = _currentSpread * 2 + 1;
        int rightNum = _currentSpread * 2 + 2;

        SetPageTitle(_leftPageTitle, leftNum <= TotalPages ? leftNum : 0);
        SetPageTitle(_rightPageTitle, rightNum <= TotalPages ? rightNum : 0);
    }

    // 페이지 1,2,3,4
    private static void SetPageTitle(TextMeshProUGUI label, int pageNumber)
    {
        if (label == null) return;

        // 페이지가 존재하면 번호 표시, 없으면 빈 문자열
        if (pageNumber > 0)
        {
            string pageText = LocalizationManager.Instance.Get("PAGE");
            label.text = $"{pageText} {pageNumber}";
        }
        else
        {
            label.text = "";
        }
    }

    private void FillGrid(Transform grid, int startIndex)
    {
        List<UnitData> units = _unitSO.Units;

        // 페이지 슬롯 수만큼 반복 생성
        for (int i = 0; i < _SlotsPerPage; i++)
        {
            int unitIndex = startIndex + i;

            // 슬롯 프리팹 생성
            GameObject slot = Instantiate(_slotPrefab, grid);
            CatalogSlotUI slotUI = slot.GetComponent<CatalogSlotUI>();

            if (slotUI == null)
            {
                DebugTool.Log("Slot UI Null", DebugType.Catalog, this);
                return;
            }

            // 해당 인덱스에 유닛 데이터가 없으면 빈 슬롯 처리
            if (unitIndex >= units.Count)
            {
                slotUI.SetEmpty();
                continue;
            }

            UnitData unitData = units[unitIndex];
            bool isSummoned = IsUnitSummoned(unitData.Id);
            slotUI.SetData(unitData, isSummoned, this);
        }
    }

    private static void ClearGridChildren(Transform grid)
    {
        // 기존 슬롯 전부 삭제
        for (int i = grid.childCount - 1; i >= 0; i--)
        {
            Destroy(grid.GetChild(i).gameObject);
        }
    }

    private bool IsUnitSummoned(int unitId)
    {
        // 도감 데이터가 없으면 기본적으로 미소환 처리
        if (_unitCatalogSO == null) return false;

        // 해당 유닛 ID를 찾아 소환 여부 반환
        foreach (UnitIdSummonedPair pair in _unitCatalogSO.UnitCatalog)
        {
            if (pair.UnitId == unitId)
                return pair.Summoned;
        }

        // 목록에 없으면 미소환 처리
        return false;
    }

    public void ShowUnitDetail(UnitData unitData)
    {
        _currentUnitData = unitData;

        _prevButton.SetActive(false);
        _nextButton.SetActive(false);
        _detailPanel.SetActive(true);
        _illustration.sprite = unitData.Illustration;
        RefreshUnitDetailText(unitData);
    }

    private void RefreshUnitDetailText(UnitData unitData)
    {
        _unitNameText.text = LocalizationManager.Instance.Get($"{unitData.UnitName}");
        _descriptionText.text = LocalizationManager.Instance.Get($"{unitData.UnitName}_DESC");
    }

    public void CloseUnitDetail()
    {
        _prevButton.SetActive(true);
        _nextButton.SetActive(true);
        _detailPanel.SetActive(false);
    }
}