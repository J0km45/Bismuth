using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SynergyEnhanceTableSO", menuName = "Bismuth/Synergy Enhance Table")]
public class SynergyEnhanceSO : ScriptableObject
{
    [Header("시너지 강화 행")]
    [SerializeField] private List<SynergyEnhanceData> _rows = new();

    private Dictionary<int, SynergyEnhanceData> _dataBySynergyEnhanceId = new();

    public IReadOnlyList<SynergyEnhanceData> Rows => _rows;

    private void OnEnable()
    {
        RebuildCache();
    }

    public void SetRows(List<SynergyEnhanceData> rows)
    {
        _rows = rows != null ? new List<SynergyEnhanceData>(rows) : new List<SynergyEnhanceData>();
        RebuildCache();
    }

    public void RebuildCache()
    {
        _dataBySynergyEnhanceId.Clear();

        for (int i = 0; i < _rows.Count; i++)
        {
            SynergyEnhanceData data = _rows[i];
            if (data == null)
                continue;

            if (_dataBySynergyEnhanceId.ContainsKey(data.SynergyEnhanceID))
            {
                Debug.LogWarning($"중복 시너지 강화 SynergyEnhanceID 가 있습니다. ID : {data.SynergyEnhanceID}", this);
                continue;
            }

            _dataBySynergyEnhanceId.Add(data.SynergyEnhanceID, data);
        }
    }
}
