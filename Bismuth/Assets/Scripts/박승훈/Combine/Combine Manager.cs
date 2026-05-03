using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;


public class CombineManager : MonoBehaviour
{
    [SerializeField] private SummonManager _summonManager;
    [SerializeField] private SummonUnit _summonUnit;
    
    [SerializeField] private UnitSO _units;
    public UnitSO Units => _units;
    
    [SerializeField] private CombineSO _combineSO;
    
    // 재료 유닛으로 합성 레시피 찾기
    private Dictionary<int, List<int>> _sourceToRecipeDict = new();
    
    // 보유중인 유닛 전체가 아닌 해당 유닛을 중복 개수로 관리 (중복 체크)
    private Dictionary<int, int> _ownedUnitCounts = new();
    
    // 실제 합성 가능한 리스트
    public List<int[]> CombineList = new();

    [SerializeField] private bool _log = true;

    public UnityEvent<int> OnAddUnit;
    public UnityEvent<int> OnRemoveUnit;
    
    public event Action OnCombineListChanged;

    private void Awake()
    {
        Init();
    }

    public void OnEnable()
    {
        OnAddUnit.AddListener(AddOwnedUnit);
        OnAddUnit.AddListener(PrintCombineList);
        
        OnRemoveUnit.AddListener(RemoveOwnedUnit);
        OnRemoveUnit.AddListener(PrintCombineList);
    }

    public void OnDisable()
    {
        OnAddUnit.RemoveListener(AddOwnedUnit);
        OnAddUnit.RemoveListener(PrintCombineList);
        
        OnRemoveUnit.RemoveListener(RemoveOwnedUnit);
        OnRemoveUnit.RemoveListener(PrintCombineList);
    }

    /// <summary>
    /// 합성소에서 버튼 클릭 시 호출 하는 함수
    /// 매개변수로 인덱스 입력
    /// </summary>
    /// <param name="index"></param>
    public bool CombineUnit(int index)
    {
        if (index < 0 || index >= CombineList.Count)
            return false;
        
        int[] recipe = CombineList[index];
        int length = recipe.Length;
        
        // 마지막 인덱스 = 조합 가능 여부
        if (recipe[length - 1] == 0)
        {
            DebugTool.Warnning("재료 유닛이 부족합니다.", DebugType.Combine, this);
            return false;
        }
        
        // 마지막에서 두번 째 : 결과 유닛 ID
        int resultUnitId = recipe[length - 2];
        
        UnitData data = null;
        
        foreach (UnitData unit in _units.Units)
        {
            if (resultUnitId == unit.Id)
            {
                data = unit;
                break;
            }
        }
        
        // 결과 대상 탐색 실패
        if (data == null)
        {
            DebugTool.Log($"{resultUnitId} : 결과 유닛을 찾지 못했습니다.", DebugType.Combine, this);
            return false;
        }
        
        // 재료 유닛 선택
        if (!TryGetConsumeTargets(recipe, out List<TowerUnit> consumeTargets))
            return false;
        
        // 선택된 재료 유닛 제거
        foreach (TowerUnit unit in consumeTargets)
        {
            _summonManager.DespawnUnit(unit);
        }
        
        // 결과 유닛 생성
        _summonManager.SummonCombineUnit(data);
        return true;
    }
    
    // 소모될 유닛 우선순위에 의해 선택
    private bool TryGetConsumeTargets(int[] recipe, out List<TowerUnit> consumeTargets)
    {
        consumeTargets = new List<TowerUnit>();

        // recipe 구조
        // [재료1,2,3 | 결과유닛ID | 조합가능여부]
        int sourceUnitCount = recipe.Length - 2;

        // 레시피에서 필요한 재료 개수 집계
        Dictionary<int, int> requiredCounts = new();

        for (int i = 0; i < sourceUnitCount; i++)
        {
            int sourceId = recipe[i];

            if (requiredCounts.ContainsKey(sourceId))
                requiredCounts[sourceId]++;
            else
                requiredCounts.Add(sourceId, 1);
        }

        // 각 재료 ID마다 후보를 모아서
        // Level 낮은 순 -> 먼저 소환된 순으로 정렬 후 필요한 개수만큼 선택
        foreach (var pair in requiredCounts)
        {
            int needId = pair.Key;
            int needCount = pair.Value;

            List<SummonUnit.SummonedTowerRecord> candidates = new();

            foreach (var record in _summonUnit.OwnedTowers)
            {
                if (record == null)
                    continue;
                if (record.towerUnit == null)
                    continue;
                if (record.Id != needId)
                    continue;

                candidates.Add(record);
            }

            candidates.Sort((a, b) =>
            {
                int levelA = a.unitStat != null ? a.unitStat.Level : int.MaxValue;
                int levelB = b.unitStat != null ? b.unitStat.Level : int.MaxValue;

                // 1. 강화 안 된 유닛 우선 소모
                if (levelA != levelB)
                    return levelA.CompareTo(levelB);

                // 2. 같으면 먼저 소환된 유닛 우선 소모
                return a.summonIndex.CompareTo(b.summonIndex);
            });

            if (candidates.Count < needCount)
            {
                DebugTool.Warnning($"재료유닛 ID {needId} 이(가) {needCount}개 필요하지만 부족합니다.", DebugType.Combine, this);
                consumeTargets = null;
                return false;
            }

            for (int i = 0; i < needCount; i++)
            {
                consumeTargets.Add(candidates[i].towerUnit);
            }
        }

        return true;
    }

