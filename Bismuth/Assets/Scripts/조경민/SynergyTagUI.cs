using TMPro;
using UnityEngine;

public class SynergyTagUI : MonoBehaviour
{
    [Tooltip("시너지 이름")]
    [SerializeField] private TMP_Text _synergyTagText;
    [SerializeField] private SynergySO _synergySO;

    private SynergyData _data;

    private void OnEnable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshText;
    }

    private void OnDisable()
    {
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshText;
    }

    public void SetData(int synergyID)
    {
        _data = GetSynergyData(synergyID);
        RefreshText();
    }

    private void RefreshText()
    {
        _synergyTagText.text = LocalizationManager.Instance.Get(_data.SynergyName);
    }

    private SynergyData GetSynergyData(int synergyId)
    {
        foreach (SynergyData row in _synergySO.Rows)
        {
            if (row.ID == synergyId) return row;
        }

        return null;
    }
}
