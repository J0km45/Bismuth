using System;
using System.Collections.Generic;
using UnityEngine;


public class CombineMangager : MonoBehaviour
{
    [SerializeField] private SummonManager _summonManager;
    [SerializeField] private UnitCatalogManager _unitCatalogManager;
    [SerializeField] private SummonUnit _summonUnit;
    
    [SerializeField] private UnitSO _units;
    [SerializeField] private CombineSO _combineSO;

    private Dictionary<int, int[]> _combineList = new(); 

    // 결과 유닛으로 재료 유닛 찾기
    private Dictionary<int, int[]> ResultToSourceDict = new();
    // 재료 유닛으로 결과 유닛 찾기
    private Dictionary<int, int[]> SourceToResultDict = new();

    [SerializeField] private bool _log = true;

    public string log1;
    public string log2;
    
    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        PrintCombineList();
    }

    public void GetCombineUnitList()
    {
        for (int i = 0; i < _summonUnit.OwnedTowers.Count; i++)
        {
            UnitData data = _summonUnit.OwnedTowers[i].unitData;
            int unitId = data.Id;

            if(SourceToResultDict.ContainsKey(unitId))
            {
                foreach (var id in ResultToSourceDict)
                {
                    for (int j = 0; j < id.Value.Length; j++)
                    {
                        if (id.Value[j] == unitId)
                        {
                            if (!_combineList.ContainsKey(id.Key))
                            {
                                _combineList.Add(id.Key, ResultToSourceDict[id.Key]);
                                DebugTool.Log($"{id.Key} + {ResultToSourceDict[id.Key][j]}", DebugType.Combine, this);
                            }
                        }
                    }
                }
            }
        }
        
        string combineList = "[조합 가능 목록}\n";
        for (int i = 0; i < _combineList.Count; i++)
        {
            foreach (var id in _combineList)
            {
                for (int j = 0; j < id.Value.Length; j++)
                    combineList += $"{id.Value[j]} + ";
                
                combineList += $"= {id.Key}\n";
            }
        }
        DebugTool.Log(combineList, DebugType.Combine, this);
        
        // return _combineList;
    }
    
    // 결과 유닛에 필요한 재료 유닛
    public void InitResultToSourceDict()
    {
        // 조합법 수 만큼 반복
        for (int i = 0; i < _combineSO.CombineDatas.Count; i++)
        {
            CombineData data = _combineSO.CombineDatas[i];
            ResultToSourceDict.Add(data.ResultUnit, data.SourceUnit);
        
            log1 += $"{data.ResultUnit} = {data.SourceUnit[0]} + {data.SourceUnit[1]} + {data.SourceUnit[2]}\n";
        }
    }

    // 모든 유닛에 대하여 합성 결과 유닛
    public void InitSourceToResultDict()
    {
        // 모든 유닛 수 만큼 반복
        for (int i = 0; i < _units.Units.Count; i++)
        {
            UnitData sourceData = _units.Units[i];
            log2 += $"{sourceData.Id} : ";
            List<int> resultList = new();
            
            // 합성법 수 만큼 반복
            for (int j = 0; j < _combineSO.CombineDatas.Count; j++)
            {
                CombineData resultdata = _combineSO.CombineDatas[j];
                // 해당 합성법에 재료 유닛과 비교
                bool add = false;
                foreach (int sourseUnit in resultdata.SourceUnit)
                {
                    // 유닛이 합성법 재료에 있으면 리스에 추가 
                    if (sourceData.Id == sourseUnit)
                    {
                        resultList.Add(resultdata.ResultUnit);
                        log2 += $"{resultdata.ResultUnit}";
                        add = true;
                        // 동일 유닛 2개 합성법 1회만 추가
                        break;
                    }
                }
                if (j < _combineSO.CombineDatas.Count - 1 && add)
                    log2 += ", ";
            }

            // 유닛에 해당하는 조합법이 있으면 추가
            if (resultList.Count > 0)
            {
                log2 += "\n";
                SourceToResultDict.Add(sourceData.Id, resultList.ToArray());
            }
        }
    }

    private void PrintCombineList()
    {
        DebugTool.Log(log1, DebugType.Combine, this);
        DebugTool.Log(log2, DebugType.Combine, this);
    }

    private void Init()
    {
        log1 = "[조합법 목록][결과 : 재료 + 재료 + 재료]\n";
        log2 = "[조합법 목록][재료 : 재료로 생산 가능한 유닛 리스트]\n";
        
        _summonManager = GetComponent<SummonManager>();
        _unitCatalogManager = GetComponent<UnitCatalogManager>();
        _summonUnit = GetComponent<SummonUnit>();
        
        DebugTool.DebugSelect(DebugType.Combine, _log);

        InitResultToSourceDict();
        InitSourceToResultDict();
    }
}
