using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스크롤뷰에 들어갈 스크립트
public class SynergyScrollViewUI : MonoBehaviour
{
    [Header("━━━━ 시너지 ━━━━")]
    [SerializeField] private Transform _synergyContent; // 시너지 스크롤뷰 속 Content
    [SerializeField] private SynergyUI _synergyPrefab;
    [SerializeField] private SynergyManager _synergyManager;
    [SerializeField] private SynergySO _synergySO;

    [Header("━━━━ 설명 ━━━━")]
    [SerializeField] private GameObject _descriptionPanel; // 클릭 시 켜질 설명 패널
    [SerializeField] private TMP_Text _dNameText; // 설명칸에 들어갈 시너지 이름 텍스트
    [SerializeField] private TMP_Text _synergyLevelText; // 시너지 레벨 텍스트
    [SerializeField] private TMP_Text _synergyCountText; // 시너지 수
    [SerializeField] private TMP_Text _descriptionText; // 시너지 설명
    [SerializeField] private TMP_Text _synergyUpgradeInfoText; // 시너지 강화 설명
    [SerializeField] private TMP_Text _upgradeBonusInfoText; // 강화 보너스 설명
    [SerializeField] private TMP_Text _synergyUpgradeText; // 시너지 강화 버튼 텍스트
    [SerializeField] private TMP_Text _enhanceCostText; // 강화 소모 비용

    private List<SynergyUI> _synergys = new List<SynergyUI>(); // 존재하는 시너지 프리팹 리스트
    private SynergyUI _currentSynergy;
    private ControlPanelUI _controlPanelUI;
    private SynergyEnhanceLevelManager _enhanceLevelManager;

    private void Awake()
    {
        _controlPanelUI = GetComponentInParent<ControlPanelUI>();
        if (_enhanceLevelManager == null) _enhanceLevelManager = FindAnyObjectByType<SynergyEnhanceLevelManager>();
    }

    private void OnEnable()
    {
        _synergyManager.OnSynergyChanged += Refresh;
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshDescriptionText;
        _enhanceLevelManager.OnEnhanceLevelChanged += RefreshEnhanceText;
    }

    private void OnDisable()
    {
        _synergyManager.OnSynergyChanged -= Refresh;
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshDescriptionText;
        _enhanceLevelManager.OnEnhanceLevelChanged -= RefreshEnhanceText;
    }

    // 시너지 목록 바뀔때 Refresh 호출
    public void Refresh(Dictionary<int, List<int>> dict)
    {
        int currentSynergyId = _currentSynergy != null ? _currentSynergy.SynergyId : 0;

        ClearSynergys(); // 변화가 있을때마다 리스트 비우고 다시 채움

        List<KeyValuePair<int, List<int>>> sortedList = dict
            .OrderByDescending(pair => IsActivated(pair.Key, pair.Value.Count))   // 활성화 먼저
            .ThenByDescending(pair => GetProgress(pair.Key, pair.Value.Count))    // 최대카운트에 가까운 순
            .ThenByDescending(pair => pair.Value.Count)                           // 현재 카운트 큰 순
            .ToList();

        _currentSynergy = null;

        // 받아온 딕셔너리에서 시너지프리팹 하나씩 생성
        foreach (KeyValuePair<int, List<int>> pair in sortedList)
        {
            int synergyId = pair.Key;
            int count = pair.Value.Count;

            SynergyUI synergy = Instantiate(_synergyPrefab, _synergyContent);
            synergy.SetData(synergyId, count, _synergySO, this);
            _synergys.Add(synergy);

            if (synergyId == currentSynergyId)
            {
                _currentSynergy = synergy;
            }
        }

        if (currentSynergyId != 0 && _currentSynergy == null)
        {
            OnCloseDescription();
            return;
        }

        if (_currentSynergy != null)
        {
            RefreshDescriptionText();
        }
    }

    // 시너지 활성화 여부 체크
    private bool IsActivated(int synergyId, int count)
    {
        SynergyData data = GetSynergyData(synergyId);

        return count >= data.Levels[0].ActiveCount;
    }

    // 현재 시너지 수가 최대 시너지 수에 얼마나 가까운지 체크
    private float GetProgress(int synergyId, int count)
    {
        SynergyData data = GetSynergyData(synergyId);
        int maxCount = data.Levels[data.Levels.Count - 1].ActiveCount;

        return (float)count / maxCount;
    }

