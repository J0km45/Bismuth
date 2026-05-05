using System;
using System.Collections;
using UnityEngine;

public class UnitDataController : MonoBehaviour
{
    [Header("━━━━ 시트 설정 ━━━━")]
    [Tooltip("구글시트 Unit 시트 URL\n공유 → 링크로 가져오기")]
    [SerializeField] private SheetData unitSheet;

    [Header("━━━━ 유닛 DB ━━━━")]
    [Tooltip("시트 데이터가 채워질 UnitSO 에셋\nCreate > Bismuth > Unit Database 로 생성 후 할당\n" +
             "티어별 정리, 전체 정리")]
    [SerializeField] private UnitSO[] unitDatabaseByTier = new UnitSO[MaxTier];

    [SerializeField] private UnitSO unitDatabaseAll;

    public const int MaxTier = 4;

    public static event Action OnUnitDataLoaded;

    public static bool IsLoaded { get; private set; }
    public static int AllUnitCount { get; private set; }
    public static int Tier1UnitCount { get; private set; }
    public static int Tier2UnitCount { get; private set; }
    public static int Tier3UnitCount { get; private set; }
    public static int Tier4UnitCount { get; private set; }

    [SerializeField] private bool _log;
    [SerializeField] private bool _isLoaded;
    [SerializeField] private int _loadedUnitCount;

    private Coroutine _loadRoutine;

    private void Awake()
    {
        IsLoaded = false;
        _isLoaded = false;
        _loadedUnitCount = 0;
        ResetUnitCounts();
    }

    private void Start()
    {
        if (!ValidateReferences())
            return;

        DebugTool.Log("[UnitSheet] 유닛 데이터 로드를 시작합니다.", DebugType.Data, this);
        _loadRoutine = StartCoroutine(LoadUnitDataRoutine());
    }

    private IEnumerator LoadUnitDataRoutine()
    {
        yield return unitSheet.Load(SetUnitDatas);
        _loadRoutine = null;
    }

    // 구글시트 로드 완료 시 호출 - 각 행을 파싱하여 UnitDatabase에 추가
    private void SetUnitDatas(char splitSymbol, string[] lines)
    {
        if (lines == null || lines.Length < 2)
        {
            MarkLoadFailed("유닛 시트에 데이터 행이 없습니다.");
            return;
        }

        ResetUnitCounts();
        ClearUnitDatas(unitDatabaseByTier);
        ClearUnitDatas(unitDatabaseAll);

        // 0행: 헤더, 1행부터 데이터
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] cells = lines[i].Split(splitSymbol);
            UnitData unitData = UnitData.CreateFromSheetRow(cells);

            if (unitData == null)
            {
                DebugTool.Warnning($"파싱 실패 row={i}, raw={lines[i]}", DebugType.Data, this);
                continue;
            }

            if (!IsValidTier(unitData.Tier))
            {
                DebugTool.Warnning(
                    $"유효하지 않은 유닛 티어입니다. row={i}, id={unitData.Id}, tier={unitData.Tier}",
                    DebugType.Data,
                    this);
                continue;
            }

            UnitSO tierDatabase = unitDatabaseByTier[unitData.Tier - 1];
            if (tierDatabase == null)
            {
                DebugTool.Warnning($"Tier {unitData.Tier} UnitSO가 할당되지 않았습니다. id={unitData.Id}", DebugType.Data, this);
                continue;
            }

            tierDatabase.AddUnit(unitData);
            unitDatabaseAll.AddUnit(unitData);
            IncreaseTierCount(unitData.Tier);
            AllUnitCount++;
        }

        if (AllUnitCount <= 0)
        {
            MarkLoadFailed("파싱된 유닛 데이터가 없습니다.");
            return;
        }

        IsLoaded = true;
        _isLoaded = true;
        _loadedUnitCount = AllUnitCount;

        DebugTool.Log($"[총 {AllUnitCount} 개 유닛 로드 완료]", DebugType.Data, this);
        DebugTool.Log($"[티어 1 : {Tier1UnitCount} 개 유닛 로드 완료]", DebugType.Data, this);
        DebugTool.Log($"[티어 2 : {Tier2UnitCount} 개 유닛 로드 완료]", DebugType.Data, this);
        DebugTool.Log($"[티어 3 : {Tier3UnitCount} 개 유닛 로드 완료]", DebugType.Data, this);
        DebugTool.Log($"[티어 4 : {Tier4UnitCount} 개 유닛 로드 완료]", DebugType.Data, this);

        OnUnitDataLoaded?.Invoke();
    }

    private bool ValidateReferences()
    {
        if (unitSheet == null)
        {
            MarkLoadFailed("unitSheet가 할당되지 않았습니다.");
            return false;
        }

        if (unitDatabaseByTier == null || unitDatabaseByTier.Length < MaxTier)
        {
            MarkLoadFailed($"unitDatabaseByTier는 {MaxTier}개가 필요합니다.");
            return false;
        }

        if (unitDatabaseAll == null)
        {
            MarkLoadFailed("unitDatabaseAll이 할당되지 않았습니다.");
            return false;
        }

        return true;
    }

    private void MarkLoadFailed(string message)
    {
        IsLoaded = false;
        _isLoaded = false;
        _loadedUnitCount = 0;
        DebugTool.Warnning($"[UnitSheet] {message}", DebugType.Data, this);
    }

    private static void ResetUnitCounts()
    {
        AllUnitCount = 0;
        Tier1UnitCount = 0;
        Tier2UnitCount = 0;
        Tier3UnitCount = 0;
        Tier4UnitCount = 0;
    }

    private static bool IsValidTier(int tier)
    {
        return tier >= 1 && tier <= MaxTier;
    }

    private static void IncreaseTierCount(int tier)
    {
        switch (tier)
        {
            case 1:
                Tier1UnitCount++;
                break;
            case 2:
                Tier2UnitCount++;
                break;
            case 3:
                Tier3UnitCount++;
                break;
            case 4:
                Tier4UnitCount++;
                break;
        }
    }

    private void ClearUnitDatas(params UnitSO[] units)
    {
        if (units == null)
            return;

        foreach (UnitSO unit in units)
        {
            if (unit == null)
                continue;

            unit.ClearUnits();
        }
    }
}
