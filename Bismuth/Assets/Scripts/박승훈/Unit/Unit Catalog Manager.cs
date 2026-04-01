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

    private void Start()
    {
        DebugTool.DebugSelect(DebugType.Catalog, _log);

        InitCatalog();
        LoadUnitCatalog();
    }

    private void OnEnable()
    {
        OnSummonUnit.AddListener(AddUnitCatalog);
    }

    private void OnDisable()
    {
        OnSummonUnit.RemoveListener(AddUnitCatalog);
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            PrintAllUnitCatalog();
    }

    // 도감에 유닛 추가
    public void AddUnitCatalog(UnitStat stat)
    {
        if (unitCatalog.ContainsKey(stat.Id))
        {
            if (!unitCatalog[stat.Id])
            {
                unitCatalog[stat.Id] = true;

                for (int i = 0; i < unitCatalog.Count -1; ++i)
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
                DebugTool.Log($"[{stat.Id} : {stat.Name}] 이미 도감에 추가되었습니다.", DebugType.Catalog, this);
        }
        else
            DebugTool.Log($"잘못돈 ID 입니다.", DebugType.Catalog, this);
    }

    // 도감 초기화
    private void ClearUnitCatalog()
    {
        unitCatalog.Clear();

        if (unitCatalogSO.FirstInit)
            unitCatalogSO.UnitCatalog.Clear();
    }

    // 도감 초기화
    private void InitCatalog()
    {
        ClearUnitCatalog();

        foreach (UnitData data in unitSO.Units)
        {
            if (unitCatalog.ContainsKey(data.Id))
            {
                DebugTool.Warnning($"중복된 Unit ID 발견: {data.Id}", DebugType.Catalog, this);
                continue;
            }
            unitCatalog.Add(data.Id, false);
            // DebugTool.Log($"{data.UnitName}", DebugType.Catalog, this);

            if (unitCatalogSO.FirstInit)
            {
                DebugTool.Log("유닛 도감 최초 초기화", DebugType.Catalog, this);
                UnitIdSummonedPair newPair = new UnitIdSummonedPair(data.Id, false, data.UnitName);
                unitCatalogSO.UnitCatalog.Add(newPair);
            }
        }
        unitCatalogSO.FirstInit = false;
    }

    // 유닛 도감 로딩
    private void LoadUnitCatalog()
    {
        foreach (UnitIdSummonedPair pair in unitCatalogSO.UnitCatalog)
        {
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
