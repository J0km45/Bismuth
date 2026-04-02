using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 시너지 프리팹에 들어갈 스크립트
public class SynergyUI : MonoBehaviour, IPointerClickHandler
{
    [Header("━━━━ 헤더 ━━━━")]
    [SerializeField] private Image _icon; // 시너지 이름 앞 네모칸
    [SerializeField] private Sprite activeSprite; // 네모칸에 들어갈 이미지(활성화)
    [SerializeField] private Sprite inactiveSprite; // 네모칸에 들어갈 이미지(비활성화)
    [SerializeField] private TMP_Text _hNameText; // 헤더에 들어갈 시너지 이름 텍스트
    [SerializeField] private TMP_Text _countText; // 시너지 수 (현재 시너지 수/가능한 최대 시너지 수)

    [Header("━━━━ 설명 ━━━━")]
    [SerializeField] private GameObject _descriptionPanel; // 마우스 올리면 켜질 설명 패널
    [SerializeField] private TMP_Text _dNameText; // 설명칸에 들어갈 시너지 이름 텍스트
    [SerializeField] private TMP_Text _descriptionText; // 시너지 설명

    private SynergyData _data;
    private int _count;

    private void OnEnable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;
    }

    private void OnDisable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;
    }

    public void SetData(int synergyId, int count, SynergySO synergySO)
    {
        _data = GetSynergyData(synergyId, synergySO);
        _count = count;

        // 최대 시너지 수
        int maxCount = 0;
        if (_data.Levels != null && _data.Levels.Count > 0)
        {
            maxCount = _data.Levels[_data.Levels.Count - 1].ActiveCount;
        }
        _countText.text = $"{_count} / {maxCount}";

        // 아이콘 활성/비활성
        bool isActive = false;
        if (_data.Levels != null && _data.Levels.Count > 0)
        {
            isActive = _count >= _data.Levels[0].ActiveCount;
        }

        _icon.sprite = isActive ? activeSprite : inactiveSprite;

        RefreshText();
    }

    private void RefreshText()
    {
        // 시너지 이름
        _hNameText.text = LocalizationManager.Instance.Get(_data.SynergyName);
        _dNameText.text = LocalizationManager.Instance.Get(_data.SynergyName);

        // 시너지 설명
        _descriptionText.text = GetDescriptionText(_data, _count);
    }

    private SynergyData GetSynergyData(int synergyId, SynergySO synergySO)
    {
        foreach (SynergyData row in synergySO.Rows)
        {
            if (row.ID == synergyId) return row;
        }

        return null;
    }

    private string GetDescriptionText(SynergyData data, int count)
    {
        if (data.Levels == null || data.Levels.Count == 0)
        {
            return "";
        }

        SynergyLevelData currentLevel = null;

        for (int i = 0; i < data.Levels.Count; i++)
        {
            if (count >= data.Levels[i].ActiveCount)
            {
                currentLevel = data.Levels[i];
            }
        }

        // 1단계도 못 채운 경우
        if (currentLevel == null)
        {
            return "---";
        }

        string key = $"{data.SynergyName}_DESC";
        return LocalizationManager.Instance.Get(key, currentLevel.EffectValues.Cast<object>().ToArray());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        bool isDescriptionOpened = _descriptionPanel.activeSelf;
        _descriptionPanel.SetActive(!isDescriptionOpened);
    }
}
