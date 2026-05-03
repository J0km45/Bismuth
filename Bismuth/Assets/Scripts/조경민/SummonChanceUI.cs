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
            LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;
        RefreshText();
        _playerData.OnLevelChanged += RefreshChanceText;
    }

    private void OnDisable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;
        _playerData.OnLevelChanged -= RefreshChanceText;
    }

    private void RefreshText()
    {
        _tierText = LocalizationManager.Instance.Get("TIER");
    }

    private SummonChanceData GetChanceData(int level)
    {
        foreach (SummonChanceData data in _summonChanceSO.Rows)
        {
            if (data != null && data.EnhancementLevel == level) return data;
        }

        return null;
    }

    public void RefreshChanceText()
    {
        SummonChanceData data = GetChanceData(_playerData.Level);
        _summonChanceText.text = $"1{_tierText} {data.Tier1}%\n" +
                                 $"2{_tierText} {data.Tier2}%\n" +
                                 $"3{_tierText} {data.Tier3}%\n" +
                                 $"4{_tierText} {data.Tier4}%";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        RefreshChanceText();
        _summonChanceTooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _summonChanceTooltip.SetActive(false);
    }
}
