using TMPro;
using UnityEngine;

public class SynergyTagUI : MonoBehaviour
{
    [Tooltip("시너지 이름")]
    [SerializeField] private TMP_Text _synergyTagText;
    [SerializeField] private SynergySO _synergySO;

    // TODO : 로컬라이징
    public void SetData(int synergyID)
    {
        SynergyData data = GetSynergyData(synergyID);
        _synergyTagText.text = data.SynergyName;
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
