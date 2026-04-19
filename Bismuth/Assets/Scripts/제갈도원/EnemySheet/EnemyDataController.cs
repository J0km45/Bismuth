using System.Collections.Generic;
using UnityEngine;

public class EnemyDataController : MonoBehaviour
{
    [Header("━━━━ SO 설정 ━━━━")]
    [Tooltip("기본 적 유닛 데이터 SO 입니다.")]
    [SerializeField] private EnemySO enemySo;

    [Header("━━━━ 시트 설정 ━━━━")]
    [Tooltip("적 유닛 시트 Url 입니다.")]
    [SerializeField] private SheetData enemySheet;

    [Header("━━━━ 로드 결과 ━━━━")]
    [SerializeField] private List<EnemyData> _rows = new();

    [SerializeField] private bool log;

    private readonly Dictionary<int, EnemyData> _dataById = new();

    public IReadOnlyList<EnemyData> Rows => _rows;

    private void Start()
    {
        if (enemySo != null)
        {
            _rows = new List<EnemyData>(enemySo.Rows);
            RebuildCache();
            DebugTool.Log($"[적 유닛] SO {_rows.Count}행 로드 완료", DebugType.Data, this);
        }

        if (enemySheet != null)
        {
            StartCoroutine(enemySheet.Load(OnSheetLoaded));
            return;
        }

        if (enemySo == null)
        {
            DebugTool.Warnning("EnemySO 또는 enemySheet 중 하나는 할당되어야 합니다.", DebugType.Data, this);
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
            EnemyData row = EnemyData.Create(cells);
            if (row != null)
                _rows.Add(row);
        }

        RebuildCache();

        if (enemySo != null)
            enemySo.SetRows(_rows);

        DebugTool.Log($"[적 유닛] {_rows.Count}행 로드 완료", DebugType.Data, this);
    }

    private void RebuildCache()
    {
        _dataById.Clear();

        for (int i = 0; i < _rows.Count; i++)
        {
            EnemyData data = _rows[i];
            if (data == null)
                continue;

            if (_dataById.ContainsKey(data.Id))
            {
                DebugTool.Warnning($"중복 적 유닛 ID 데이터가 있습니다. ID : {data.Id}", DebugType.Data, this);
                continue;
            }

            _dataById.Add(data.Id, data);
        }
    }

    public EnemyData GetById(int id)
    {
        if (_dataById.TryGetValue(id, out EnemyData data))
            return data;
        return null;
    }
}
