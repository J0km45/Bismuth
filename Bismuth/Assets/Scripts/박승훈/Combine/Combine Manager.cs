using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;


public class CombineManager : MonoBehaviour
{
    [SerializeField] private SummonManager _summonManager;
    [SerializeField] private UnitCatalogManager _unitCatalogManager;
    [SerializeField] private SummonUnit _summonUnit;
    
    [SerializeField] private UnitSO _units;
    [SerializeField] private CombineSO _combineSO;
    
    //보유중인 유닛의 중복 개수
    private Dictionary<int, int> _ownedUnitCounts = new();
    // 재료 유닛으로 결과 유닛 찾기
    private Dictionary<int, List<int>> SourceToRecipeDict = new();
    public List<int[]> CombineList = new();

    [SerializeField] private bool _log = true;

    public UnityEvent<int> OnAddUnit;
    public UnityEvent<int> OnRemoveUnit;
    
    
    private void Awake()
    {
        Init();
    }

    public void OnEnable()
    {
        OnAddUnit.AddListener(AddOwnedUnit);
        OnAddUnit.AddListener(PrintCombineList);
        OnRemoveUnit.AddListener(RemoveOwnedUnit);
    }

    public void OnDisable()
    {
        OnAddUnit.RemoveListener(AddOwnedUnit);
        OnAddUnit.RemoveListener(PrintCombineList);
        OnRemoveUnit.RemoveListener(RemoveOwnedUnit);
    }

    // 보유한 유닛으로 조합 가능 여부 판단
    public bool CanCombine(CombineData recipe)
    {
        Dictionary<int, int> requiredUnit = new();

        foreach (int sourceId in recipe.SourceUnit)
        {
            if(sourceId == 0)
                continue;
            
            if (requiredUnit.ContainsKey(sourceId))
                requiredUnit[sourceId]++;
            else
                requiredUnit.Add(sourceId, 1);
        }

        foreach (var reaquired in requiredUnit)
        {
            if (!_ownedUnitCounts.TryGetValue(reaquired.Key, out int ownedCount))
                return false;
            if (ownedCount < reaquired.Value)
                return false;
        }

        return true;
    }

    // 모든 합성법 초기화 
    public void InitSourceToResultDict()
    {
        SourceToRecipeDict.Clear();
        
        // 모든 유닛 수 만큼 반복
        for (int recipeIndex = 0; recipeIndex < _combineSO.CombineDatas.Count; recipeIndex++)
        {
            CombineData data = _combineSO.CombineDatas[recipeIndex];
            
            HashSet<int> uniqueSources = new HashSet<int>(data.SourceUnit);
            // 합성법 수 만큼 반복
            foreach (int sourceId in uniqueSources)
            {
                if (sourceId == 0)
                    continue;
                // 유닛이 합성법 재료에 있으면 리스트에 추가 
                if (!SourceToRecipeDict.TryGetValue(sourceId, out List<int> resultList))
                {
                    resultList = new();
                    SourceToRecipeDict.Add(sourceId, resultList);
                }
                
                resultList.Add(recipeIndex);
            }
        }
    }
    
    // 유닛으로 만들 수 있는 합성법 추가
    private List<int> GetRelatedRecipe()
    {
        List<int> result = new();
        HashSet<int> addedRecipes = new();

        foreach (var owned in _ownedUnitCounts)
        {
            int ownedUnitId = owned.Key;
            if (!SourceToRecipeDict.TryGetValue(ownedUnitId, out List<int> resultList))
                continue;

            foreach (int recipeIndex in resultList)
            {
                if (addedRecipes.Add(recipeIndex))
                {
                    result.Add(recipeIndex);
                }
            }
        }

        return result;
    }

    // 유닛 생성 시 해당유닛 보유량 변경
    private void AddOwnedUnit(int unitId)
    {
        if(_ownedUnitCounts.ContainsKey(unitId))
            _ownedUnitCounts[unitId]++;
        else
            _ownedUnitCounts.Add(unitId, 1);
    }

    // 유닛 삭제 시 해당 유닛 보유량 변경
    private void RemoveOwnedUnit(int unitId)
    {
        if (!_ownedUnitCounts.ContainsKey(unitId))
            return;
            
        _ownedUnitCounts[unitId]--;
        if (_ownedUnitCounts[unitId] == 0)
            _ownedUnitCounts.Remove(unitId);
    }

    // 추가된 조합법 리스트 중 조합 가능 여부 출력
    private void PrintCombineList(int a)
    {
        StringBuilder log = new();
        log.AppendLine("[관련 조합법 목록]");

        List<int> recipes = GetRelatedRecipe();
        CombineList.Clear();

        foreach (int recipeIndex in recipes)
        {
            CombineData recipe = _combineSO.CombineDatas[recipeIndex];
            List<int> validSources = new();
            
            List<int> ids = new();
            foreach (int sourceId in recipe.SourceUnit)
            {
                if (sourceId == 0)
                    continue;
                validSources.Add(sourceId);
            }

            for (int i = 0; i < validSources.Count; i++)
            {
                log.Append(validSources[i]);
                ids.Add(validSources[i]);
                if (i < validSources.Count - 1)
                    log.Append(" + ");
            }

            log.Append($" = {recipe.ResultUnit}");
            ids.Add(recipe.ResultUnit);
            if (CanCombine(recipe))
                log.Append(" [조합 가능]");
            else
                log.Append(" [조합 불가]");
            
            log.AppendLine();
            CombineList.Add(ids.ToArray());
        }
        string result = log.ToString();
        DebugTool.Log(result, DebugType.Combine, this);
    }

    private void Init()
    {
        _summonManager = GetComponent<SummonManager>();
        _unitCatalogManager = GetComponent<UnitCatalogManager>();
        _summonUnit = GetComponent<SummonUnit>();
        
        DebugTool.DebugSelect(DebugType.Combine, _log);
        InitSourceToResultDict();
    }
}
