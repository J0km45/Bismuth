using System.Collections.Generic;
using UnityEngine;

public class UnitEnhanceDataController : MonoBehaviour
{
    [Header("━━━━ SO 설정 ━━━━")]
    [Tooltip("기본 유닛 강화 데이터 SO 입니다.")]
    [SerializeField] private UnitEnhanceSO unitEnhanceSo;

    [Header("━━━━ 시트 설정 ━━━━")]
    [Tooltip("유닛 강화 시트 Url 입니다.")]
    [SerializeField] private SheetData _unitEnhanceSheet;

    private List<UnitEnhanceData> _rows = new();

    [SerializeField] private bool log;

    private Dictionary<int, UnitEnhanceData> _dataByUnitId = new();

    private void Start()
    {
        // SO가 있으면 먼저 기본값 로드
        if (unitEnhanceSo != null)
        {
            _rows = new List<UnitEnhanceData>(unitEnhanceSo.UnitEnhanceDatas);
            RebuildCache();
            DebugTool.Log($"[유닛 강화] SO {_rows.Count}행 로드 완료", DebugType.Data, this);
        }

        // SheetData가 있으면 런타임 시트 값으로 갱신
        if (_unitEnhanceSheet != null)
        {
            StartCoroutine(_unitEnhanceSheet.Load(OnSheetLoaded));
            return;
        }

        // 3) SO/시트 둘 다 없으면 경고
        if (unitEnhanceSo == null)
        {
            DebugTool.Warnning("UnitEnhanceTableSO 또는 unitEnhanceSheet 중 하나는 할당되어야 합니다.", DebugType.Data, this);
        }
    }

    private void OnSheetLoaded(char splitSymbol, string[] lines)
    {
        _rows.Clear();

        if (lines == null || lines.Length < 2) return;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] cells = lines[i].Split(splitSymbol);
            UnitEnhanceData row = UnitEnhanceData.Create(cells);

            if (row != null)
                _rows.Add(row);
        }

        RebuildCache();

        // 시트 값을 SO에도 반영(런타임 메모리 기준)
        if (unitEnhanceSo != null)
            unitEnhanceSo.SetRows(_rows);

        DebugTool.Log($"[유닛 강화] {_rows.Count}행 로드 완료", DebugType.Data, this);
    }

    private void RebuildCache()
    {
        _dataByUnitId.Clear();

        for (int i = 0; i < _rows.Count; i++)
        {
            UnitEnhanceData data = _rows[i];
            if (data == null)
                continue;

            if (_dataByUnitId.ContainsKey(data.UnitId))
            {
                DebugTool.Warnning($"중복된 Unit ID가 있습니다. ID : {data.UnitId}", DebugType.Data, this);
                continue;
            }

            _dataByUnitId.Add(data.UnitId, data);
        }
    }
  
}