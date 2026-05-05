using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SummonChanceUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("확률 툴팁 패널")]
    [SerializeField] private GameObject _summonChanceTooltip;
    [Tooltip("확률 텍스트")]
    [SerializeField] private TMP_Text _summonChanceText;
    [SerializeField] private SummonChanceSO _summonChanceSO;

    private PlayerDataManager _playerData;
    private string _tierText;

    private void Awake()
    {
        if (_playerData == null)
        {
            _playerData = FindAnyObjectByType<PlayerDataManager>();
        }
    }

    private void Start()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;

        RefreshText();

        if (_playerData != null)
            _playerData.OnLevelChanged += RefreshChanceText;
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;

        if (_playerData != null)
            _playerData.OnLevelChanged -= RefreshChanceText;
    }

    private void RefreshText()
    {
        if (LocalizationManager.Instance == null)
            return;

        _tierText = LocalizationManager.Instance.Get("TIER");
    }

    private SummonChanceData GetChanceData(int level)
    {
        if (_summonChanceSO == null || _summonChanceSO.Rows == null)
            return null;

        SummonChanceData fallbackData = null;

        foreach (SummonChanceData data in _summonChanceSO.Rows)
        {
            if (data == null)
                continue;

            if (data.EnhancementLevel == level)
                return data;

            if (data.EnhancementLevel <= level)
            {
                if (fallbackData == null || data.EnhancementLevel > fallbackData.EnhancementLevel)
                    fallbackData = data;
            }
        }

        return fallbackData;
    }

    public void RefreshChanceText()
    {
        if (_playerData == null || _summonChanceText == null)
            return;

        SummonChanceData data = GetChanceData(_playerData.Level);

        if (data == null)
        {
            Debug.LogWarning($"소환 확률 데이터를 찾을 수 없습니다. level={_playerData.Level}", this);
            return;
        }

        _summonChanceText.text = $"1{_tierText} {data.Tier1}%\n" +
                                 $"2{_tierText} {data.Tier2}%\n" +
                                 $"3{_tierText} {data.Tier3}%\n" +
                                 $"4{_tierText} {data.Tier4}%";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        RefreshChanceText();

        if (_summonChanceTooltip != null)
            _summonChanceTooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_summonChanceTooltip != null)
            _summonChanceTooltip.SetActive(false);
    }
}
