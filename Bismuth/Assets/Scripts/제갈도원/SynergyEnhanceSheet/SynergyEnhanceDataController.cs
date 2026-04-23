using System.Collections.Generic;
using UnityEngine;

public class SynergyEnhanceDataController : MonoBehaviour
{
    [Header("━━━━ SO 설정 ━━━━")]
    [Tooltip("기본 시너지 강화 데이터 SO")]
    [SerializeField] private SynergyEnhanceSO synergyEnhanceSo;

    [Header("━━━━ 시트 설정 ━━━━")]
    [SerializeField] private SheetData _synergyEnhanceSheet;

    private List<SynergyEnhanceData> _rows = new();

    [SerializeField] private bool log;

    private Dictionary<int, SynergyEnhanceData> _dataBySynergyEnhanceId = new();
    private Dictionary<int, SynergyEnhanceData> _dataByTarget = new();

    private void Start()
    {
        DebugTool.DebugSelect(DebugType.Data, log);

        if (synergyEnhanceSo != null)
        {
            _rows = new List<SynergyEnhanceData>(synergyEnhanceSo.Rows);
            RebuildCache();
            DebugTool.Log($"[시너지 강화] SO {_rows.Count}행 로드 완료", DebugType.Data, this);
        }

        if (_synergyEnhanceSheet != null)
        {
            StartCoroutine(_synergyEnhanceSheet.Load(OnSheetLoaded));
            return;
        }

    }

    private void OnSheetLoaded(char splitSymbol, string[] lines)
    {
        _rows.Clear();

        if (lines == null || lines.Length < 2)
            return;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] cells = lines[i].Split(splitSymbol);
            SynergyEnhanceData row = SynergyEnhanceData.Create(cells);

            if (row != null)
                _rows.Add(row);
        }

        RebuildCache();

        if (synergyEnhanceSo != null)
            synergyEnhanceSo.SetRows(_rows);

        DebugTool.Log($"[시너지 강화] {_rows.Count}행 로드 완료", DebugType.Data, this);
    }
    
    // 중복 검사
    private void RebuildCache()
    {
        _dataBySynergyEnhanceId.Clear();
        _dataByTarget.Clear();

        for (int i = 0; i < _rows.Count; i++)
        {
            SynergyEnhanceData data = _rows[i];
            if (data == null)
                continue;

            if (_dataBySynergyEnhanceId.ContainsKey(data.SynergyEnhanceID))
            {
                DebugTool.Warnning(
                    $"중복 시너지 강화 SynergyEnhanceID 가 있습니다. ID : {data.SynergyEnhanceID}",
                    DebugType.Data,
                    this);
                continue;
            }

            if (_dataByTarget.ContainsKey(data.Target))
            {
                DebugTool.Warnning($"중복 시너지 강화 target 이 있습니다. Target : {data.Target}", DebugType.Data, this);
                continue;
            }

            _dataBySynergyEnhanceId.Add(data.SynergyEnhanceID, data);
            _dataByTarget.Add(data.Target, data);
        }
    }

    public SynergyEnhanceData GetBySynergyEnhanceId(int synergyEnhanceId)
    {
        if (_dataBySynergyEnhanceId.TryGetValue(synergyEnhanceId, out SynergyEnhanceData data))
            return data;

        return null;
    }

    public SynergyEnhanceData GetByTarget(int target)
    {
        if (_dataByTarget.TryGetValue(target, out SynergyEnhanceData data))
            return data;

        return null;
    }
}
