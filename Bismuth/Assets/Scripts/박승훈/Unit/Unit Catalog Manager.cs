using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class UnitCatalogManager : MonoBehaviour
{
    [SerializeField] private UnitSO unitSO;
    [SerializeField] private UnitCatalogSO unitCatalogSO;

    private Dictionary<int, bool> unitCatalog = new();
    public Dictionary<int, bool> UnitCatalog => unitCatalog;

    public UnityEvent<UnitStat> OnSummonUnit;

    [SerializeField] private bool _log = true;
    [SerializeField] private bool _isCatalogInitialized;

    private void Start()
    {
        if (UnitDataController.IsLoaded)
            InitializeCatalogAfterUnitDataLoaded();
        else
            DebugTool.Log("유닛 데이터 로드 대기 중입니다. 도감 초기화를 보류합니다.", DebugType.Catalog, this);
    }

    private void OnEnable()
    {
        OnSummonUnit.AddListener(AddUnitCatalog);
        UnitDataController.OnUnitDataLoaded += InitializeCatalogAfterUnitDataLoaded;
    }

    private void OnDisable()
    {
        OnSummonUnit.RemoveListener(AddUnitCatalog);
        UnitDataController.OnUnitDataLoaded -= InitializeCatalogAfterUnitDataLoaded;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            PrintAllUnitCatalog();
    }

    private void InitializeCatalogAfterUnitDataLoaded()
    {
        InitCatalog();
        LoadUnitCatalog();
        _isCatalogInitialized = true;
    }

    // 도감에 유닛 추가
    public void AddUnitCatalog(UnitStat stat)
    {
        if (!_isCatalogInitialized)
        {
            DebugTool.Warnning("도감 초기화가 끝나지 않아 유닛 등록을 건너뜁니다.", DebugType.Catalog, this);
            return;
        }

        if (stat == null)
            return;

        if (unitCatalog.ContainsKey(stat.Id))
        {
            if (!unitCatalog[stat.Id])
            {
                unitCatalog[stat.Id] = true;

                for (int i = 0; i < unitCatalogSO.UnitCatalog.Count; ++i)
                {
                    if (unitCatalogSO.UnitCatalog[i].UnitId == stat.Id)
                    {
                        unitCatalogSO.UnitCatalog[i].Name = stat.Name;
                        unitCatalogSO.UnitCatalog[i].UnitId = stat.Id;
                        unitCatalogSO.UnitCatalog[i].Summoned = unitCatalog[stat.Id];
                        break;
                    }
                }

                DebugTool.Log($"도감 개수 : {unitCatalogSO.UnitCatalog.Count}, 등록 ID : {stat.Id}", DebugType.Catalog, this);
                PrintUnitCatalog(stat);
            }
            else
            {
                DebugTool.Log($"[{stat.Id} : {stat.Name}] 이미 도감에 추가되었습니다.", DebugType.Catalog, this);
            }
        }
        else
        {
            DebugTool.Log($"잘못된 ID 입니다. id={stat.Id}", DebugType.Catalog, this);
        }
    }

    // 도감 초기화
    private void ClearUnitCatalog()
    {
        unitCatalog.Clear();

        if (unitCatalogSO != null && unitCatalogSO.FirstInit)
            unitCatalogSO.UnitCatalog.Clear();
    }

    // 유닛 데이터 로드 완료 후 도감 초기화
    private void InitCatalog()
    {
        if (unitSO == null || unitCatalogSO == null)
        {
            DebugTool.Warnning("UnitSO 또는 UnitCatalogSO가 할당되지 않았습니다.", DebugType.Catalog, this);
            return;
        }

        if (unitSO.Units == null || unitSO.Units.Count == 0)
        {
            DebugTool.Warnning("유닛 데이터가 비어 있어 도감 초기화를 보류합니다.", DebugType.Catalog, this);
            return;
        }

        ClearUnitCatalog();

        foreach (UnitData data in unitSO.Units)
        {
            if (data == null)
                continue;

            if (unitCatalog.ContainsKey(data.Id))
            {
                DebugTool.Warnning($"중복된 Unit ID 발견: {data.Id}", DebugType.Catalog, this);
                continue;
            }

            unitCatalog.Add(data.Id, false);

            if (unitCatalogSO.FirstInit)
            {
                UnitIdSummonedPair newPair = new UnitIdSummonedPair(data.Id, false, data.UnitName);
                unitCatalogSO.UnitCatalog.Add(newPair);
            }
        }

        if (unitCatalog.Count > 0)
            unitCatalogSO.FirstInit = false;
    }

    // 유닛 도감 로딩
    private void LoadUnitCatalog()
    {
        if (unitCatalogSO == null)
            return;

        foreach (UnitIdSummonedPair pair in unitCatalogSO.UnitCatalog)
        {
            if (!unitCatalog.ContainsKey(pair.UnitId))
                unitCatalog.Add(pair.UnitId, pair.Summoned);
            else
                unitCatalog[pair.UnitId] = pair.Summoned;
        }

        PrintAllUnitCatalog();
    }

    // 유닛 도감 전체 출력
    private void PrintAllUnitCatalog()
    {
        string dict = "[유닛 도감 개방 목록]\n";
        foreach (KeyValuePair<int, bool> key in unitCatalog)
            dict += $"{key.Key.ToString()} : {key.Value}\n";

        DebugTool.Log(dict, DebugType.Catalog, this);
    }

    // 유닛 도감 대상 출력
    private void PrintUnitCatalog(UnitStat stat)
        => DebugTool.Log($"[{stat.Id} : {stat.Name}] 도감에 추가됨", DebugType.Catalog, this);
}
