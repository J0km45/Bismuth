using System.Collections.Generic;
using UnityEngine;

public class CombinationScrollViewUI : MonoBehaviour
{
    [SerializeField] private Transform _combinationContent; // 조합 스크롤뷰 속 Content
    [SerializeField] private GameObject _pairPrefab; // 2개 조합 프리팹
    [SerializeField] private GameObject _trioPrefab; // 3개조합 프리팹
    [SerializeField] private CombineManager _combineManager;
    [SerializeField] private CombineSO _combineSO;
    [SerializeField] private SummonManager _summonManager;
    [SerializeField] private SummonUnit _summonUnit;

    private List<GameObject> _combinations = new List<GameObject>();

    private void OnEnable()
    {
        _combineManager.OnCombineListChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        _combineManager.OnCombineListChanged -= Refresh;
    }

    public void Refresh()
    {
        ClearCombinations();

        foreach (int[] combis in _combineManager.CombineList)
        {
            CombineData data = FindCombineData(combis);

            if (data == null) continue;

            InitCombination(data);
        }
    }

    // 조합식 데이터 찾기
    private CombineData FindCombineData(int[] combis)
    {
        for (int i = 0; i < _combineSO.CombineDatas.Count; i++)
        {
            CombineData data = _combineSO.CombineDatas[i];

            // Dictionary<재료Id, 필요수량> - SO속 CombineData의 재료Id와 수량 계산
            Dictionary<int, int> requiredCounts = new Dictionary<int, int>();
            // 필요한 재료의 총 수량
            int requiredTotalCount = 0;

            foreach (int sourceId in data.SourceUnit)
            {
                if (sourceId == 0) continue;

                // 이미 있으면 수량 증가, 없으면 새로 추가
                if (requiredCounts.ContainsKey(sourceId))
                {
                    requiredCounts[sourceId]++;
                }
                else
                {
                    requiredCounts.Add(sourceId, 1);
                }

                requiredTotalCount++;
            }

            // combis와 SO속 CombineData의 총 재료 수가 다르면 다른 조합식
            if (combis.Length != requiredTotalCount + 1) continue;

            // Dictionary<재료Id, 필요 수량> - 받아온 데이터의 재료Id와 수량 계산
            Dictionary<int, int> inputCounts = new Dictionary<int, int>();

            for (int j = 0; j < combis.Length - 1; j++)
            {
                int sourceId = combis[j];

                // 이미 있으면 수량 증가, 없으면 새로 추가
                if (inputCounts.ContainsKey(sourceId))
                {
                    inputCounts[sourceId]++;
                }
                else
                {
                    inputCounts.Add(sourceId, 1);
                }
            }

            // 필요한 재료 종류 수와 받아온 데이터의 재료 종류 수가 다르면 다른 조합식
            if (requiredCounts.Count != inputCounts.Count) continue;

            bool isSame = true;

            // (필요한 재료 종류와 수량)이 (받아온 데이터의 재료 종류와 수량)과 모두 일치하는지 확인
            foreach (KeyValuePair<int, int> pair in requiredCounts)
            {
                if (!inputCounts.TryGetValue(pair.Key, out int count) || count != pair.Value)
                {
                    isSame = false;
                    break;
                }
            }

            if (!isSame) continue;

            // 결과값(조합물) 확인
            if (combis[combis.Length - 1] != data.ResultUnit) continue;

            return data;
        }

        return null;
    }

    private void InitCombination(CombineData data)
    {
        GameObject combination;

        int sourceCount = GetSourceCount(data);

        // 재료 수 3개 이상이면 trio 프리팹, 아니면 pair 프리팹 생성
        if (sourceCount >= 3)
        {
            combination = Instantiate(_trioPrefab, _combinationContent);
        }
        else
        {
            combination = Instantiate(_pairPrefab, _combinationContent);
        }

        if (combination.TryGetComponent(out ICombinationUI combi))
        {
            bool canCombine = _combineManager.CanCombine(data);
            combi.Init(_summonManager, _summonUnit);
            combi.SetData(data, canCombine);
        }

        _combinations.Add(combination);
    }

    // 재료 수 계산
    private int GetSourceCount(CombineData data)
    {
        int count = 0;

        foreach (int sourceId in data.SourceUnit)
        {
            if (sourceId != 0) count++;
        }

        return count;
    }

    // _combinations 리스트에 있는 모든 프리팹 삭제 후 리스트 비워줌
    private void ClearCombinations()
    {
        for (int i = 0; i < _combinations.Count; i++)
        {
            if (_combinations[i] != null)
            {
                Destroy(_combinations[i]);
            }
        }

        _combinations.Clear();
    }
}
