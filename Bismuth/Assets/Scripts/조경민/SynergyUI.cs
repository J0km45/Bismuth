using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 시너지 프리팹에 들어갈 스크립트
public class SynergyUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("━━━━ 헤더 ━━━━")]
    [SerializeField] private Image _icon; // 시너지 이름 앞 네모칸
    [SerializeField] private Sprite activeSprite; // 네모칸에 들어갈 이미지(활성화)
    [SerializeField] private Sprite inactiveSprite; // 네모칸에 들어갈 이미지(비활성화)
    [SerializeField] private TMP_Text _NameText; // 시너지 이름 텍스트
    [SerializeField] private TMP_Text _countText; // 시너지 수 (현재 시너지 수/가능한 최대 시너지 수)

    private SynergyData _data;
    private int _count;
    private SynergyScrollViewUI _scrollView;

    private void OnEnable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;
    }

    private void OnDisable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;
    }

    public void SetData(int synergyId, int count, SynergySO synergySO, SynergyScrollViewUI scrollView)
    {
        _data = GetSynergyData(synergyId, synergySO);
        _count = count;
        _scrollView = scrollView;

        if (_data == null) return;

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
        _NameText.text = LocalizationManager.Instance.Get(_data.SynergyName);
    }

    private SynergyData GetSynergyData(int synergyId, SynergySO synergySO)
    {
        foreach (SynergyData row in synergySO.Rows)
        {
            if (row.ID == synergyId) return row;
        }

        return null;
    }

    public string GetDescriptionText()
    {
        if (_data.Levels == null || _data.Levels.Count == 0)
        {
            return "";
        }

        SynergyLevelData currentLevel = null;

        for (int i = 0; i < _data.Levels.Count; i++)
        {
            if (_count >= _data.Levels[i].ActiveCount)
            {
                currentLevel = _data.Levels[i];
            }
        }

        // 1단계도 못 채운 경우
        if (currentLevel == null)
        {
            return $"({GetSynergyCount(null)})\n---";
        }

        string key = $"{_data.SynergyName}_DESC";
        string desc = LocalizationManager.Instance.Get(key, currentLevel.EffectValues.Cast<object>().ToArray());
        return $"({GetSynergyCount(currentLevel)})\n{desc}";
    }

    public string GetSynergyName()
    {
        if (_data == null) return "";

        return LocalizationManager.Instance.Get(_data.SynergyName);
    }

    private string GetSynergyCount(SynergyLevelData currentLevel)
    {
        string synergyCount = "";

        for (int i = 0; i < _data.Levels.Count; i++)
        {
            SynergyLevelData level = _data.Levels[i];
            if(level == currentLevel)
            {
                synergyCount += $"<color=red>{level.ActiveCount}</color>";
            }
            else
            {
                synergyCount += $"<color=grey>{level.ActiveCount}</color>";
            }
            
            if (i < _data.Levels.Count - 1)
            {
                synergyCount += " > ";
            }
        }

        return synergyCount;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _scrollView.ShowDescription(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _scrollView.CloseDescription();
    }
}