    // 보유한 유닛으로 조합 가능 여부 판단
    public bool CanCombine(CombineData recipe)
    {
        Dictionary<int, int> requiredUnit = new();

        foreach (int sourceId in recipe.SourceUnit)
        {
            if (sourceId == 0)
                continue;

            if (requiredUnit.ContainsKey(sourceId))
                requiredUnit[sourceId]++;
            else
                requiredUnit.Add(sourceId, 1);
        }

        foreach (var required in requiredUnit)
        {
            if (!_ownedUnitCounts.TryGetValue(required.Key, out int ownedCount))
                return false;

            if (ownedCount < required.Value)
                return false;
        }

        return true;
    }

    // 모든 합성법 초기화 
    public void InitSourceToResultDict()
    {
        _sourceToRecipeDict.Clear();
        
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
                if (!_sourceToRecipeDict.TryGetValue(sourceId, out List<int> resultList))
                {
                    resultList = new();
                    _sourceToRecipeDict.Add(sourceId, resultList);
                }
                
                resultList.Add(recipeIndex);
            }
        }
    }

    // 유닛 생성 시 해당 유닛 중복 보유량 변경
    private void AddOwnedUnit(int unitId)
    {
        if(_ownedUnitCounts.ContainsKey(unitId))
            _ownedUnitCounts[unitId]++;
        else
            _ownedUnitCounts.Add(unitId, 1);
    }

    // 유닛 삭제 시 해당 유닛 중복 보유량 변경
    private void RemoveOwnedUnit(int unitId)
    {
        if (!_ownedUnitCounts.ContainsKey(unitId))
            return;
            
        _ownedUnitCounts[unitId]--;
        if (_ownedUnitCounts[unitId] == 0)
            _ownedUnitCounts.Remove(unitId);
    }
    
    // 합성 목록 리스트 정렬
    // 합성 가능한 순, 유닛 ID 가 높은 순으로 정렬
    private List<int> GetSortedRecipeIndices()
    {
        List<int> result = new();
        HashSet<int> addedRecipes = new();

        foreach (var owned in _ownedUnitCounts)
        {
            int ownedUnitId = owned.Key;

            if (!_sourceToRecipeDict.TryGetValue(ownedUnitId, out List<int> recipeIndices))
                continue;

            foreach (int recipeIndex in recipeIndices)
            {
                if (addedRecipes.Add(recipeIndex))
                    result.Add(recipeIndex);
            }
        }

        result.Sort((a, b) =>
        {
            CombineData recipeA = _combineSO.CombineDatas[a];
            CombineData recipeB = _combineSO.CombineDatas[b];

            bool aCan = CanCombine(recipeA);
            bool bCan = CanCombine(recipeB);

            // 1. 합성 가능한 순
            if (aCan != bCan)
                return aCan ? -1 : 1;

            // 2. 등급이 높은 순
            if (recipeA.Tier != recipeB.Tier)
                return recipeB.Tier.CompareTo(recipeA.Tier);
            
            if (recipeA.ResultUnit != recipeB.ResultUnit)
                return recipeA.ResultUnit.CompareTo(recipeB.ResultUnit);
            
            // 3. 유닛 ID 순
            return a.CompareTo(b);
        });

        return result;
    }

    // 추가된 조합법 리스트 중 조합 가능 여부 출력 (디버깅 용)
    private void PrintCombineList(int a)
    {
        StringBuilder log = new();
        log.AppendLine("[관련 조합법 목록]");

        List<int> recipes = GetSortedRecipeIndices();
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
            ids.Add(CanCombine(recipe) ? 1 : 0);

            if (CanCombine(recipe))
                log.Append(" [조합 가능]");
            else
                log.Append(" [조합 불가]");

            log.AppendLine();

            CombineList.Add(ids.ToArray());
        }

        DebugTool.Log(log.ToString(), DebugType.Combine, this);

        OnCombineListChanged?.Invoke();
    }

    private void Init()
    {
        _summonManager = GetComponent<SummonManager>();
        _summonUnit = GetComponent<SummonUnit>();
        
        InitSourceToResultDict();
    }
}
