using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitEnhanceSO", menuName = "Bismuth/Unit Enhance Table")]
public class UnitEnhanceSO : ScriptableObject
{
    [Header("유닛별 강화 수치")] [SerializeField] private List<UnitEnhanceData> _rows = new();

    private Dictionary<int, UnitEnhanceData> _dataByUnitId = new();

    public IReadOnlyList<UnitEnhanceData> Rows => _rows;

    private void OnEnable()
    {
        RebuildCache();
    }

    public void SetRows(List<UnitEnhanceData> rows)
    {
        _rows = rows != null ? new List<UnitEnhanceData>(rows) : new List<UnitEnhanceData>();
        RebuildCache();
    }

    public void RebuildCache()
    {
        _dataByUnitId.Clear();

        for (int i = 0; i < _rows.Count; i++)
        {
            UnitEnhanceData data = _rows[i];
            if (data == null) continue;

            if (_dataByUnitId.ContainsKey(data.UnitId))
            {
                Debug.LogWarning($"중복된 Unit ID가 있습니다. ID : {data.UnitId}", this);
                continue;
            }

            _dataByUnitId.Add(data.UnitId, data);
        }
    }


}