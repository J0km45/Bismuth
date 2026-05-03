using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 시트를 런타임에 로드하고 행 데이터를 보관/조회하는 MonoBehaviour.
/// </summary>
public class SkillSheetRowProvider : MonoBehaviour
{
    [Header("===구글 시트 로더===")]
    [Tooltip("스킬 시트 CSV를 불러오는 설정")]
    [SerializeField] private GoogleSheetLoader _skillSheetLoader;

    [Header("===런타임 로드 상태===")]
    [Tooltip("스킬 시트 로드 성공 여부")]
    [SerializeField] private bool _isLoaded;

    [Tooltip("보관 중인 스킬 행 데이터 개수")]
    [SerializeField] private int _loadedCount;

    private readonly List<SkillSheetRow> _rows = new();
    private readonly Dictionary<int, SkillSheetRow> _rowById = new();

    private SkillSheetParser _skillSheetParser;

    public IReadOnlyList<SkillSheetRow> Rows => _rows;
    public bool IsLoaded => _isLoaded;
    public int LoadedCount => _loadedCount;

    private void Awake()
    {
        _skillSheetParser = new SkillSheetParser();
    }

    private void Start()
    {
        Debug.Log("[SkillSheetRowProvider] Start 진입", this);

        if (string.IsNullOrWhiteSpace(_skillSheetLoader.Url))
        {
            ClearLoadedData();
            Debug.LogError("[SkillSheetRowProvider] GoogleSheetLoader URL이 비어 있습니다.", this);
            return;
        }

        Debug.Log($"[SkillSheetRowProvider] 시트 로드 시작 - URL: {_skillSheetLoader.Url}", this);
        StartCoroutine(_skillSheetLoader.Load(OnSkillSheetLoaded));
    }

    public bool TryGetRowById(int skillId, out SkillSheetRow row)
    {
        if (!_isLoaded)
        {
            row = default;
            return false;
        }

        return _rowById.TryGetValue(skillId, out row);
    }

    private void OnSkillSheetLoaded(string[] lines)
    {
        Debug.Log($"[SkillSheetRowProvider] OnSkillSheetLoaded 진입 - lines 수: {(lines == null ? -1 : lines.Length)}", this);

        if (lines == null || lines.Length == 0)
        {
            ClearLoadedData();
            Debug.LogError("[SkillSheetRowProvider] CSV lines가 비어 있습니다.", this);
            return;
        }

        // 첫 줄(헤더) 그대로 보여주기 — 공백/한글/콤마 깨짐 등 진단용
        Debug.Log($"[SkillSheetRowProvider] 헤더 라인: {lines[0]}", this);
        if (lines.Length > 1)
            Debug.Log($"[SkillSheetRowProvider] 첫 데이터 라인: {lines[1]}", this);

        string csvText = string.Join("\n", lines);

        if (!_skillSheetParser.TryParse(csvText, out List<SkillSheetRow> rows))
        {
            ClearLoadedData();
            Debug.LogError("[SkillSheetRowProvider] 스킬 시트 파싱에 실패했습니다.", this);
            return;
        }

        if (rows.Count == 0)
        {
            ClearLoadedData();
            Debug.LogWarning("[SkillSheetRowProvider] 스킬 데이터가 비어 있습니다.", this);
            return;
        }

        StoreRows(rows);
        Debug.Log($"[SkillSheetRowProvider] 로드 성공 - 행 개수: {_loadedCount}", this);
    }

    private void StoreRows(List<SkillSheetRow> rows)
    {
        _rows.Clear();
        _rows.AddRange(rows);

        RebuildLookup();

        _isLoaded = true;
        _loadedCount = _rows.Count;
    }

    private void RebuildLookup()
    {
        _rowById.Clear();

        foreach (SkillSheetRow row in _rows)
        {
            if (_rowById.ContainsKey(row.Id))
            {
                Debug.LogWarning(
                    $"[SkillSheetRowProvider] 중복 스킬 ID가 있습니다." +
                    $"\n마지막으로 읽은 행으로 덮어씁니다. ID: {row.Id}", this);
            }
            _rowById[row.Id] = row;
        }
    }

    private void ClearLoadedData()
    {
        _rows.Clear();
        _rowById.Clear();

        _isLoaded = false;
        _loadedCount = 0;
    }
}