    private SynergyData GetSynergyData(int synergyId)
    {
        foreach (SynergyData row in _synergySO.Rows)
        {
            if (row.ID == synergyId) return row;
        }

        return null;
    }

    // _synergys 리스트에 있는 모든 프리팹 삭제 후 리스트 비워줌
    private void ClearSynergys()
    {
        for (int i = 0; i < _synergys.Count; i++)
        {
            if (_synergys[i] != null)
                Destroy(_synergys[i].gameObject);
        }

        _synergys.Clear();
        DebugTool.Log("Synergys cleared", DebugType.Synergy, this);
    }

    public void ShowDescription(SynergyUI synergyUI)
    {
        // 같은 시너지 다시 클릭 → 닫기
        if (_currentSynergy == synergyUI)
        {
            OnCloseDescription();
            return;
        }

        _currentSynergy = synergyUI;
        _descriptionPanel.SetActive(true);

        RefreshDescriptionText();
    }

    private void RefreshDescriptionText()
    {
        Debug.Log("시너지 설명 텍스트 갱신 호출됨", this);
        if (_currentSynergy == null) return;

        _synergyUpgradeText.text = LocalizationManager.Instance.Get("SYNERGY_UPGRADE");
        _dNameText.text = _currentSynergy.GetSynergyName();
        _synergyCountText.text = _currentSynergy.GetSynergyCountText();
        _descriptionText.text = _currentSynergy.GetDescriptionText();

        RefreshEnhanceText();
        ForceLayoutRebuild();
    }

    private void RefreshEnhanceText(Dictionary<int, int> levels)
    {
        RefreshEnhanceText();
    }

    private void RefreshEnhanceText()
    {
        if (_currentSynergy == null) return;

        if (_enhanceLevelManager == null)
        {
            _synergyLevelText.text = "";
            _synergyUpgradeInfoText.text = "";
            _upgradeBonusInfoText.text = "";
            return;
        }

        int synergyId = _currentSynergy.SynergyId;
        string synergyNameKey = _currentSynergy.SynergyNameKey;

        int currentLevel = _enhanceLevelManager.GetLevel(synergyId);
        int maxLevel = _enhanceLevelManager.MaxLevel;

        _synergyLevelText.text = $"Lv {currentLevel}";

        int levelValue = _enhanceLevelManager.GetLevelValue(synergyId, currentLevel);
        int bonusValue = _enhanceLevelManager.GetBonusValue(synergyId);

        _synergyUpgradeInfoText.text = LocalizationManager.Instance.Get($"{synergyNameKey}_UPGRADE", levelValue);

        bool isMaxLevel = currentLevel >= maxLevel;

        if (isMaxLevel)
        {
            _upgradeBonusInfoText.text = LocalizationManager.Instance.Get($"{synergyNameKey}_BONUS", bonusValue);
            _enhanceCostText.text = "MAX";
        }
        else
        {
            _upgradeBonusInfoText.text = "";
            int cost = _enhanceLevelManager.CalculateEnhanceCost(synergyId, currentLevel);
            _enhanceCostText.text = $"{cost}";
        }
    }

    public void OnClickEnhance()
    {
        if (_currentSynergy == null)
        {
            Debug.LogWarning("현재 선택된 시너지가 없습니다.", this);
            return;
        }

        if (_enhanceLevelManager == null)
        {
            Debug.LogWarning("SynergyEnhanceLevelManager가 없습니다.", this);
            return;
        }

        int synergyId = _currentSynergy.SynergyId;

        int currentLevel = _enhanceLevelManager.GetLevel(synergyId);
        int cost = _enhanceLevelManager.CalculateEnhanceCost(synergyId, currentLevel);

        if (!_enhanceLevelManager.CanEnhance(synergyId))
        {
            SFXController.Instance.OnUIFailure();
            _controlPanelUI.ShowWarningText(LocalizationManager.Instance.Get("MAX_LEVEL"));
            RefreshEnhanceText();
            return;
        }

        bool success = _enhanceLevelManager.TryEnhance(synergyId);

        if (success)
        {
            SFXController.Instance.OnEnforce();
            RefreshEnhanceText();
            ForceLayoutRebuild();
        }
    }

    private void ForceLayoutRebuild()
    {
        Canvas.ForceUpdateCanvases();

        RectTransform rect = _descriptionPanel.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    public void OnCloseDescription()
    {
        _descriptionPanel.SetActive(false);
        _currentSynergy = null;
    }
}
